using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.Managers.AppManager
{
    public static class InstalledApps
    {
        public static List<AppInfo> GetInstalledApps()
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 1, 7601)) // >= Windows 6.1.7601
                return OS.Win.GetApps();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OS.MacOS.GetApps();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OS.Linux.GetApps();
            throw new PlatformNotSupportedException("Unsupported OS.");
        }
    }
}