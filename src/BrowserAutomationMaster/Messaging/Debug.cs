using BrowserAutomationMaster.Parsing;
using BrowserAutomationMaster.Managers.Common;
using BrowserAutomationMaster.Managers.OS.Unix;
using BrowserAutomationMaster.Managers.SystemInfo;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Python.RuntimeManager;

namespace BrowserAutomationMaster.Messaging
{
    public static class Debug
    {
        private static string FormatMemory(double? memoryMiB)
        {
            var MiB_GiB_Factor = 1024.0; // A double is chosen here due to the precision of floating point division.
            var MB_GB_Factor = 1000.0;

            if (memoryMiB == null)
            {
                return "Unknown";
            }

            var memory = memoryMiB.Value;
            double memoryGB = memoryMiB.Value / MB_GB_Factor;

            // Checks if memory is greater than or equal to 1 GiB (1024 MiB)
            if (memory >= MiB_GiB_Factor)
            {
                double memoryGiB = memoryMiB.Value / MiB_GiB_Factor;
                return $"{memoryGiB:F1} GiB ({memoryGB:F1} GB)"; 
            }
            else
            {
                double memoryMB = memoryMiB.Value / MB_GB_Factor;
                // Formats the long into MiB for smaller amounts (less than 1 GiB)
                return $"{memory} MiB ({memoryMB:F1} GB)"; 
            }
        }

        public static string GetPlatformInfoForErrorLog()
        {
            var rawMemoryInfo = GetMemoryInfo();
            var totalMemoryAmount = "Unknown";
            var freeMemoryAmount = "Unknown";


            // Sanitizes rawMemoryInfo via pattern matching, modifies totalMemoryAmount and freeMemoryAmount
            if (rawMemoryInfo is {
                TotalMemory: not null,
                FreeMemory: not null
            } memoryInfo){ 
                totalMemoryAmount = FormatMemory(memoryInfo.TotalMemory);
                freeMemoryAmount = FormatMemory(memoryInfo.FreeMemory);
            }


            if (Platforms.IsWindows)
            {
                var windowsVersion = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    Windows Version: {windowsVersion} (Build {Environment.OSVersion.Version.Build})
                    Platform: {Environment.OSVersion.Platform}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    AppData Dir: {DirectoryManager.AppDataDirectory}
                    UserScripts Dir: {Parser.userScriptsDirectory}
                    GUI Downloaded: {Directory.Exists(DirectoryManager.GetGUIDirectoryPath())}
                    ---------------- SYSTEM SPEC INFO ----------------
                    CPU Name: {CPUInfoManager.GetCPUName()}
                    CPU Core Count: {GetCoreCount()}
                    CPU Architecture: {Platforms.CurrentArchitecture}
                    Total RAM: {totalMemoryAmount}
                    Free RAM: {freeMemoryAmount}".Replace("    ", "");
            }

            else if (Platforms.IsMacOS)
            {
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    macOS Version: {MacOS.GetMacOSVersion()}
                    Kernel Version: {Environment.OSVersion.Version.ToString().Replace("Unix", "")}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    AppData Dir: {DirectoryManager.AppDataDirectory}
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
                return @$"---------------- PLATFORM DEBUG INFO ----------------
                    Distro Name: {GetFullDistroName()}
                    Kernel Version: {Environment.OSVersion.Version.ToString().Replace("Unix", "")}
                    Current Dir: {Environment.CurrentDirectory}
                    Installation Dir: {AppContext.BaseDirectory}
                    AppData Dir: {DirectoryManager.AppDataDirectory}
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
                    AppData Dir: {DirectoryManager.AppDataDirectory}
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
