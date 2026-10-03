using System.Text.Json.Serialization;

namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// The build's record of which BAMM-GUI tree the tests are running against. <br/>
    /// Written by Gui/BammGui.props to <c>$(OutDir)Gui/fixture.json</c>.
    /// </summary>
    /// <remarks>
    /// A test failure is only useful if you can say what it ran against, so the SHA and the route
    /// it came from are part of the fixture rather than something a developer has to remember.
    /// </remarks>
    public sealed class GuiFixtureManifest
    {
        /// <summary>The commit in Static-Codes/BAMM-GUI that the tree was read at.</summary>
        [JsonPropertyName("sha")]
        public string Sha { get; set; } = "";

        /// <summary>The BAMM-GUI tag that commit carries, for humans reading a failure.</summary>
        [JsonPropertyName("tag")]
        public string Tag { get; set; } = "";

        /// <summary>Which of the props file's three resolution routes produced the tree.</summary>
        [JsonPropertyName("resolvedFrom")]
        public string ResolvedFrom { get; set; } = "";

        /// <summary>
        /// GUI_VERSION, scraped out of the tree by the props file's own regex.
        /// </summary>
        /// <remarks>
        /// Cross-checked against <see cref="BrowserAutomationMaster.Core.Common.RegexManager.GuiVersionRegex"/>
        /// when the manifest is loaded. Two independent derivations of the same value is the point:
        /// if the props file's regex and BAMM's runtime regex ever drift, the disagreement is caught
        /// here rather than showing up as a mysterious version mismatch much later.
        /// </remarks>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";
    }
}
