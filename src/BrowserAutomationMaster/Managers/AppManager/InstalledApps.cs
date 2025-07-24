using System.Diagnostics.CodeAnalysis;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers.AppManager
{
    public static class InstalledApps
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static List<AppInfo> GetInstalledApps()
        {
            if (RuntimeManager.IsSupportedWindowsVersion()) // >= Windows 10 Build 10240
                return OS.Win.GetApps();
            if (RuntimeManager.IsSupportedOSXVersion())
                return OS.MacOS.GetApps();
            if (OperatingSystem.IsLinux())
                return OS.Linux.GetApps();
            Errors.ThrowUnsupportedPlatformException();
            return []; // This wont be executed, roslyn has no idea an exception has been thrown, so this is required.
        }
    }
}