using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The chrome around the script creator: sidebar, mobile menu, dark mode, and the responsive
    /// stylesheets. <br/>
    /// Source: scripts/sidebar.js, scripts/responsive.js, index.html
    /// </summary>
    /// <remarks>
    /// Asserted on element presence and on state that survives a reload, not on computed layout. The
    /// two responsive mechanisms disagree — responsive.js switches at 1050 and 1600 pixels while the
    /// stylesheets' media queries are at 1080 and 1599 — so a test that asserted a width produced one
    /// particular layout would be asserting which of the two numbers happens to land where, and would
    /// go red for a change to either.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class SidebarAndChromeTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        [SkippableFact]
        public async Task TheResponsiveScripts_AreLoadedAndDefineTheirSwitches()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string report = await page.EvaluateAsync<string>(
                """
                () => JSON.stringify({
                  responsiveLoaded: typeof setResponsiveness === 'function',
                  tabletDefined: typeof tabletChild !== 'undefined',
                  widescreenDefined: typeof widescreenChild !== 'undefined',
                  appendStyle: typeof appendStyle === 'function',
                  removeStyle: typeof removeStyle === 'function',
                })
                """
            );

            output.WriteLine(report);

            Assert.Contains("\"responsiveLoaded\":true", report, StringComparison.Ordinal);
            Assert.Contains("\"tabletDefined\":true", report, StringComparison.Ordinal);
            Assert.Contains("\"widescreenDefined\":true", report, StringComparison.Ordinal);
        }

        /// <remarks>
        /// The viewports are chosen from inside responsive.js's two bands, which are narrow and do not
        /// cover the sizes they sound like they cover. The widescreen branch is
        /// <c>width &gt;= 1600 &amp;&amp; width &lt; 1920 &amp;&amp; height &gt;= 1050</c>, so 1920 exactly
        /// falls out of it; the tablet branch is <c>1024 &lt;= width &lt;= 1600 &amp;&amp; 768 &lt;= height
        /// &lt; 1050</c>, so 800x600 falls out of that too. A phone-sized viewport gets neither
        /// stylesheet, which is the third branch.
        /// </remarks>
        [SkippableTheory]
        [InlineData(1200, 800, "tablet-style")]
        [InlineData(1700, 1080, "widescreen-style")]
        [InlineData(1920, 1080, "none")]
        [InlineData(800, 600, "none")]
        [InlineData(2560, 1440, "none")]
        public async Task TheViewportDecidesWhichStylesheetIsPresent(int width, int height, string expected)
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.Page.SetViewportSizeAsync(width, height);
            await page.Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Presence, not computed layout — see the remarks on the file.
            string[] present = await page.EvaluateAsync<string[]>(
                """
                () => ['tablet-style', 'widescreen-style'].filter(id => document.getElementById(id) !== null)
                """
            );

            output.WriteLine($"{width}x{height} -> [{string.Join(", ", present)}]");

            string actual = present.Length == 0 ? "none" : present[0];

            Assert.Equal(expected, actual);
        }

        [SkippableFact]
        public async Task TheStylesheetSwitchIsIdempotent()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.Page.SetViewportSizeAsync(1200, 800);
            await page.Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Calling it repeatedly must not stack copies of the same <link>: appendStyle checks by id
            // and removeStyle takes the other one out, so a second call is where a leak would show.
            int afterManyCalls = await page.EvaluateAsync<int>(
                """
                () => {
                  for (let i = 0; i < 5; i++) {
                    setResponsiveness();
                  }
                  return document.querySelectorAll('#tablet-style').length;
                }
                """
            );

            output.WriteLine($"tablet-style elements after 6 calls: {afterManyCalls}");

            Assert.Equal(1, afterManyCalls);
        }

        [SkippableFact]
        public async Task DarkMode_SurvivesAReload()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // Set the way the switch does, then reload and read back what the page applied on load.
            await page.EvaluateAsync<object>("() => localStorage.setItem('dark-mode', 'true')");
            await page.Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

            string applied = await page.EvaluateAsync<string>("() => localStorage.getItem('dark-mode')");

            output.WriteLine($"stored after reload: {applied}");

            Assert.Equal("true", applied);

            await page.EvaluateAsync<object>("() => localStorage.setItem('dark-mode', 'false')");
            await page.Page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle });

            Assert.Equal("false", await page.EvaluateAsync<string>("() => localStorage.getItem('dark-mode')"));
        }

        [SkippableFact]
        public async Task TheSidebarCollapseControl_Exists()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // sidebar.js binds .sidebar .collapse-btn, .toggle-mob-menu and .switch input. Asserted as
            // presence because sidebar.js wires them once at load, outside any function the page exposes,
            // so there is nothing to call directly.
            string report = await page.EvaluateAsync<string>(
                """
                () => JSON.stringify({
                  collapse: document.querySelectorAll('.sidebar .collapse-btn').length,
                  mobileMenu: document.querySelectorAll('.toggle-mob-menu').length,
                  switchInput: document.querySelectorAll('.switch input').length,
                })
                """
            );

            output.WriteLine(report);

            Assert.Contains("\"collapse\":1", report, StringComparison.Ordinal);
            Assert.Contains("\"mobileMenu\":1", report, StringComparison.Ordinal);
            Assert.Contains("\"switchInput\":1", report, StringComparison.Ordinal);
        }

        [SkippableFact]
        public async Task TheSidebarLinks_DoNotNavigateAwayFromTheGui()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The sidebar's anchors are in-page view switches. A real href would leave the GUI served over
            // HTTP with no server behind it, which is the failure the in-page routing exists to avoid.
            string[] hrefs = await page.EvaluateAsync<string[]>(
                """
                () => Array.from(document.querySelectorAll('.sidebar a'))
                            .map(a => a.getAttribute('href') ?? '')
                            .filter(h => h !== '')
                """
            );

            foreach (string href in hrefs)
            {
                output.WriteLine($"href: {href}");

                Assert.True(
                    href.StartsWith('#') || Uri.TryCreate(href, UriKind.Absolute, out _),
                    $"'{href}' is neither a fragment nor an absolute URL, so it would resolve against the " +
                    "origin this suite serves from."
                );
            }

            // The three view switches the sidebar exists for are fragments.
            foreach (string view in new[] { "#create", "#export", "#terminate" })
            {
                Assert.Contains(hrefs, href => href == view);
            }
        }
    }
}