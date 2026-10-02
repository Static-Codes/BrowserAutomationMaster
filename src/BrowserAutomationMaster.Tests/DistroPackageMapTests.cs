using BrowserAutomationMaster.Core.Types.Linux;
using System.Text.Json;
using WhichDistroSharp;
using Xunit;
using PackageFormatDocument = BrowserAutomationMaster.Core.Types.Linux.PackageFormatDocument;
using WhichDistro = WhichDistroSharp.Distro;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Ensures the ID to package-format map the Linux installer reads is complete. <br/>
    /// Source: Core/Types/Linux/DistroPackageMap.cs
    /// </summary>
    public class DistroPackageMapTests
    {
        // A key missing from the map makes the installer report a supported distro as unsupported.
        [Fact]
        public void EveryLibraryDistroAppearsInTheMap()
        {
            var map = DistroPackageMap.Build();
            var missing = DistroMap.Map.Keys
                .Where(id => !map.ContainsKey(id.ToLowerInvariant()))
                .ToList();

            Assert.True(
                missing.Count == 0,
                $"These distros are absent from the map: {string.Join(", ", missing)}"
            );
        }

        /// <summary> Ensures "no package" is a null value instead of an omitted key. </summary>
        [Fact]
        public void EveryEntryIsPresentEvenWhenBammShipsNoPackage()
        {
            foreach (string id in DistroMap.Map.Keys) {
                Assert.True(
                    DistroPackageMap.Build().ContainsKey(id.ToLowerInvariant()),
                    $"'{id}' is missing rather than mapped to null."
                );
            }
        }

        /// <summary> Ensures every key is lowercase, as os-release IDs are matched without casing. </summary>
        [Fact]
        public void KeysAreLowercasedBecauseOsReleaseIdsAreMatchedCaseInsensitively()
        {
            foreach (string key in DistroPackageMap.Build().Keys) {
                Assert.Equal(key.ToLowerInvariant(), key);
            }
        }

        // The two families BAMM actually ships packages for, plus the distros that were named in
        // the previous changelog as supported.
        [Theory]
        [InlineData("ubuntu", "deb")]
        [InlineData("debian", "deb")]
        [InlineData("raspbian", "deb")]
        [InlineData("fedora", "rpm")]
        [InlineData("ol", "rpm")]
        [InlineData("almalinux", "rpm")]
        [InlineData("rocky", "rpm")]
        [InlineData("pclinuxos", "rpm")]
        [InlineData("altlinux", "rpm")]
        [InlineData("arch", "pkg.tar.xz")]
        [InlineData("gentoo", "tbz2")]
        /// <summary> Ensures the distros the installer is expected to handle resolve to the right format. </summary>
        public void TheDistrosTheInstallerMustHandleResolveCorrectly(string id, string expected)
        {
            Assert.Equal(expected, DistroPackageMap.Build()[id]);
        }

        /// <summary> Ensures the map describes the published artifact and not BAMM's support list. </summary>
        [Theory]
        [InlineData("manjaro", "pkg.tar.xz")]
        [InlineData("steamos", "pkg.tar.xz")]
        public void DistrosBammHasNoSupportEntryForStillCarryAPublishedFormat(string id, string expected)
        {
            Assert.Equal(expected, DistroPackageMap.Build()[id]);
        }

        /// <summary> Ensures a distro with no package manager is listed with a null format. </summary>
        [Theory]
        [InlineData("buildroot")]
        [InlineData("coreos")]
        [InlineData("flatcar")]
        [InlineData("clear-linux-os")]
        [InlineData("nixos")]
        public void DistrosWithNoPackageManagerArePresentWithANullFormat(string id)
        {
            var map = DistroPackageMap.Build();

            Assert.True(map.ContainsKey(id), $"'{id}' should be present.");
            Assert.Null(map[id]);
        }

        /// <summary> Ensures every PackageType the library reports reaches the map. </summary>
        /// <remarks>
        /// A type with no entry in PackageArtifactFormats throws here instead of silently
        /// vanishing from the map.
        /// </remarks>
        [Fact]
        public void EveryPackageTypeTheLibraryReportsReachesTheMap()
        {
            var seen = DistroMap.Map.Values
                .Select(d => PackageManagerInfo.GetInfo(d)?.PackageType)
                .Where(t => t != null)
                .Select(t => t!.Value)
                .Distinct()
                .ToList();

            Assert.Equal(Enum.GetValues<WhichDistroSharp.PackageType>().Length, seen.Count);

            foreach (var type in seen) {
                PackageArtifactFormats.GetArtifactExtension(type);
            }
        }

        /// <summary> Ensures the map is larger than the two hardcoded lists it replaced. </summary>
        [Fact]
        public void TheMapIsLargerThanTheTwoListsItReplaces()
        {
            // The old lists named 12. A map no larger than that is not buying the coverage.
            Assert.True(
                DistroPackageMap.Build().Count > 12,
                $"The map has only {DistroPackageMap.Build().Count} entries."
            );
        }

        /// <summary> Ensures a null value survives serialisation, which it does not if nulls are skipped. </summary>
        [Fact]
        public void SerialisationRoundTripsWithNullsIntact()
        {
            string json = DistroPackageMap.Serialize("v1.0.0A8");

            var parsed = JsonSerializer.Deserialize<PackageFormatDocument>(json);

            Assert.NotNull(parsed);
            Assert.Equal(DistroPackageMap.SchemaVersion, parsed.Schema);
            Assert.Equal("v1.0.0A8", parsed.GeneratedFor);

            Assert.Equal(DistroPackageMap.Build().Count, parsed.Distros.Count);
            Assert.Null(parsed.Distros["coreos"]);
            Assert.Equal("deb", parsed.Distros["ubuntu"]);
        }

        /// <summary> Ensures the schema is pinned, since bumping it is a breaking change for the installer. </summary>
        [Fact]
        public void TheSchemaVersionIsPinned()
        {
            Assert.Equal(1, DistroPackageMap.SchemaVersion);
        }

        /// <summary> Ensures the serialised output is valid JSON with the expected field names. </summary>
        [Fact]
        public void SerialisedOutputIsValidJsonWithTheExpectedFieldNames()
        {
            using var document = JsonDocument.Parse(DistroPackageMap.Serialize("test"));

            Assert.Equal(1, document.RootElement.GetProperty("schema").GetInt32());
            Assert.Equal("test", document.RootElement.GetProperty("generatedFor").GetString());
            Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("distros").ValueKind);
        }
    }
}
