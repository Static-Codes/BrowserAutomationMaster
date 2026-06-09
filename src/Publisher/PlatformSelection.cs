// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

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
