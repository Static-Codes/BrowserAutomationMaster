using BrowserAutomationMaster.Core.Parsing;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// The three command-text builders, and the JavaScript block sequence they exist for. <br/>
    /// Source: scripts/create/create_script.js (buildCommandText, buildStandardCommandText,
    /// buildFeatureCommandText, advanceAfterAddingCommand)
    /// </summary>
    /// <remarks>
    /// Every assertion here ends at <c>Parser.IsValidFileContents</c>. The GUI's job is to emit lines
    /// BAMM accepts, and checking the emitted string's shape would only prove it matches the shape the
    /// GUI happens to use today — which is the thing that has been wrong before.
    /// </remarks>
    [Trait("Category", "Browser")]
    [Collection(BrowserFixture.COLLECTION_NAME)]
    public class CommandTextTests(BrowserFixture browser, ITestOutputHelper output)
    {
        private static void RequireBrowser() => Skip.IfNot(BrowserFixture.IsAvailable, BrowserFixture.SkipReason ?? "No browser.");

        [SkippableFact]
        public async Task EveryNonFeatureCommand_EmitsALineTheParserAccepts()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            List<string> failures = [];

            foreach (string command in GuiSourceParser.Instance.CommandNames
                .Where(name => !name.StartsWith("Feature:", StringComparison.Ordinal))

                // Add-JS-Code's payload is {"add-to-js": <base64>}, not a command line, and the two
                // block markers are written as bare text by a special case in both the creator and
                // /export. Both are covered by the block sequence test below.
                .Where(name => name is not ("Add-JS-Code" or "Start-Javascript" or "End-Javascript")))
            {
                // The argument object is built inside the page rather than passed in. The builders do
                // Object.assign(payload, args), so a JSON string would be spread character by character
                // and every command would emit "0 {" — which is a marshalling bug in the test, not
                // something the GUI can do.
                string payload = await page.EvaluateAsync<string>(
                    """
                    name => {
                      const quoted = value => '"' + value + '"';
                      const samples = {
                        Browser: { browser: quoted('chrome') },
                        Visit: { url: quoted('https://example.com') },
                        'Wait-For-Seconds': { 'wait-for-seconds': '2' },
                        'Take-Screenshot': { path: quoted('shot.png') },
                        'Get-Text': { selector: quoted('#id') },
                      };
                      return buildCommandText(name, samples[name] ?? {});
                    }
                    """,
                    command
                );

                string? line = ReadFirstLine(payload);

                output.WriteLine($"{command}: {payload} -> {line ?? "(empty, skipped by validateScriptContents)"}");

                if (line is null)
                {
                    // No arguments were supplied for this sample, so the builder emitted an empty object
                    // and the GUI's own validation loop drops it. Nothing to check.
                    continue;
                }

                // Only the browser line around it. A script's command order is enforced separately, and
                // putting every command before a visit would fail on position rather than on the line
                // being well-formed, which is what this asserts.
                if (!Parser.IsValidFileContents([line]))
                {
                    failures.Add($"{command} emitted '{line}', which the parser rejected");
                }
            }

            Assert.True(
                failures.Count == 0,
                $"{failures.Count} command(s) emitted a line BAMM rejects:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}"
            );
        }

        /// <remarks>
        /// Fails for the four proxy features, and that is the finding rather than a broken assertion:
        /// see <see cref="EveryProxyFeature_EmitsALineTheParserRejects"/>. The no-argument features are
        /// swept here because those are the ones that must pass.
        /// </remarks>
        [SkippableFact]
        public async Task EveryArgumentlessFeature_EmitsALineTheParserAccepts()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            List<string> failures = [];

            foreach (string feature in GuiSourceParser.Instance.FeatureNames
                .Where(name => !Parser.proxyFeatureArgs.Contains(name)))
            {
                string payload = await page.EvaluateAsync<string>(
                    """
                    feature => {
                      // A proxy feature's line needs its proxy string; without one the parser rejects it
                      // on the proxy format rather than on the feature name, which is a different test.
                      const isProxy = feature.startsWith('use-');
                      const args = isProxy ? { 'proxy-string': '"USER:PASS@IP:PORT"' } : {};
                      return buildFeatureCommandText('Feature: ' + feature, args);
                    }
                    """,
                    feature
                );

                string? line = ReadFirstLine(payload);

                output.WriteLine($"{feature}: {payload} -> {line ?? "(empty)"}");

                if (line is null)
                {
                    continue;
                }

                if (!Parser.IsValidFileContents(["browser \"chrome\"", line]))
                {
                    failures.Add($"{feature} emitted '{line}', which the parser rejected");
                }
            }

            Assert.True(
                failures.Count == 0,
                $"{failures.Count} feature(s) emitted a line BAMM rejects:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}"
            );
        }

        [SkippableFact]
        public async Task TheJavascriptBlockSequence_IsReachableAndAccepted()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The sequence the Add-JS-Code fix made reachable. Before it, the entry was absent from
            // commandCollection, so advanceAfterAddingCommand set the select to a value no option had,
            // renderArguments(undefined) threw, and the whole flow was unreachable.
            // The block markers are stored and written as bare text, not as builder output: both
            // /export and the creator's own add path special-case them, and buildStandardCommandText
            // over an empty argument object produces {}.
            string[] lines = await page.EvaluateAsync<string[]>(
                """
                () => [
                  'start-javascript',
                  buildCommandText('Add-JS-Code', { 'javascript-code': 'document.title' }),
                  'end-javascript',
                ]
                """
            );

            foreach (string line in lines)
            {
                output.WriteLine(line);
            }

            // Add-JS-Code serialises as {"add-to-js": <base64>}, which /export decodes into the block as
            // raw content. That is not a line the parser sees, so it is decoded here rather than passed
            // through: the parser must be handed the decoded text.
            string block = string.Join('\n', [lines[0], DecodeAddToJs(lines[1]), lines[2]]);

            output.WriteLine($"as the parser sees it:{Environment.NewLine}{block}");

            Assert.True(
                Parser.IsValidFileContents(["browser \"chrome\"", .. block.Split('\n')]),
                "The Start-Javascript / Add-JS-Code / End-Javascript sequence is not accepted by the parser."
            );
        }

        /// <summary>
        /// Every proxy feature produces a line BAMM rejects, because its argument is discarded.
        /// </summary>
        /// <remarks>
        /// A defect in the shipped pairing, pinned rather than fixed. <c>buildFeatureCommandText</c> stores
        /// the feature name and its argument as two properties of one object —
        /// <c>{"feature": "\"use-http-proxy\"", "proxy-string": "\"USER:PASS@IP:PORT\""}</c> — and
        /// both consumers read only the first. <c>validateScriptContents</c> takes <c>keys[0]</c>, and
        /// <c>Export</c> takes <c>contentDict.First()</c>, so the proxy string never reaches either.
        ///
        /// The line that results is <c>feature "use-http-proxy"</c>, and the parser requires the proxy
        /// string: <c>IsValidProxyFormat</c> rejects the line and <c>/validate</c> answers
        /// <c>success: false</c>. So a user can add a proxy feature through every guard the creator
        /// applies — <c>isDuplicateFeature</c> and <c>isOtherProxyFeaturePresent</c> both accept it — and
        /// only discover at validation time that the script will not run. <br/>
        /// The fix belongs in whichever side owns the format, and both are in different repositories, so
        /// this test states the current behaviour exactly so the day it changes someone has to look.
        /// </remarks>
        [SkippableFact]
        public async Task EveryProxyFeature_EmitsALineTheParserRejects()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            List<string> rejected = [];

            foreach (string feature in GuiSourceParser.Instance.FeatureNames
                .Where(Parser.proxyFeatureArgs.Contains))
            {
                string payload = await page.EvaluateAsync<string>(
                    """
                    feature => buildFeatureCommandText(
                      'Feature: ' + feature,
                      { 'proxy-string': '"USER:PASS@IP:PORT"' }
                    )
                    """,
                    feature
                );

                string? line = ReadFirstLine(payload);

                output.WriteLine($"{feature}: {payload} -> {line ?? "(empty)"}");

                Assert.NotNull(line);

                Assert.False(
                    Parser.IsValidFileContents(["browser \"chrome\"", line]),
                    $"{feature} now emits '{line}', which the parser accepts. If that is the intended fix, " +
                    "this test and the note in ExportFormatTests about the dropped argument both need updating."
                );

                rejected.Add(feature);
            }

            Assert.Equal(["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"], rejected);
        }

        [SkippableFact]
        public async Task AddJsCode_RoundTripsThroughItsOwnPayload()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // Returned as two separate reads rather than a tuple: Playwright's argument converter has
            // no mapping for a C# value tuple, and the failure is an opaque converter error.
            string encoded = await page.EvaluateAsync<string>(
                """
                () => JSON.parse(
                  buildCommandText('Add-JS-Code', { 'javascript-code': 'document.title = "x";' })
                )['add-to-js']
                """
            );

            string decoded = await page.EvaluateAsync<string>(
                """
                () => {
                  const payload = buildCommandText('Add-JS-Code', { 'javascript-code': 'document.title = "x";' });
                  const bytes = Uint8Array.from(atob(JSON.parse(payload)['add-to-js']), c => c.charCodeAt(0));
                  return new TextDecoder().decode(bytes);
                }
                """
            );

            output.WriteLine($"encoded: {encoded}");
            output.WriteLine($"decoded: {decoded}");

            // The exact round trip BackendFunctions.Export performs: base64 in, UTF-8 out, written as
            // raw block content.
            Assert.Equal("document.title = \"x\";", decoded);
        }

        [SkippableFact]
        public async Task AddJsCode_SurvivesTheAdvanceAfterAddingCommand()
        {
            RequireBrowser();

            await using GuiTestPage page = await GuiTestPage.OpenAsync(browser);

            // The failure the fix removed, asserted through the flow that had it: advance sets the
            // select to the next command and renders its arguments, which is where renderArguments
            // received undefined and threw.
            string rendered = await page.EvaluateAsync<string>(
                """
                () => {
                  commandSelect.value = 'Add-JS-Code';
                  commandSelect.dispatchEvent(new Event('change', { bubbles: true }));

                  const form = document.getElementById('command-form');
                  return form ? form.innerHTML.length.toString() : 'no command-form';
                }
                """
            );

            output.WriteLine($"command form length: {rendered}");

            Assert.NotEqual("0", rendered);
        }

        /// <summary>
        /// The command line the GUI posts to /validate, reproduced exactly.
        /// </summary>
        /// <remarks>
        /// Mirrors <c>validateScriptContents</c>: parse the entry, take <c>keys[0]</c> as the command name
        /// and <c>parsedObj[keys[0]]</c> as its data, and render <c>name data</c>. Any further property
        /// in the object is never read — which is the defect
        /// <see cref="EveryProxyFeature_EmitsALineTheParserRejects"/> pins.
        /// </remarks>
        /// <returns>
        /// The line, or null when the payload is an empty object — which is what
        /// <c>validateScriptContents</c> does with it, via its <c>keys.length === 0</c> continue.
        /// </returns>
        private static string? ReadFirstLine(string payload)
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(payload);

            // JsonProperty is a struct, so FirstOrDefault returns a default rather than null and the
            // emptiness has to be tested on the enumerator instead.
            if (!document.RootElement.EnumerateObject().Any())
            {
                return null;
            }

            System.Text.Json.JsonProperty first = document.RootElement.EnumerateObject().First();

            return $"{first.Name} {first.Value.GetString()}";
        }

        /// <summary>
        /// Decodes the <c>add-to-js</c> payload the way /export does, to the text inside the block.
        /// </summary>
        private static string DecodeAddToJs(string payload)
        {
            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(payload);

            return System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(document.RootElement.GetProperty("add-to-js").GetString()!)
            );
        }
    }
}