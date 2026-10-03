namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// Members of <c>Core/GUI</c> that exist but have no caller.
    /// </summary>
    /// <remarks>
    /// "Full coverage of Core/GUI" can only mean full coverage of reachable code. Each member here is
    /// named so that "this line is never executed" has an explanation rather than looking like a gap,
    /// and UnreachableMemberTests scans the sources to prove the explanation still holds. If someone
    /// wires one of these up, the test fails and forces a decision: use it properly, or delete it.
    /// <para>
    /// Deleting them is a separate cleanup and deliberately not done here. These are the last four
    /// references to behaviour that is still correct, and removing them would be an unrelated change
    /// riding along with a test suite.
    /// </para>
    /// <para>
    /// <b>Coverage of this namespace needs two collectors.</b> Tier B drives the listener from
    /// <c>GuiServerHost</c>, a separate process, because <c>Errors.WriteAndExit</c> would take the test
    /// host down with it. <c>coverlet.collector</c> instruments only the test host and shadow-loads its
    /// rewritten assemblies rather than touching the files on disk, so the child loads the original
    /// <c>BrowserAutomationMaster.dll</c> and every Tier B assertion passes while the report shows all of
    /// <c>Core/GUI</c> as untested. <c>GuiServerProcess</c> therefore wraps the child's launch in
    /// <c>dotnet-coverage</c> when that tool is installed, and CI merges the two reports. Without it,
    /// a run's coverage describes the test host alone and this list cannot be checked against it.
    /// </para>
    /// <para>
    /// Line-level figures from that merge are a floor rather than an exact reading: cobertura lists an
    /// async body on both the outer class and its state machine, so a per-line number depends on how the
    /// two producers are combined. Which classes ran is reliable; individual lines are not. This list
    /// plus <see cref="BrowserAutomationMaster.Tests.Gui.Compliance.UnreachableMemberTests"/> is the
    /// authority for what is reachable.
    /// </para>
    /// </remarks>
    public static class UnreachableMembers
    {
        /// <summary>
        /// <c>BackendFunctions.Terminate</c>. The router inlines the same three statements instead of
        /// calling it: set <c>isRunning</c> false, write <c>{ "terminated": true }</c>, yield.
        /// </summary>
        public const string TERMINATE = "Terminate";

        /// <summary>
        /// <c>Server.StopExecution</c>. Its only caller is <see cref="TERMINATE"/>, which is itself
        /// unreachable, so this is dead by one more hop.
        /// </summary>
        public const string STOP_EXECUTION = "StopExecution";

        /// <summary>
        /// <c>Server.IsRunning</c>. No caller anywhere. The listener loop reads the <c>isRunning</c>
        /// field directly, so nothing outside Server.cs has ever asked for the answer as a method.
        /// </summary>
        public const string IS_RUNNING = "IsRunning";

        /// <summary>
        /// <c>Response.validResponse</c>. A prebuilt success response that is never read; each handler
        /// constructs its own.
        /// </summary>
        public const string VALID_RESPONSE = "validResponse";

        /// <summary>
        /// <c>BackendFunctions.Upload</c> is not unreachable — it is unreachable *and* marked
        /// <c>[Obsolete]</c>, and the router's <c>case "/upload"</c> is commented out. Named separately
        /// because the reason is different: this one was retired deliberately.
        /// </summary>
        public const string UPLOAD = "Upload";

        /// <summary>Every member, with the reason it is unreachable.</summary>
        public static readonly IReadOnlyDictionary<string, string> Members = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [TERMINATE] = "The router inlines the same logic rather than calling it (Server.cs, case \"/terminate\").",
            [STOP_EXECUTION] = "Only caller is Terminate, which is itself unreachable. Dead by one more hop.",
            [IS_RUNNING] = "No caller anywhere. The listener loop reads the isRunning field directly.",
            [VALID_RESPONSE] = "Never read. Every handler constructs its own response.",
            [UPLOAD] = "Marked [Obsolete] and its router case is commented out. Retired deliberately.",
        };

        /// <summary>The members that are unreachable purely because nothing calls them.</summary>
        public static IReadOnlyList<string> Uncallable => [.. Members.Keys];

        /// <summary>The members a coverage report should show as the only uncovered lines.</summary>
        public static IReadOnlyList<string> ExpectedUncovered => [.. Members.Keys];
    }
}
