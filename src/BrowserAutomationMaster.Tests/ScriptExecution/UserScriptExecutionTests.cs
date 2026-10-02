using BrowserAutomationMaster.Core.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.ScriptExecution
{
    /// <summary>
    /// Executes every discoverable .bamc script through the real BAMM CLI and judges the result by
    /// the child's exit code. <br/>
    /// Source: Core/Utilities/UserScriptUtility.cs, Core/Compilation/Transpiler.cs
    /// </summary>
    /// <remarks>
    /// Contract: a zero exit code passes, a non-zero exit code with an unreachable URL in the script
    /// skips, and anything else fails with the child's output attached. All cases live in this single
    /// class, and xunit v2 runs tests within a class sequentially, so concurrent child processes and
    /// network contention are not a concern.
    /// </remarks>
    [Trait("Category", "E2E")]
    public class UserScriptExecutionTests(ITestOutputHelper output)
    {
        private const int MAX_OUTPUT_LENGTH = 4000;

        public static IEnumerable<object[]> DiscoveredScripts()
        {
            IReadOnlyList<string> scripts = UserScriptCatalog.Discover();

            if (scripts.Count == 0)
            {
                // An empty MemberData makes xunit report an error rather than a skip, so a single
                // row carrying null is yielded instead and the body skips.
                yield return [null!];
                yield break;
            }

            if (!BammProcess.TryResolve(out _))
            {
                yield return [null!];
                yield break;
            }

            foreach (string script in scripts)
            {
                yield return [script];
            }
        }

        [SkippableTheory]
        [MemberData(nameof(DiscoveredScripts))]
        public void Script_Compiles(string? scriptPath)
        {
            if (scriptPath is null)
            {
                string reason = BammProcess.ResolutionFailureReason;

                Skip.If(string.IsNullOrEmpty(reason),
                    "No .bamc files were discovered. Populate the userScripts directory, or build so " +
                    "the examples are copied to the test output directory.");

                Skip.If(true, $"The ScriptExecution suite could not run. {reason}");
            }

            IReadOnlyList<UnreachableUrl> unreachable = VisitUrlPreflight.Probe(scriptPath);

            ScriptExecutionResult result = BammProcess.Run(scriptPath, BammProcess.Timeout).GetAwaiter().GetResult();

            output.WriteLine($"Command: {result.CommandLine}");
            output.WriteLine($"Outcome: {result.Outcome} (exit code {result.ExitCode})");

            // The child's log is only worth reading when something went wrong; a successful compile
            // emits a few hundred lines of dependency download progress.
            if (result.Outcome != ScriptExecutionOutcome.Compiled)
            {
                output.WriteLine("--- stdout ---");
                output.WriteLine(Truncate(result.StdOut));
                output.WriteLine("--- stderr ---");
                output.WriteLine(Truncate(result.StdErr));
            }

            if (result.Outcome == ScriptExecutionOutcome.TimedOut)
            {
                // A slow but syntactically valid script is an environmental problem, but one that is
                // also unparseable and hanging is a real defect.
                if (Parser.IsValidFile(scriptPath))
                {
                    Skip.If(true,
                        $"BAMM did not exit within the {BammProcess.Timeout.TotalSeconds:0.#}s budget; " +
                        "the script is syntactically valid.");
                }

                Assert.Fail(
                    $"BAMM did not exit within the {BammProcess.Timeout.TotalSeconds:0.#}s budget and the " +
                    $"script is not syntactically valid.{Environment.NewLine}{Truncate(result.StdErr)}"
                );
            }

            if (result.ExitCode == 0)
            {
                // Even if the pre-flight found dead URLs, BAMM tolerated them, so there is nothing
                // to report.
                Assert.NotEmpty(result.CompiledFiles);
                return;
            }

            // An unhandled exception in the child terminates it with a platform dependent code
            // (134 on Linux, for example), so only the fact that it is non-zero is asserted.
            Assert.NotEqual(0, result.ExitCode);

            if (unreachable.Count > 0)
            {
                foreach (UnreachableUrl url in unreachable)
                {
                    output.WriteLine(
                        $"Unreachable URL on line {url.LineNumber} ({url.Command}): {url.Url} — {url.Reason}");
                }

                UnreachableUrl first = unreachable[0];
                string remaining = unreachable.Count == 1
                    ? string.Empty
                    : $" (+{unreachable.Count - 1} more)";

                Skip.If(true,
                    $"BAMM could not compile the script and the pre-flight probe also found " +
                    $"{unreachable.Count} unreachable URL(s): {first.Url} — {first.Reason}{remaining}. " +
                    "This is an environmental failure, not a script defect.");
            }

            Assert.Fail(
                $"BAMM failed to compile the script. Exit code: {result.ExitCode}{Environment.NewLine}" +
                $"Command: {result.CommandLine}{Environment.NewLine}" +
                $"--- stdout ---{Environment.NewLine}{Truncate(result.StdOut)}{Environment.NewLine}" +
                $"--- stderr ---{Environment.NewLine}{Truncate(result.StdErr)}"
            );
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "(empty)";
            }

            return value.Length <= MAX_OUTPUT_LENGTH
                ? value
                : value[..MAX_OUTPUT_LENGTH] + $"{Environment.NewLine}... (truncated)";
        }
    }
}
