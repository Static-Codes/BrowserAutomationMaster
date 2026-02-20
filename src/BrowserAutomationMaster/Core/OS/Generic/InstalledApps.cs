using BrowserAutomationMaster.Core.OS.Unix;
using BrowserAutomationMaster.Core.Types;
using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Messaging.Errors;

namespace BrowserAutomationMaster.Core.OS.Generic
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