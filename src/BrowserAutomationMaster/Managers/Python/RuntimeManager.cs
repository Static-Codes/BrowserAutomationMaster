using System.Runtime.InteropServices;
using BrowserAutomationMaster.AppManager.OS;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers.Python
{
    // This class is responsible for executing the compiled python scripts.
    public class RuntimeManager(string scriptFilePath) // VEnvManager.RunScriptInVEnv(); SHOULD WORK but it needs to be passed InterpreterPath, ScriptFilePath
    {
        private string SanitizedScriptPath { get; set; } = string.Empty;
        public static OSPlatform Platform { get; } = GetPlatform();
        public string InterpreterPath { get; } = GetInterpreterFromPath();

        private static OSPlatform GetPlatform()
        {
            if (!Environment.Is64BitOperatingSystem) {
                Errors.WriteErrorAndExit("Due to a variety of factors, BAM Manager (BAMM) is unable to run on x86 (32bit) CPUs.  Ensure your CPU supports 64 bit operating systems, and try again.", 1);
            }
            if (RuntimeInformation.OSArchitecture == Architecture.Arm64) { Warning.Write("BAM Manager (BAMM) supports ARM64 architecture, but performance for browser automation can vary widely depending on your specific ARM processor. Some lower-power ARM systems may experience degraded performance."); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return OSPlatform.Windows; }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return OSPlatform.OSX; }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return OSPlatform.Linux; }
            else { throw new PlatformNotSupportedException("Unsupported OS."); }
        }
        private static string GetInterpreterFromPath()
        {
            if (Platform == OSPlatform.Windows)
            {
#pragma warning disable IDE0079
#pragma warning disable CA1416 // Since RuntimeInformation.IsOSPlatform() is executed when a RuntimeManager instance is created, an eception is created, see below
                return Windows.GetInterpreterPath();
#pragma warning restore CA1416 // This call site is reachable on all platforms. 'Windows.GetInterpreterPath()' is only supported on: 'windows'. 
#pragma warning restore IDE0079
            }
            if (Platform == OSPlatform.OSX || Platform == OSPlatform.Linux) { return "python3"; }
            throw new PlatformNotSupportedException("Unsupported OS.");
        }
        public static bool HasEnoughMemory()
        {
            Dictionary<string, double> memoryInfo = MemoryInfoManager.RunCheck();
            if (memoryInfo.Count != 5)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nMemoryInfoManager.CheckForWindows() returned an invalid dictionary.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }
            memoryInfo.TryGetValue("totalMemoryMB", out double totalMemoryMB);
            memoryInfo.TryGetValue("usedMemoryMB", out double usedMemoryMB);
            memoryInfo.TryGetValue("freeMemoryMB", out double freeMemoryMB);
            memoryInfo.TryGetValue("usedPercent", out double usedPercent);
            memoryInfo.TryGetValue("freePercent", out double freePercent);

            // Less than 2GiB Total
            if (totalMemoryMB < 2048)
            {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) determined you are running below the minimum RAM requirements to properly use bamm.\nPlease run BAMM on a system with atleast 4GB of DDR3 RAM.", 1);
            }

            // Less than 512MiB Free
            if (freeMemoryMB < 512)
            {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) determined you don't have enough free RAM to continue.\n\nPlease ensure atleast 512MB of RAM is free before trying to run BAMM again.", 1);
            }

            // Less than 4GiB Total but between 512MiB and 1GiB Free.
            else if (totalMemoryMB < 4096 && freeMemoryMB < 1024)
            {
                Warning.Write("BAM Manager (BAMM) determined you are running below the minimum RAM requirements.\nCompiling BAMC scripts will work just fine, however running compiled scripts WILL cause system instability, please avoid compiling on the current device.");
            }

            // 4GiB Total but under 1GiB Free.
            else if (totalMemoryMB == 4096 && freeMemoryMB < 1024)
            {
                Warning.Write("BAM Manager (BAMM) determined you running on the minimum RAM requirements.\nCompiling BAMC scripts will work just fine, however you will need to close more applications/processes before attempting to run any compiled scripts.\nRunning scripts containing multiple tabs WILL cause system instability, please avoid the use of the 'new-tab' command, and try to free up 1GB of RAM before running compiled scripts.");
            }

            // 4GiB Total and 1GiB free.
            else if (totalMemoryMB == 4096 && freeMemoryMB >= 1024)
            {
                Success.WriteSuccessMessage("BAM Manager (BAMM) determined you running on the minimum RAM requirements, but you have enough free RAM (1GB) for most automation tasks.");
            }
            return true;
        }
        public static void DoRuntimeCheck()
        {
            HasEnoughMemory();
            CPUInfoManager cpuInfoManager = new();
            if (!cpuInfoManager.HasEnoughCores()) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) requires atleast a 2 core cpu, unfortunately your CPU is not powerful enough for modern browser automation, if you believe this is an error, please submit a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nBAM Manager (BAMM) detected {cpuInfoManager.Cores} physical CPU cores.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }

        }
        private void ValidateScript()
        {
            SanitizedScriptPath = scriptFilePath.EndsWith(".py") ? scriptFilePath : string.Empty;
            if (string.IsNullOrEmpty(SanitizedScriptPath)) { Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to run the file provided as it isn't a python file.\nIf you believe this is an error, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n: Raw script file path provided for 'bamm run' was: '{scriptFilePath}'\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1); }
            PythonValidationResult result = ScriptValidationManager.ValidateSyntax(InterpreterPath, SanitizedScriptPath);
            Console.WriteLine(result.Output);
            if (!result.IsValid) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable run the specified file as it contains syntax errors.\nIf you believe this is a bug, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n{result.Errors}'\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }
        }

        public static string HandleUserScriptChoice()
        {
            string saveDirectory = DirectoryManager.GetDesiredSaveDirectory();
            List<string> compiledScriptDirectories = [];
            string[] pythonFilePaths = [];
            string usersChoice = string.Empty;
            try {
                compiledScriptDirectories.AddRange(Directory.GetDirectories(saveDirectory).Where(directory => !directory.EndsWith("venv", StringComparison.CurrentCultureIgnoreCase)));
                if (compiledScriptDirectories.Count == 0) { Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to find any compiled scripts, please ensure you have atleast one compiled script before selecting this option.\n\nIf you believe this is an error, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n: No compiled scripts found in {saveDirectory}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1); }
                string menu = string.Empty;
                int index = 0;
                foreach (string scriptDirectory in compiledScriptDirectories) {
                    // Modify this to check scriptDirectory for .py files or pass the actual script.
                    pythonFilePaths = [..pythonFilePaths.Concat([..Directory.GetFiles(scriptDirectory).Where(file => file.EndsWith(".py"))])];
                    foreach (string pythonFilePath in pythonFilePaths)
                    {
                        string fileName = Path.GetFileName(pythonFilePath);
                        if (string.IsNullOrEmpty(fileName) || !File.Exists(Path.Combine(scriptDirectory, pythonFilePath))) { continue; }

                        if (!menu.Contains(pythonFilePath)) { 
                            index++; 
                            menu += $"{index}. {fileName} -> {pythonFilePath}\n";
                        }
                    }
                }
                if (index == 0) { Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to find any compiled scripts, please ensure you have atleast one compiled script before selecting this option.\n\nIf you believe this is an error, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n: No compiled scripts found in {saveDirectory}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1); }
                
                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully detected {index} scripts.\n");
                while (true)
                {
                    string choice = Input.WriteTextAndReturnRawInput($"Please choose the number corresponding to your desired script from the list below:\n\n{menu}") ?? string.Empty;
                    if (string.IsNullOrEmpty(choice) || !int.TryParse(choice, out int result)) {
                        Errors.WriteErrorAndContinue($"Invalid option, please choose a number between 1 and {index}\n");
                        continue;
                    }
                    usersChoice = pythonFilePaths[result - 1];
                    break;
                }
            }
            catch (Exception e) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to find any compiled scripts, please ensure you have atleast one compiled script before selecting this option.\n\nIf you believe this is an error, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n {e.Message}", 1);
            }
            return usersChoice;
        }

        public void RunScript()
        {
            ValidateScript();
            VEnvManager vEnvManager = new(InterpreterPath, scriptFilePath);
            vEnvManager.RunScriptInVEnv();
        }
    }

}
