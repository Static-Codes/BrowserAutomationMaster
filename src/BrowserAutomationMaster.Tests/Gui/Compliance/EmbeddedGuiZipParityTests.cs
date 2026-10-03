using System.IO.Compression;
using System.Reflection;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// The GUI BAMM actually ships, against the GUI the suite is pinned to. <br/>
    /// Source: BrowserAutomationMaster.csproj (GUI download), Core/GUI/BackendFunctions.cs (GetGuiVersion)
    /// </summary>
    /// <remarks>
    /// BAMM's csproj downloads <c>gui.zip</c> from <c>releases/latest/download</c> at build time and
    /// embeds it, so the GUI a user gets is whatever the newest BAMM-GUI release happens to contain —
    /// not the tree in this repository, and not necessarily the commit these tests are pinned to. <br/>
    /// That makes "the embedded GUI is older than the pinned GUI" a real, invisible condition: nothing
    /// fails, the version string is just quietly behind, and no test would notice without this one.
    /// <para>
    /// The expected failure is deliberate and load-bearing. The embedded archive is only refreshed when
    /// a new non-prerelease BAMM-GUI release exists, because <c>releases/latest/</c> skips prereleases
    /// and drafts. Until that release exists, this test is red — which is the honest signal.
    /// </para>
    /// </remarks>
    public class EmbeddedGuiZipParityTests(ITestOutputHelper output)
    {
        private readonly GuiSourceFixture fixture = GuiSourceFixture.Instance;

        /// <summary>
        /// The pinned GUI's version, read with the same regex BAMM uses at runtime.
        /// </summary>
        [Fact]
        public void ThePinnedFixture_DeclaresAUsableVersion()
        {
            output.WriteLine($"pinned GUI_VERSION {fixture.Version} ({fixture.Manifest.Tag})");

            Assert.Matches(@"^\d+(\.\d+){3}$", fixture.Version);
        }

        /// <summary>
        /// The GUI embedded in this build is the same version as the pinned fixture.
        /// </summary>
        [Fact]
        public void TheEmbeddedGui_MatchesThePinnedFixture()
        {
            string pinned = fixture.Version;
            string? embedded = GetEmbeddedGuiVersion();

            output.WriteLine($"pinned   {pinned} ({fixture.Manifest.Tag}, {fixture.Manifest.Sha})");
            output.WriteLine($"embedded {embedded ?? "<could not be read>"}");

            Assert.True(embedded is not null, "The embedded archive's version could not be read at all.");
            Assert.NotEqual("unknown", embedded);

            Assert.True(
                pinned == embedded,
                $"BAMM is embedding GUI {embedded}, but this suite is pinned to {pinned} " +
                $"({fixture.Manifest.Tag}). BAMM's csproj pulls gui.zip from releases/latest/download, which " +
                "skips prereleases and drafts, so a newer GUI only reaches a build once a proper release " +
                $"exists for it. Either publish the GUI release for {fixture.Manifest.Tag} and rebuild, or " +
                "re-pin the fixture to the version BAMM currently ships."
            );
        }

        /// <summary>
        /// The embedded archive carries a <c>version.js</c>, so <c>/gui_version</c> can answer at all.
        /// </summary>
        [Fact]
        public void TheEmbeddedArchive_CarriesAVersionScript()
        {
            using ZipArchive archive = OpenEmbeddedArchive();

            ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(candidate =>
                candidate.FullName.EndsWith("scripts/version.js", StringComparison.Ordinal));

            output.WriteLine(string.Join(Environment.NewLine, archive.Entries.Select(candidate => candidate.FullName)));

            Assert.True(
                entry is not null,
                "The embedded gui.zip has no scripts/version.js, so GetGuiVersion has nothing to read and " +
                "/gui_version answers \"unknown\". The GUI's release process has to include it."
            );
        }

        /// <summary>
        /// Every file the archive and the pinned tree share is byte-identical, and every file the GUI
        /// itself loads is in the archive.
        /// </summary>
        /// <remarks>
        /// Asserted over the intersection rather than as an equality of the two file sets, because the
        /// archive is built from the deployable subset of the repository and never contained
        /// <c>changelog.md</c> or <c>README.md</c>. Demanding equal sets would fail on packaging, not on
        /// drift. What matters is that a file BAMM ships and the suite reads is the same file in both
        /// places — otherwise the version check above could pass while the content behind it differed.
        /// </remarks>
        [Fact]
        public void TheEmbeddedArchive_ServesTheSameContentAsThePinnedTree()
        {
            using ZipArchive archive = OpenEmbeddedArchive();

            Dictionary<string, byte[]> embedded = [];

            foreach (ZipArchiveEntry entry in archive.Entries.Where(entry => entry.Length > 0))
            {
                using Stream content = entry.Open();
                using MemoryStream buffer = new();

                content.CopyTo(buffer);

                embedded[StripArchivePrefix(entry.FullName)] = buffer.ToArray();
            }

            List<string> mismatched = [];

            foreach (string relative in EnumeratePinnedFiles())
            {
                if (!embedded.TryGetValue(relative, out byte[]? shipped))
                {
                    continue;
                }

                if (!fixture.ReadBytes(relative).AsSpan().SequenceEqual(shipped))
                {
                    mismatched.Add(relative);
                }
            }

            output.WriteLine($"{embedded.Count} archive file(s); {mismatched.Count} content mismatch(es)");

            // The files the GUI is built from. Asserted present in the archive because a GUI that
            // shipped without index.html or create_script.js would be broken in a way no content
            // comparison would notice.
            string[] loadBearing =
            [
                "index.html",
                "inactive.html",
                "scripts/create/commands.js",
                "scripts/create/create_script.js",
                "scripts/sidebar.js",
                "scripts/responsive.js",
                "scripts/version.js",
            ];

            List<string> absent = [.. loadBearing.Where(file => !embedded.ContainsKey(file))];

            Assert.True(
                absent.Count == 0,
                $"The embedded gui.zip is missing {string.Join(", ", absent)}. The GUI cannot load without them."
            );

            Assert.True(
                mismatched.Count == 0,
                $"These files differ between the embedded gui.zip and the pinned tree: {string.Join(", ", mismatched)}. " +
                "The archive is not a build of the pinned commit."
            );
        }

        private static IEnumerable<string> EnumeratePinnedFiles()
        {
            return Directory
                .EnumerateFiles(GuiSourceFixture.Instance.Root, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(GuiSourceFixture.Instance.Root, path).Replace('\\', '/'));
        }

        /// <summary>
        /// The GUI version read out of this build's embedded archive, exactly as the runtime reads it.
        /// </summary>
        private static string? GetEmbeddedGuiVersion()
        {
            // The extracted copy is what the runtime falls back to, and this process has already
            // extracted the embedded archive during a Tier B run if one happened. Prefer the archive
            // itself so the answer does not depend on test ordering.
            using ZipArchive archive = OpenEmbeddedArchive();

            ZipArchiveEntry? entry = archive.Entries.FirstOrDefault(candidate =>
                candidate.FullName.EndsWith("scripts/version.js", StringComparison.Ordinal));

            if (entry is null)
            {
                return "unknown";
            }

            using StreamReader reader = new(entry.Open());
            Match match = RegexManager.GuiVersionRegex.Match(reader.ReadToEnd());

            return match.Success ? match.Groups[1].Value : "unknown";
        }

        private static ZipArchive OpenEmbeddedArchive()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "bamm.dll");

            Assert.True(
                File.Exists(path),
                $"No bamm.dll at '{path}'. The embedded archive lives in the main project's assembly, " +
                "which the test project references."
            );

            Stream stream = File.OpenRead(path);
            Assembly assembly = Assembly.LoadFrom(path);

            Stream resource = assembly
                .GetManifestResourceStream("BrowserAutomationMaster.Resources.gui.zip")
                ?? throw new InvalidOperationException(
                    $"bamm.dll has no embedded BrowserAutomationMaster.Resources.gui.zip. Resource names: " +
                    $"{string.Join(", ", assembly.GetManifestResourceNames())}."
                );

            stream.Dispose();

            return new ZipArchive(resource, ZipArchiveMode.Read, leaveOpen: false);
        }

        /// <summary>
        /// Archive members are prefixed with the directory they were built from, which is not part of the
        /// path as the GUI sees it.
        /// </summary>
        private static string StripArchivePrefix(string entryName)
        {
            int slash = entryName.IndexOf('/');

            return slash < 0 ? entryName : entryName[(slash + 1)..];
        }
    }
}
