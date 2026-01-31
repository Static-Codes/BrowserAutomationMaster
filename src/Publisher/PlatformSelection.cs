using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.Architecture;
using static Publisher.PlatformSelection;

namespace Publisher
{
    // Removed the empty primary constructor brackets () as they aren't needed here
    public class PlatformSelection 
    {
        public class Platform
        {
            public required string OSName { get; set; }
            public required PlatformBuildInfo[] PlatformBuildInfos { get; set; }
        }

        public class PlatformBuildInfo(Architecture Arch, string RID)
        {
            public Architecture Arch = Arch;
            public string RID = RID;
        }
        
        public readonly static Platform[] PlatformOptions =
        [
            new() 
            {
                OSName = "Linux",
                PlatformBuildInfos =
                [
                    new PlatformBuildInfo(X64, "linux-x64"),
                    new PlatformBuildInfo(Arm64, "linux-arm64"),
                    new PlatformBuildInfo(Arm, "linux-arm")
                ],
            },

            new()
            {
                OSName = "Windows",
                PlatformBuildInfos =
                [
                    new PlatformBuildInfo(X64, "win-x64"),
                    new PlatformBuildInfo(Arm64, "win-arm64")
                ],
            },

            new() 
            {
                OSName = "macOS",
                PlatformBuildInfos = 
                [
                    new PlatformBuildInfo(X64, "osx-x64" ),
                    new PlatformBuildInfo(Arm64, "osx-arm64" ),
                ],
            }

        ];

        public string[] LinuxOptions = [
            "Debian Package (.deb)",
            "Fedora Package (.rpm)",
            "Arch Package (.pkg.tar.xz)",
            "Gentoo Package (.tbz2)",
            "Standalone Binary"
        ];


        public static List<string> GetAvailableOSNames()
        {
            return [.. PlatformOptions.Select(platform => platform.OSName)];
        }

        public static Architecture[] GetAvailableArchitectures(string OSName)
        {
            return [.. PlatformOptions
                .Where(platform => platform.OSName == OSName)
                .SelectMany(platformInfo => platformInfo.PlatformBuildInfos)
                .Select(platform => platform.Arch)
            ];
        }

        public static string? GetRID(string OSName, Architecture architecture) {
            return 
                PlatformOptions
                .Where(option => option.OSName == OSName)     // Ensure OSName matches 
                .SelectMany(a => a.PlatformBuildInfos)        // Flattening the PlatformBuildInfo array
                .Where(option => option.Arch == architecture) // Ensuring architecture matches
                .Select(a => a.RID)                           // Selecting the Runtime ID
                .First();                                     // returning either the RID or null
        }
    }

    public class PlatformOption 
    {
        public required string OSName;
        public required PlatformBuildInfo ArchitectureInfo;

        public bool IsValidOption() 
        {
            // Checking the Architecture of the current option against the defined choices.
            var validArchitechure = PlatformOptions.Any(
                option => option.PlatformBuildInfos.Any(
                    buildInfo => buildInfo.Arch.Equals(ArchitectureInfo.Arch)
                )
            );

            // Checking the OS Name of the current option against the defined choices.
            var validOS = PlatformOptions.Any(
                option => option.OSName == OSName
            );

            return validArchitechure && validOS;
        }

        

    }

}
