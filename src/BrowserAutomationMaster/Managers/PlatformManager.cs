using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;

namespace BrowserAutomationMaster.Managers
{
    public static class PlatformManager
    {
        public static bool IsWindows { get; private set; }
        public static bool IsOSX { get; private set; }
        public static bool IsLinux { get; private set; }
        public static bool IsUnixLike { get; private set; } // Linux + OSX
        
        public static void SetPlatform()
        {
            if (!Environment.Is64BitOperatingSystem && !IsChromeOS)
            {
                Errors.WriteAndExit(
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
                    "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                    "Windows 10/11\n" +
                    "Linux\n" +
                    "MacOS 11+\n"
                );
            }
        }
    }
}
