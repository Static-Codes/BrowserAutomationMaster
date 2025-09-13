using System.Diagnostics;
using System.Text;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using BrowserAutomationMaster.Managers.Python.BrowserStack;

namespace BrowserAutomationMaster.Managers.Python
{
    /// <summary>
    /// This class is responsible for managing all Virtual Environments for BAM Manager.
    /// </summary>
    /// <param name="InterpreterPath">Path to the Python Interpreter Executable.</param>
    /// <param name="ScriptFilePath">Path to the Script being ran in the Virtual Environment.</param>
    internal class VEnvManager(string InterpreterPath, string ScriptFilePath)
    {
        string VEnvPath { get; set; } = string.Empty;
        private string? ParentDirectory = null;

        /// <summary>
        /// Checks if the Virtual Environment used for individual project packages exists.
        /// </summary>
        /// <param name="global">If you wish to check the global config.  | If false the current project directory is checked for a Virtual Environment.</param>
        /// <returns>True if the VEnv exists | False if not.</returns>
        private bool VEnvExists(bool global = false)
        {
            if (global)
                return Directory.Exists(GetGlobalVEnvPath());

            try
            {
                ParentDirectory = Path.GetDirectoryName(ScriptFilePath);
                if (ParentDirectory == null)
                    return false;

                VEnvPath = Path.Combine(ParentDirectory, "venv");
                return Directory.Exists(VEnvPath);
            }
            catch (Exception e) { 
                Errors.WriteAndExit(e.Message, 1); 
                return false; 
            }
        }
        /// <summary>
        /// Ensure the browser
        /// </summary>
        public static VEnvManager CheckBSConfigAtRuntime(string scriptFilePath)
        {
            var config = InstanceManager.LoadConfig();
            if (config == null)
                Errors.WriteAndExit($"Unable to load BrowserStack Config from:\n{GetBrowserStackConfigPath()}", 1);
            return new VEnvManager("browserstack-sdk python", scriptFilePath);
        }

        /// <summary>
        /// Creates a Virtual Environment used for project packages.
        /// </summary>
        /// <param name="global">If you wish to create the global config.  | If false a Virtual Environment is created in the current project directory.</param>
        /// <returns>True if the VEnv exists | False if not.</returns>
        public void CreateVEnv(bool global = false)
        {
            if (VEnvExists(global))
                return;


            Console.WriteLine(VEnvPath);
            Console.WriteLine(GetGlobalVEnvPath());

            Warning.Write("Creating Global Virtual Environment, please wait up to 60 seconds for this process to complete.\n");

            var psi = new ProcessStartInfo
            {
                FileName = InterpreterPath,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            // Global Virtual Environment
            if (global)
                psi.Arguments = $"-m venv \"{GetGlobalVEnvPath()}\"";


            // Project Virtual Environment
            else
            {
                if (string.IsNullOrEmpty(VEnvPath)) { }
                psi.Arguments = $"-m venv \"{VEnvPath}\"";
            }

            try
            {
                using Process process = ProcessFactory.SpawnProcess(psi, "create a virtual environment with the interpreter", writeSTDInOut: false, runSync: true).Result;
                (int ExitCode, List<string> STDOut, List<string> STDErr) = ProcessFactory.GetProcessResponse(process).Result;
                
                // If the process returned an error or the venv is not able to be accessed.
                if (ExitCode != 0 || !VEnvExists(global)) 
                { 
                    var path = !string.IsNullOrEmpty(VEnvPath) ? VEnvPath : GetGlobalVEnvPath();
                    Errors.WriteAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to create a virtual environment with the interpreter:\n" +
                            $"{InterpreterPath}.\n\nIf this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nCommand: '{InterpreterPath} -m venv {path}' " +
                            $"failed with exit code {ExitCode}",
                        status: 1
                    ); 
                }
                Success.WriteSuccessMessage("Successfully created Global Virtual Environment!\n");
            }


            catch (Exception e) 
            {
                Errors.WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n{InterpreterPath}.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nCommand: '{InterpreterPath} -m venv {VEnvPath}' failed.\n\n" +
                        $"Interpreter Response:\n{e.Message}",
                    status: 1
                );
            }
        }

        private string GetVEnvStartArgs(string pythonPath)
        {
            if (IsUnixLike)
                return $"-c \"source \"{ParentDirectory}/venv/bin/activate\" && \"{pythonPath}\" \"{ScriptFilePath}\"";

            if (IsWindows)
                return $"\"{ScriptFilePath}\"";

            Errors.ThrowUnsupportedPlatformException();
            return string.Empty; // Will not be executed.
        }

        

        public static async Task InstallGlobalPackages()
        {
            Success.WriteSuccessMessage("Installing Browserstack Python SDK...");

            var baseMessage =
                    "Unable to install the Browserstack Python SDK.\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n" +
                    "Error Log:\n";

            var pipExecutablePath = GetGlobalVEnvPipPath();

            var version = PackageManager.Get("browserstack-sdk", GetPythonVersion());

            var installCMD = $"install browserstack-sdk=={version}";
            var checkCMD = $"{pipExecutablePath} show browserstack-sdk";

            var errMessage = baseMessage + $"Command:\n{installCMD} returned a non-zero status, indicating an unspecified error.";

            var psi = new ProcessStartInfo
            {
                FileName = pipExecutablePath,
                Arguments = checkCMD,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            var process = await ProcessFactory.SpawnProcess(psi, "install the BrowserStack Python SDK", timeout: 10);
            
            // the discards are STDOut and STDErr
            (var ExitCode, List<string> _, List<string> _) = await ProcessFactory.GetProcessResponse(process);


            switch (ExitCode)
            {
                case 0:
                    Warning.Write("Browserstack's Python SDK is installed in the Global Virtual Environment, continuing...");
                    break;

                default:
                    Errors.WriteAndExit(errMessage, 1);
                    break;
            }

        }

        public async Task InstallProjectPackages()
        {
            Success.WriteSuccessMessage("Installing required project packages, please wait..");
            await Task.Delay(200);

            if (ParentDirectory == null)
                Errors.WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory is null in InstallProjectPackages()",
                    status: 1
                );

            var pipExecutable = GetProjectVEnvPipPath(ParentDirectory);
            var requirementsFilePath = GetProjectRequirementsPath(ParentDirectory);

            // Will add -c on unix
            var installCMD = $"{(IsUnixLike ? $"-c {pipExecutable}" : "")} install -r {requirementsFilePath}";

            

            ProcessStartInfo psi = new()
            {
                Arguments = $"{installCMD}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ParentDirectory,
            };
            SetProcessFileName(ref psi, useCMD: false, fileName: pipExecutable);

            using Process proc = await ProcessFactory.SpawnProcess(psi, "start the virtual environment for runtime");
            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(proc);

            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);
                // string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                var userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                          $"If this continues, please make a bug report at {ISSUES_LINK}";

                var detailedLog = "Error log:\n" +
                                  $"Command: {installCMD} failed with exit code {ExitCode}\n\n" +
                                  $"Stack Trace:\n{fullStackTrace}\n\n";

                Errors.WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
            }
        }

        public async Task<bool> RunScriptInVEnv()
        {
            CreateVEnv();
            await InstallProjectPackages();
            return await RunScript(); // By this point there have been many checks regarding the user's OS, it's safe to proceed.
        }

        public async Task<bool> RunScript()
        {

            if (IsChromeOS || GetBrowserStackStatus())
                // Replace with BrowserStack
                return true;
            

            if (ParentDirectory == null)
                Errors.WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory is null in InstallProjectPackages()",
                    status: 1
                );

            var pythonPath = GetProjectVEnvPythonPath(ParentDirectory);
            var scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;

            if (string.IsNullOrEmpty(scriptFileName))
                scriptFileName = ScriptFilePath;

            if (!File.Exists(pythonPath))
                Errors.WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to run '{scriptFileName}', " +
                        $"if this issue persists.please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nUnable to find the python executable in virtual environment:\n{GetGlobalVEnvPath()}",
                    status: 1
                );

            ProcessStartInfo psi = new()
            {
                Arguments = GetVEnvStartArgs(pythonPath),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ParentDirectory,
            };
            SetProcessFileName(ref psi, useCMD: false, fileName: pythonPath);

            using Process proc = await ProcessFactory.SpawnProcess(psi, "start the virtual environment for runtime");

            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(proc);

            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);
                // string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                var userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                          $"If this continues, please make a bug report at {ISSUES_LINK}";

                var detailedLog = "Error log:\n" +
                                  $"Command: {psi.FileName} {psi.Arguments} failed with exit code {ExitCode}\n\n" +
                                  $"Stack Trace:\n{fullStackTrace}\n\n";

                Errors.WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
            }


            return true;
        }

        /// <summary>
        /// Sets the FileName argument with the associated ProcessStartInfo
        /// </summary>
        /// <param name="psi">The ProcessStartInfo object.</param>
        /// <param name="useCMD">(Windows Only) Whether or not to use cmd.exe for the execution of the command, defaults to true.</param>
        /// <param name="fileName">The FileName you wish to execute as a string, defaults to null. (Must be provided if you specify useCMD=false) </param>
        private static void SetProcessFileName(ref ProcessStartInfo psi, bool useCMD = true, string? fileName = null)
        {
            if (!useCMD && string.IsNullOrEmpty(fileName))
                Errors.WriteAndExit("A filename param must be specified for SetProcessFileName when useShell = false", 1);

            // Set for Windows regardless of global status
            if (IsWindows && useCMD)
            {
                psi.FileName = "cmd.exe";
                // Proactively preventing any encoding issues caused by crossplatform development
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }


            else if (IsWindows && !useCMD)
            {
                psi.FileName = fileName;
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }

            else if (IsUnixLike)
                psi.FileName = "/bin/bash";
        }
    }
}
