using Xunit;
using Microsoft.Playwright;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The Playwright browser, launched once per collection. <br/>
    /// Source: index.html, scripts/sidebar.js, scripts/responsive.js
    /// </summary>
    /// <remarks>
    /// Installed once rather than per-test because a launch costs about a second and the suite would
    /// otherwise spend most of its time starting Chromium. <br/>
    /// This is the only tier that skips. Tiers A and B read files and talk to the server directly and
    /// have no excuse for not running, so a missing browser is the sole situation where "skipped" is
    /// honest — and <see cref="SkipReason"/> spells out how to install it, because a green build with
    /// twenty skips is indistinguishable from one where twenty things work.
    /// </remarks>
    public sealed class BrowserFixture : IAsyncLifetime
    {
        /// <summary>The collection name, so a client test class opts in with one attribute.</summary>
        public const string COLLECTION_NAME = "GuiClient";

        private static string? probeReason;
        private static bool probed;

        /// <summary>
        /// Whether a browser is installed and usable.
        /// </summary>
        /// <remarks>
        /// Set by <see cref="InitializeAsync"/>, which runs before any test in the collection. Not
        /// computed on first read: the probe is async, and a lazily started Task read synchronously from
        /// a test body has not settled yet, which would report "no browser" on a machine that has one.
        /// </remarks>
        public static bool IsAvailable => probed && probeReason is null;

        /// <summary>
        /// Why no browser is available, or null when one is. Names the install command rather than
        /// just reporting absence, because a skipped test whose reason is "no browser" tells nobody
        /// what to do.
        /// </summary>
        public static string? SkipReason => probeReason;

        /// <summary>The shared Playwright driver.</summary>
        public IPlaywright Playwright { get; private set; } = null!;

        /// <summary>The shared browser.</summary>
        public IBrowser Browser { get; private set; } = null!;

        /// <summary>The static server serving the pinned GUI over HTTP.</summary>
        public GuiTestServer Server { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            probeReason = await ProbeAsync();
            probed = true;

            if (!IsAvailable)
            {
                return;
            }

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,

                // The plan's memory budget: one Chromium for the whole assembly rather than one per test,
                // because thirty sequential launches is where this would cost gigabytes instead of
                // megabytes. These are the flags that make one instance stay small — a capped JS heap and
                // a single renderer, plus the container flags Chromium needs under a CI runner.
                ChromiumSandbox = false,
                Args =
                [
                    "--disable-dev-shm-usage",
                    "--disable-gpu",
                    "--disable-extensions",
                    "--renderer-process-limit=1",
                    "--js-flags=--max-old-space-size=256",
                ]
            });

            Server = await GuiTestServer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (Server is not null)
            {
                await Server.DisposeAsync();
            }

            // Reverse order: the browser cannot outlive the driver that owns it.
            if (Browser is not null)
            {
                await Browser.DisposeAsync();
            }

            Playwright?.Dispose();
        }

        /// <summary>
        /// Whether a browser is installed, and if not, what to run to install one.
        /// </summary>
        /// <remarks>
        /// Probed with a trivial launch rather than by looking for the browser cache directory. A
        /// directory can exist while a download inside it is truncated or the wrong platform, and
        /// discovering that in the first test's error message costs far more than a short probe.
        /// </remarks>
        private static async Task<string?> ProbeAsync()
        {
            try
            {
                using IPlaywright probe = await Microsoft.Playwright.Playwright.CreateAsync();

                // Launched rather than merely located: a browser directory can exist while the binary
                // inside it is missing or the wrong architecture, and finding that out from the first
                // test's error message costs far more than a short launch here.
                await using IBrowser launched = await probe.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                if (!File.Exists(probe.Chromium.ExecutablePath))
                {
                    return $"Playwright's Chromium is not installed. Expected it at " +
                        $"'{probe.Chromium.ExecutablePath}'. Install it with " +
                        "'pwsh src/BrowserAutomationMaster.Tests/bin/Debug/net10.0/playwright.ps1 install chromium'.";
                }

                return null;
            }
            catch (Exception ex)
            {
                return $"Playwright could not start: {ex.Message}. Install it with " +
                    "'pwsh bin/Debug/net10.0/playwright.ps1 install chromium'.";
            }
        }
    }

    /// <summary>Collections that need a browser, so a test class opts in by name.</summary>
    [CollectionDefinition(BrowserFixture.COLLECTION_NAME)]
    public sealed class BrowserCollectionDefinition : ICollectionFixture<BrowserFixture>;
}
