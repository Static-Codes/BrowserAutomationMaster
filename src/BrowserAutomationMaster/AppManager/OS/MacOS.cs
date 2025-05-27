using System;
using System.Collections.Generic;
using System.IO;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.AppManager.OS
{
    public static class MacOS
    {
        public static List<AppInfo> GetApps()
        {
            var apps = new List<AppInfo>();

            string[] searchDirs = [
                "/Applications",
                "/System/Applications",
                "/System/Library/CoreServices",
                "/usr/local/bin"
            ];
            try
            {
                foreach (var dir in searchDirs)
                {
                    if (!Directory.Exists(dir)) { continue; }

                    foreach (var item in Directory.GetFileSystemEntries(dir, "*.app"))
                    {
                        var appName = Path.GetFileNameWithoutExtension(item);
                        apps.Add(new AppInfo
                        {
                            Name = appName,
                            Version = "Unable to parse.", // Will implement the logic for this in the future
                            Publisher = "Unable to parse."
                        });
                    }
                }
            }
            catch { Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to find any installed applications, exiting...", 1); }
            return apps;
        }
    }
}