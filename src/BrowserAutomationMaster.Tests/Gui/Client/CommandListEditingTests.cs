using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The command list: removing entries, reindexing, and what survives. <br/>
    /// Source: scripts/create/create_script.js (removeSelectedCommand, removeJsBlockCommand,
    /// reindexCommands, getNextIndexAfterDelete)
    /// </summary>
    /// <remarks>
    /// The list is the only record of what a script contains — the commands themselves live in
    /// localStorage and the list is what <c>reindexCommands</c> reads to rebuild them. So an editing
    /// bug shows up as commands that silently stop being part of the script.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class CommandListEditingTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        /// <summary>
        /// Fills the command list with the given lines, the way adding commands would, and reindexes.
        /// </summary>
        private static async Task SeedListAsync(GuiTestPage page, params string[] lines)
        {
            await page.EvaluateAsync<object>(
                """
                lines => {
                  commandList.innerHTML = '';
                  for (const line of lines) {
                    const item = document.createElement('li');
                    item.textContent = line;
                    item.addEventListener('click', handleCommandListClick);
                    commandList.appendChild(item);
                  }
                  reindexCommands();
                }
                """,
                lines
            );
        }

        /// <remarks>
        /// Returns string[] rather than List&lt;string&gt;: Playwright's argument converter cannot
        /// rebuild a List from a JSON array here and throws a bare NullReferenceException, so the
        /// array type is what the whole suite uses for anything the page returns as a list.
        /// </remarks>
        private static Task<string[]> ListContentsAsync(GuiTestPage page)
            => page.EvaluateAsync<string[]>(
                """
                () => Array.from(commandList.children).map(li => li.textContent)
                """);

        [SkippableTheory]
        [InlineData(1, 0, -1)]
        [InlineData(2, 0, 0)]
        [InlineData(2, 1, 0)]
        [InlineData(3, 0, 0)]
        [InlineData(3, 1, 1)]
        [InlineData(3, 2, 1)]
        public async Task GetNextIndexAfterDelete_SelectsTheNeighbour(int count, int index, int expected)
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            int actual = await page.EvaluateAsync<int>(
                """
                ([index, count]) => getNextIndexAfterDelete(index, count)
                """,
                new object[] { index, count }
            );

            output.WriteLine($"count={count} index={index} -> {actual}");

            Assert.Equal(expected, actual);
        }

        [SkippableFact]
        public async Task RemovingAnEntry_RenumbersTheRest()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await SeedListAsync(page, "browser \"chrome\"", "visit \"https://a\"", "visit \"https://b\"");

            string[] before = await ListContentsAsync(page);
            output.WriteLine($"before: {string.Join(" | ", before)}");

            await page.EvaluateAsync<object>(
                """
                () => {
                  const target = commandList.children[1];
                  target.classList.add('list-item');
                  removeSelectedCommand();
                }
                """
            );

            string[] after = await ListContentsAsync(page);
            output.WriteLine($"after:  {string.Join(" | ", after)}");

            // The removed line is gone and the survivors are contiguous. A gap in the reindexed map
            // would make the commands after it unreachable by index, which is how getIndexOfSelectedCommand
            // silently addresses the wrong entry.
            Assert.Equal(["browser \"chrome\"", "visit \"https://b\""], after);

            int[] keys = await page.EvaluateAsync<int[]>("() => Object.keys(commands).map(Number)");
            output.WriteLine($"command keys: {string.Join(", ", keys)}");

            Assert.Equal([0, 1], keys);
        }

        [SkippableFact]
        public async Task RemovingWithNothingSelected_IsRefusedWithAnAlert()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await SeedListAsync(page, "browser \"chrome\"", "visit \"https://a\"");

            string outcome = await page.EvaluateAsync<string>(
                """
                () => {
                  try {
                    removeSelectedCommand();
                    return 'no error';
                  } catch (e) {
                    return e.message;
                  }
                }
                """
            );

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            output.WriteLine($"outcome: {outcome}");
            output.WriteLine(string.Join(Environment.NewLine, alerts.Select(a => a.Type + ": " + a.Message)));

            // It throws as well as alerting. The throw is deliberate — the caller is the delete button's
            // handler and there is nothing sensible to continue with — but the alert is what a user sees.
            Assert.Contains(alerts, alert => alert.Message.Contains("select an element", StringComparison.OrdinalIgnoreCase));

            // The list is untouched.
            Assert.Equal(["browser \"chrome\"", "visit \"https://a\""], await ListContentsAsync(page));
        }

        [SkippableFact]
        public async Task RemovingAJavascriptBlock_RemovesTheWholeBlock()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The block is three entries in the list — start marker, code, end marker — and removing the
            // marker has to take all three. Removing only the selected one would leave an unterminated
            // block, which the parser treats as an unclosed JS section.
            // The list holds what the creator stored, so an Add-JS-Code entry appears as its
            // {"add-to-js": <base64>} payload. removeJsBlockCommand identifies a block by that marker:
            // getJsBlockStartIndex looks for an "add-to-js" entry between the two markers. Seeding the
            // bare code instead would leave the block unrecognised and the test would be measuring a
            // shape the list never holds.
            await SeedListAsync(
                page,
                "browser \"chrome\"",
                "start-javascript",
                "{\"add-to-js\":\"ZG9jdW1lbnQudGl0bGUgPSB4Ijs=\"}",
                "end-javascript",
                "visit \"https://example.com\"");

            string[] before = await ListContentsAsync(page);
            output.WriteLine($"before: {string.Join(" | ", before)}");

            await page.EvaluateAsync<object>(
                """
                () => {
                  const target = commandList.children[1];
                  target.classList.add('list-item');
                  removeSelectedCommand();
                }
                """
            );

            string[] after = await ListContentsAsync(page);
            output.WriteLine($"after:  {string.Join(" | ", after)}");

            Assert.Equal(["browser \"chrome\"", "visit \"https://example.com\""], after);
            Assert.DoesNotContain(after, line => line.Contains("javascript", StringComparison.Ordinal));
        }

        [SkippableFact]
        public async Task TheListAndStorageStayInStep()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await SeedListAsync(page, "browser \"chrome\"", "visit \"https://a\"");

            // reindexCommands writes to storage on every edit, and getData reads from storage on load,
            // so the two agreeing here is what makes a reload lossless.
            string stored = await page.EvaluateAsync<string>("() => localStorage.getItem('commands')");

            string[] listed = await ListContentsAsync(page);

            output.WriteLine($"stored:  {stored}");
            output.WriteLine($"listed:  {string.Join(" | ", listed)}");

            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(stored!);

            string[] values = [.. document.RootElement.EnumerateObject().Select(property => property.Value.GetString()!)];

            Assert.Equal(listed, values);
        }
    }
}