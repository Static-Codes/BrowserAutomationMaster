using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.InstanceManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;


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
        private bool VEnvExists()
        {

            try
            {
                ParentDirectory = Path.GetDirectoryName(ScriptFilePath);

                if (ParentDirectory == null)
                    return false;

                VEnvPath = Path.Combine(ParentDirectory, "venv");

                return Directory.Exists(VEnvPath);
            }
            
            catch (Exception e)
            {
                WriteAndExit(e.Message, 1);
                return false;
            }
        }
        /// <summary>
        /// Returns a VEnvManager to run the specified script using BrowserStack
        /// </summary>
        /// <param name="scriptFilePath">The desired script to be ran.</param>
        public static VEnvManager CheckBSConfigAtRuntime(string scriptFilePath)
        {
            var config = LoadConfig();
            if (config == null)
                WriteAndExit($"Unable to load BrowserStack Config from:{NLC}{GetBrowserStackConfigPath()}", 1);
            return new VEnvManager("browserstack-sdk python", scriptFilePath);
        }

        /// <summary>
        /// Creates a Virtual Environment used for project packages.
        /// </summary>
        /// <param name="global">If you wish to create the global config.  | If false a Virtual Environment is created in the current project directory.</param>
        /// <returns>True if the VEnv exists | False if not.</returns>
        public async Task CreateVEnv()
        {
            if (VEnvExists()) {
                return;
            }
            
            var psi = new ProcessStartInfo
            {
                FileName = InterpreterPath,
                Arguments = $"-m venv \"{VEnvPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            try
            {
                using Process process = await ProcessFactory.SpawnProcess(psi, "create a virtual environment with the interpreter", writeSTDInOut: false, runSync: true);
                (int ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(process);
                
                // If the process returned an error or the venv is not able to be accessed.
                if (ExitCode != 0 || !VEnvExists())
                {
                    WriteAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to create a virtual environment with the interpreter:\n" +
                            $"{InterpreterPath}.\n\nIf this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nCommand: {psi.FileName} {psi.Arguments} " +
                            $"failed with exit code {ExitCode}",
                        status: 1
                    );
                }
                WriteSuccessMessage("Successfully created Project Virtual Environment!\n");
            }


            catch (Exception e)
            {

                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n{InterpreterPath}.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nCommand: {psi.FileName} {psi.Arguments} failed.\n\n" +
                        $"Interpreter Response:\n{e.Message}",
                    status: 1
                );
            }
        }

        private string GetVEnvStartArgs(string pythonPath)
        {
            if (ParentDirectory == null)
                throw new ArgumentException("ParentDirectory == null");

            if (Platforms.IsWindows || Platforms.IsRaspi)
                return $"\"{ScriptFilePath}\"";

            if (Platforms.IsARMel || Platforms.IsARMhf)
                return $"-c \"source '{Path.Combine(ParentDirectory, "venv", "bin", "activate")}'";

            if (Platforms.IsLinux)
                return $"-c \"source '{ParentDirectory}/venv/bin/activate' && python3 '{ScriptFilePath}'\"";

            if (Platforms.IsOSX)
                return $"-c \"source '{ParentDirectory}/venv/bin/activate' && '{pythonPath}' '{ScriptFilePath}'";


            ThrowUnsupportedPlatformException();
            return string.Empty; // Will not be executed.
        }

        public async Task InstallProjectPackages()
        {
            var usingBrowserStack = GetBrowserStackStatus();

            WriteSuccessMessage("Installing required project packages in the project's virtual environment, please wait..");
            await Task.Delay(1000);

            if (ParentDirectory == null)
                WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in InstallProjectPackages()",
                    status: 1
                );

            var pipExecutable = GetProjectVEnvPipPath(ParentDirectory);

            var requirementsFilePath = GetProjectRequirementsPath(ParentDirectory);

            var installCMD = $"install -r \"{requirementsFilePath}\"";
            

            var psi = new ProcessStartInfo()
            {
                Arguments = installCMD,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            SetProcessFileName(ref psi, useCMD: false, fileName: pipExecutable);

            // 3 minutes 20 seconds normally or 10 minutes if using browserstack.
            int timeout = usingBrowserStack ? 600 : 200;

            // Uses async if browserstack is selected else sync call of async func.
            using Process proc = usingBrowserStack ?
                await ProcessFactory.SpawnProcess(psi, "start the virtual environment for runtime", whiteOutput: true, timeout: timeout) :
                ProcessFactory.SpawnProcess(psi, "start the virtual environment for runtime", whiteOutput: true, timeout: timeout).Result;


            await Task.Delay(1000);

            (var ExitCode, List<string> STDOut, List<string> STDErr) = ProcessFactory.GetProcessResponse(proc).Result;

            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);
                // string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                var userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                          $"If this continues, please make a bug report at {ISSUES_LINK}";

                var detailedLog = "Error log:\n" +
                                  $"Command: {installCMD} failed with exit code {ExitCode}\n\n" +
                                  $"Stack Trace:\n{fullStackTrace}\n\n";

                WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
            }
        }

        public async Task RunScriptInVEnv(bool usingBrowserStack = false)
        {
            await CreateVEnv();

            await InstallProjectPackages();
            await Task.Delay(1000);

            if (Platforms.IsChromeOS || GetBrowserStackStatus() || usingBrowserStack)
                await RunScriptWithBrowserStack();

            else
                await StartScriptExecution(); // By this point there have been many checks regarding the user's OS, it's safe to proceed.
        }

        private async Task StartScriptExecution()
        {
            if (ParentDirectory == null)
                WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in InstallProjectPackages()",
                    status: 1
                );

            // Special case where OSX needs to be difficult for developers in the pursuit of ease of access for its users.
            // Runs from /bin/bash instead of the VEnv's path.
            var pythonPath = GetProjectVEnvPythonPath(ParentDirectory);
            var scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;

            if (string.IsNullOrEmpty(scriptFileName))
                scriptFileName = ScriptFilePath;

            if (!File.Exists(pythonPath))
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to run '{scriptFileName}', " +
                        $"if this issue persists.please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nUnable to find the python executable in virtual environment:\n{GetProjectVEnvPath(ParentDirectory)}",
                    status: 1
                );

            //var args = IsOSX ? GetVEnvStartArgs(pythonPath).Replace("Application Support/", "Application\\ Support/") : GetVEnvStartArgs(pythonPath);
            var args = GetVEnvStartArgs(pythonPath);

            var psi = new ProcessStartInfo()
            {
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ParentDirectory,
            };

            pythonPath = Platforms.IsUnixLike ? "/bin/bash" : pythonPath;

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

                WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
            }
        }

        public async Task RunScriptWithBrowserStack()
        {
            if (string.IsNullOrEmpty(ParentDirectory))
                WriteAndExit
                (
                    message:
                        "Unable to run the requested test, please try again.\n" +
                       $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in VEnvManager.RunScript()",
                    status: 1
                );

            var ProjectName = $"{Path.GetDirectoryName(ParentDirectory)}/" ?? "latest/";
            AnsiManager.WriteBrowserStackHeader(ProjectName, ScriptFilePath);

            StackConfig = LoadConfig();

            if (StackConfig == null)
                WriteAndExit
                (
                    message:
                        "Unable to run the requested test, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nStackConfig == null in VEnvManager.RunScript()",
                    status: 1
                );

            WriteSuccessMessage($"A valid BrowserStack Config file was found at: {browserStackConfig}\n");
            await Task.Delay(1000);
            
            WriteSuccessMessage("Config Info:");
            Console.WriteLine($"{StackConfig}\n");
            await Task.Delay(1000);

            var userChoice = Input.AskForInput("Would you like to use this config for the current test? [y/n]: ");
            await Task.Delay(1000);

            if (Input.ConditionRejected(userChoice))
                StackConfig = BuildConfig();

            var projectConfigPath = Path.Combine(ParentDirectory, "browserstack.yml");

            // This is either a copy of browserStackConfig's contents or the newly built config above.
            File.WriteAllText(projectConfigPath, StackConfig.ToString());

            // UnixLike: bin/browserstack-sdk
            // Windows: Scripts/browserstack-sdk.exe
            var browserStackExecutable = Path.Combine(
                GetProjectVEnvPath(ParentDirectory),
                Platforms.IsUnixLike ? "bin" : "Scripts",
                Platforms.IsUnixLike ? "browserstack-sdk" : "browserstack-sdk.exe"
            );

            var browserStackArgs = $"python \"{ScriptFilePath}\"";


            var psi = new ProcessStartInfo()
            {
                Arguments = browserStackArgs,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = VEnvPath,
            };

            psi.Environment.Add("GIT_PYTHON_REFRESH", "quiet");

            SetProcessFileName(ref psi, useCMD: false, fileName: browserStackExecutable); 

            using Process proc = await ProcessFactory.SpawnProcess(psi, "start browserstack script execution");

            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(proc);
             
            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);
                var userFriendlyMessage = $"BAM Manager (BAMM) was unable to run the BrowserStack script.\n\n" +
                                          $"If this continues, please make a bug report at {ISSUES_LINK}";

                var detailedLog = "Error log:\n" +
                                  $"Command: {psi.FileName} {psi.Arguments} failed with exit code {ExitCode}\n\n" +
                                  $"Stack Trace:\n{fullStackTrace}\n\n";
                WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
            }

            var baseProjectLink = $"https://automate.browserstack.com/projects";
            var baseMessage = $"To view a recording of this test, please visit:\n{baseProjectLink}";

            var projectName = Path.GetFileNameWithoutExtension(ScriptFilePath);
            string fullMessage = baseMessage;

            if (projectName != null)
                fullMessage += $"/project/{projectName}/builds";

            WriteSuccessMessage(fullMessage);
        }

        /// <summary>
        /// Sets the FileName argument with the associated ProcessStartInfo
        /// </summary>
        /// <param name="psi">The ProcessStartInfo object.</param>
        /// <param name="useCMD">Whether or not to use (cmd.exe or bin/bash) for the execution of the command, defaults to true.</param>
        /// <param name="fileName">The FileName you wish to execute as a string, defaults to null. (Must be provided if you specify useCMD=false) </param>
        public static void SetProcessFileName(ref ProcessStartInfo psi, bool useCMD = true, string? fileName = null)
        {
            if (!useCMD && string.IsNullOrEmpty(fileName))
                WriteAndExit("A filename param must be specified for SetProcessFileName when useShell = false", 1);

            // Set for Windows regardless of global status
            if (Platforms.IsWindows && useCMD)
            {
                psi.FileName = "cmd.exe";
                // Proactively preventing any encoding issues caused by crossplatform development
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }


            else if (Platforms.IsWindows && !useCMD)
            {
                psi.FileName = fileName;
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }

            else if (Platforms.IsUnixLike && !useCMD)
                psi.FileName = fileName;


            else if (Platforms.IsUnixLike)
                psi.FileName = "/bin/bash";
        }
    }
}
