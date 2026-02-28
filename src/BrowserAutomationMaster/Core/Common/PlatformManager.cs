using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using BrowserAutomationMaster.Core.Python;
using BrowserAutomationMaster.Core.Types;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux.Functions;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Core.Common
{
    public static class PlatformManager
    {

        public static Architecture[] ValidArchitectures { get; private set; } =
        [
            Arm,   // ARMv7 (32 bit)
            Arm64, // ARMv8 (64 bit)
            X64, // x86-64 (64 bit)
        ];
        
        public static void SetPlatform(UserInfo UserInfo)
        {

            // Checks if ChromeOS is in use.
            ChromeOSCheck();

            // Checks if ARM32 is in use, as it requires cross-compiled wheels.
            ARM32Check();

            // Checks if a Raspberry Pi is in use.
            RaspberryPiCheck();

            
            if (!UserInfo.HardwareInformation.IsSupportedArchitecture())
            {
                WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "You're attempting to run BAM Manager (BAMM) on an unsupported CPU Architecture.",
                            $"Current Architecture: {UserInfo.HardwareInformation.CurrentArchitecture}",
                            NLC,
                            "Supported Architectures:",
                            $"{string.Join(NLC, ValidArchitectures)}"
                        ]),
                    status: 1
                );
            }


            if (Runtime.IsSupportedWindowsVersion()) {
                UserInfo.PlatformInfo.IsWindows = true;
            }

            else if (Runtime.IsSupportedOSXVersion())
            {
                UserInfo.PlatformInfo.IsMacOS = true;
                UserInfo.PlatformInfo.IsUnixLike = true;
            }

            else if (OperatingSystem.IsLinux())
            {
                UserInfo.PlatformInfo.IsLinux = true;
                UserInfo.PlatformInfo.IsUnixLike = true;
                UserInfo.PlatformInfo.CurrentDistribution = DistroManager.DetermineDistro();
            }

            // Acts a fallthrough so the exception below is not thrown.
            else if (UserInfo.PlatformInfo.IsRaspi) {
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

            if (UserInfo.HardwareInformation.CurrentArchitecture is Arm64)
            {
                Warning.Write(
                    string.Join("", [
                        "BAM Manager (BAMM) supports ARM64 architecture, ",
                        "but performance for browser automation can vary widely depending on your specific ARM processor. ",
                        "Some lower-power ARM systems may experience degraded performance."
                    ])
                );
            }
        }
    }
}
