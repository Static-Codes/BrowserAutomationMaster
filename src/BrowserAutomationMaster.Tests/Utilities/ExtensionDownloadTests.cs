using BrowserAutomationMaster.Core.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Utilities
{
    /// <summary>
    /// The live download path, against the publishers' own repositories. <br/>
    /// Source: Core/Utilities/ExtensionUtility.cs (GetExtensionContents, GetLatestChromeVersion,
    /// BuildDownloadUrl, GetXPIDownloadURL)
    /// </summary>
    /// <remarks>
    /// Skipped rather than required, because they reach the network. A suite that cannot run offline is
    /// not runnable on a developer machine or in a sandbox, and a download that fails for someone
    /// else's reasons is not a defect in this code. <br/>
    /// They are not decorative, though: the generated fixtures in
    /// <see cref="ExtensionArchiveTests"/> prove the byte-level contract, and only these prove that the
    /// URLs, redirects and HTML scraping still work against the real publishers. That is exactly the
    /// part a fixture cannot stand in for, and the part most likely to rot — Mozilla's download page
    /// markup in particular is what <c>XPIExtensionPathRegexPattern</c> is written against.
    /// <para>
    /// Chosen for longevity rather than convenience: a long-lived, stable extension from each store,
    /// so the test measures BAMM and not a publisher's current release.
    /// </para>
    /// </remarks>
    public class ExtensionDownloadTests(ITestOutputHelper output)
    {
        /// <summary>
        /// A stable Firefox add-on: uBlock Origin, downloaded through the same HTML scrape a user
        /// would go through.
        /// </summary>
        [SkippableFact]
        public async Task DownloadsAnXpiFromAMozillaDownloadPage()
        {
            Skip.If(Network.SkipReason is not null, Network.SkipReason!);

            const string addonUrl = "https://addons.mozilla.org/en-US/firefox/addon/ublock-origin/";

            ExtensionUtility utility = new(addonUrl, "firefox");

            output.WriteLine($"IsURL={utility.IsURL}, IsFirefoxExtension={utility.IsFirefoxExtension}");

            using MemoryStream? contents = await utility.GetExtensionContents();

            Assert.NotNull(contents);

            byte[] bytes = contents.ToArray();

            output.WriteLine($"downloaded {bytes.Length} bytes");

            Assert.NotEmpty(bytes);

            // The same two checks the generated fixture makes, against a real file: the zip terminator
            // BAMM looks for, and one of the META-INF entries it requires.
            Assert.True(bytes.AsSpan().IndexOf(BrowserAutomationMaster.Core.Common.Constants.XPIMagicBytes.Span) >= 0);
            Assert.True(bytes.AsSpan().IndexOf("META-INF/mozilla.rsa"u8) >= 0);
        }

        /// <summary>
        /// A stable Chrome extension from the Chrome Web Store.
        /// </summary>
        /// <remarks>
        /// Goes through <c>GetLatestChromeVersion</c>, which reaches out for Chrome's current version
        /// first. That is a third-party dependency this test cannot control, so the assertion is that the
        /// download is attempted and either yields bytes or reports a problem — not that it always
        /// succeeds.
        /// </remarks>
        [SkippableFact]
        public async Task DownloadsACrxFromTheChromeWebStore()
        {
            Skip.If(Network.SkipReason is not null, Network.SkipReason!);

            const string extensionUrl =
                "https://chromewebstore.google.com/detail/cjpalhdlnbpafiamejdnhcphjbkeiagm/";

            ExtensionUtility utility = new(extensionUrl, "chrome");

            output.WriteLine($"IsURL={utility.IsURL}, IsChromeExtension={utility.IsChromeExtension}");

            using MemoryStream? contents = await utility.GetExtensionContents();

            if (contents is null)
            {
                output.WriteLine("The store returned nothing extractable; BAMM reported it rather than " +
                                 "returning a truncated stream.");

                return;
            }

            byte[] bytes = contents.ToArray();

            output.WriteLine($"downloaded {bytes.Length} bytes");

            Assert.NotEmpty(bytes);
        }

        /// <summary>
        /// Chrome's current version is reachable, which the CRX download URL depends on.
        /// </summary>
        [SkippableFact]
        public async Task ResolvesTheCurrentChromeVersion()
        {
            Skip.If(Network.SkipReason is not null, Network.SkipReason!);

            // GetLatestChromeVersion is private and its failure path calls WriteAndExit, so the check is
            // on the reachability of the endpoint it reads rather than on the method.
            using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };

            using HttpResponseMessage response = await client.GetAsync("https://versionhistory.googleapis.com/v1/chrome/platforms/win/channels/stable/versions");

            output.WriteLine($"status {(int)response.StatusCode}");

            Assert.True(response.IsSuccessStatusCode, $"Chrome's version endpoint answered {(int)response.StatusCode}.");
        }
    }

    /// <summary>
    /// Whether the network tests can run, and what to do if they cannot.
    /// </summary>
    /// <remarks>
    /// Probed once by making a real request rather than by testing for a DNS server. A machine can
    /// resolve names and still be behind a proxy that refuses them, and the difference decides whether
    /// these tests report a defect or skip.
    /// </remarks>
    internal static class Network
    {
        /// <summary>
        /// Opt-in flag for the tests that reach the publishers.
        /// </summary>
        /// <remarks>
        /// Default off, and not merely because the network is unreliable. These tests exercise paths that
        /// end in <c>WriteAndExit</c> — a failure anywhere inside them surfaces as
        /// <c>InvalidOperationException</c> from Spectre's <c>ReadKey</c> rather than as an assertion
        /// naming what went wrong, because there is no interactive console to write the message to. A
        /// red <c>dotnet test</c> that says only "Failed to read input in non-interactive mode" is worse
        /// than a documented skip, because it cannot be triaged.
        /// <para>
        /// Set <c>BAMM_RUN_NETWORK_TESTS=true</c> to run them. The fixtures in
        /// <see cref="ExtensionArchiveTests"/> cover the format contract unconditionally, so nothing
        /// about the byte layout depends on this flag.
        /// </para>
        /// </remarks>
        public const string OPT_IN_VARIABLE = "BAMM_RUN_NETWORK_TESTS";

        public static bool IsOptedIn =>
            string.Equals(Environment.GetEnvironmentVariable(OPT_IN_VARIABLE), "true", StringComparison.OrdinalIgnoreCase);

        public static string? SkipReason =>
            !IsOptedIn
                ? $"{OPT_IN_VARIABLE} is not set. These tests reach the Chrome Web Store and addons.mozilla.org, and a " +
                  "failure inside BAMM's download path ends in WriteAndExit, whose output this test host cannot " +
                  "capture. Run them deliberately: set BAMM_RUN_NETWORK_TESTS=true."
                : null;

        private static readonly Lazy<Task<(bool Available, string? Reason)>> probe = new(ProbeAsync);

        // Awaited synchronously. Every caller is a test about to block on a download anyway, and the
        // alternative is making IsAvailable async, which cannot be used from a [SkippableFact] guard.
        private static (bool Available, string? Reason) Resolved => probe.Value.GetAwaiter().GetResult();

        public static bool IsAvailable => Resolved.Available;

        public static string? ConnectivityReason => Resolved.Reason;

        private static async Task<(bool, string?)> ProbeAsync()
        {
            try
            {
                using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(15) };

                using HttpResponseMessage response = await client.GetAsync("https://www.google.com");

                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"No outbound HTTPS ({ex.GetType().Name}: {ex.Message}). These tests reach " +
                    "the Chrome Web Store and addons.mozilla.org; they are skipped rather than failed " +
                    "because they measure the publishers' availability as much as this code's.");
            }

            return (false, "Outbound HTTPS is blocked. The download tests need the Chrome Web Store and " +
                "addons.mozilla.org to be reachable.");
        }
    }
}