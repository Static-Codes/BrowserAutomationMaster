using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Compilation;
using BrowserAutomationMaster.Core.Parsing;
using Xunit;
using Xunit.Abstractions;
using static BrowserAutomationMaster.Core.Common.RegexManager;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Covers the command and comment handling of the compilation pass. <br/>
    /// Source: Core/Compilation/Transpiler.cs, Core/Parsing/Parser.cs
    /// </summary>
    /// <remarks>
    /// These are the defects found by the ScriptExecution suite. All of them share one root cause:
    /// the compilation pass re-implements parsing rather than calling the parser, so the two drift
    /// apart and a script the parser rejects still compiles.
    /// </remarks>
    public class CompilationValidationTests(ITestOutputHelper output)
    {
        [Fact]
        public void ValidCommands_MatchTheParsersCommandList()
        {
            // Transpiler.validCommands exists because the transpiler's switch cannot detect unknown
            // commands: most of its cases are guarded by a `when` clause matching only on failure,
            // so a successful command falls through and is indistinguishable from an unrecognised
            // one. The list is therefore hand-maintained, and this test is what stops it drifting.
            string[] transpilerCommands = Transpiler.validCommands;

            string[] parserCommands =
            [
                "click", "get-text", "save-as-html", "save-as-html-exp", "select-element",
                "take-screenshot", "visit", "add-cookie", "add-header", "click-at-position",
                "click-exp", "close-current-tab", "fill-text", "fill-text-exp", "open-new-tab",
                "select-option", "set-custom-useragent", "wait-for-seconds", "browser", "feature"
            ];

            output.WriteLine("Only in transpiler: " + string.Join(", ", transpilerCommands.Except(parserCommands)));
            output.WriteLine("Only in parser: " + string.Join(", ", parserCommands.Except(transpilerCommands)));

            Assert.Equal(parserCommands.OrderBy(c => c), transpilerCommands.OrderBy(c => c));
        }

        [SkippableFact]
        public void EveryExampleScript_PassesTheParser()
        {
            // no-ssl-example.bamc was the only example declaring 'feature' commands, and it declared
            // an invalid one after a 'visit', so the multi-feature and ordering rules it exercises
            // were the only ones with no example coverage at all.
            string examplesDirectory = Path.Combine(AppContext.BaseDirectory, "userScripts");

            if (!Directory.Exists(examplesDirectory))
            {
                Skip.If(true, "The examples were not copied to the test output directory.");
            }

            string[] examples = Directory.GetFiles(examplesDirectory, "*.bamc", SearchOption.AllDirectories);

            output.WriteLine($"Checking {examples.Length} example script(s).");

            List<string> rejected = [];

            foreach (string example in examples)
            {
                int before = ConsoleCapture.Captured.Length;

                if (!Parser.IsValidFile(example))
                {
                    rejected.Add($"{Path.GetFileName(example)}: {ConsoleCapture.Captured[before..].Trim()}");
                }
            }

            output.WriteLine(string.Join(Environment.NewLine, rejected));

            Assert.Empty(rejected);
        }

        [Theory]
        [InlineData("browser \"chrome\"\nfeature \"disable-ssl\"\nfeature \"disable-pycache\"\nvisit \"https://example.com/\"\n")]
        [InlineData("browser \"chrome\"\nfeature \"disable-pycache\"\nfeature \"disable-ssl\"\nfeature \"run-headless\"\nvisit \"https://example.com/\"\n")]
        public void MultipleFeatureCommands_AreAccepted(string content)
        {
            // The first 'feature' line used to close the feature block, making a second one
            // "misplaced", so a script could declare at most one feature.
            Assert.True(IsValid(content), ConsoleCapture.Captured);
        }

        [Fact]
        public void FeatureAfterAnotherCommand_IsStillRejected()
        {
            // Guard against over-correcting: the ordering rule itself must survive.
            string content = "browser \"chrome\"\nvisit \"https://example.com/\"\nfeature \"disable-ssl\"\n";

            Assert.False(IsValid(content));
        }

        [Theory]
        [InlineData("// a short note")]
        [InlineData("// a longer note with punctuation, e.g. firefox, chrome")]
        [InlineData("//")]
        public void TrailingComments_AreAcceptedOnCommandLines(string comment)
        {
            // HandleCompilation and GetDesiredUrls both used to split the raw line, so a trailing
            // comment broke the token count and the line was reported as invalid syntax, or the
            // visit command went unrecognised. Both now strip the comment first.
            string content =
                $"browser \"chrome\" {comment}\n" +
                $"visit \"https://example.com/\" {comment}\n" +
                $"wait-for-seconds 1 {comment}\n";

            Assert.True(IsValid(content), ConsoleCapture.Captured);
        }

        [Fact]
        public void TrailingComment_DoesNotHideTheVisitCommand()
        {
            // The compile path collects visit targets with the same 2-token rule, so a comment there
            // made the script report that it contained no 'visit' commands at all.
            string content =
                "browser \"chrome\"\n" +
                "visit \"https://example.com/\" // this is a comment\n";

            Assert.True(IsValid(content), ConsoleCapture.Captured);
        }

        private static bool IsValid(string content)
        {
            string path = Path.Combine(Path.GetTempPath(), $"bamm-parse-{Guid.NewGuid():N}.bamc");

            try
            {
                File.WriteAllText(path, content);
                return Parser.IsValidFile(path);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
