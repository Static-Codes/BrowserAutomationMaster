using BrowserAutomationMaster.Core.OS.Generic;
using static BrowserAutomationMaster.Core.Common.ANSI;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Messaging.Errors;

namespace BrowserAutomationMaster.Core.OS.Unix
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
                    if (!Directory.Exists(dir)) { 
                        continue; 
                    }

                    foreach (var item in Directory.GetFiles(dir))
                    {
                        var appName = Path.GetFileNameWithoutExtension(item);
                        apps.Add(new AppInfo
                        {
                            Name = appName,
                            Version = "Not supported currently.",
                            Publisher = "Not supported currently.",
                            Path = Path.Combine(dir, item)
                        });
                    }

                    foreach (var item in Directory.GetFileSystemEntries(dir, "*.app"))
                    {
                        var appName = Path.GetFileNameWithoutExtension(item);
                        apps.Add(new AppInfo { 
                            Name = appName,
                            Version = "Not supported currently.",
                            Publisher = "Not supported currently.",
                            Path = Path.Combine(dir, item)
                        });
                    }
                }
            }
            catch { 
                WriteAndExit(
                    message: "BAM Manager (BAMM) was unable to find any installed applications, exiting...", 
                    status: 1
                ); 
            }
            if (apps.Count == 0) { 
                WriteAndExit(
                    message: "BAM Manager (BAMM) was unable to find any installed applications, exiting...", 
                    status: 1
                ); 
            }
            return apps;
        }


        public static void HandleVEnvExceptions(string exMessage)
        {
            if (exMessage.StartsWith("xcode-select: note: no developer tools were found at '/Applications/Xcode.app'"))
            {
                WriteAndExit(
                    message:
                        "Python requires a package installation prior to working with virtual environments for the first time.\n" +
                        "Please click the 'install' button on the popup window, then restart BAMM.",
                    status: 1
                );
            }
        }

        public static string GetMacOSVersion()
        {
            (var result, _) = RunCommand("/bin/bash", "-c \"sw_vers -productVersion\"");
            var match = PrecompiledMacOSVersionRegex().Match(result);

            if (!match.Success)
            {
                return "Unknown";
            }

            return match.Groups[1].Value;
        }

        [Obsolete("Unused but left for future references")]
        public static void HandleMultipleInstances(string procName)
        {
            // Since osx and linux are both unix like systems, they share relational similarities regarding certain command execution.
            (string output, _) = RunCommand("pgrep", procName);
            string[] instancePIDs = output.Split('\n');
            int numberOfInstances = instancePIDs.Length;
            if (numberOfInstances != 1)
            {
                WriteMessage(
                    "Only one instance of BAMM can be running at once, please close the current session and open bamm again.",
                    isError: true
                );
                Environment.Exit(1);
            }
        }
    }
}