using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.AppManager.OS.MacOS;

namespace BrowserAutomationMaster.Managers.Python
{
    // This class is responsible for executing the compiled python scripts.
    public class RuntimeManager(string scriptFilePath)
    {
        private string SanitizedScriptPath { get; set; } = string.Empty;
        
        public string InterpreterPath { get; } = GetInterpreterFromPath();

        private static string[] BuildScriptMenu(List<string> scriptPaths)
        {
            string[] menu = new string[scriptPaths.Count];
            for (int i = 0; i < scriptPaths.Count; i++)
            {
                string? fileName = null;
                try { 
                    fileName = Path.GetFileName(scriptPaths[i]); 
                }
                catch (Exception ex) { 
                    Errors.Write(ex.Message); 
                    continue; 
                }
                menu[i] = $"{i + 1}. {fileName} -> {scriptPaths[i]}";
            }
            return [.. menu.Where(a => a != null)];
        }
        
        public static void DoRuntimeCheck()
        {
            HasEnoughMemory();
            CPUInfoManager cpuInfoManager = new();
            if (!cpuInfoManager.HasEnoughCores())
            {
                Errors.WriteAndExit(
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
            if (Platforms.IsWindows)
                return Win.GetInterpreterPath();

            // Path to full executable is required to replicate the expected behavior due to OSX being built off BSD 
            if (Platforms.IsUnixLike || Platforms.IsChromeOS)
                return "python3";

            throw new PlatformNotSupportedException(
                string.Join(NLC, [
                    "Unsupported OS.\n" +
                    "BAM Manager (BAMM) currently supports:\n" +
                    "Windows 10/11\n" +
                    "Linux\n" +
                    "MacOS 11+\n"
                ])
            );
        }
        
        private static string GetUserScriptChoice(List<string> scriptPaths, string[] menu)
        {
            while (true)
            {
                var message = "Unable to parse selected menu option.\n" +
                              $"If this continues please make a bug report at {ISSUES_LINK}" +
                              "Error Log:\nchoice returned null.";

                string rawChoice = Input.WriteListFromOptions(menu);
                string? choice = Parser.GetFileNumber(rawChoice);

                if (choice == null)
                    Errors.WriteAndExit(message, 1);

                if (int.TryParse(choice, out int result) && result >= 1 && result <= scriptPaths.Count)
                    return scriptPaths[result - 1];

                Errors.Write($"Invalid option, please choose a number between 1 and {scriptPaths.Count}\n");
            }
        }
        
        public static string HandleUserScriptChoice()
        {
            string saveDirectory = DirectoryManager.GetDesiredSaveDirectory();

            try
            {
                var scriptPaths = GetCompiledScriptPaths(saveDirectory);

                if (scriptPaths.Count == 0)
                    Errors.WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to find any compiled scripts, " +
                            $"please ensure you have atleast one compiled script before selecting this option.\n\n" +
                            $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nNo compiled scripts found in {saveDirectory}",
                        status: 1
                    );

                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully detected {scriptPaths.Count} scripts.\n");

                string[] menu = BuildScriptMenu(scriptPaths);

                return GetUserScriptChoice(scriptPaths, menu);
            }
            catch (Exception e)
            {
                Errors.WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to find any compiled scripts, " +
                        $"please ensure you have atleast one compiled script before selecting this option.\n\n" +
                        $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n {e.Message}",
                    status: 1
                );
                return string.Empty; // This won't execute due to WriteAndExit, but satisfies compiler
            }
        }
        
        public static bool HasEnoughMemory()
        {
            Dictionary<string, double> memoryInfo = MemoryInfoManager.RunCheck();
            
            if (memoryInfo.Count != 5)
                Errors.WriteAndExit(
                    $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                    $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\nMemoryInfoManager.HasEnoughMemory() returned an invalid dictionary.",
                    status: 1
                );
            
            memoryInfo.TryGetValue("totalMemoryMB", out double totalMemoryMB);
            //memoryInfo.TryGetValue("usedMemoryMB", out double usedMemoryMB); // Will be used in a later update to display various info.
            memoryInfo.TryGetValue("freeMemoryMB", out double freeMemoryMB);
            //memoryInfo.TryGetValue("usedPercent", out double usedPercent);
            //memoryInfo.TryGetValue("freePercent", out double freePercent);

            // Less than 2GiB Total
            if (totalMemoryMB < 2048)
            {
                Errors.WriteAndExit(
                    "BAM Manager (BAMM) determined you are running below the minimum RAM requirements to properly use bamm.\n" +
                    "Please run BAMM on a system with atleast 4GB of DDR3 RAM.",
                    status: 1
                );
            }

            // Less than 512MiB Free
            if (freeMemoryMB < 512)
            {
                Errors.WriteAndExit(
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
                // I hate nested conditionals, but this allows for a graceful passthrough
                if (GlobalConfig.ShowMemoryCheck)
                    Success.WriteSuccessMessage(
                        "BAM Manager (BAMM) determined you running on the minimum RAM requirements, " +
                        "but you have enough free RAM (1GB) for most automation tasks."
                    );
            }

            return true;
        }
        
        public static bool IsSupportedWindowsVersion()
        {
            return 
                OperatingSystem.IsWindows() && 
                OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240);
        }
        
        public static bool IsSupportedOSXVersion() { return OperatingSystem.IsMacOSVersionAtLeast(11); }
        
        private void ValidateScript()
        {
            SanitizedScriptPath = scriptFilePath.EndsWith(".py") ? scriptFilePath : string.Empty;
            if (string.IsNullOrEmpty(SanitizedScriptPath))
                Errors.WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to run the file provided as it isn't a python file.\n" +
                        $"If you believe this is an error, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n: Raw script file path provided for 'bamm run' was: '{scriptFilePath}'\n\n", 
                    status: 1
                ); 
            
            PythonValidationResult result = ScriptValidationManager.ValidateSyntax(InterpreterPath, SanitizedScriptPath);

            if (result.IsValid)
                return;
                
            if (Platforms.IsOSX)
                HandleVEnvExceptions(result.Errors);  // Will exit if an exception is found.

            Errors.WriteAndExit(
                message:
                    $"BAM Manager (BAMM) was unable run the specified file as it contains syntax errors.\n" +
                    $"If you believe this is a bug, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\n{result.Errors}'",
                status: 1
            );
            
        }

        // Readd error handling
        public async Task<bool> RunScript(bool usingBrowserstack = false)
        {
            // For the current commit this is intentionally unwrapped from the try catch block to invoke an Exception and have its StackTrace automatically output for debugging purposes.
            ValidateScript();
            var vEnvManager = usingBrowserstack switch
            {
                true => VEnvManager.CheckBSConfigAtRuntime(scriptFilePath),
                false => new VEnvManager(InterpreterPath, scriptFilePath),
            };
            await vEnvManager.RunScriptInVEnv(usingBrowserStack: usingBrowserstack);

            //try
            //{
            //    ValidateScript();
            //    VEnvManager vEnvManager = new(InterpreterPath, scriptFilePath);
            //    await vEnvManager.RunScriptInVEnv();
            //}
            //catch (Exception ex)
            //{
            //    Errors.WriteAndExit(
            //        message:
            //            $"BAM Manager (BAMM) was unable finish execution of the selected file.\n" +
            //            $"If you believe this is a bug, please make a bug report at {ISSUES_LINK}\n\n" +
            //            $"Error log:\n{ex.Message}'",
            //        status: 1
            //    );
            //}

            return true;
        }
    }

}
