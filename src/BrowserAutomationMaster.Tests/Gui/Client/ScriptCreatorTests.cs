using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The script creator page: selecting a command, adding it, and the guards around the two fixes
    /// this release made. <br/>
    /// Source: scripts/create/create_script.js, scripts/create/commands.js
    /// </summary>
    /// <remarks>
    /// These are the only claims in the suite that need a real browser. Everything the command
    /// registry contains is already asserted in Tier A by reading the source; what Tier A cannot see is
    /// whether the page can actually <em>use</em> it — that the dropdown holds the entry, that
    /// <c>renderArguments</c> does not throw on it, and that the duplicate-feature comparison works at
    /// runtime rather than only in principle.
    /// </remarks>
    // Category=Browser is what the CI split keys on. The bare `dotnet test` in the README and in
    // the workflow's other job runs everything regardless, so this is a convenience filter and
    // never a requirement for a green result.
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class ScriptCreatorTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        [SkippableFact]
        public async Task TheScriptCreator_LoadsAndOffersEveryCommand()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            IReadOnlyList<string> options = await page.EvaluateAsync<string[]>(
                """
                () => Array.from(document.getElementById('command-select').options).map(o => o.value)
                """
            );

            output.WriteLine(string.Join(", ", options));

            foreach (string command in GuiSourceParser.Instance.CommandNames)
            {
                Assert.Contains(command, options);
            }
        }

        [SkippableFact]
        public async Task SelectingACommand_RendersItsArgumentsWithoutThrowing()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            foreach (string command in GuiSourceParser.Instance.CommandNames)
            {
                List<(string Type, string Message)> alerts = await SelectAndCollectAsync(page, command);

                output.WriteLine($"{command}: {string.Join(", ", alerts.Select(a => a.Type + ": " + a.Message))}");

                // A command whose commandArgs is null reaches Object.keys(null) the moment it is
                // selected, which is the whole of the use-mobile-user-agent defect. Asserted per command
                // rather than once for the registry so a failure names which entry broke.
                Assert.DoesNotContain(
                    alerts,
                    alert => alert.Message.Contains("Cannot convert undefined or null to object", StringComparison.Ordinal)
                );
            }
        }

        [SkippableFact]
        public async Task AddJsCode_IsSelectableAndReachableBetweenItsBlockMarkers()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            bool offered = await page.EvaluateAsync<bool>(
                """
                () => Array.from(document.getElementById('command-select').options)
                            .some(o => o.value === 'Add-JS-Code')
                """
            );

            Assert.True(offered, "Add-JS-Code is in commandCollection but no <option> holds it, so " +
                                "advanceAfterAddingCommand set the select to a value that does not exist.");

            // The value the Add-JS-Code branch of buildCommandText produces. /export decodes it and
            // writes the line into the JavaScript block as raw content.
            string line = await page.EvaluateAsync<string>(
                """
                () => buildCommandText('Add-JS-Code', { 'javascript-code': 'document.title' })
                """
            );

            output.WriteLine($"Add-JS-Code -> {line}");

            string decoded = System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(System.Text.Json.JsonDocument.Parse(line).RootElement.GetProperty("add-to-js").GetString()!));

            Assert.Equal("document.title", decoded);
        }

        [SkippableFact]
        public async Task AddingTheSameFeatureTwice_IsRefusedWithAnAlert()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.EvaluateAsync<object>(
                """
                async () => {
                  commands = {};
                  validateFeatureCommand('Feature: use-http-proxy');
                  commands = { proxy: buildFeatureCommandText('Feature: use-http-proxy', { 'proxy-string': '"USER:PASS@IP:PORT"' }) };
                }
                """
            );

            bool accepted = await page.EvaluateAsync<bool>(
                "() => validateFeatureCommand('Feature: use-http-proxy')"
            );

            output.WriteLine($"second use-http-proxy accepted: {accepted}");

            Assert.False(accepted);

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            output.WriteLine(string.Join(", ", alerts.Select(a => a.Type + ": " + a.Message)));

            Assert.Contains(alerts, alert => alert.Message.Contains("Feature: use-http-proxy", StringComparison.Ordinal));
        }

        [SkippableFact]
        public async Task AddingASecondProxyFeature_IsRefusedWithAnAlert()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.EvaluateAsync<object>(
                """
                () => {
                  commands = {
                    http: buildFeatureCommandText('Feature: use-http-proxy', { 'proxy-string': '"USER:PASS@IP:PORT"' }),
                  };
                }
                """
            );

            bool accepted = await page.EvaluateAsync<bool>(
                "() => validateFeatureCommand('Feature: use-socks4-proxy')"
            );

            output.WriteLine($"use-socks4-proxy after use-http-proxy accepted: {accepted}");

            Assert.False(accepted);

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            output.WriteLine(string.Join(", ", alerts.Select(a => a.Type + ": " + a.Message)));

            Assert.Contains(alerts, alert => alert.Message.Contains("Only one proxy feature", StringComparison.Ordinal));
        }

        [SkippableFact]
        public async Task ANonProxyFeature_IsNotBlockedByTheProxyRule()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.EvaluateAsync<object>(
                """
                () => {
                  commands = {
                    http: buildFeatureCommandText('Feature: use-http-proxy', { 'proxy-string': '"USER:PASS@IP:PORT"' }),
                    ssl: buildFeatureCommandText('Feature: disable-ssl', {}),
                  };
                }
                """
            );

            bool accepted = await page.EvaluateAsync<bool>(
                "() => validateFeatureCommand('Feature: disable-pycache')"
            );

            output.WriteLine($"disable-pycache alongside a proxy accepted: {accepted}");

            Assert.True(accepted);
        }

        [SkippableFact]
        public async Task MobileUserAgent_EmitsACommandBammAccepts()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The rendered line, not just the JSON: this is the text that becomes a .bamc line, and it
            // is what Parser.IsValidFileContents has to accept.
            string line = await page.EvaluateAsync<string>(
                """
                () => {
                  const payload = buildFeatureCommandText('Feature: use-mobile-user-agent', {});
                  return `feature ${JSON.parse(payload).feature}`;
                }
                """
            );

            output.WriteLine($"emitted: {line}");

            Assert.Equal("feature \"use-mobile-user-agent\"", line);
            Assert.True(
                BrowserAutomationMaster.Core.Parsing.Parser.IsValidFileContents(["browser \"chrome\"", line]),
                $"BAMM rejected the line the GUI emits for use-mobile-user-agent: {line}"
            );
        }

        /// <summary>Selects a command and returns the alerts it raised.</summary>
        private static async Task<List<(string Type, string Message)>> SelectAndCollectAsync(GuiTestPage page, string command)
        {
            List<(string Type, string Message)> before = await page.ReadAlertsAsync();

            await page.SelectCommandAsync(command);

            return [.. before, .. await page.ReadAlertsAsync()];
        }
    }
}
