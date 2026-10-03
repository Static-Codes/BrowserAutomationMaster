namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// BAMM features the GUI does not expose, each with the reason it is not a parity failure.
    /// </summary>
    /// <remarks>
    /// A GUI command that BAMM cannot compile would be a real defect. A BAMM feature the GUI cannot
    /// express is not, and pretending otherwise by editing the GUI is a product decision rather than a
    /// bug fix. Naming each one here keeps the gap visible and keeps the parity tests honest: if a new
    /// gap appears, the test fails with the name in the message, and adding it to this list is a
    /// conscious edit someone has to justify.
    /// <para>
    /// Each exclusion is also asserted to equal itself by UnreachableMemberTests' sibling check, so
    /// adding an entry cannot be a silent diff: the reason text has to be filled in.
    /// </para>
    /// </remarks>
    public static class ParityExclusions
    {
        /// <summary>
        /// <c>feature "run-headless"</c> is implemented by BAMM — BAMConfig reads it off the feature
        /// line into <c>runHeadless</c> — and the GUI has no entry for it, so a user cannot express it
        /// through the script creator.
        /// </summary>
        /// <remarks>
        /// Tracked rather than fixed. Adding the GUI entry is a product decision: it changes what the
        /// GUI can produce, which is a different kind of change from making it agree with BAMM on
        /// what already exists. Closing this silently would lose the only record that the capability
        /// is unreachable.
        /// </remarks>
        public const string RUN_HEADLESS = "run-headless";

        /// <summary>BAMM features the GUI cannot express, with the reason each one is excluded.</summary>
        public static readonly IReadOnlyDictionary<string, string> Features = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [RUN_HEADLESS] =
                "BAMM implements it (BAMConfig.runHeadless reads the feature line) but the GUI has no entry for it. " +
                "A functional gap, tracked, not a contract break.",
        };

        /// <summary>
        /// Commands in <c>Parser.actionArgs</c> that <c>Transpiler.validCommands</c> does not list.
        /// </summary>
        /// <remarks>
        /// All three are consumed by the parser before the transpiler's command switch ever sees them,
        /// so the transpiler does not need to name them. The reverse direction is the one that matters:
        /// a name in <c>validCommands</c> that the parser does not know would be a command the
        /// transpiler accepts and the parser cannot execute.
        /// </remarks>
        public static readonly IReadOnlyDictionary<string, string> TranspilerCommands = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["add-headers"] = "Consumed by the parser before Transpiler's command switch; the switch never sees it.",
            ["start-javascript"] = "Consumed by the parser before Transpiler's command switch; the switch never sees it.",
            ["end-javascript"] = "Consumed by the parser before Transpiler's command switch; the switch never sees it.",
        };

        /// <summary>GUI commands with no BAMM line-command counterpart. Expected to stay empty unless justified.</summary>
        public static readonly IReadOnlyDictionary<string, string> GuiOnlyCommands = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["add-js-code"] =
                "Not a line command at all. The GUI's name for code placed inside a " +
                "start-javascript/end-javascript block: /export receives it as {\"add-to-js\": <base64>} and " +
                "writes the decoded line out as raw block content, which Parser accepts because the whole " +
                "block is treated as JavaScript. There is nothing for Parser.actionArgs to name.",
        };

        /// <summary>BAMM feature names the GUI does not expose, and why.</summary>
        public static IReadOnlyList<string> ExcludedFeatures => [.. Features.Keys];
    }
}
