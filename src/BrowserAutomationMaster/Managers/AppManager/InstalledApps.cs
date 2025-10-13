using System.Diagnostics.CodeAnalysis;
using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.AppManager
{
    public static class InstalledApps
    {
        public readonly static List<AppInfo> AppInfoList = [];
        private static Installations? InstallationsList;

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        private static async Task<List<AppInfo>> GetInstalledApps()
        {
            if (Platforms.IsWindows)
                return await Task.Run(Win.GetApps);

            if (Platforms.IsOSX)
                return await Task.Run(MacOS.GetApps);

            if (Platforms.IsLinux)
                return await Task.Run(Linux.GetApps);

            ThrowUnsupportedPlatformException();
            return [];
        }


        private static async Task PopulateAppInfoList() { 
            AppInfoList.AddRange(await GetInstalledApps()); 
        }

        public static async Task PopulateInstallations()
        {

            static void install() { Linux.InstallRequiredLinuxPackages(AppInfoList); }

            await PopulateAppInfoList();
            InstallationsList = new Installations(AppInfoList);


            if (Platforms.IsLinux)
                await Task.Run(install);
        }

        public static Installations GetInstallations() { return InstallationsList ?? new Installations(); }

        
    }
}