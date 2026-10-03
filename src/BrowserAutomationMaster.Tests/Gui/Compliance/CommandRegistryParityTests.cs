using BrowserAutomationMaster.Core.Compilation;
using BrowserAutomationMaster.Core.Parsing;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// The four axes along which the GUI's commandCollection and BAMM's parsers have to agree. <br/>
    /// Source: scripts/create/commands.js, Core/Parsing/Parser.cs, Core/Compilation/Transpiler.cs
    /// </summary>
    /// <remarks>
    /// Each axis catches a different class of breakage, so they are asserted separately rather than
    /// as one combined set: <br/>
    /// action names — a GUI command BAMM cannot execute is a dead button. <br/>
    /// feature names — a feature the GUI offers but the parser rejects fails at validation time, with
    /// the reason on a console the user cannot see. <br/>
    /// feature arguments — an argument the GUI offers that BAMM cannot accept fails the same way. <br/>
    /// argument options — a radio offering a value the parser does not recognise fails on that value
    /// alone, which is the hardest of the four to diagnose. <br/>
    /// Failures name the command on both sides rather than just the set difference, because "these two
    /// sets differ" is not actionable on its own.
    /// </remarks>
    public class CommandRegistryParityTests(ITestOutputHelper output)
    {
        private readonly GuiSourceParser gui = GuiSourceParser.Instance;

        /// <summary>
        /// Every GUI action command, in BAMM's own hyphenated spelling, has a counterpart in
        /// <c>Parser.actionArgs</c>.
        /// </summary>
        [Fact]
        public void EveryGuiActionCommand_IsExecutableByBamm()
        {
            // "browser" is registered in Parser.otherFeatureArgs, not actionArgs, but it is a line
            // token in its own right, so it is compared against the union rather than actionArgs alone.
            IReadOnlyList<string> guiCommands =
            [
                .. gui.ActionCommands
                    .Where(command => command.Name != "Browser")
                    .Select(command => command.ScriptCommandName)
                    .Where(name => !ParityExclusions.GuiOnlyCommands.ContainsKey(name))
            ];

            output.WriteLine($"GUI action commands ({guiCommands.Count}): {string.Join(", ", guiCommands)}");
            output.WriteLine($"Parser.actionArgs ({Parser.actionArgs.Length}): {string.Join(", ", Parser.actionArgs)}");

            Report(
                "a GUI command BAMM cannot execute",
                guiCommands.Except(Parser.actionArgs, StringComparer.Ordinal).ToList(),
                guiCommands.Intersect(Parser.actionArgs, StringComparer.Ordinal).ToList()
            );
        }

        /// <summary>
        /// Every feature the GUI offers is one <c>Parser.featureArgs</c> holds.
        /// </summary>
        [Fact]
        public void EveryGuiFeature_IsAValidBammFeature()
        {
            IReadOnlyList<string> guiFeatures = [.. gui.FeatureNames];

            output.WriteLine($"GUI features ({guiFeatures.Count}): {string.Join(", ", guiFeatures)}");
            output.WriteLine($"Parser.featureArgs ({Parser.featureArgs.Length}): {string.Join(", ", Parser.featureArgs)}");

            // Not Assert.Equal on the two lists: BAMM implements features the GUI cannot express, and
            // ParityExclusions names the one that is a tracked gap. The direction that must be empty is
            // GUI minus BAMM.
            Report(
                "a feature the GUI offers that the parser rejects",
                guiFeatures.Except(Parser.featureArgs, StringComparer.Ordinal).ToList(),
                guiFeatures
            );
        }

        /// <summary>
        /// Every feature argument the GUI offers is one BAMM's feature-line grammar accepts.
        /// </summary>
        /// <remarks>
        /// Checked structurally rather than by running <c>IsValidFileContents</c>, because the
        /// malformed cases call <c>WriteAndExit</c> and would take the test host down. The rule being
        /// asserted is the grammar itself: a proxy feature line is <c>feature "name" "proxy"</c> and
        /// takes exactly one positional argument; every other feature line is
        /// <c>feature "name"</c> plus at most one free-text argument, which becomes the rest of the
        /// line. A GUI entry with two arguments, or with a second named argument, could only produce a
        /// line the parser would reject at compile time.
        /// </remarks>
        [Fact]
        public void EveryGuiFeatureArgument_FitsBammSFeatureLineGrammar()
        {
            List<string> offered = [];
            List<string> malformed = [];

            foreach (GuiCommand command in gui.Commands.Where(command => command.IsFeature))
            {
                bool isProxy = Parser.proxyFeatureArgs.Contains(command.FeatureName);

                foreach (string argument in command.ArgumentNames)
                {
                    offered.Add($"{command.FeatureName}/{argument}");

                    if (isProxy && command.ArgumentNames.Count != 1)
                    {
                        malformed.Add($"{command.FeatureName} declares {command.ArgumentNames.Count} arguments; a proxy feature takes exactly one");
                    }
                }

                if (!isProxy && command.ArgumentNames.Count > 1)
                {
                    malformed.Add($"{command.FeatureName} declares {command.ArgumentNames.Count} arguments; a non-proxy feature takes at most one free-text argument");
                }
            }

            output.WriteLine($"GUI feature arguments ({offered.Count}): {string.Join(", ", offered)}");
            output.WriteLine($"Proxy features: {string.Join(", ", Parser.proxyFeatureArgs)}");

            Assert.True(
                malformed.Count == 0,
                $"{string.Join("; ", malformed)}."
            );
        }

        /// <summary>
        /// Every value a GUI radio offers is one the parser recognises for that argument.
        /// </summary>
        [Fact]
        public void EveryGuiArgumentOption_IsAValueBammAccepts()
        {
            List<string> offered = [];
            List<string> unrecognised = [];

            foreach (GuiCommand command in gui.Commands)
            {
                foreach ((string argument, IReadOnlyList<string> options) in command.ArgumentOptions)
                {
                    if (options.Count == 0)
                    {
                        // A free-text argument. There is nothing to enumerate, and the plan's rule
                        // applies instead: only commands with no options get skipped silently.
                        continue;
                    }

                    foreach (string option in options)
                    {
                        offered.Add($"{command.ScriptCommandName}/{argument}/{option}");

                        if (!IsKnownOption(command, argument, option))
                        {
                            unrecognised.Add($"{command.ScriptCommandName}/{argument}/{option}");
                        }
                    }
                }
            }

            output.WriteLine($"GUI argument options ({offered.Count}): {string.Join(", ", offered)}");

            Report("an argument option the GUI offers that BAMM would reject", unrecognised, offered);
        }

        /// <summary>
        /// Every command the transpiler claims to handle is one the parser can produce.
        /// </summary>
        /// <remarks>
        /// This is the reverse of the axes above, and the direction that matters most: a name in
        /// <c>validCommands</c> that the parser does not know is a command the transpiler would compile
        /// and the parser could never run. <c>Transpiler.validCommands</c> is <c>internal</c>, reachable
        /// because the main project has an <c>InternalsVisibleTo</c> for this assembly.
        /// </remarks>
        [Fact]
        public void EveryTranspilerCommand_IsOneTheParserCanProduce()
        {
            output.WriteLine($"validCommands ({Transpiler.validCommands.Length}): {string.Join(", ", Transpiler.validCommands)}");

            // "feature" is the line token every Parser.featureArgs name is written with, so it is a
            // token the parser produces even though it is not itself a feature name.
            IReadOnlyList<string> reachable = [.. Parser.actionArgs, .. Parser.featureArgs, "feature"];

            Report(
                "a command the transpiler compiles but no parser produces",
                [.. Transpiler.validCommands.Except(reachable, StringComparer.Ordinal)
                    .Except(ParityExclusions.TranspilerCommands.Keys, StringComparer.Ordinal)],
                [.. Transpiler.validCommands]
            );
        }

        /// <summary>
        /// The GUI offers nothing BAMM does not know, in any spelling.
        /// </summary>
        [Fact]
        public void NoGuiCommand_IsUnknownToBamm()
        {
            IReadOnlyList<string> known =
            [
                .. Parser.actionArgs,
                .. Parser.featureArgs,
                .. Parser.browserArgs
            ];

            // Only non-feature commands. A "Feature: x" entry's name is not the token on the wire —
            // the token is "feature" and the name rides in a quoted argument — so comparing names here
            // would report every feature as unknown. EveryGuiFeature_IsAValidBammFeature covers that
            // direction instead.
            List<string> unknown = [.. gui.Commands
                .Where(command => !command.IsFeature)
                .Select(command => command.ScriptCommandName)
                .Where(name => !known.Contains(name, StringComparer.Ordinal))
                .Where(name => !ParityExclusions.GuiOnlyCommands.ContainsKey(name))];

            output.WriteLine(string.Join(Environment.NewLine, unknown));

            Assert.True(
                unknown.Count == 0,
                $"The GUI offers {string.Join(", ", unknown)}, which appears in none of Parser.actionArgs, " +
                "featureArgs or browserArgs. Adding one to ParityExclusions.GuiOnlyCommands is only correct " +
                "if the mismatch is expected rather than a regression."
            );
        }

        /// <summary>
        /// Reports a set difference as an assertion failure, naming what is missing and what was compared.
        /// </summary>
        private void Report(string problem, List<string> offenders, IReadOnlyList<string> compared)
        {
            Assert.True(
                offenders.Count == 0,
                $"Found {offenders.Count} instance(s) of {problem}: {string.Join(", ", offenders)}. " +
                $"Compared against: {string.Join(", ", compared)}. " +
                "Either the GUI changed without BAMM, or BAMM changed without the GUI."
            );
        }

        private static bool IsKnownOption(GuiCommand command, string argument, string option)
        {
            if (command.ScriptCommandName == "browser")
            {
                return Parser.browserArgs.Contains(option, StringComparer.Ordinal);
            }

            // No other command in the registry offers an enumerable option list, so anything else that
            // does is new and needs deciding on rather than assuming.
            return false;
        }
    }
}
