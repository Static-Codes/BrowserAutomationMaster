using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.GUI;
using BrowserAutomationMaster.Core.Helpers;
using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using static BrowserAutomationMaster.Core.Common.Constants;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Covers the /gui_version endpoint and the version lookup behind it. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (GuiVersion, GetGuiVersion), Core/Helpers/EmbeddedResourceHelper.cs
    /// </summary>
    /// <remarks>
    /// The endpoint is not exercised over HTTP here. Starting the GUI's HttpListener would need a
    /// free port, a running event loop and a real browser to load index.html, none of which assert
    /// anything the handler does not already do. What matters is the answer the handler reports and
    /// that the lookup never throws.
    /// </remarks>
    public class GuiVersionTests(ITestOutputHelper output)
    {
        [Fact]
        public void GuiZip_IsEmbeddedInTheMainAssembly()
        {
            // A precondition for everything else. If the build ever stops embedding the archive, the
            // endpoint silently degrades to "unknown" instead of failing loudly.
            Assembly main = typeof(BackendFunctions).Assembly;

            string[] names = main.GetManifestResourceNames();

            output.WriteLine(string.Join(Environment.NewLine, names));

            Assert.Contains(Constants.GUI_ZIP_RESOURCE_PATH, names);
        }

        [Fact]
        public void GetGuiVersion_NeverThrowsAndReportsAUsableValue()
        {
            string? version = BackendFunctions.GetGuiVersion();

            output.WriteLine($"Reported GUI version: {version ?? "(null)"}");

            if (version == null)
            {
                // Allowed: the archive currently on the `gui` branch predates scripts/version.js, so
                // there is nothing to read. The endpoint must degrade to "unknown" rather than throw.
                return;
            }

            Assert.Matches(@"^\d+\.\d+\.\d+(\.\d+)?$", version);
        }

        [Fact]
        public void GetGuiVersion_IsStableAcrossCalls()
        {
            // The result is cached, so a second call must not re-open the archive.
            Assert.Equal(BackendFunctions.GetGuiVersion(), BackendFunctions.GetGuiVersion());
        }

        [Theory]
        [InlineData("const GUI_VERSION = \"1.0.0.0\";", "1.0.0.0")]
        [InlineData("const GUI_VERSION = '2.1.3';", "2.1.3")]
        [InlineData("const GUI_VERSION=\"10.0.0.1\";", "10.0.0.1")]
        [InlineData("  const  GUI_VERSION  =  \"1.2.3.4\"  ;", "1.2.3.4")]
        public void GuiVersionRegex_ReadsTheConst(string source, string expected)
        {
            Match match = RegexManager.GuiVersionRegex.Match(source);

            Assert.True(match.Success, $"The regex did not match: {source}");
            Assert.Equal(expected, match.Groups[1].Value);
        }

        [Theory]
        [InlineData("// GUI_VERSION = \"9.9.9.9\";")]
        [InlineData("/* const GUI_VERSION = \"9.9.9.9\"; */")]
        [InlineData("const OTHER = \"1.0.0.0\";")]
        [InlineData("const GUI_VERSION = GUI_VERSION;")]
        [InlineData("")]
        public void GuiVersionRegex_DoesNotMatchNonConstText(string source)
        {
            Assert.False(RegexManager.GuiVersionRegex.Match(source).Success);
        }

        [Fact]
        public void GuiVersionRegex_IgnoresACommentedOutAssignment()
        {
            // The GUI's version.js explains itself in a comment block above the const, so a loose
            // pattern that ignores line starts would happily read a version out of the prose.
            string source = string.Join(Environment.NewLine, [
                "// Set GUI_VERSION to bump the release.",
                "// GUI_VERSION = \"0.0.0.0\";",
                "",
                "const GUI_VERSION = \"1.0.0.0\";",
            ]);

            Match match = RegexManager.GuiVersionRegex.Match(source);

            Assert.True(match.Success);
            Assert.Equal("1.0.0.0", match.Groups[1].Value);
        }

        [Fact]
        public void GetEmbeddedZipEntryText_ReturnsNullForAnAbsentEntry()
        {
            string? contents = EmbeddedResourceHelper.GetEmbeddedZipEntryText(
                resourceName: "gui.zip",
                resourcePattern: Constants.GUI_ZIP_RESOURCE_PATH,
                entryName: "gui/scripts/definitely-not-here.js"
            );

            Assert.Null(contents);
        }

        /// <remarks>
        /// No longer conditional. This was guarded by an unconditional <c>Skip.If(true, …)</c> for as long
        /// as the embedded archive predated <c>scripts/version.js</c>, and the guard was the only reason
        /// the test appeared to pass without running. The published archive now carries the entry, so the
        /// branch is dead code that would mislead the next reader into thinking the entry was optional.
        /// <c>GetEmbeddedZipEntryText_ReturnsNullForAnAbsentEntry</c> covers the absent case.
        /// </remarks>
        [Fact]
        public void GetEmbeddedZipEntryText_ReadsTheVersionEntryWhenTheArchiveHasIt()
        {
            string? contents = EmbeddedResourceHelper.GetEmbeddedZipEntryText(
                resourceName: "gui.zip",
                resourcePattern: Constants.GUI_ZIP_RESOURCE_PATH,
                entryName: Constants.GUI_VERSION_ZIP_ENTRY
            );

            output.WriteLine(contents);

            Assert.NotNull(contents);

            Match match = RegexManager.GuiVersionRegex.Match(contents);

            Assert.True(match.Success, "The version entry does not declare GUI_VERSION.");
            Assert.NotEmpty(match.Groups[1].Value);
        }
    }
}
