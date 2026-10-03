using System.Diagnostics;
using System.Net.Sockets;

namespace BrowserAutomationMaster.Tests.Gui.Server
{
    /// <summary>
    /// Runs one child process under <c>dotnet-coverage</c>, so the listener's own coverage is recorded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why this exists: Tier B drives the listener from a child process, and
    /// <c>coverlet.collector</c> instruments only the test host. It shadow-loads rewritten assemblies
    /// rather than touching the files on disk, so the child loads the original
    /// <c>BrowserAutomationMaster.dll</c> and the collector never sees a line of <c>Core/GUI</c>
    /// executed. Every Tier B assertion passes while the coverage report shows the whole namespace as
    /// untested — which makes the report useless as a gate, and "is Core/GUI covered" unanswerable.
    /// </para>
    /// <para>
    /// <c>dotnet-coverage</c> collects from any process over the diagnostic IPC pipe, so wrapping the
    /// child's launch in it records exactly what the child executed. The two reports are then merged;
    /// see <c>MergeChildCoverage</c>.
    /// </para>
    /// <para>
    /// Optional rather than required. When the tool is absent the child is launched directly and a note
    /// goes to the test log, so a plain <c>dotnet test</c> still works on a machine without it — but CI
    /// installs it, so the coverage the build publishes is the merged kind.
    /// </para>
    /// </remarks>
    public static class ChildCoverage
    {
        private const string TOOL = "dotnet-coverage";

        private static bool? available;

        /// <summary>Whether the collector is on PATH. Probed once per run.</summary>
        public static bool IsAvailable
        {
            get
            {
                available ??= ProbeOnPath();

                return available.Value;
            }
        }

        /// <summary>How to install it, for the message when it is missing.</summary>
        public const string INSTALL_HINT = "dotnet tool install --global dotnet-coverage";

        /// <summary>
        /// Where every child process's report is written.
        /// </summary>
        /// <remarks>
        /// One directory for all of them, not just the GUI server's: <c>BammProcess</c> also launches a
        /// child to run the compiler, and Core/Compilation is invisible to coverlet for the same reason
        /// Core/GUI is. Beside the test assembly rather than under the repository, so a run leaves
        /// nothing in the working tree.
        /// </remarks>
        public static string OutputDirectory => Path.Combine(AppContext.BaseDirectory, "coverage-child");

        /// <summary>
        /// Wraps a launch command in <c>dotnet-coverage collect</c>, or returns it unchanged.
        /// </summary>
        /// <param name="startInfo">The child's launch, already configured.</param>
        /// <param name="outputDirectory">Where to write the child's report.</param>
        /// <param name="label">A name for the report file, so two children in one run do not collide.</param>
        public static void Instrument(ProcessStartInfo startInfo, string outputDirectory, string label)
        {
            if (!IsAvailable)
            {
                return;
            }

            Directory.CreateDirectory(outputDirectory);

            string[] launch = [startInfo.FileName, .. startInfo.ArgumentList];

            startInfo.FileName = TOOL;
            startInfo.ArgumentList.Clear();

            startInfo.ArgumentList.Add("collect");
            startInfo.ArgumentList.Add("--output-format");
            startInfo.ArgumentList.Add("cobertura");
            startInfo.ArgumentList.Add("--output");
            startInfo.ArgumentList.Add(Path.Combine(outputDirectory, $"child-{label}.cobertura.xml"));

            // -- separates the collector's own options from the command it wraps.
            startInfo.ArgumentList.Add("--");
            foreach (string argument in launch) { startInfo.ArgumentList.Add(argument); }

            // dotnet-coverage is a global tool, whose apphost cannot find the runtime without this.
            // Derived from the running dotnet rather than required from the environment, so it works on a
            // machine where DOTNET_ROOT is not set — which is the common case for a global tool.
            if (startInfo.Environment.TryGetValue("DOTNET_ROOT", out string? configured) && configured.Length > 0)
            {
                return;
            }

            string? root = Path.GetDirectoryName(
                Environment.ProcessPath is string host ? Path.GetFullPath(host) : string.Empty
            );

            if (root is not null && File.Exists(Path.Combine(root, "dotnet")))
            {
                startInfo.Environment["DOTNET_ROOT"] = root;
            }
        }

        /// <summary>
        /// Merges every report in a directory into one, so the child's coverage joins the test host's.
        /// </summary>
        /// <remarks>
        /// Returns the merged path, or null when there was nothing to merge. Cobertura from two processes
        /// describes the same assemblies, so a merge has to combine hit counts per line rather than
        /// concatenating documents — which is what <c>dotnet-coverage merge</c> does. Without the merge
        /// the two reports sit side by side and the child's is the one with Core/GUI in it, so the
        /// headline number would still be wrong.
        /// </remarks>
        public static string? Merge(string directory, string outputPath)
        {
            if (!IsAvailable)
            {
                return null;
            }

            string[] reports = [.. Directory.EnumerateFiles(directory, "*.cobertura.xml")];

            if (reports.Length == 0)
            {
                return null;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            ProcessStartInfo merge = new()
            {
                FileName = TOOL,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            merge.ArgumentList.Add("merge");
            merge.ArgumentList.Add("--output-format");
            merge.ArgumentList.Add("cobertura");
            merge.ArgumentList.Add("-o");
            merge.ArgumentList.Add(outputPath);
            foreach (string report in reports) { merge.ArgumentList.Add(report); }

            using Process? merged = Process.Start(merge);

            if (merged is null)
            {
                return null;
            }

            merged.WaitForExit();

            return File.Exists(outputPath) ? outputPath : null;
        }

        private static bool ProbeOnPath()
        {
            string? path = Environment.GetEnvironmentVariable("PATH");

            if (path is null)
            {
                return false;
            }

            return path
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Any(directory => File.Exists(Path.Combine(directory, TOOL))
                    || File.Exists(Path.Combine(directory, TOOL + ".exe")));
        }
    }

    /// <summary>
    /// A running GUI HTTP server, started once per collection. <br/>
    /// Source: Core/GUI/Server.cs (StartServer), Gui/Server/GuiServerHost/Program.cs
    /// </summary>
    /// <remarks>
    /// A child process because <see cref="BrowserAutomationMaster.Core.Messaging.Errors.WriteAndExit"/>
    /// ends the process on a taken port, on low memory, or on a failed GUI extraction. In-proc that
    /// would take the test host with it and the whole run would be lost rather than one test failing.
    /// <para>
    /// That isolation is also why <see cref="ChildCoverage"/> exists: the cost of a separate process is
    /// that the usual coverage collector cannot see it.
    /// </para>
    /// </remarks>
    public sealed class GuiServerProcess : IAsyncDisposable
    {
        /// <summary>
        /// How long to wait for the listener to answer, and how long a test's own request may take.
        /// </summary>
        /// <remarks>
        /// The first start has to download and extract the embedded gui.zip, which on a cold cache is
        /// the slowest thing the server does; everything after it is in-process work. Budgeted for the
        /// cold case rather than the warm one so a clean machine does not report a spurious timeout.
        /// </remarks>
        private static readonly TimeSpan STARTUP_TIMEOUT = TimeSpan.FromSeconds(60);

        /// <summary>
        /// How many extra attempts a port collision buys.
        /// </summary>
        /// <remarks>
        /// One. The window between releasing the probe socket and the child binding it is sub-millisecond;
        /// a second collision means something is churning ports, and retrying would paper over it.
        /// </remarks>
        private const int PORT_COLLISION_RETRIES = 1;


        private readonly Process process;
        private readonly string appData;
        private readonly string userScripts;

        private GuiServerProcess(Process process, int port, string appData, string userScripts)
        {
            this.process = process;
            Port = port;
            this.appData = appData;
            this.userScripts = userScripts;
        }

        /// <summary>The port the listener was bound to.</summary>
        public int Port { get; }

        /// <summary>The origin the GUI would talk to, built the same way index.html builds it.</summary>
        public string BaseUrl => $"http://127.0.0.1:{Port}";

        /// <summary>
        /// Where this server extracted the GUI's index.html.
        /// </summary>
        /// <remarks>
        /// Computed from the redirected AppData root rather than read from
        /// <c>DirectoryManager.GetMainGUIPage</c>, because that would answer with the <em>test host's</em>
        /// AppData. The child runs under its own redirected root — which is the whole point of the
        /// redirection — so only the child's path is the one a response can name. <br/>
        /// The layout appended under the root is GetAppDataDirectory's: the app name, then gui/.
        /// </remarks>
        public string ExtractedMainGuiPage => Path.Combine(appData, APP_NAME, "gui", "index.html");

        /// <summary>Builds a URL for a route on this server.</summary>
        public string Url(string route) => $"{BaseUrl}{route}";

        /// <summary>
        /// Starts a server on a free port and waits until it answers.
        /// </summary>
        /// <param name="testOutput">Writes the child's output here, so a start-up failure is diagnosable.</param>
        /// <exception cref="InvalidOperationException">
        /// The launcher could not be found, the port was taken between choosing it and binding it, or
        /// the child died before answering. Never swallows the reason.
        /// </exception>
        public static async Task<GuiServerProcess> StartAsync(Action<string> testOutput)
        {
            // Retried once. ReserveFreePort binds port 0 to learn a free port and releases it, so
            // something else can take it in the gap before the child's bind — and if it does,
            // Server.StartServer hits ScanForUsedLHPorts and WriteAndExit, which ends the child. One
            // retry turns that race into an invisible detail instead of a failed suite.
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    return await StartOnceAsync(ReserveFreePort(), testOutput);
                }
                catch (InvalidOperationException ex) when (attempt <= PORT_COLLISION_RETRIES && IsPortCollision(ex))
                {
                    testOutput($"Port collision starting the GUI server, retrying: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Whether a start-up failure looks like the port being taken rather than anything else.
        /// </summary>
        /// <remarks>
        /// Matched on the child's own words because there is no error code to read: WriteAndExit prints
        /// to the console and exits, and the message it prints for a taken port is the only signal.
        /// Everything else propagates, so a real failure is never retried into a second real failure.
        /// </remarks>
        private static bool IsPortCollision(Exception ex)
            => ex.Message.Contains("in use", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("port", StringComparison.OrdinalIgnoreCase);

        private static async Task<GuiServerProcess> StartOnceAsync(int port, Action<string> testOutput)
        {
            string appData = Path.Combine(Path.GetTempPath(), "bamm-gui-server", Path.GetRandomFileName());

            // Matches DirectoryManager's layout under the redirected AppData root, so /load has a
            // userScripts directory to list before the first request arrives.
            string userScripts = Path.Combine(appData, APP_NAME, "userScripts");
            Directory.CreateDirectory(userScripts);

            ProcessStartInfo startInfo = BuildStartInfo(port, appData);

            // Before the child is started, so the collector wraps the real launch command.
            if (ChildCoverage.IsAvailable)
            {
                ChildCoverage.Instrument(startInfo, ChildCoverage.OutputDirectory, $"gui-{port}");
            }
            else
            {
                testOutput($"dotnet-coverage is not installed, so this server's coverage will not be recorded. Install it with '{ChildCoverage.INSTALL_HINT}'.");
            }

            Process child = new() { StartInfo = startInfo };

            if (!child.Start())
            {
                child.Dispose();

                throw new InvalidOperationException($"Failed to start the GUI server host '{startInfo.FileName}'.");
            }

            // Every error path in the child funnels through WriteAndExit -> ANSI.ReadKey(), which blocks
            // on console input. Closing stdin makes ReadKey throw immediately, so a genuine failure dies
            // quickly with a non-zero code rather than being misreported as a readiness timeout. The same
            // reason BammProcess closes it.
            child.StandardInput.Close();

            // Fire and forget on purpose: draining the pipes has to happen concurrently with the
            // readiness poll, but neither drain is awaited, or a server that never exits would hold up
            // the test. A discarded task says that; leaving the call bare says it by warning.
            _ = child.StandardOutput.ReadToEndAsync().ContinueWith(read => testOutput("[gui-server stdout] " + read.Result));
            _ = child.StandardError.ReadToEndAsync().ContinueWith(read => testOutput("[gui-server stderr] " + read.Result));

            try
            {
                await WaitUntilListeningAsync(child, port, testOutput);
            }
            catch
            {
                child.Dispose();
                throw;
            }

            return new GuiServerProcess(child, port, appData, userScripts);
        }

        /// <summary>
        /// Writes a .bamc into the server's isolated userScripts directory, so /load has something to
        /// list and /export has a name to collide with.
        /// </summary>
        public string StageScript(string fileName, params string[] commandLines)
        {
            string path = Path.Combine(userScripts, fileName);

            File.WriteAllText(path, string.Join('\n', commandLines));

            return path;
        }

        /// <summary>Every .bamc the server currently sees.</summary>
        public IReadOnlyList<string> StagedScriptNames
            => Directory.Exists(userScripts)
                ? [.. Directory.GetFiles(userScripts, "*.bamc").Select(Path.GetFileName).OfType<string>()]
                : [];

        public async ValueTask DisposeAsync()
        {
            // Ask nicely first. The server's own loop notices isRunning going false and exits, which
            // leaves no chance of it holding the port while the next run tries to bind it.
            try
            {
                using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };

                await client.GetAsync(Url("/terminate"));
            }
            catch
            {
                // The server may already be gone; killing it below covers every case.
            }

            if (!process.WaitForExit(5000))
            {
                KillQuietly();
            }

            process.Dispose();
        }

        private const string APP_NAME = "BrowserAutomationMaster";

        /// <summary>
        /// Builds the launch command.
        /// </summary>
        /// <remarks>
        /// <c>dotnet exec</c> with the test project's own runtimeconfig and deps file, for the same two
        /// reasons <c>BammProcess</c> uses it: the main project is <c>SelfContained=true</c>, so its
        /// apphost cannot start beside the test assembly, and its own runtimeconfig/deps describe a
        /// self-contained layout that <c>dotnet exec</c> cannot satisfy.
        /// </remarks>
        private static ProcessStartInfo BuildStartInfo(int port, string appData)
        {
            string testOutputDirectory = AppContext.BaseDirectory;
            string host = Path.Combine(testOutputDirectory, "GuiServerHost.dll");
            string runtimeConfig = Path.Combine(testOutputDirectory, "BrowserAutomationMaster.Tests.runtimeconfig.json");
            string depsFile = Path.Combine(testOutputDirectory, "BrowserAutomationMaster.Tests.deps.json");

            if (!File.Exists(host))
            {
                throw new InvalidOperationException(
                    $"No GUI server host at '{host}'. It is a ProjectReference of this project, so its absence " +
                    "means the build did not run or the reference was removed."
                );
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                WorkingDirectory = appData,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            // Same reasoning as BammProcess: GuiServerHost is framework-dependent, and the main
            // project is SelfContained, so the host's own runtimeconfig and deps file describe a
            // layout that `dotnet exec` cannot satisfy. The test project's pair has to be passed.
            startInfo.ArgumentList.Add("exec");
            startInfo.ArgumentList.Add("--runtimeconfig");
            startInfo.ArgumentList.Add(runtimeConfig);
            startInfo.ArgumentList.Add("--depsfile");
            startInfo.ArgumentList.Add(depsFile);
            startInfo.ArgumentList.Add(host);
            startInfo.ArgumentList.Add(port.ToString());

            // Redirects AppData so nothing touches the developer's real ~/.config/BrowserAutomationMaster.
            // GetAppDataLinux reads $XDG_CONFIG_HOME, GetAppDataMacOS reads $HOME, and GetAppDataWindows
            // reads %APPDATA%; see DirectoryManager.GetAppDataDirectory for the mapping.
            if (OperatingSystem.IsWindows())
            {
                startInfo.Environment["APPDATA"] = appData;
            }
            else if (OperatingSystem.IsMacOS())
            {
                startInfo.Environment["HOME"] = appData;
            }
            else
            {
                startInfo.Environment["XDG_CONFIG_HOME"] = appData;
            }

            return startInfo;
        }

        /// <summary>
        /// Polls <c>/gui_version</c> until it answers.
        /// </summary>
        /// <remarks>
        /// Probing a route rather than waiting on the child's stdout is deliberate: the readiness
        /// condition is "the socket accepts requests", and that is the thing the tests depend on. A
        /// child that has exited is detected immediately instead of costing the full timeout.
        /// </remarks>
        private static async Task WaitUntilListeningAsync(Process child, int port, Action<string> testOutput)
        {
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
            DateTime deadline = DateTime.UtcNow + STARTUP_TIMEOUT;

            while (DateTime.UtcNow < deadline)
            {
                if (child.HasExited)
                {
                    throw new InvalidOperationException(
                        $"The GUI server host exited with code {child.ExitCode} before it began listening. " +
                        "Its output is in the test log above."
                    );
                }

                try
                {
                    using HttpResponseMessage answer = await client.GetAsync($"http://127.0.0.1:{port}/gui_version");

                    if (answer.IsSuccessStatusCode)
                    {
                        testOutput($"GUI server listening on port {port} after the first GUI extraction.");
                        return;
                    }
                }
                catch
                {
                    // Not listening yet.
                }

                await Task.Delay(250);
            }

            throw new InvalidOperationException(
                $"The GUI server host did not begin listening on port {port} within {STARTUP_TIMEOUT.TotalSeconds:0}s."
            );
        }

        /// <summary>
        /// Binds a socket to port 0 to learn a free port, then releases it.
        /// </summary>
        /// <remarks>
        /// Racy in principle — something else could take it between release and the child's bind — but
        /// the alternative, letting the server choose, means the tests cannot address it. StartAsync
        /// reports a bind failure as a child exit with a distinct message rather than hanging.
        /// </remarks>
        private static int ReserveFreePort()
        {
            using TcpListener probe = new(System.Net.IPAddress.Loopback, 0);

            probe.Start();
            int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return port;
        }

        private void KillQuietly()
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch
            {
                // Already gone, or too late to matter.
            }
        }
    }
}
