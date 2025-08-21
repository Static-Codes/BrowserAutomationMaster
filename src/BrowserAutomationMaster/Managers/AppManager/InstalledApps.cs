using System.Diagnostics.CodeAnalysis;
using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers.AppManager
{
    public static class InstalledApps
    {
        private static List<AppInfo> AppInfoList = [];
        private static Installations? InstallationsList;

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        private static async Task<List<AppInfo>> GetInstalledApps()
        {
            if (RuntimeManager.IsSupportedWindowsVersion()) // >= Windows 10 Build 10240 (First Public Windows 10 Build)
                return await Task.Run(OS.Win.GetApps);

            if (RuntimeManager.IsSupportedOSXVersion())
                return await Task.Run(OS.MacOS.GetApps);

            if (OperatingSystem.IsLinux())
                return await Task.Run(OS.Linux.GetApps);

            Errors.ThrowUnsupportedPlatformException();
            return [];
        }


        private static async Task PopulateAppInfoList() { AppInfoList = await GetInstalledApps(); }

        public static async Task PopulateInstallations()
        {
            await PopulateAppInfoList();
            InstallationsList = new Installations(AppInfoList);
        }

        public static Installations GetInstallations() { return InstallationsList ?? new Installations(); }

        
    }
}