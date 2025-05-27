using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.AppManager
{
    public static class InstalledApps
    {
        public static List<AppInfo> GetInstalledApps()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OS.Windows.GetApps();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OS.MacOS.GetApps();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OS.Linux.GetApps();
            throw new PlatformNotSupportedException("Unsupported OS.");
        }
    }
}