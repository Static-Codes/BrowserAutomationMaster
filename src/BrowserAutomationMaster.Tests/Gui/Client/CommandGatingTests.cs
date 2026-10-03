using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// Which commands the GUI offers at each point in building a script. <br/>
    /// Source: scripts/create/create_script.js (populateCommandSelect, isCommandDisabled, recalculateState)
    /// </summary>
    /// <remarks>
    /// The dropdown is a state machine, and each of its five rules is a bug that shows up as an
    /// unreachable or an invalid command rather than as a visible failure. Asserted through
    /// <c>isCommandDisabled</c> directly rather than by reading option elements: the options are
    /// rebuilt on every repopulate, and the function is what decides their enabled state.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class CommandGatingTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        /// <summary>
        /// Drives <c>isCommandDisabled</c> for one command, with the command list seeded and the given
        /// commands already added.
        /// </summary>
        private static async Task<bool> IsDisabledAsync(
            GuiTestPage page,
            string commandName,
            (bool BrowserExists, bool FeaturesAllowed, bool VisitAdded) state)
        {
            return await page.EvaluateAsync<bool>(
                """
                ([commandName, browserExists, features, visited]) => {
                  commands = {};
                  // Prefixed names, deliberately. featuresAllowed and visitAdded are globals the function
                  // reads, and destructuring into those exact names would shadow them, so
                  // `featuresAllowed = featuresAllowed` would assign the parameter to itself and leave
                  // the global at whatever recalculateState last set it to.
                  featuresAllowed = features;
                  visitAdded = visited;

                  const command = commandCollection.find(c => c.commandName === commandName);
                  if (!command) {
                    throw new Error('No such command: ' + commandName);
                  }

                  return isCommandDisabled(command, browserExists);
                }
                """,
                new object[] { commandName, state.BrowserExists, state.FeaturesAllowed, state.VisitAdded }
            );
        }

        [SkippableFact]
        public async Task Browser_IsDisabledOncePresent()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            output.WriteLine("absent:  " + await IsDisabledAsync(page, "Browser", (false, false, false)));
            output.WriteLine("present: " + await IsDisabledAsync(page, "Browser", (true, false, false)));

            // A script has one browser. Offering a second would emit two `browser "..."` lines, which the
            // parser rejects as a duplicate.
            Assert.False(await IsDisabledAsync(page, "Browser", (false, false, false)));
            Assert.True(await IsDisabledAsync(page, "Browser", (true, false, false)));
        }

        [SkippableFact]
        public async Task Visit_IsDisabledUntilFeaturesAreAllowedAndAfterItIsAdded()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            output.WriteLine("features closed: " + await IsDisabledAsync(page, "Visit", (true, false, false)));
            output.WriteLine("features open:   " + await IsDisabledAsync(page, "Visit", (true, true, false)));
            output.WriteLine("visit added:     " + await IsDisabledAsync(page, "Visit", (true, true, true)));

            // Features come before a visit; two visits are two navigations in one script line.
            Assert.True(await IsDisabledAsync(page, "Visit", (true, false, false)));
            Assert.False(await IsDisabledAsync(page, "Visit", (true, true, false)));
            Assert.True(await IsDisabledAsync(page, "Visit", (true, true, true)));
        }

        [SkippableFact]
        public async Task Features_AreDisabledUntilTheFirstNonFeatureCommand()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // Every Feature: entry, not one of them: a feature added before the script can act on it is a
            // line BAMM would accept and a script that would not run.
            foreach (string feature in GuiSourceParser.Instance.FeatureNames)
            {
                bool closed = await IsDisabledAsync(page, $"Feature: {feature}", (false, false, false));
                bool open = await IsDisabledAsync(page, $"Feature: {feature}", (false, true, false));

                output.WriteLine($"Feature: {feature}: closed={closed} open={open}");

                Assert.True(closed, $"Feature: {feature} was offered before any command was added");
                Assert.False(open, $"Feature: {feature} stayed disabled once features were allowed");
            }
        }

        [SkippableFact]
        public async Task EverythingElse_IsDisabledOnceAVisitIsAdded()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The non-feature, non-Browser, non-Visit commands: the body of the script.
            foreach (string command in GuiSourceParser.Instance.CommandNames
                .Where(name => !name.StartsWith("Feature:", StringComparison.Ordinal))
                .Where(name => name is not ("Browser" or "Visit")))
            {
                bool before = await IsDisabledAsync(page, command, (false, true, false));
                bool after = await IsDisabledAsync(page, command, (true, true, true));

                output.WriteLine($"{command}: before={before} after={after}");

                Assert.False(before, $"{command} was unavailable before a visit was added");
                Assert.True(after, $"{command} stayed available after a visit was added");
            }
        }

        [SkippableFact]
        public async Task PopulateCommandSelect_OffersEveryRegistryEntry()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string[] offered = await page.EvaluateAsync<string[]>(
                """
                () => {
                  populateCommandSelect();
                  return Array.from(commandSelect.options).map(o => o.value);
                }
                """
            );

            output.WriteLine($"{offered.Length} option(s): {string.Join(", ", offered)}");

            foreach (string command in GuiSourceParser.Instance.CommandNames)
            {
                Assert.Contains(command, offered);
            }
        }

    }
}