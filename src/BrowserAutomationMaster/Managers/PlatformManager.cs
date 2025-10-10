using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers
{
    public class InternalPlatforms()
    {
        public bool IsARMel { get; set; } // 32 Bit ARMv7 (el = EABI Little Endian)
        public bool IsARMhf { get; set; } // 32 Bit ARMv7 (hf = Hard Float)
        public bool IsChromeOS { get; set; }
        public bool IsWindows { get; set; }
        public bool IsOSX { get; set; }
        public bool IsLinux { get; set; }
        public bool IsUnixLike { get; set; } // Linux + OSX
        public Architecture CurrentArchitecture { get; private set; } = RuntimeInformation.OSArchitecture;

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

            if (!ValidArchitectures.Contains(Platforms.CurrentArchitecture))
                Errors.WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "You're attempting to run BAM Manager (BAMM) on an unsupported CPU Architecture.",
                            $"Current Architecture:{Platforms.CurrentArchitecture}{NLC}",
                            "Supported Architecture:",
                            $"{string.Join(NLC, ValidArchitectures)}"
                        ]),
                    status: 1
                );

            if (Platforms.CurrentArchitecture == Arm64)
                Warning.Write(
                    string.Format("{0}{1}{2}", [
                        "BAM Manager (BAMM) supports ARM64 architecture, ",
                        "but performance for browser automation can vary widely depending on your specific ARM processor. ",
                        "Some lower-power ARM systems may experience degraded performance.",
                    ])
                );


            if (RuntimeManager.IsSupportedWindowsVersion())
                Platforms.IsWindows = true;

            else if (RuntimeManager.IsSupportedOSXVersion())
            {
                Platforms.IsOSX = true;
                Platforms.IsUnixLike = true;
            }

            else if (OperatingSystem.IsLinux())
            {
                Platforms.IsLinux = true;
                Platforms.IsUnixLike = true;
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
