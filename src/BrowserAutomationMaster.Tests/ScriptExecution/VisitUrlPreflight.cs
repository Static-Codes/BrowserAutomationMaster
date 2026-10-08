using System.IO;
using System.Net;

namespace BrowserAutomationMaster.Tests.ScriptExecution
{
    /// <summary>A single URL target that a script visits but which could not be reached.</summary>
    public readonly record struct UnreachableUrl(string Url, int LineNumber, string Command, string Reason);

    /// <summary>
    /// Classifies environmental URL failures *before* the BAMM child is launched, so an unreachable
    /// target becomes a skip instead of a red build. <br/>
    /// Source: Core/Compilation/Transpiler.cs (IsLocalFile, IsResolvableLink)
    /// </summary>
    /// <remarks>
    /// This deliberately does not call <c>Transpiler.IsResolvableLink</c>: it prompts for input and can
    /// call <c>WriteAndExit</c>, either of which would terminate the test host. Only the reachability
    /// rule is mirrored, and no other suite in this project probes URLs.
    /// </remarks>
    public static class VisitUrlPreflight
    {
        private const int PROBE_TIMEOUT_SECONDS = 8;

        // The only two commands that Transpiler resolves with IsResolvableLink.
        private static readonly string[] TARGET_COMMANDS = ["visit", "open-new-tab"];

        // Transpiler.InvalidResponseEnums. BAMM deliberately tolerates these: the server is
        // responding that the content is or was at this location, it simply is not accessible.
        private static readonly HttpStatusCode[] InvalidResponseEnums =
        [
            HttpStatusCode.Forbidden,        // 403
            HttpStatusCode.Unauthorized,     // 401
            HttpStatusCode.Locked,           // 423
            HttpStatusCode.MovedPermanently  // 301
        ];

        // Cached per run so a URL repeated across scripts is probed once.
        private static readonly Dictionary<string, string?> probeCache = new();

        private static readonly Lazy<HttpClient> httpClient = new(CreateClient);

        /// <summary>
        /// Probes every <c>visit</c> / <c>open-new-tab</c> target in <paramref name="scriptPath"/> and
        /// returns only those that could not be reached.
        /// </summary>
        /// <remarks>
        /// A script with no such lines yields an empty list, so a genuine "no 'visit' commands found"
        /// compile failure stays a failure rather than becoming a skip. A probe that cannot complete
        /// is treated as reachable, so it can never manufacture a skip.
        /// </remarks>
        public static IReadOnlyList<UnreachableUrl> Probe(string scriptPath)
        {
            try
            {
                List<UnreachableUrl> unreachable = [];

                foreach ((string url, int lineNumber, string command) in ReadTargets(scriptPath))
                {
                    string? reason = Resolve(url);

                    if (reason != null)
                    {
                        unreachable.Add(new UnreachableUrl(url, lineNumber, command, reason));
                    }
                }

                return unreachable;
            }
            catch
            {
                return [];
            }
        }

        /// <summary>
        /// Returns a human readable reason why the URL is unreachable, or null when it is reachable
        /// or could not be judged.
        /// </summary>
        private static string? Resolve(string url)
        {
            lock (probeCache)
            {
                if (probeCache.TryGetValue(url, out string? cached))
                {
                    return cached;
                }
            }

            string? reason = ProbeUncached(url);

            lock (probeCache)
            {
                probeCache[url] = reason;
            }

            return reason;
        }

        private static string? ProbeUncached(string url)
        {
            // Mirrors Transpiler.IsLocalFile: a file:// target is reachable when the file exists, and
            // no HTTP request is made.
            if (url.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                string filePath = url[7..];

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return "the file:// target is empty";
                }

                return File.Exists(filePath) ? null : $"the local file does not exist: '{filePath}'";
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) || uriResult == null)
            {
                return "the URL could not be parsed into an absolute Uri";
            }

            try
            {
                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(PROBE_TIMEOUT_SECONDS));

                // ResponseHeadersRead matches Transpiler.IsResolvableLink: only the headers are read.
                using HttpResponseMessage response = httpClient.Value
                    .GetAsync(uriResult, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .GetAwaiter()
                    .GetResult();

                if (InvalidResponseEnums.Contains(response.StatusCode))
                {
                    return null; // Tolerated by BAMM, therefore reachable for our purposes.
                }

                return response.IsSuccessStatusCode
                    ? null
                    : $"the server responded with HTTP {(int)response.StatusCode} ({response.StatusCode})";
            }
            catch (OperationCanceledException)
            {
                return $"the request timed out after {PROBE_TIMEOUT_SECONDS}s";
            }
            catch (HttpRequestException ex)
            {
                return $"the request failed: {ex.InnerException?.Message ?? ex.Message}";
            }
            catch (Exception ex)
            {
                // A probe that cannot complete must never manufacture a skip.
                return $"the probe could not be completed: {ex.GetType().Name}";
            }
        }

        /// <summary>
        /// Collects the target of every <c>visit</c> / <c>open-new-tab</c> line, mirroring how
        /// Transpiler derives <c>sanitizedArg2</c> for those commands.
        /// </summary>
        private static IEnumerable<(string Url, int LineNumber, string Command)> ReadTargets(string scriptPath)
        {
            if (!File.Exists(scriptPath))
            {
                yield break;
            }

            string[] lines = File.ReadAllLines(scriptPath);

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();

                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] splitLine = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (splitLine.Length < 2)
                {
                    continue;
                }

                string command = splitLine[0];

                if (!TARGET_COMMANDS.Contains(command, StringComparer.Ordinal))
                {
                    continue;
                }

                // Transpiler strips double quotes (and single quotes for codeemulation scripts)
                // from the argument; both are stripped here so extraction is never the reason a
                // target is missed.
                string argument = splitLine[1]
                    .Replace('"', ' ')
                    .Replace('\'', ' ')
                    .Trim();

                if (argument.Length == 0)
                {
                    continue;
                }

                yield return (argument, index + 1, command);
            }
        }

        private static HttpClient CreateClient()
        {
            // Mirrors RequestManager.NetworkClient.Instance, which Transpiler.IsResolvableLink uses.
            // The User-Agent must match: it decides the response, and some hosts serve a non-success
            // status to a recognized browser agent while serving 200 to a default one. If the two
            // clients disagree, a genuine compile failure is misreported as an environmental skip.
            HttpClientHandler handler = new() { AllowAutoRedirect = true };

            HttpClient client = new(handler)
            {
                Timeout = TimeSpan.FromSeconds(PROBE_TIMEOUT_SECONDS)
            };

            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"
            );

            return client;
        }
    }
}
