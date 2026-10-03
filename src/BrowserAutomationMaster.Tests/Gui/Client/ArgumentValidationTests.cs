using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The four argument validators, and which commands demand a quoted value. <br/>
    /// Source: scripts/create/create_script.js (validateSecondsArgument, validateHeadersArgument,
    /// validateQuotedArgument, requiresQuotedArgument, isQuotedString)
    /// </summary>
    /// <remarks>
    /// Each validator exists because the command it guards has a failure mode the parser reports with
    /// no useful location: <c>wait-for-seconds "2"</c> and <c>wait-for-seconds 2</c> differ only in the
    /// quotes, and the second one fails deep in the compilation pass rather than at the point of entry.
    /// The GUI is the only place a user can be told which.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class ArgumentValidationTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        [SkippableTheory]
        [InlineData("2", true)]
        [InlineData("0.5", true)]
        [InlineData("0", true)]
        [InlineData("", false)]
        [InlineData("two", false)]
        [InlineData("\"2\"", false)]
        public async Task WaitForSeconds_AcceptsOnlyAnUnquotedNumber(string value, bool expected)
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            bool accepted = await page.EvaluateAsync<bool>(
                """
                value => validateSecondsArgument('wait-for-seconds', value)
                """,
                value
            );

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            output.WriteLine($"'{value}' -> accepted={accepted}; {string.Join(" | ", alerts.Select(a => a.Message))}");

            Assert.Equal(expected, accepted);

            // The reason is the whole point of the guard, so both halves are asserted: the verdict and
            // whether the user was actually told why.
            Assert.Equal(!expected, alerts.Count > 0);
        }

        [SkippableFact]
        public async Task TheSecondsRejection_NamesTheArgumentAndTheCommand()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            await page.EvaluateAsync<object>("() => validateSecondsArgument('wait-for-seconds', 'two')");

            List<(string Type, string Message)> alerts = await page.ReadAlertsAsync();

            output.WriteLine(string.Join(Environment.NewLine, alerts.Select(a => a.Type + ": " + a.Message)));

            string message = Assert.Single(alerts).Message;

            Assert.Contains("wait-for-seconds", message, StringComparison.Ordinal);
            Assert.Contains("must be a valid number", message, StringComparison.Ordinal);
        }

        /// <remarks>
        /// Two rules, not one: the value must be quoted, and what is inside the quotes must parse as
        /// JSON. A quoted string that is not JSON is refused with a different message, which is the
        /// point of checking both — the user is told whether to add quotes or to fix their object.
        /// <para>
        /// The cases are built inside the page rather than passed from <c>[InlineData]</c>. A header
        /// value contains quotes of its own, and routing that through a C# attribute literal, JSON, and
        /// then into a JavaScript string means three escaping layers to get wrong for one assertion.
        /// </para>
        /// </remarks>
        [SkippableFact]
        public async Task Headers_AcceptsOnlyAQuotedJsonObject()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string[] report = await page.EvaluateAsync<string[]>(
                """
                () => {
                  const cases = [
                    ['quoted JSON object', '"' + JSON.stringify({ Header: 'Value' }) + '"', true],
                    ['quoted empty object', '"{}"', true],
                    ['quoted but not JSON', '"a:b@c"', false],
                    ['JSON but unquoted', JSON.stringify({ Header: 'Value' }), false],
                    ['quoted prose', '"not json"', false],
                    ['empty', '', false],
                  ];

                  return cases.map(([label, value, expected]) => {
                    const actual = validateHeadersArgument(value);
                    return `${label}: expected=${expected} actual=${actual} value=${value}`;
                  });
                }
                """
            );

            foreach (string line in report) {
                output.WriteLine(line);
            }

            Assert.All(report, line => Assert.Contains("expected=", line, StringComparison.Ordinal));
            Assert.All(report, line =>
            {
                string expected = line[(line.IndexOf("expected=", StringComparison.Ordinal) + 9)..line.IndexOf(" actual=", StringComparison.Ordinal)];
                string actual = line[(line.IndexOf(" actual=", StringComparison.Ordinal) + 8)..line.IndexOf(" value=", StringComparison.Ordinal)];

                Assert.Equal(expected, actual);
            });
        }

        [SkippableFact]
        public async Task TheHeadersRejections_DistinguishQuotesFromJson()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            string[] messages = await page.EvaluateAsync<string[]>(
                """
                () => {
                  const read = () => (window.__guiAlerts ?? []).splice(0).map(a => a.text);

                  validateHeadersArgument('{ not quoted }');
                  const unquoted = read();

                  validateHeadersArgument('"not json"');
                  const badJson = read();

                  return [...unquoted, ...badJson];
                }
                """
            );

            foreach (string message in messages)
            {
                output.WriteLine(message);
            }

            // Two different fixes, so two different messages. One message covering both would leave the
            // user guessing which of the two things to change.
            Assert.Contains(messages, message => message.Contains("quoted JSON string", StringComparison.Ordinal));
            Assert.Contains(messages, message => message.Contains("valid JSON", StringComparison.Ordinal));
        }

        [SkippableTheory]
        [InlineData("\"chrome\"", true)]
        [InlineData("chrome", false)]
        [InlineData("\"\"", true)]
        public async Task QuotedArgument_AcceptsAnyStringWrappedInQuotes(string value, bool expected)
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            bool accepted = await page.EvaluateAsync<bool>(
                """
                value => validateQuotedArgument('arg', 'Visit', value)
                """,
                value
            );

            output.WriteLine($"'{value}' -> accepted={accepted}");

            Assert.Equal(expected, accepted);
        }

/// <remarks>
        /// Driven from the registry rather than from a hand-written table.
        /// <c>requiresQuotedArgument</c> has two halves: a placeholder beginning with a quote, and a
        /// hardcoded name list. A table here would restate whichever half happened to be true for each
        /// command, which is the part least likely to be wrong.
        /// </remarks>
        [SkippableFact]
        public async Task OnlyTwoCommandsAreReachedByTheNameListRatherThanTheirPlaceholder()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // A command whose placeholder does not begin with a quote but which the function still
            // treats as quoted can only have been reached by name, so reading that set back reads the
            // name list.
            string[] byName = await page.EvaluateAsync<string[]>(
                """
                () => commandCollection
                  .filter(command => !(command.placeholder && command.placeholder.startsWith('"')))
                  .filter(requiresQuotedArgument)
                  .map(command => command.commandName)
                  .sort()
                """
            );

            output.WriteLine($"reached by name: {string.Join(", ", byName)}");

            // Every command the source lists by name — Add-Cookie, Add-Header, Click-At-Position,
            // Fill-Text, Fill-Text-Exp, Open-New-Tab, Select-Option — already has a quoted placeholder
            // ('"REQUEST-ID", "123456789"' and so on), so the placeholder rule catches them first. Those
            // seven entries in the name list are redundant: deleting them changes nothing.
            //
            // Only the two feature prefixes are load-bearing, because a feature's placeholder is its
            // argument name rather than an example value. These two are also the commands whose input
            // box gives no hint that quotes are required — see the next test.
            Assert.Equal(["Feature: add-extension", "Feature: use-mobile-user-agent"], byName);
        }

        [SkippableFact]
        public async Task EveryCommandThatDemandsQuotes_RefusesAnUnquotedValue()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The end-to-end property, over every command rather than a sample: a command that asks for
            // quotes and would accept them unquoted produces a .bamc line the parser rejects, with the
            // failure reported at compile time rather than here.
            string[] offenders = await page.EvaluateAsync<string[]>(
                """
                () => commandCollection
                  .filter(requiresQuotedArgument)
                  .filter(command => {
                    const bare = command.placeholder && command.placeholder.replace(/^"/, '');
                    const bareValue = bare || 'value';
                    return validateQuotedArgument('arg', command.commandName, bareValue);
                  })
                  .map(command => command.commandName)
                """
            );

            foreach (string name in offenders)
            {
                output.WriteLine($"accepted unquoted: {name}");
            }

            Assert.Empty(offenders);
        }

        [SkippableFact]
        public async Task WaitForSeconds_IsTheOneCommandThatRefusesAQuotedValue()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The inverse rule. `wait-for-seconds "2"` is refused by validateSecondsArgument, and it is
            // the only command whose argument is deliberately left unquoted — a quoted number reaches
            // the compiler as a string.
            bool quotedAccepted = await page.EvaluateAsync<bool>(
                "() => validateSecondsArgument('wait-for-seconds', '\"2\"')"
            );

            bool unquotedAccepted = await page.EvaluateAsync<bool>(
                "() => validateSecondsArgument('wait-for-seconds', '2')"
            );

            output.WriteLine($"quoted={quotedAccepted} unquoted={unquotedAccepted}");

            Assert.False(quotedAccepted);
            Assert.True(unquotedAccepted);
        }

        [SkippableFact]
        public async Task TheCommandsRequiringQuotesWhosePlaceholdersDoNotSaySo()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // A cosmetic gap, pinned rather than fixed.
            //
            // requiresQuotedArgument has two halves: a placeholder starting with a quote, and a list of
            // command names. Most commands are caught by the placeholder, which is also what the user
            // sees in the empty input box. Two are caught only by the name list, so the box offers no
            // hint that quotes are required and validation then refuses the unquoted value.
            //
            // Asserted as an exact set so that adding a third name-list-only command is noticed, and so
            // that fixing the two placeholders fails this test rather than passing unnoticed.
            string[] unadvertised = await page.EvaluateAsync<string[]>(
                """
                () => commandCollection
                  .filter(requiresQuotedArgument)
                  .filter(command => !(command.placeholder && command.placeholder.startsWith('"')))
                  .map(command => command.commandName)
                """
            );

            output.WriteLine($"require quotes but do not say so: {string.Join(", ", unadvertised)}");

            Assert.Equal(["Feature: add-extension", "Feature: use-mobile-user-agent"], unadvertised);
        }

        [SkippableFact]
        public async Task IsQuotedString_RejectsTheAmbiguousCases()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            (string value, bool expected)[] cases =
            [
                ("\"x\"", true),
                ("'x'", false),
                ("\"x", false),
                ("x\"", false),
                // Length is checked against 1, not 2, so a pair of quotes with nothing between
                // them counts as quoted. Whether the parser then accepts an empty value is a
                // separate question; what this pins is that the quote check does not catch it.
                ("\"\"", true),
                ("\"", false),
                ("", false),
            ];

            foreach ((string value, bool expected) in cases)
            {
                bool actual = await page.EvaluateAsync<bool>(
                    """
                    value => isQuotedString(value)
                    """,
                    value
                );

                output.WriteLine($"'{value}' -> {actual}");

                Assert.True(expected == actual, $"isQuotedString('{value}') returned {actual}");
            }
        }
    }
}