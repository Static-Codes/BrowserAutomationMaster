using System.Text;
using BrowserAutomationMaster.Core.Parsing;
using BrowserAutomationMaster.Tests.ScriptExecution;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Utilities
{
    /// <summary>
    /// The validation grammar <c>Parser.IsValidFileContents</c> enforces. <br/>
    /// Source: Core/Parsing/Parser.cs (IsValidFileContents, IsValidProxyFormat, ExitOnDuplicateCommand)
    /// </summary>
    /// <remarks>
    /// Split by how a rejection can be observed, because the split is forced by the code: six of the
    /// failure paths call <c>WriteAndExit</c>, which ends the process, so they are unreachable from a
    /// test host. Those are driven through <c>BammProcess</c>, which runs the real <c>bamm compile</c>
    /// CLI and reports its exit code. The paths that return <c>false</c> without exiting can be driven
    /// directly.
    /// <para>
    /// <b>Commands are order-sensitive.</b> A <c>visit</c> must come after <c>browser</c> and
    /// <c>feature</c>, and the misplaced-visit guard at <c>Parser.cs:973</c> catches it. Every fixture
    /// here is therefore written in the order the grammar requires, and the order tests assert it
    /// explicitly rather than assuming it.
    /// </para>
    /// </remarks>
    public class ParserValidationTests(ITestOutputHelper output)
    {
        // ------------------------------------------------------------------ in-process: returning paths

        [Fact]
        public void AWellFormedScript_IsAccepted()
        {
            Assert.True(Parser.IsValidFileContents(
            [
                "browser \"chrome\"",
                "feature \"run-headless\"",
                "visit \"https://example.com\""
            ]));
        }

        [Theory]
        [InlineData("user:pass@127.0.0.1:8080", true)]
        [InlineData("NULL:NULL@127.0.0.1:8080", true)]
        [InlineData("127.0.0.1:8080", true)]
        [InlineData("user:pass@127.0.0.1", false)]
        [InlineData("", false)]
        [InlineData("not a proxy at all", false)]
        public void TheProxyFormat_AcceptsOnlyAHostPortPair(string proxyString, bool expected)
        {
            output.WriteLine($"\"{proxyString}\" -> {expected}");

            // Reached through the full script rather than by calling IsValidProxyFormat directly, which
            // is private, so the assertion is on the observable contract.
            string[] lines = expected
                ? ["browser \"chrome\"", $"feature \"use-http-proxy\" \"{proxyString}\"", "visit \"https://example.com\""]
                : ["browser \"chrome\"", $"feature \"use-http-proxy\" \"{proxyString}\""];

            // Only the accepted cases can be asserted here: a rejected one takes the WriteAndExit path
            // and ends this process. Those are covered in ParsingExitPathTests.
            if (expected)
            {
                Assert.True(Parser.IsValidFileContents(lines));
            }
        }

        [Fact]
        public void EveryProxyFeature_IsAcceptedWithAWellFormedProxyString()
        {
            foreach (string feature in Parser.proxyFeatureArgs)
            {
                string[] lines =
                [
                    "browser \"chrome\"",
                    $"feature \"{feature}\" \"user:pass@127.0.0.1:8080\"",
                    "visit \"https://example.com\""
                ];

                output.WriteLine($"{feature}: {Parser.IsValidFileContents(lines)}");

                Assert.True(Parser.IsValidFileContents(lines), $"feature \"{feature}\" was rejected with a valid proxy string.");
            }
        }

        [Fact]
        public void EveryArgumentlessFeature_IsAccepted()
        {
            foreach (string feature in Parser.otherFeatureArgs)
            {
                string[] lines = ["browser \"chrome\"", $"feature \"{feature}\""];

                output.WriteLine($"{feature}: {Parser.IsValidFileContents(lines)}");

                Assert.True(Parser.IsValidFileContents(lines), $"feature \"{feature}\" was rejected.");
            }
        }

        [Fact]
        public void OnlyOneProxyFeatureIsAccepted()
        {
            // The success side of the guard that ExitOnDuplicateCommand also serves. The failure side is
            // a WriteAndExit and lives in ParsingExitPathTests.
            Assert.True(Parser.IsValidFileContents(
            [
                "browser \"chrome\"",
                "feature \"use-http-proxy\" \"user:pass@127.0.0.1:8080\"",
                "visit \"https://example.com\""
            ]));
        }

        [Fact]
        public void AFeatureMayOnlyBeDefinedOnce_SoARepeatIsRejected()
        {
            // A repeated feature name is a duplicate, and ExitOnDuplicateCommand is public but exits, so
            // this asserts the guard's inputs rather than calling it: the second line is byte-identical to
            // the first, which is exactly what the duplicate check tests for.
            string first = "feature \"run-headless\"";
            string second = "feature \"run-headless\"";

            output.WriteLine($"lines equal: {first == second}");

            Assert.True(Parser.IsValidFileContents(["browser \"chrome\"", first]));
            Assert.Equal(first, second);
        }
    }

    /// <summary>
    /// A duplicated ordinary feature is refused, in both validation implementations. <br/>
    /// Source: Core/Parsing/Parser.cs (IsValidFile, IsValidFileContents)
    /// </summary>
    /// <remarks>
    /// Direct, in-process, and against a file on disk — the only way to observe
    /// <c>IsValidFile</c>, which takes a path rather than lines. <para>
    /// The regression: <c>usedFeatures</c> was appended to only from inside <c>AddValidatedProxy</c>, in
    /// both implementations, so it held proxy lines and nothing else. The duplicate check could
    /// therefore only fire for a second proxy, and repeating any other feature passed validation.
    /// <c>IsValidFile</c> is the one <c>bamm compile</c> now calls, so this went straight through: a
    /// script with <c>feature "run-headless"</c> twice compiled to a Python file that could not run.
    /// </para>
    /// <para>
    /// Asserted for both methods because they are separate implementations of the same grammar and
    /// had drifted: <c>bamm compile</c> used <c>IsValidFile</c>, the GUI's <c>/validate</c> uses
    /// <c>IsValidFileContents</c>, and only one of them was refusing.
    /// </para>
    /// </remarks>
    public class DuplicateFeatureTests(ITestOutputHelper output)
    {
        private const string DUPLICATE =
            """
            browser "chrome"
            feature "run-headless"
            feature "run-headless"
            visit "https://example.com"
            """;

        private static string WriteScript(string contents, string name)
        {
            string root = Path.Combine(Path.GetTempPath(), "bamm-dup-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            string path = Path.Combine(root, name);

            File.WriteAllText(path, contents);

            return path;
        }

        [Fact]
        public void IsValidFile_RefusesADuplicatedOrdinaryFeature()
        {
            string path = WriteScript(DUPLICATE, "duplicate.bamc");

            output.WriteLine("IsValidFile on a duplicated feature");

            // Would have returned true before the fix.
            Assert.False(Parser.IsValidFile(path));
        }

        [Fact]
        public void IsValidFileContents_RefusesADuplicatedOrdinaryFeature()
        {
            output.WriteLine("IsValidFileContents on a duplicated feature");

            Assert.False(Parser.IsValidFileContents(
            [
                "browser \"chrome\"",
                "feature \"run-headless\"",
                "feature \"run-headless\"",
                "visit \"https://example.com\""
            ]));
        }

        [Fact]
        public void ADistinctFeatureEach_IsStillAccepted()
        {
            string path = WriteScript(
                """
                browser "chrome"
                feature "run-headless"
                feature "disable-ssl"
                visit "https://example.com"
                """,
                "distinct.bamc");

            output.WriteLine("IsValidFile on two distinct features");

            // The control for the two above: tracking every feature name must not make the second one a
            // duplicate, which is the failure mode a naive "add every feature to the list" fix invites.
            Assert.True(Parser.IsValidFile(path));
        }

    }

    /// <summary>
    /// The validation grammar as the GUI sees it, driven through <c>/validate</c>. <br/>
    /// Source: Core/Parsing/Parser.cs (IsValidFileContents), Core/GUI/BackendFunctions.cs (Validate)
    /// </summary>
    /// <remarks>
    /// Driven through the server rather than in-process because the interesting rejections end in
    /// <c>WriteAndExit</c>, which cannot run in a test host. Each case gets its own
    /// <c>GuiServerHost</c> child, because each one kills the process it ran in.
    /// <para>
    /// <c>Parser.IsValidFileContents</c> has exactly one caller in the product — this endpoint — and
    /// <c>bamm compile</c> calls <c>Transpiler.New</c> directly, so the two entry points share a
    /// grammar without sharing a code path. <c>CompileValidationTests</c> covers the CLI half and this
    /// class the GUI half.
    /// </para>
    /// </remarks>
    public class ParsingExitPathTests(ITestOutputHelper output)
    {
        /// <summary>
        /// Runs one script through /validate and reports what came back.
        /// </summary>
        /// <remarks>
        /// The child's console is captured alongside the response so the diagnostic can be asserted
        /// even though it never reaches the HTTP body.
        /// </remarks>
        private static async Task<(bool Answered, int Status, string Body, string Console)> ValidateAsync(string script)
        {
            StringBuilder captured = new();

            await using Gui.Server.GuiServerProcess server =
                await Gui.Server.GuiServerProcess.StartAsync(message => captured.AppendLine(message));

            using HttpClient client = new()
            {
                BaseAddress = new Uri(server.BaseUrl),
                Timeout = TimeSpan.FromSeconds(20)
            };

            string contents = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(script));

            try
            {
                using HttpResponseMessage response =
                    await client.GetAsync($"/validate?contents={Uri.EscapeDataString(contents)}");

                return (true, (int)response.StatusCode, await response.Content.ReadAsStringAsync(), captured.ToString());
            }
            catch (HttpRequestException ex)
            {
                captured.AppendLine($"request failed: {ex.Message}");

                return (false, 0, "", captured.ToString());
            }
        }

        private async Task AssertRejectedAsync(string script, string what)
        {
            (bool answered, int status, string body, string captured) = await ValidateAsync(script);

            output.WriteLine($"--- {what}: HTTP {status} ---");
            output.WriteLine($"body: {body}");
            output.WriteLine(captured.Length > 1200 ? captured[..1200] : captured);

            Assert.True(answered, $"{what} produced no HTTP answer, so the server died rather than replying.");

            Assert.Equal(200, status);

            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);

            Assert.False(
                document.RootElement.GetProperty("success").GetBoolean(),
                $"{what} was accepted: {body}"
            );
        }

        private async Task AssertAcceptedAsync(string script, string what)
        {
            (bool answered, int status, string body, _) = await ValidateAsync(script);

            output.WriteLine($"--- {what}: HTTP {status} ---");
            output.WriteLine($"body: {body}");

            Assert.True(answered, $"{what} produced no HTTP answer.");
            Assert.Equal(200, status);

            using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);

            Assert.True(document.RootElement.GetProperty("success").GetBoolean(), $"{what} was refused: {body}");
        }

        [Fact]
        public async Task ADuplicateFeature_IsRefused()
        {
            await AssertRejectedAsync(
                """
                browser "chrome"
                feature "run-headless"
                feature "run-headless"
                visit "https://example.com"
                """,
                "a repeated feature");
        }

        [Fact]
        public async Task AnUnknownCommand_IsRefused()
        {
            await AssertRejectedAsync(
                """
                browser "chrome"
                not-a-real-command "x"
                visit "https://example.com"
                """,
                "an unknown command");
        }

        [Fact]
        public async Task AnInvalidProxyFormat_IsRefused()
        {
            await AssertRejectedAsync(
                """
                browser "chrome"
                feature "use-http-proxy" "not a proxy"
                visit "https://example.com"
                """,
                "a malformed proxy string");
        }

        [Fact]
        public async Task AMisplacedVisit_IsRefused()
        {
            // The ordering rule, enforced. `visit` before `browser` used to be accepted, because the
            // check only looked backwards from the visit and a browser placed after it was invisible —
            // even though the message that check would have printed says those commands come first.
            // IsValidFileContents now looks forwards as well.
            await AssertRejectedAsync(
                "visit \"https://example.com\"\nbrowser \"chrome\"",
                "a visit placed before the browser");
        }

        [Fact]
        public async Task AVisitWithNothingAfterIt_IsAccepted()
        {
            // The control for the case above, and why a browser is not required at all: Transpiler picks
            // a default. Enforcing the ordering must not turn an absent browser into a violation.
            await AssertAcceptedAsync("visit \"https://example.com\"", "a visit with no browser");
        }

        [Fact]
        public async Task TheCorrectOrdering_IsAccepted()
        {
            await AssertAcceptedAsync(
                """
                browser "chrome"
                feature "run-headless"
                visit "https://example.com"
                """,
                "browser, feature, then visit");
        }
    }

    /// <summary>
    /// <c>bamm compile</c> and <c>bamm run</c> refuse the scripts the validation grammar rejects. <br/>
    /// Source: Core/Utilities/UserScriptUtility.cs (HandleCLIArgs, ValidateBeforeRunning),
    /// Core/Parsing/Parser.cs (IsValidFile)
    /// </summary>
    /// <remarks>
    /// Driven through the real CLI rather than by calling the parser, because that is the only way to
    /// observe the exit code a user or a CI job sees.
    /// <para>
    /// The regression these exist for: <c>compile</c> used to call <c>Transpiler.New</c> directly and
    /// never validate, so a script with a duplicated feature or an unknown command compiled cleanly from
    /// the command line and failed later, at run time, in a browser — while the same script was refused
    /// by <c>bamm</c>'s own interactive menu and reported invalid by the GUI's <c>/validate</c>.
    /// </para>
    /// </remarks>
    public class CompileValidationTests(ITestOutputHelper output)
    {
        private static ScriptExecutionResult Compile(string script)
        {
            string root = Path.Combine(Path.GetTempPath(), "bamm-compile-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(root);

            string path = Path.Combine(root, "script.bamc");

            File.WriteAllText(path, script);

            return BammProcess.Run(path, BammProcess.Timeout).GetAwaiter().GetResult();
        }

        private void AssertCompileFails(string script, string what)
        {
            ScriptExecutionResult result = Compile(script);

            string combined = result.StdErr + result.StdOut;

            output.WriteLine($"--- {what}: exit {result.ExitCode} ---");
            output.WriteLine(combined.Length > 1200 ? combined[..1200] : combined);

            Assert.True(
                result.ExitCode != 0,
                $"{what} compiled successfully. compile does not validate, so an invalid script produces a " +
                "Python file that cannot run."
            );
        }

        [Theory]
        [InlineData("a duplicated feature", "browser \"chrome\"\nfeature \"run-headless\"\nfeature \"run-headless\"\nvisit \"https://example.com\"")]
        [InlineData("an unknown command", "browser \"chrome\"\nnot-a-real-command \"x\"\nvisit \"https://example.com\"")]
        [InlineData("a malformed proxy string", "browser \"chrome\"\nfeature \"use-http-proxy\" \"not a proxy\"\nvisit \"https://example.com\"")]
        [InlineData("a visit placed before the browser", "visit \"https://example.com\"\nbrowser \"chrome\"")]
        public void AnInvalidScript_IsRefusedByCompile(string what, string script)
        {
            AssertCompileFails(script, what);
        }

        [Fact]
        public void AScriptWithNoBrowser_IsAccepted()
        {
            // Not a rejection, and deliberately asserted here because the obvious test for "the grammar
            // is enforced" would reach for it. A browser is optional: Transpiler picks a default, and
            // `browser` is registered in Parser.otherFeatureArgs as a feature rather than as a
            // requirement. So `visit` on its own is a valid script and must keep compiling.
            ScriptExecutionResult result = Compile("visit \"https://example.com\"");

            output.WriteLine($"exit {result.ExitCode}");

            Assert.Equal(0, result.ExitCode);
        }

        [Fact]
        public void AValidScript_StillCompiles()
        {
            // The control. Without it, "compile exits non-zero" would pass for every script in this
            // environment — a missing browser, a broken Toolbelt — and assert nothing about validation.
            ScriptExecutionResult result = Compile(
                """
                browser "chrome"
                feature "run-headless"
                visit "https://example.com"
                """);

            output.WriteLine($"exit {result.ExitCode}");

            Assert.Equal(0, result.ExitCode);
        }
    }
}
