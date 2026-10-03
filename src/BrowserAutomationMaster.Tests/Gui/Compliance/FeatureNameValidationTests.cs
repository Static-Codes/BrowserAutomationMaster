using BrowserAutomationMaster.Core.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// Feature-name validation, which the GUI depends on when it builds a script's contents. <br/>
    /// Source: Core/Parsing/Parser.cs (IsValidFileContents)
    /// </summary>
    /// <remarks>
    /// These tests need no pinned GUI tree, which is what makes them a usable gate: the fix they
    /// justified landed in the GUI, and this file is what would have caught it breaking. <br/>
    /// Only <c>WriteErrorAndReturnBool</c> paths are driven. It returns without exiting; the
    /// <c>WriteAndExit</c> paths (duplicate features, bad proxy format, a misplaced <c>visit</c>)
    /// terminate the process and are deliberately not reachable from in-process tests.
    /// </remarks>
    public class FeatureNameValidationTests(ITestOutputHelper output)
    {
        /// <summary>
        /// The exact line shape the GUI's script creator puts on the wire. <c>buildFeatureCommandText</c>
        /// emits the feature name JSON-quoted, <c>{"feature":"\"use-http-proxy\""}</c>, and
        /// <c>validateScriptContents</c> renders it as <c>feature "use-http-proxy" "arg"</c>.
        /// </summary>
        [Fact]
        public void GuiShapedFeatureLine_IsValid()
        {
            string[] lines = [
                "browser \"chrome\"",
                "feature \"use-http-proxy\" \"NULL:NULL@127.0.0.1:8080\"",
                "visit \"https://example.com\"",
            ];

            foreach (string line in lines)
            {
                output.WriteLine(line);
            }

            Assert.True(Parser.IsValidFileContents(lines));
        }

        /// <summary>
        /// The no-argument form the GUI emits for a feature declared with <c>{}</c>, such as
        /// <c>use-mobile-user-agent</c>.
        /// </summary>
        [Fact]
        public void GuiShapedFeatureLineWithNoArgument_IsValid()
        {
            string[] lines = [
                "browser \"chrome\"",
                "feature \"use-mobile-user-agent\"",
            ];

            foreach (string line in lines)
            {
                output.WriteLine(line);
            }

            Assert.True(Parser.IsValidFileContents(lines));
        }

        /// <summary>
        /// A feature name that merely contains a valid one as a substring. The guards at
        /// <c>Parser.cs:538</c> and <c>Parser.cs:863</c> test <c>line.Contains(arg)</c>, so a name
        /// containing a valid feature as a substring satisfies them without being one.
        /// </summary>
        /// <remarks>
        /// <c>browser</c> is in <c>otherFeatureArgs</c> but not in <c>featureArgs</c>, so
        /// <c>bogus-browser-thing</c> does not in fact match any feature and is correctly rejected.
        /// Which branch did the rejecting is observable from here: the <c>Parser.cs:863</c> guard
        /// returns through <c>WriteErrorAndReturnBool</c> without exiting, whereas the
        /// <c>HandleLineValidation</c> path at <c>Parser.cs:1006</c> accumulates into
        /// <c>invalidLines</c> and reaches <c>WriteAndExit</c>. This test passing at all is the
        /// evidence for which one ran.
        /// </remarks>
        [Fact]
        public void FeatureNameContainingAValidOneAsASubstring_IsRejected()
        {
            string[] lines = [
                "browser \"chrome\"",
                "feature \"bogus-browser-thing\"",
            ];

            foreach (string line in lines)
            {
                output.WriteLine(line);
            }

            Assert.False(Parser.IsValidFileContents(lines));
        }

        /// <summary>
        /// The control for the test above: a name containing no valid feature as a substring. Rejecting
        /// only this one would mean the substring name is accepted, so the pair is what pins the
        /// behaviour rather than an accident of the browser line above it.
        /// </summary>
        [Fact]
        public void FeatureNameContainingNoValidFeatureAsASubstring_IsRejected()
        {
            string[] lines = [
                "browser \"chrome\"",
                "feature \"bogus-thing\"",
            ];

            foreach (string line in lines)
            {
                output.WriteLine(line);
            }

            Assert.False(Parser.IsValidFileContents(lines));
        }

        /// <summary>
        /// Every feature BAMM accepts is accepted in the shape the GUI emits, not merely in some other
        /// spelling.
        /// </summary>
        /// <remarks>
        /// This is the whole reason the quoted form is what the GUI emits: a bare unquoted name and a
        /// quoted one take different paths through the parser, and only the second is what a user
        /// actually produces. Looping over the registry rather than listing names keeps it honest when
        /// <c>otherFeatureArgs</c> grows.
        /// </remarks>
        [Fact]
        public void EveryBammFeature_IsValidInTheFormTheGuiEmits()
        {
            // Proxy features excluded: their line carries a proxy string, and `feature "use-http-proxy"`
            // with no argument fails IsValidProxyFormat rather than answering this question. The proxy
            // form is asserted with an argument in GuiShapedFeatureLine_IsValid.
            foreach (string feature in Parser.otherFeatureArgs)
            {
                string[] lines = ["browser \"chrome\"", $"feature \"{feature}\""];

                output.WriteLine($"{feature}: {lines[1]}");

                Assert.True(Parser.IsValidFileContents(lines), $"feature \"{feature}\" was rejected in the quoted form the GUI emits.");
            }
        }
    }
}
