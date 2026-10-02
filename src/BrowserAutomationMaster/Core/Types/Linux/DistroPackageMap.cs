using System.Text.Json;
using System.Text.Json.Serialization;
using WhichDistroSharp;

namespace BrowserAutomationMaster.Core.Types.Linux
{
    /// <summary> Produces the ID to package format mapping the BAMM for Linux installer reads to choose an artifact. </summary>
    /// <remarks>
    /// Every ID the from the upstream os-release repository is included; This includes those that BAMM does not publish for. <br/><br/>
    /// 
    /// When a null value is provided, it signifies that "BAMM doesn't publish a package for this Distro". <br/><br/>
    /// 
    /// This is done to distinquish publishing from recognition, as an unrecognized distro will return "Distro not found". <br/><br/>
    /// </remarks>
    public static class DistroPackageMap
    {
        /// <summary> Updated when the schema changes, so the installer can refuse a format it doesn't recognize instead of misreading it. </summary>
        public const int SchemaVersion = 1;

        public const string FileName = "package-formats.json";

        private static readonly JsonSerializerOptions options = new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.Never };

        /// <summary>
        /// Builds the mapping used by the os-release ID. 
        /// Keys are converted to lowercase to match the case-insensitive put forth by the installer.
        /// </summary>
        public static SortedDictionary<string, string?> Build()
        {
            var map = new SortedDictionary<string, string?>(StringComparer.Ordinal);

            foreach (string id in DistroMap.Map.Keys) {
                var distro = DistroMap.Map[id];
                map[id.ToLowerInvariant()] = PackageArtifactFormats.GetArtifactExtension(distro);
            }

            return map;
        }

        public static string Serialize(string releaseTag)
        {
            var document = new PackageFormatDocument {
                Schema = SchemaVersion,
                GeneratedFor = releaseTag,
                Distros = Build(),
            };

            return JsonSerializer.Serialize(document, options);
        }
    }

    /// <summary>Serialization shape of the emitted map. Public for the test to assert against.</summary>
    public sealed class PackageFormatDocument
    {
        [JsonPropertyName("schema")]
        public int Schema { get; set; }

        [JsonPropertyName("generatedFor")]
        public string GeneratedFor { get; set; } = string.Empty;

        [JsonPropertyName("distros")]
        public SortedDictionary<string, string?> Distros { get; set; } = new(StringComparer.Ordinal);
    }
}
