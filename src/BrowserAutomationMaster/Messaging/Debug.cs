using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.RuntimeManager;
using BrowserAutomationMaster.Parsing;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers;

namespace BrowserAutomationMaster.Messaging
{
    public static class Debug
    {

        public static string GetPlatformInfoForErrorLog()
        {
            var memoryInfo = GetMemoryInfo();
            var totalMemoryAmount = "Unknown";
            var freeMemoryAmount = "Unknown";

            bool[] conditions = [
                memoryInfo is not null,
                memoryInfo.HasValue,
                memoryInfo!.Value.FreeMemory is not null,
                memoryInfo!.Value.TotalMemory is not null
            ];

            if (conditions.All(condition => condition))
            {
                totalMemoryAmount = $"{memoryInfo!.Value.TotalMemory / 1000}GB";
                freeMemoryAmount = $"{memoryInfo!.Value.FreeMemory / 1000}GB";
            }

            if (Platforms.IsWindows)
            {
                var windowsVersion = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    Windows Version: {windowsVersion} (Build {Environment.OSVersion.Version.Build})
                    Platform: {Environment.OSVersion.Platform}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    UserScripts Dir: {Parser.userScriptsDirectory}
                    GUI Downloaded: {Directory.Exists(DirectoryManager.GetGUIDirectoryPath())}
                    ---------------- SYSTEM SPEC INFO ----------------
                    CPU Name: {CPUInfoManager.GetCPUName()}
                    CPU Core Count: {GetCoreCount()}
                    CPU Architecture: {Platforms.CurrentArchitecture}
                    Total RAM: {totalMemoryAmount}
                    Free RAM: {freeMemoryAmount}".Replace("    ", "");
            }

            else if (Platforms.IsOSX)
            {
                // Make this a part of the Debug class and implement bamm info
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    macOS Version: {MacOS.GetMacOSVersion()}
                    Kernel Version: {Environment.OSVersion.Version.ToString().Replace("Unix", "")}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    UserScripts Dir: {Parser.userScriptsDirectory}
                    GUI Downloaded: {Directory.Exists(DirectoryManager.GetGUIDirectoryPath())}
                    ---------------- SYSTEM SPEC INFO ----------------
                    CPU Name: {CPUInfoManager.GetCPUName()}
                    CPU Core Count: {GetCoreCount()}
                    CPU Architecture: {Platforms.CurrentArchitecture}
                    Total RAM: {totalMemoryAmount}
                    Free RAM: {freeMemoryAmount}".Replace("    ", "");
            }

            else if (Platforms.IsLinux)
            {
                // Make this a part of the Debug class and implement bamm info
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    Distro Name: {Linux.GetDistroNameString()}
                    Kernel Version: {Environment.OSVersion.Version.ToString().Replace("Unix", "")}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    UserScripts Dir: {Parser.userScriptsDirectory}
                    GUI Downloaded: {Directory.Exists(DirectoryManager.GetGUIDirectoryPath())}
                    ---------------- SYSTEM SPEC INFO ----------------
                    CPU Name: {CPUInfoManager.GetCPUName()}
                    CPU Core Count: {GetCoreCount()}
                    CPU Architecture: {Platforms.CurrentArchitecture}
                    Total RAM: {totalMemoryAmount}
                    Free RAM: {freeMemoryAmount}".Replace("    ", "");
            }
            else
            {

                return @$"Platform: {Environment.OSVersion.Platform}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    UserScripts Dir: {Parser.userScriptsDirectory}
                    GUI Downloaded: {Directory.Exists(DirectoryManager.GetGUIDirectoryPath())}
                    ---------------- SYSTEM SPEC INFO ----------------
                    CPU Name: Unknown
                    CPU Core Count: {GetCoreCount()}
                    CPU Architecture: {Platforms.CurrentArchitecture}
                    Total RAM: {totalMemoryAmount}
                    Free RAM: {freeMemoryAmount}".Replace("    ", "");
            }
        }

    }
}
