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

﻿using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.Functions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers
{
    public class InternalPlatforms()
    {
        public bool IsARMel { get; set; } // 32 Bit ARMv7 (el = EABI Little Endian)
        public bool IsARMhf { get; set; } // 32 Bit ARMv7 (hf = Hard Float)
        public bool IsChromeOS { get; set; }
        public bool IsRaspi { get; set; } // Raspberry Pi
        public bool IsWindows { get; set; }
        public bool IsMacOS { get; set; }
        public bool IsLinux { get; set; }
        public bool IsUnixLike { get; set; } // Linux + OSX

        public Distro? CurrentDistribution = null;
        public Architecture CurrentArchitecture { get; private set; } = RuntimeInformation.OSArchitecture;
        public KeyValuePair<string, bool>? RaspiModelInfo { get; set; }

        public string GetRaspiModelName()
        {
            if (!IsRaspi) {
                return string.Empty;
            }
            
            if (RaspiModelInfo == null) {
                return string.Empty;
            }
            
            return RaspiModelInfo.Value.Key;
        }
        
        public void SetRaspiModel(string Name, bool SupportsGUI)
        {
            if (!IsRaspi) {
                return;
            }
            RaspiModelInfo = new(Name, SupportsGUI);
        }

    }


    public static class PlatformManager
    {

        public static Architecture[] ValidArchitectures { get; private set; } =
        [
            Arm,   // ARMv7 (32 bit)
            Arm64, // ARMv8 (64 bit)
            X86, // x86 (32 bit)
            X64, // x86-64 (64 bit)
        ];

        public static InternalPlatforms Platforms { get; private set; } = new InternalPlatforms();
        
        public static void SetPlatform()
        {

            // Checks if ChromeOS is in use.
            ChromeOSCheck();

            // Checks if ARM32 is in use, as it requires cross-compiled wheels.
            ARM32Check();

            // Checks if a Raspberry Pi is in use.
            RPICheck();


            if (!ValidArchitectures.Contains(Platforms.CurrentArchitecture))
            {
                WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "You're attempting to run BAM Manager (BAMM) on an unsupported CPU Architecture.",
                            $"Current Architecture:{Platforms.CurrentArchitecture}{NLC}",
                            "Supported Architecture:",
                            $"{string.Join(NLC, ValidArchitectures)}"
                        ]),
                    status: 1
                );
            }

            if (Platforms.CurrentArchitecture is Arm64)
            {
                Warning.Write(
                    string.Join("", [
                        "BAM Manager (BAMM) supports ARM64 architecture, ",
                        "but performance for browser automation can vary widely depending on your specific ARM processor. ",
                        "Some lower-power ARM systems may experience degraded performance."
                        ]
                    )
                );
            }

            if (RuntimeManager.IsSupportedWindowsVersion())
            {
                Platforms.IsWindows = true;
            }

            else if (RuntimeManager.IsSupportedOSXVersion())
            {
                Platforms.IsMacOS = true;
                Platforms.IsUnixLike = true;
            }

            else if (OperatingSystem.IsLinux())
            {
                Platforms.IsLinux = true;
                Platforms.IsUnixLike = true;
                Platforms.CurrentDistribution = DistroManager.DetermineDistro();
                // This will be added in a later update.
                // Platforms.CurrentDistribution = DistroManager.GetDistro.FromBase("elementary OS", DistroBase.Debian);
            }

            // Acts a fallthrough so the exception below is not thrown.
            else if (Platforms.IsRaspi) 
            {
                return;
            }

            else
            {
                throw new PlatformNotSupportedException(
                    string.Join(NLC, [
                        "Unsupported OS.",
                        "BAM Manager (BAMM) currently supports:",
                        "-> Windows 10/11",
                        "-> Linux",
                        "-> MacOS 11+"
                        ]
                    )
                );
            }
        }
    }
}
