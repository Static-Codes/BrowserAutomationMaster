using BrowserAutomationMaster.Managers.Helpers;
using BrowserAutomationMaster.Managers.OS.Unix;
using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.OS.Generic
{
    public static class InstalledApps
    {
        public readonly static List<AppInfo> AppInfoList = [];
        private static Installations? InstallationsList;

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        private static async Task<List<AppInfo>> GetInstalledApps()
        {
            if (Platforms.IsWindows) {
                return await Task.Run(Win.GetApps);
            }

            if (Platforms.IsMacOS) {
                return await Task.Run(MacOS.GetApps);
            }

            if (Platforms.IsLinux) {
                return await Task.Run(GetApps);
            }

            ThrowUnsupportedPlatformException();
            return [];
        }


        private static async Task PopulateAppInfoList() { 
            AppInfoList.AddRange(await GetInstalledApps()); 
        }

        public static async Task PopulateInstallations()
        {

            await PopulateAppInfoList();
            InstallationsList = new Installations(AppInfoList);

            if (Platforms.IsLinux) {
                await InstallRequiredLinuxPackages();
            }
        }

        public static Installations GetInstallations() { return InstallationsList ?? new Installations(); }

        
    }
}