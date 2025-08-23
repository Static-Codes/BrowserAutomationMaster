using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.Managers
{
    public static class PlatformManager
    {
        public static OSPlatform PlatformName { get; private set; }
        public readonly static OSPlatform[] UnixLikePlatforms = [ OSPlatform.Linux, OSPlatform.OSX ];
        public static void SetPlatformName()
        {
            if (!Environment.Is64BitOperatingSystem && !Linux.IsChromeOS)
            {
                Errors.WriteErrorAndExit(
                    message: "Due to a variety of factors, BAM Manager (BAMM) is unable to run on x86 (32bit) CPUs.  Ensure your CPU supports 64 bit operating systems, and try again.",
                    status: 1
                );
            }
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                Warning.Write(
                    message:
                        "BAM Manager (BAMM) supports ARM64 architecture, " +
                        "but performance for browser automation can vary widely depending on your specific ARM processor. " +
                        "Some lower-power ARM systems may experience degraded performance."
                );
            }


            if (RuntimeManager.IsSupportedWindowsVersion())
                PlatformName = OSPlatform.Windows;


            else if (RuntimeManager.IsSupportedOSXVersion())
                PlatformName = OSPlatform.OSX;

            else if (OperatingSystem.IsLinux())
                PlatformName = OSPlatform.Linux;

            else
            {
                throw new PlatformNotSupportedException(
                    "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                    "Windows 10/11\n" +
                    "Linux\n" +
                    "MacOS 11+\n"
                );
            }
        }
    }
}
