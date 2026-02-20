using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Messaging;
using System.Diagnostics;
using System.Text;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Compilation.Transpiler;
using static BrowserAutomationMaster.Core.Python.BrowserStack.Instance;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;


namespace BrowserAutomationMaster.Core.Python
{
    /// <summary>
    /// This class is responsible for managing all Virtual Environments for BAM Manager.
    /// </summary>
    /// <param name="InterpreterPath">Path to the Python Interpreter Executable.</param>
    /// <param name="ScriptFilePath">Path to the Script being ran in the Virtual Environment.</param>
    internal class VirtualEnvironment(string InterpreterPath, string ScriptFilePath)
    {
        string VEnvPath { get; set; } = string.Empty;
        private string? ParentDirectory = null;

        public string GetBrowserStackSDKPath() 
        {
            ParentDirectory ??= Path.GetDirectoryName(ScriptFilePath);

            // UnixLike: bin/browserstack-sdk
            // Windows: Scripts\browserstack-sdk.exe
            return Path.Combine(
                // Null forgiveness is used here due to the coalesce operation above. 
                GetProjectVEnvPath(ParentDirectory!), 
                Platforms.IsUnixLike ? "bin" : "Scripts",
                Platforms.IsUnixLike ? "browserstack-sdk" : "browserstack-sdk.exe"
            );
        }

        public async Task EnsureBrowserStackSDKIsInstalled(string browserStackSDKPath) 
        {  
            ParentDirectory ??= Path.GetDirectoryName(ScriptFilePath);

            if (!File.Exists(browserStackSDKPath)) 
            {
                // Null forgiveness is used here due to the coalesce operation above. 
                var pipPath = GetProjectVEnvPipPath(ParentDirectory!);

                var bsVersion = PyPi.GetVersion("browserstack-sdk", GetGlobalPythonVersion());

                await InstallIndividualPackage(pipPath, $"browserstack-sdk=={bsVersion}");
                await InstallIndividualPackage(pipPath, $"browserstack-local");
            }
        }

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

                if (ParentDirectory == null) {
                    return false;
                }

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
        /// Returns a VirtualEnvironment object to run the specified script using BrowserStack
        /// </summary>
        /// <param name="scriptFilePath">The desired script to be ran.</param>
        public static VirtualEnvironment CheckBSConfigAtRuntime(string scriptFilePath)
        {
            var config = LoadConfig();
            
            if (config == null) 
            {
                WriteAndExit
                (
                    message: $"Unable to load BrowserStack Config from: {GetBrowserStackConfigPath()}",
                    status: 1
                );
            }

            return new VirtualEnvironment("browserstack-sdk python", scriptFilePath);
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
                if (ExitCode != 0 || !VEnvExists()) {
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

        private static string GetNameOfBrowserInUse(FileStream scriptFileStream) 
        {
            // Helper to check if a byte array exists at a specific index
            static bool IsMatch(byte[] array, int index, byte[] pattern)
            {
                if (index + pattern.Length > array.Length) return false;
                for (int i = 0; i < pattern.Length; i++)
                {
                    if (array[index + i] != pattern[i]) {
                        return false;
                    }
                }
                return true;
            }

            var defaultValue = "firefox";

            // Once this line is found, the execution stops.
            // If no browser is found within the searched area, a default of firefox is returned.
            byte[] stopCheck = "stdout.write('''Made using BAM Manager (BAMM!)"u8.ToArray();

            // If the stream contains the byte representation of either of these checks
            byte[] firefoxCheck = "from webdriver_manager.firefox import GeckoDriverManager"u8.ToArray();
            byte[] chromeCheck = "from webdriver_manager.chrome import ChromeDriverManager"u8.ToArray();

            scriptFileStream.Position = 0;

            using (var ms = new MemoryStream())
            {
                scriptFileStream.CopyTo(ms);
                byte[] fileBytes = ms.ToArray();

                for (int i = 0; i < fileBytes.Length; i++)
                {
                    if (IsMatch(fileBytes, i, stopCheck)) {
                        break;
                    }

                    // Check for Browser matches
                    if (IsMatch(fileBytes, i, firefoxCheck)) {
                        return "firefox";
                    }

                    if (IsMatch(fileBytes, i, chromeCheck)) {
                        return "chrome";
                    }
                }
            }

            return defaultValue;
        }

        private string GetVEnvStartArgs(string pythonPath)
        {
            if (ParentDirectory == null) {
                throw new ArgumentException("ParentDirectory == null");
            }

            if (Platforms.IsWindows || Platforms.IsRaspi) {
                return $"\"{ScriptFilePath}\"";
            }

            if (Platforms.IsARMel || Platforms.IsARMhf) {
                return $"-c \"source '{Path.Combine(ParentDirectory, "venv", "bin", "activate")}'";
            }

            if (Platforms.IsLinux) {
                return $"-c \"source '{ParentDirectory}/venv/bin/activate' && python3 '{ScriptFilePath}'\"";
            }

            if (Platforms.IsMacOS) {
                return $"-c \"source '{ParentDirectory}/venv/bin/activate' && '{pythonPath}' '{ScriptFilePath}'";
            }

            ThrowUnsupportedPlatformException();
            return string.Empty; // Will not be executed.
        }

        // Currently only used in RunScriptWithBrowserStack()
        public async Task InstallIndividualPackage(string pipPath, string packageString) 
        {
            var psi = new ProcessStartInfo()
            {
                FileName = pipPath,
                Arguments = $"install {packageString}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = VEnvPath,
            };

            using Process proc = await ProcessFactory.SpawnProcess(psi, $"install the python package: {packageString}");

            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(proc);
             
            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);
                var userFriendlyMessage = string.Join(NLC, [
                    $"BAM Manager (BAMM) was unable to install the python package: {packageString}.",
                    $"If this continues, please make a bug report at {ISSUES_LINK}"
                ]);

                var detailedLog = string.Join(NLC, [
                    "Error log:",
                    $"Command: {psi.FileName} {psi.Arguments} failed with exit code {ExitCode}",
                    "Stack Trace:",
                    fullStackTrace,
                ]);
                
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        userFriendlyMessage,
                        NLC,
                        detailedLog
                    ]),
                    status: 1
                );
            }


        }

        public async Task InstallProjectPackages()
        {
            var usingBrowserStack = GetBrowserStackStatus();

            WriteSuccessMessage("Installing required project packages in the project's virtual environment, please wait..");
            await Task.Delay(1000);

            if (ParentDirectory == null) {
                WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in InstallProjectPackages()",
                    status: 1
                );
            }

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

            if (Platforms.IsChromeOS || GetBrowserStackStatus() || usingBrowserStack) {
                await RunScriptWithBrowserStack();
            }

            else
                await StartScriptExecution(); // By this point there have been many checks regarding the user's OS, it's safe to proceed.
        }

        private async Task StartScriptExecution()
        {
            if (ParentDirectory == null) {
                WriteAndExit(
                    message:
                        "Unable to install the required Python packages for the current project, please try again.\n" +
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in InstallProjectPackages()",
                    status: 1
                );
            }

            // Special case where OSX needs to be difficult for developers in the pursuit of ease of access for its users.
            // Runs from /bin/bash instead of the VEnv's path.
            var executablePath = GetProjectVEnvPythonPath(ParentDirectory);
            var scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;

            if (string.IsNullOrEmpty(scriptFileName)) {
                scriptFileName = ScriptFilePath;
            }

            if (!File.Exists(executablePath)) {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to run '{scriptFileName}', " +
                        $"if this issue persists.please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:" +
                        "Unable to find the python executable in virtual environment:\n{GetProjectVEnvPath(ParentDirectory)}",
                    status: 1
                );
            }

            //var args = IsMacOS ? GetVEnvStartArgs(pythonPath).Replace("Application Support/", "Application\\ Support/") : GetVEnvStartArgs(pythonPath);
            var args = GetVEnvStartArgs(executablePath);

            var psi = new ProcessStartInfo()
            {
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = ParentDirectory,
            };

            executablePath = Platforms.IsUnixLike ? "/bin/bash" : executablePath;

            SetProcessFileName(ref psi, useCMD: false, fileName: executablePath);

            var scriptFileStream = new FileStream(ScriptFilePath, FileMode.Open, FileAccess.Read);

            var browserName = GetNameOfBrowserInUse(scriptFileStream); 
 

            using Process process = await ProcessFactory.SpawnProcess(
                psi, 
                "start the virtual environment for runtime",
                preventMemoryLeaks: true,
                browserName: browserName
            );

            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(process);

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
            {
                WriteAndExit
                (
                    message:
                        "Unable to run the requested test, please try again.\n" +
                       $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        "Error log:\nParentDirectory == null in VirtualEnvironment.RunScript()",
                    status: 1
                );
            }

            var ProjectName = $"{Path.GetDirectoryName(ParentDirectory)}/" ?? "latest/";
            ANSI.WriteBrowserStackHeader(ProjectName, ScriptFilePath);

            StackConfig = LoadConfig();

            if (StackConfig == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to run the requested test, please try again.",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                        "Error log:",
                        "StackConfig == null in VirtualEnvironment.RunScript()"
                    ]),
                    status: 1
                );
            }

            WriteSuccessMessage($"A valid BrowserStack Config file was found at: {browserStackConfig}\n");
            await Task.Delay(1000);
            
            WriteSuccessMessage("Config Info:");
            Console.WriteLine("{0}{1}", StackConfig, NLC);
            await Task.Delay(750);

            var userChoice = Input.AskForInput("Would you like to use this config for the current test? [y/n]: ");
            await Task.Delay(750);

            if (Input.ConditionRejected(userChoice)) {
                StackConfig = BuildConfig();
            }

            var projectConfigPath = Path.Combine(ParentDirectory, "browserstack.yml");

            // This is either a copy of browserStackConfig's contents or the newly built config above.
            File.WriteAllText(projectConfigPath, StackConfig.ToString());

            var browserStackSDKPath = GetBrowserStackSDKPath();

            // If a BAMC file was compiled with usingBrowserstack = false
            // Then is later ran with usingBrowserstack = true
            // The BrowserStack SDK will not exist in compiled/<project>/venv/bin/
            await EnsureBrowserStackSDKIsInstalled(browserStackSDKPath);

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

            SetProcessFileName(ref psi, useCMD: false, fileName: browserStackSDKPath);

            using var process = await ProcessFactory.SpawnProcess(psi, "start browserstack script execution");

            (var ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(process);
             
            if (ExitCode != 0)
            {
                var fullStackTrace = string.Join("\n", STDErr);

                var userFriendlyMessage = string.Join(NLC, [
                    $"BAM Manager (BAMM) was unable to run the BrowserStack script.",
                    NLC,
                    $"If this continues, please make a bug report at {ISSUES_LINK}"
                ]);

                var detailedLog = string.Join(NLC, [
                    "Error log:",
                    $"Command: {psi.FileName} {psi.Arguments} failed with exit code {ExitCode}",
                    "Stack Trace:",
                    fullStackTrace
                ]);

                WriteAndExit
                (
                    message: string.Join(NLC, [
                        userFriendlyMessage,
                        NLC,
                        detailedLog
                    ]),
                    status: 1
                );
            }

            var baseProjectLink = $"https://automate.browserstack.com/projects";
            var baseMessage = $"To view a recording of this test, please visit:\n{baseProjectLink}";

            var projectName = Path.GetFileNameWithoutExtension(ScriptFilePath);
            string fullMessage = baseMessage;

            if (projectName != null) {
                fullMessage += $"/project/{projectName}/builds";
            }

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
            if (!useCMD && string.IsNullOrEmpty(fileName)) {
                WriteAndExit("A fileName param must be specified for SetProcessFileName when useShell = false", 1);
            }

            psi.FileName = (Platforms.IsWindows, Platforms.IsUnixLike, useCMD) switch 
            {
                (true, false, true) => "cmd.exe",
                (false, true, true) => psi.FileName = "/bin/bash",
                (true, false, false) => fileName,
                (false, true, false) => fileName,
                _ => throw new ArgumentException("Invalid data passed to switch statement in SetProcessFileName")
            };

            // Proactively preventing any encoding issues caused by crossplatform development
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;


        }
    }
}

