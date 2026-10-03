using BrowserAutomationMaster.Core.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// The pinned BAMM-GUI tree, materialised into the test output directory by Gui/BammGui.props. <br/>
    /// Root: $(OutDir)Gui/source
    /// </summary>
    /// <remarks>
    /// Files are read once and memoised. The tree is immutable for the duration of a run — it came
    /// out of <c>git archive</c> at a pinned commit — so re-reading it per assertion would be pure
    /// waste, and the static server in Tier C reads the same bytes over and over.
    /// <para>
    /// This type never skips. If the tree is missing, the build already failed at the props target,
    /// so by the time a test runs the tree is there; a missing file at this point is a real defect in
    /// the fixture layout and throws with the path in the message rather than returning null for a
    /// caller to quietly accept.
    /// </para>
    /// </remarks>
    public sealed class GuiSourceFixture
    {
        private const string OUTPUT_RELATIVE_PATH = "Gui";

        private static readonly Lazy<GuiSourceFixture> shared = new(() => new GuiSourceFixture());

        private readonly Dictionary<string, byte[]> contents = new(StringComparer.Ordinal);

        /// <summary>The single instance. The tree is read-only, so sharing it is safe.</summary>
        public static GuiSourceFixture Instance => shared.Value;

        private GuiSourceFixture()
        {
            Root = Path.Combine(AppContext.BaseDirectory, OUTPUT_RELATIVE_PATH, "source");
            ManifestPath = Path.Combine(AppContext.BaseDirectory, OUTPUT_RELATIVE_PATH, "fixture.json");

            Manifest = LoadManifest();
        }

        /// <summary>Absolute path of the pinned tree's root.</summary>
        public string Root { get; }

        /// <summary>Absolute path of the build-written manifest.</summary>
        public string ManifestPath { get; }

        /// <summary>The build's record of this fixture.</summary>
        public GuiFixtureManifest Manifest { get; }

        /// <summary>GUI_VERSION as declared by the pinned tree.</summary>
        public string Version => Manifest.Version;

        /// <summary>Resolves a tree-relative path to an absolute one. Named FullPath rather than Path so the
        /// class's own members can still say Path.Combine.</summary>
        public string FullPath(string relativePath)
            => System.IO.Path.Combine(Root, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

        /// <summary>Whether the tree has the given file. Used to assert a removal really happened.</summary>
        public bool Exists(string relativePath) => File.Exists(FullPath(relativePath));

        /// <summary>The raw bytes of a tree file, read once and memoised.</summary>
        public byte[] ReadBytes(string relativePath)
        {
            string absolute = FullPath(relativePath);

            lock (contents)
            {
                if (contents.TryGetValue(absolute, out byte[]? cached))
                {
                    return cached;
                }

                if (!File.Exists(absolute))
                {
                    throw new FileNotFoundException(
                        $"The pinned BAMM-GUI tree at {Root} has no '{relativePath}'. The fixture is incomplete; " +
                        "Gui/BammGui.props should have failed the build rather than producing this.",
                        absolute
                    );
                }

                byte[] bytes = File.ReadAllBytes(absolute);
                contents[absolute] = bytes;

                return bytes;
            }
        }

        /// <summary>A tree file as UTF-8 text.</summary>
        public string ReadText(string relativePath) => Encoding.UTF8.GetString(ReadBytes(relativePath));

        /// <summary>Whether the tree has a directory at the given relative path.</summary>
        public bool DirectoryExists(string relativePath) => Directory.Exists(FullPath(relativePath));

        private GuiFixtureManifest LoadManifest()
        {
            if (!File.Exists(ManifestPath))
            {
                throw new FileNotFoundException(
                    $"No BAMM-GUI fixture manifest at {ManifestPath}. Gui/BammGui.props writes it during the build, " +
                    "so this means the props file was not imported by BrowserAutomationMaster.Tests.csproj.",
                    ManifestPath
                );
            }

            GuiFixtureManifest manifest =
                JsonSerializer.Deserialize<GuiFixtureManifest>(File.ReadAllText(ManifestPath))
                ?? throw new InvalidOperationException($"The fixture manifest at {ManifestPath} is empty or malformed.");

            if (manifest.Version == "")
            {
                throw new InvalidOperationException($"The fixture manifest at {ManifestPath} records no GUI_VERSION.");
            }

            // The props file scrapes the version with its own regex because MSBuild property functions
            // cannot reach Match.Groups[1].Value. Re-derive it here with the regex BAMM itself uses at
            // runtime and insist the two agree.
            string source = ReadText("scripts/version.js");
            Match match = RegexManager.GuiVersionRegex.Match(source);

            if (!match.Success)
            {
                throw new InvalidOperationException(
                    $"scripts/version.js in the pinned tree declares no GUI_VERSION. The props file recorded " +
                    $"'{manifest.Version}', which it could not have read from a file with no assignment."
                );
            }

            string rederived = match.Groups[1].Value;

            if (rederived != manifest.Version)
            {
                throw new InvalidOperationException(
                    $"GUI_VERSION disagreement. The build's regex read '{manifest.Version}' from " +
                    $"{FullPath("scripts/version.js")}, but RegexManager.GuiVersionRegex reads '{rederived}'. " +
                    "The two regexes are meant to agree; one of them has drifted."
                );
            }

            return manifest;
        }
    }
}
