using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.PlatformManager;

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
            if (IsWindows)
                return await Task.Run(OS.Win.GetApps);

            if (IsOSX)
                return await Task.Run(OS.MacOS.GetApps);

            if (IsLinux)
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