using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The GUI's own requests: what it sends, and what it does with the answer. <br/>
    /// Source: index.html, scripts/create/create_script.js (validateScriptContents, loadScriptData)
    /// </summary>
    /// <remarks>
    /// End-to-end through the GUI's real <c>fetch</c>, with only the server's answers synthetic. The
    /// point is the client's half of the contract: which status it treats as success, and which body
    /// fields it reads — the two things a change on the server side silently breaks.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class EndpointRoundTripTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        /// <summary>
        /// Drives the GUI's own validation flow and reports what the user would be shown.
        /// </summary>
        /// <remarks>
        /// Runs <c>validateScriptContents</c> rather than reimplementing the client's handling, so the
        /// assertion is on what a user sees and not on a copy of the logic that could drift with it.
        /// </remarks>
        private static async Task<string> ValidateAndReadAsync(GuiTestPage page, string[] script)
        {
            // Seeded as JSON objects, because that is what the list holds: addCommandToCommandList is
            // called with buildCommandText's output, and validateScriptContents JSON.parses each entry.
            // A bare command line here is refused by the parse before any request is made.
            await page.EvaluateAsync<object>(
                """
                lines => {
                  commands = {};
                  lines.forEach((line, index) => {
                    commands[index] = line;
                  });
                  localStorage.setItem('commands', JSON.stringify(commands));
                }
                """,
                script
            );

            await page.EvaluateAsync<object>("() => validateScriptContents()");
            await page.Page.WaitForFunctionAsync(
                "() => new Promise(resolve => setTimeout(() => resolve((window.__guiAlerts ?? []).length > 0), 250))"
            );

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            return string.Join(" | ", alerts.Select(alert => alert.Type + ": " + alert.Message));
        }

        [SkippableFact]
        public async Task AValidScript_IsConfirmedSuccessful()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string shown = await ValidateAndReadAsync(page, ["""{"browser":"\"chrome\""}""", """{"visit":"\"https://example.com\""}"""]);

            output.WriteLine($"shown: {shown}");

            Assert.Contains("Script validation successful", shown, StringComparison.Ordinal);
        }

        [SkippableFact]
        public async Task AnInvalidScript_IsReportedAsInvalid()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // Routed to the verdict path: a 200 carrying success:false. The default route answers
            // success:true, so without this the client's failure branch is never taken.
            await page.Context.RouteAsync(
                "**/validate*",
                routeCall => routeCall.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "application/json",
                    Body = """{ "success": false }"""
                })
            );

            string shown = await ValidateAndReadAsync(page, ["""{"browser":"\"chrome\""}"""]);

            output.WriteLine($"shown: {shown}");

            Assert.DoesNotContain("Script validation successful", shown, StringComparison.Ordinal);

            // The verdict path renders the reason it was given, and my routed body carries none — so
            // the client's own fallback is what appears. A real rejection from /validate does carry
            // one, which is what Phase 2 fixed; see the guard-path test below for the other half.
            Assert.Contains("Script validation failed", shown, StringComparison.Ordinal);
        }

        [SkippableFact]
        public async Task AGuardPathRejection_ReportsTheStatusRatherThanTheServersReason()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // What the Phase 2 changes look like from the client. /validate now answers 400 on a guard
            // path — the status it should always have carried — and the GUI gates on response.ok. It
            // reports the status it saw and never reads the body, so the reason the server put there is
            // still discarded.
            //
            // Worth stating plainly because the two fixes interact: the status change is what moved this
            // request from the success branch to the failure branch, and neither branch shows the
            // reason. Before it, a rejected request answered 200 and the user saw "Valid" for a script
            // that could not compile; now they see a 400. Both are wrong in different ways, and the
            // actual resolution is in the client — read the body when !ok — which is a GUI change and
            // not part of this suite.
            await page.Context.RouteAsync(
                "**/validate*",
                routeCall => routeCall.FulfillAsync(new Microsoft.Playwright.RouteFulfillOptions
                {
                    Status = 400,
                    ContentType = "application/json",
                    Body = """{"success":false,"error":"Invalid request, missing param \"contents\""}"""
                })
            );

            string shown = await ValidateAndReadAsync(page, ["""{"browser":"\"chrome\""}"""]);

            output.WriteLine($"shown: {shown}");

            Assert.Contains("400", shown, StringComparison.Ordinal);
            Assert.DoesNotContain("missing param", shown, StringComparison.Ordinal);
        }

        [SkippableFact]
        public async Task TheValidationRequest_AsksForBase64EncodedContents()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string url = await page.EvaluateAsync<string>(
                """
                async () => {
                  let seen = null;
                  const original = window.fetch;

                  window.fetch = (target, init) => {
                    seen = target;
                    return original(target, init);
                  };

                  commands = { 0: '{"browser":"\\"chrome\\""}' };
                  validateScriptContents();

                  await new Promise(resolve => setTimeout(resolve, 250));
                  window.fetch = original;
                  return String(seen);
                }
                """
            );

            output.WriteLine($"requested: {url}");

            // The query parameter name and the base64 encoding are the whole contract with /validate:
            // the server reads `contents`, base64-decodes it, and splits on newlines.
            Assert.Contains("/validate?contents=", url, StringComparison.Ordinal);

            string encoded = url[(url.IndexOf("contents=", StringComparison.Ordinal) + 9)..];

            output.WriteLine($"encoded: {encoded}");

            Assert.Equal(
                "browser \"chrome\"",
                System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Uri.UnescapeDataString(encoded)))
            );
        }

        [SkippableFact]
        public async Task TheLoadRequest_ReadsTheItemsMapAndSurvivesAnEmptyOne()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // Read straight after load, which is when index.html fetches it.
            object stored = await page.EvaluateAsync<object>(
                """
                () => (typeof localUserScripts === 'undefined' ? 'undefined' : JSON.stringify(localUserScripts))
                """
            );

            output.WriteLine($"localUserScripts: {stored}");

            // The default route answers 200 with an empty Items map, and the GUI must land with that
            // rather than with undefined. A /load failure navigates to inactive.html instead, so getting
            // this wrong moves the page out from under the test.
            Assert.NotEqual("undefined", stored!.ToString()!);
        }
    }
}