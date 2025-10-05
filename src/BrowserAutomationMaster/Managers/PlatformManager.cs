using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers
{
    public static class PlatformManager
    {
        private static Architecture[] ValidArchitectures { get; set; } =
        [
            Arm,   // ARMv7 (32 bit)
            Arm64, // ARMv8 (64 bit)
            X86, // x86 (32 bit)
            X64, // x86-64 (64 bit)
        ];

        public static bool IsWindows { get; private set; }
        public static bool IsOSX { get; private set; }
        public static bool IsLinux { get; private set; }
        public static bool IsUnixLike { get; private set; } // Linux + OSX
        public static Architecture CurrentArchitecture { get; private set; } = RuntimeInformation.OSArchitecture;

        
        public static void SetPlatform()
        {

            // Checks if ChromeOS is in use.
            ChromeOSCheck();

            // Checks if ARMHF is in use, as it requires cross-compiled wheels.
            ARMHFCheck();

            if (!ValidArchitectures.Contains(CurrentArchitecture))
                Errors.WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "You're attempting to run BAM Manager (BAMM) on an unsupported CPU Architecture.",
                            $"Current Architecture:{CurrentArchitecture}{NLC}",
                            "Supported Architecture:",
                            $"{string.Join(NLC, ValidArchitectures)}"
                        ]),
                    status: 1
                );

            if (CurrentArchitecture == Arm64)
                Warning.Write(
                    string.Format("{0}{1}{2}", [
                        "BAM Manager (BAMM) supports ARM64 architecture, ",
                        "but performance for browser automation can vary widely depending on your specific ARM processor. ",
                        "Some lower-power ARM systems may experience degraded performance.",
                    ])
                );


            if (RuntimeManager.IsSupportedWindowsVersion())
                IsWindows = true;

            else if (RuntimeManager.IsSupportedOSXVersion())
            {
                IsOSX = true;
                IsUnixLike = true;
            }

            else if (OperatingSystem.IsLinux())
            {
                IsLinux = true;
                IsUnixLike = true;
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
