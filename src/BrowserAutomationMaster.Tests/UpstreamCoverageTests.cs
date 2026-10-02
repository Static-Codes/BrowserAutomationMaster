using BrowserAutomationMaster.Core.Helpers;
using WhichDistroSharp;
using Xunit;
using Distro = BrowserAutomationMaster.Core.Types.Linux.Distro;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Ensures the distributions BAMM has no support entry for are listed in the README. <br/>
    /// Source: Core/SystemInfo/OS/Unix/Linux/Distros.cs
    /// </summary>
    public class UpstreamCoverageTests
    {
        // WhichDistroSharp knows 110 distributions and the 15 entries in Distros.cs claim 17 of them. 
        // The rest resolve to null, so DistroManager asks the user to pick a base on every run. 
        // DistroMappingTests checks the claimed ones resolve and that none is claimed twice.
        // Nothing there notices a distribution being left unclaimed.
        // The list lives in the gaps document for easy access. 
        // The gaps document also details the process of adding a distribution involves.

        // Relative to the repository root, resolved by walking up from the test output directory.
        private const string GapsDocument = "sections/known-distribution-gaps.md";

        private const string ListStartMarker = "<!-- BEGIN UNCLAIMED DISTROS -->";
        private const string ListEndMarker = "<!-- END UNCLAIMED DISTROS -->";

        /// <summary> Ensures every upstream distro is claimed by an entry or listed as unclaimed. </summary>
        [Fact]
        public void EveryUpstreamDistroIsClaimedOrDeliberatelyUnclaimed()
        {
            var unclaimed = UnclaimedUpstreamDistros();
            var listed = ListedUnclaimedDistros();

            var newlyUnclaimed = unclaimed.Except(listed, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();

            Assert.True(
                newlyUnclaimed.Count == 0,
                $"WhichDistroSharp now knows {newlyUnclaimed.Count} distribution(s) BAMM has no support entry for, " +
                $"and they are not listed in {GapsDocument}: {string.Join(", ", newlyUnclaimed)}." +
                $" Add a support entry in Distros.cs, or list them here if BAMM will not support them."
            );
        }

        /// <summary> Ensures a listed distro is not now supported, which would leave the document stale. </summary>
        [Fact]
        public void NoListedDistroHasGainedSupportWithoutBeingRemovedFromTheGapsDocument()
        {
            var unclaimed = UnclaimedUpstreamDistros();
            var listed = ListedUnclaimedDistros();

            var nowClaimed = listed.Except(unclaimed, StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();

            Assert.True(
                nowClaimed.Count == 0,
                $"These distributions are listed in {GapsDocument} as unclaimed but BAMM now resolves them: " +
                $"{string.Join(", ", nowClaimed)}. Remove them from the gaps document."
            );
        }

        /// <summary> Ensures the gaps document does not name a distro WhichDistroSharp is unaware of. </summary>
        [Fact]
        public void EveryListedDistroExistsUpstream()
        {
            var known = DistroMap.Map.Keys.ToHashSet(StringComparer.Ordinal);

            var unknown = ListedUnclaimedDistros()
                .Where(id => !known.Contains(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();

            Assert.True(
                unknown.Count == 0,
                $"{GapsDocument} names distributions WhichDistroSharp does not know: {string.Join(", ", unknown)}."
            );
        }

        /// <summary> Ensures the gaps document is not a full coverage list, which means it is out of date. </summary>
        [Fact]
        public void TheUnclaimedShareIsReported()
        {
            var total = DistroMap.Map.Count;
            var unclaimed = UnclaimedUpstreamDistros().Count;
            var claimed = total - unclaimed;

            Assert.True(unclaimed > 0, "Every upstream distribution is now supported; delete this test and the gaps document.");

            Assert.True(
                claimed > 0,
                $"BAMM claims none of the {total} upstream distributions, which cannot be intended."
            );
        }

        /// <summary> Returns the upstream distro IDs that no entry in Distros.cs claims. </summary>
        private static List<string> UnclaimedUpstreamDistros()
        {
            var entries = ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Core.SystemInfo.OS.Unix.Linux.Distros), true);

            var claimed = new HashSet<string>(StringComparer.Ordinal);

            foreach (var entry in entries) {
                foreach (var supported in entry.SupportedDistros) {
                    // The entry holds the enum and not the ID, so the map is walked backwards to find
                    // which ID(s) resolve to it. An enum may carry more than one ID, and a distro is
                    // claimed through the enum regardless of which ID is detected.
                    foreach (string id in DistroMap.Map.Where(kv => kv.Value == supported).Select(kv => kv.Key)) {
                        claimed.Add(id);
                    }
                }
            }

            return [.. DistroMap.Map.Keys.Where(id => !claimed.Contains(id))];
        }

        /// <summary> Returns the unclaimed IDs read from the marked region of the gaps document. </summary>
        private static List<string> ListedUnclaimedDistros()
        {
            var path = LocateGapsDocument();

            var content = File.ReadAllText(path);
            var start = content.IndexOf(ListStartMarker, StringComparison.Ordinal);
            var end = content.IndexOf(ListEndMarker, StringComparison.Ordinal);

            Assert.True(start >= 0 && end > start, $"{GapsDocument} is missing the list markers.");

            return content[(start + ListStartMarker.Length)..end]
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().Trim('`'))
                .Where(line => line.Length > 0)
                .ToList();
        }

        /// <summary> Returns the path to the gaps document. </summary>
        /// <remarks>
        /// The document is not copied to the output directory, and the working directory differs
        /// between `dotnet test` and an IDE runner, so walk up to the root instead.
        /// </remarks>
        private static string LocateGapsDocument()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null) {
                var candidate = Path.Combine(directory.FullName, GapsDocument.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(candidate)) {
                    return candidate;
                }
                directory = directory.Parent;
            }

            Assert.Fail($"{GapsDocument} was not found in any parent of {AppContext.BaseDirectory}.");
            return string.Empty;
        }
    }
}
