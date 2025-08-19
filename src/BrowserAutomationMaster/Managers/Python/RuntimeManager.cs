using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers.Python
{
    // This class is responsible for executing the compiled python scripts.
    public class RuntimeManager(string scriptFilePath)
    {
        private string SanitizedScriptPath { get; set; } = string.Empty;
        public static OSPlatform Platform { get; } = GetPlatform();
        public string InterpreterPath { get; } = GetInterpreterFromPath();

        private static string BuildScriptMenu(List<string> scriptPaths)
        {
            var menu = new StringBuilder();
            for (int i = 0; i < scriptPaths.Count; i++)
            {
                string fileName = Path.GetFileName(scriptPaths[i]);
                menu.AppendLine($"{i + 1}. {fileName} -> {scriptPaths[i]}");
            }
            return menu.ToString();
        }
        public static void DoRuntimeCheck()
        {
            HasEnoughMemory();
            CPUInfoManager cpuInfoManager = new();
            if (!cpuInfoManager.HasEnoughCores())
            {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) requires atleast a 2 core cpu, " +
                        $"unfortunately your CPU is not powerful enough for modern browser automation, " +
                        $"if you believe this is an error, please submit a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nBAM Manager (BAMM) detected {cpuInfoManager.Cores} physical CPU cores.",
                    status: 1
                );
            }
        }
        private static List<string> GetCompiledScriptPaths(string saveDirectory)
        {
            return [.. Directory.GetDirectories(saveDirectory)
                .Where(dir => !dir.EndsWith("venv", CCIC))
                .SelectMany(dir => Directory.GetFiles(dir, "*.py"))
                .Where(File.Exists)
                .Distinct()];
        }
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        private static string GetInterpreterFromPath()
        {
            if (IsSupportedWindowsVersion())
                return Win.GetInterpreterPath();

            if (IsSupportedOSXVersion() || OperatingSystem.IsLinux() || Linux.IsChromeOS)
                return "python3";

            throw new PlatformNotSupportedException(
                "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                "Windows 10/11\n" +
                "Linux\n" +
                "MacOS 11+\n"
            );
        }
        private static OSPlatform GetPlatform()
        {
            if (!Environment.Is64BitOperatingSystem && !Linux.IsChromeOS)
            {
                Errors.WriteErrorAndExit(
                    message: "Due to a variety of factors, BAM Manager (BAMM) is unable to run on x86 (32bit) CPUs.  Ensure your CPU supports 64 bit operating systems, and try again.",
                    status: 1
                );
            }
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64)
            {
                Warning.Write(
                    message:
                        "BAM Manager (BAMM) supports ARM64 architecture, " +
                        "but performance for browser automation can vary widely depending on your specific ARM processor. " +
                        "Some lower-power ARM systems may experience degraded performance."
                );
            }

            if (IsSupportedOSXVersion())
                return OSPlatform.OSX;

            if (IsSupportedWindowsVersion())
                return OSPlatform.Windows;

            if (OperatingSystem.IsLinux())
                return OSPlatform.Linux;

            else
            {
                throw new PlatformNotSupportedException(
                    "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                    "Windows 10/11\n" +
                    "Linux\n" +
                    "MacOS 11+\n"
                );
            }
        }
        private static string GetUserScriptChoice(List<string> scriptPaths, string menu)
        {
            while (true)
            {
                var message = "Unable to parse selected menu option.\n" +
                              $"If this continues please make a bug report at {ISSUES_LINK}" +
                              "Error Log:\nchoice returned null.";

                string rawChoice = Input.WriteListFromOptions(menu.Split('\n'));
                string? choice = Parser.GetFileNumber(rawChoice);

                if (choice == null)
                {
                    Errors.WriteErrorAndExit(message, 1);
                }

                if (int.TryParse(choice, out int result) && result >= 1 && result <= scriptPaths.Count)
                {
                    return scriptPaths[result - 1];
                }

                Errors.WriteErrorAndContinue($"Invalid option, please choose a number between 1 and {scriptPaths.Count}\n");
            }
        }
        public static string HandleUserScriptChoice()
        {
            string saveDirectory = DirectoryManager.GetDesiredSaveDirectory();

            try
            {
                var scriptPaths = GetCompiledScriptPaths(saveDirectory);

                if (scriptPaths.Count == 0)
                {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to find any compiled scripts, " +
                            $"please ensure you have atleast one compiled script before selecting this option.\n\n" +
                            $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nNo compiled scripts found in {saveDirectory}",
                        status: 1
                    );
                }

                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully detected {scriptPaths.Count} scripts.\n");

                string menu = BuildScriptMenu(scriptPaths);

                return GetUserScriptChoice(scriptPaths, menu);
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to find any compiled scripts, " +
                        $"please ensure you have atleast one compiled script before selecting this option.\n\n" +
                        $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n {e.Message}",
                    status: 1
                );
                return string.Empty; // This won't execute due to WriteErrorAndExit, but satisfies compiler
            }
        }
        public static bool HasEnoughMemory()
        {
            Dictionary<string, double> memoryInfo = MemoryInfoManager.RunCheck();
            if (memoryInfo.Count != 5)
            {
                Errors.WriteErrorAndExit(
                    $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                    $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\nMemoryInfoManager.HasEnoughMemory() returned an invalid dictionary.",
                    status: 1
                );
            }
            memoryInfo.TryGetValue("totalMemoryMB", out double totalMemoryMB);
            //memoryInfo.TryGetValue("usedMemoryMB", out double usedMemoryMB); // Will be used in a later update to display various info.
            memoryInfo.TryGetValue("freeMemoryMB", out double freeMemoryMB);
            //memoryInfo.TryGetValue("usedPercent", out double usedPercent);
            //memoryInfo.TryGetValue("freePercent", out double freePercent);

            // Less than 2GiB Total
            if (totalMemoryMB < 2048)
            {
                Errors.WriteErrorAndExit(
                    "BAM Manager (BAMM) determined you are running below the minimum RAM requirements to properly use bamm.\n" +
                    "Please run BAMM on a system with atleast 4GB of DDR3 RAM.",
                    status: 1
                );
            }

            // Less than 512MiB Free
            if (freeMemoryMB < 512)
            {
                Errors.WriteErrorAndExit(
                    "BAM Manager (BAMM) determined you don't have enough free RAM to continue.\n\n" +
                    "Please ensure atleast 512MB of RAM is free before trying to run BAMM again.",
                    status: 1
                );
            }

            // Less than 4GiB Total but between 512MiB and 1GiB Free.
            else if (totalMemoryMB < 4096 && freeMemoryMB < 1024)
            {
                Warning.Write(
                    "BAM Manager (BAMM) determined you are running below the minimum RAM requirements.\n" +
                    "Compiling BAMC scripts will work just fine, " +
                    "however running compiled scripts WILL cause system instability, " +
                    "please avoid compiling on the current device."
                );
            }

            // 4GiB Total but under 1GiB Free.
            else if (totalMemoryMB == 4096 && freeMemoryMB < 1024)
            {
                Warning.Write(
                    "BAM Manager (BAMM) determined you running on the minimum RAM requirements.\n" +
                    "Compiling BAMC scripts will work just fine, " +
                    "however you will need to close more applications/processes before attempting to run any compiled scripts.\n" +
                    "Running scripts containing multiple tabs WILL cause system instability, " +
                    "please avoid the use of the 'new-tab' command, " +
                    "and try to free up 1GB of RAM before running compiled scripts."
                );
            }

            // 4GiB Total and 1GiB free.
            else if (totalMemoryMB == 4096 && freeMemoryMB >= 1024)
            {
                if (GlobalConfig.ShowMemoryCheck)
                {
                    Success.WriteSuccessMessage(
                        "BAM Manager (BAMM) determined you running on the minimum RAM requirements, " +
                        "but you have enough free RAM (1GB) for most automation tasks."
                    );
                }
            }

            return true;
        }
        public static bool IsSupportedWindowsVersion()
        {
            return OperatingSystem.IsWindows() && 
                   OperatingSystem.IsWindowsVersionAtLeast(
                       10, 0, 10240
                   );
        }
        public static bool IsSupportedOSXVersion()
        {
            return OperatingSystem.IsMacOSVersionAtLeast(11);
        }
        private void ValidateScript()
        {
            SanitizedScriptPath = scriptFilePath.EndsWith(".py") ? scriptFilePath : string.Empty;
            if (string.IsNullOrEmpty(SanitizedScriptPath)) { 
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to run the file provided as it isn't a python file.\n" +
                        $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n: Raw script file path provided for 'bamm run' was: '{scriptFilePath}'\n\n", 
                    status: 1
                ); 
            }
            PythonValidationResult result = ScriptValidationManager.ValidateSyntax(InterpreterPath, SanitizedScriptPath);
            Spectre.Console.AnsiConsole.Write($"{result.Output}\n");
            if (!result.IsValid) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable run the specified file as it contains syntax errors.\n" +
                        $"If you believe this is a bug, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n{result.Errors}'", 
                    status: 1
                );
            }
        }
        public async Task<bool> RunScript()
        {
            try
            {
                ValidateScript();
                VEnvManager vEnvManager = new(InterpreterPath, scriptFilePath);
                await vEnvManager.RunScriptInVEnv();
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable finish execution of the selected file.\n" +
                        $"If you believe this is a bug, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n{ex.Message}'",
                    status: 1
                );
            }

            return true;
        }
        public async Task<bool> RunScriptFromTranspiler()
        {
            ValidateScript();
            VEnvManager vEnvManager = new(InterpreterPath, scriptFilePath);
            await vEnvManager.RunScriptInVEnv();
            return true;
        }
    }

}
