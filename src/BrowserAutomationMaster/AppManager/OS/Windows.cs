using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using BrowserAutomationMaster.Messaging;
using Microsoft.Win32;

namespace BrowserAutomationMaster.AppManager.OS
{
    [SupportedOSPlatform("windows")]
    public static class Windows
    {
        public static List<AppInfo> GetApps()
        {
            var apps = new List<AppInfo>();
            try {
                apps.AddRange(QueryRegistryForApps(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
                apps.AddRange(QueryRegistryForApps(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
                apps.AddRange(QueryRegistryForApps(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
            }
            catch { Errors.WriteErrorAndExit("BAM Manager was unable to query Windows Registry, please try again; if this issue persists, it's likely a bug.", 1); }
            return apps;
        }

        private static List<AppInfo> QueryRegistryForApps(RegistryHive hive, string subKeyPath)
        {
            var list = new List<AppInfo>();
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (RegistryKey? key = baseKey.OpenSubKey(subKeyPath))
            {
                if (key == null)
                    return list;

                foreach (var subkeyName in key.GetSubKeyNames())
                {
                    using RegistryKey? subkey = key.OpenSubKey(subkeyName);
                    if (subkey == null) { continue; }
                    string? name = subkey?.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(name)) { continue; }

                    string? version = subkey?.GetValue("DisplayVersion") as string;
                    string? publisher = subkey?.GetValue("Publisher") as string;

                    list.Add(new AppInfo{
                        Name = name,
                        Version = version ?? "Not Found",
                        Publisher = publisher ?? "Not Found"
                    });
                }
            }
            return list;
        }

        public static void VerifyRootDrive(string[] args)
        {
            if (args.Contains("--ignore-drive-root")) { return; }
            string? rootDrive = Path.GetPathRoot(AppContext.BaseDirectory);

            if (rootDrive == null || !rootDrive.StartsWith("C:")) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was developed to be ran on the C: drive.\n\nRunning this application on a different drive caused too many unforseeable bugs, so i've decided to prevent it from happening all together.\n\nIf you are contributing to development, you can bypass this restriction by passing the argument '--ignore-drive-root'.", 1);
            }
        }
    }
}