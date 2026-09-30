namespace BrowserAutomationMaster.Tests.ScriptExecution
{
    /// <summary>How a spawned <c>bamm compile</c> child process finished.</summary>
    public enum ScriptExecutionOutcome
    {
        /// <summary>The child exited with a zero exit code.</summary>
        Compiled,

        /// <summary>The child exited with a non-zero exit code.</summary>
        Failed,

        /// <summary>The child did not exit within the allotted budget and was killed.</summary>
        TimedOut
    }

    /// <summary>The captured result of a single <c>bamm compile</c> invocation.</summary>
    public readonly record struct ScriptExecutionResult(
        ScriptExecutionOutcome Outcome,
        int ExitCode,
        string StdOut,
        string StdErr)
    {
        /// <summary>The process start info that was used, included in failure messages for diagnosis.</summary>
        public string CommandLine { get; init; } = string.Empty;

        /// <summary>
        /// The compiled Python output produced by the child, captured before its temp AppData was deleted.
        /// </summary>
        public IReadOnlyList<string> CompiledFiles { get; init; } = [];
    }
}
