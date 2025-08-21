using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;

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
                Errors.WriteErrorAndExit(e.Message, 1); 
                return false; 
            }
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

            ProcessStartInfo psi;

            // Global Virtual Environment
            if (global)
                psi = new()
                {
                    FileName = InterpreterPath,
                    Arguments = $"-m venv \"{GetGlobalVEnvPath()}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

            // Project Virtual Environment
            else
                psi = new()
                {
                    FileName = InterpreterPath,
                    Arguments = $"-m venv \"{VEnvPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };


            try
            {
                Process createVEnvProcess = new() { StartInfo = psi };
                createVEnvProcess.Start();
                createVEnvProcess.WaitForExit();

                if (createVEnvProcess.ExitCode != 0 || !VEnvExists()) { // If the process returned an error or the venv is not able to be accessed.
                    Errors.WriteErrorAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n" +
                            $"{InterpreterPath}.\n\nIf this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nCommand: '{InterpreterPath} -m venv {VEnvPath}' " +
                            $"failed with exit code {createVEnvProcess.ExitCode}",
                        status: 1
                    ); 
                }
            }
            catch (Exception e) {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n{InterpreterPath}.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nCommand: '{InterpreterPath} -m venv {VEnvPath}' failed.\n\n" +
                        $"Interpreter Response:\n{e.Message}",
                    status: 1
                );
            }
        }

        public void InstallGlobalPackages()
        {

        }

        public async Task<bool> RunScriptInVEnv()
        {
            CreateVEnv();
            return await RunScript(); // By this point there have been many checks regarding the user's OS, it's safe to proceed.
        }

        public async Task<bool> RunScript()
        {
            if (Linux.IsChromeOS)
                { return true; } // Replace with BrowserStack

            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string executablePath;
            
            if (isWindows)
                executablePath = Path.Combine(VEnvPath, "Scripts", "python.exe");

            else
                executablePath = Path.Combine(VEnvPath, "bin", InterpreterPath);

            string scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;
            
            if (string.IsNullOrEmpty(scriptFileName))
                scriptFileName = ScriptFilePath; 

            try
            {
                if (!File.Exists(executablePath))
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to run '{scriptFileName}', " +
                            $"if this issue persists.please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nUnable to find python executable in virtual environment.",
                        status: 1
                    );

                var outputLines = new List<string>();
                var errorLines = new List<string>();

                ProcessStartInfo startVEnvStartInfo = new()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = ParentDirectory,
                };

                if (isWindows)
                {
                    startVEnvStartInfo.FileName = $"\"{executablePath}\"";
                    startVEnvStartInfo.Arguments = $"\"{ScriptFilePath}\"";
                    startVEnvStartInfo.StandardOutputEncoding = Encoding.UTF8; // Proactively preventing any encoding issues caused by crossplatform development
                    startVEnvStartInfo.StandardErrorEncoding = Encoding.UTF8;
                }
                else
                {
                    startVEnvStartInfo.FileName = "/bin/bash";
                    startVEnvStartInfo.Arguments = $"-c \"source \"{ParentDirectory}/venv/bin/activate\" && \"{executablePath}\" \"{ScriptFilePath}\"";
                }

                using Process startVEnvProcess = new() { StartInfo = startVEnvStartInfo };
                startVEnvProcess.EnableRaisingEvents = true; // Enabling events to be reported to the handlers below.

                // Declaring required event handlers
                startVEnvProcess.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        outputLines.Add(args.Data);
                        Success.WriteSuccessMessage(args.Data + '\n');
                    }
                };
                startVEnvProcess.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        errorLines.Add(args.Data);
                        Errors.Write(args.Data + '\n');
                    }
                };


                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(200));
                startVEnvProcess.Start();
                startVEnvProcess.BeginOutputReadLine();
                startVEnvProcess.BeginErrorReadLine();
                await startVEnvProcess.WaitForExitAsync(cts.Token);

                if (startVEnvProcess.ExitCode != 0)
                {
                    var fullStackTrace = string.Join("\n", errorLines);
                    // string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                    var userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                              $"If this continues, please make a bug report at {ISSUES_LINK}";

                    var detailedLog = $"Error log:\n" +
                                      $"Command: '\"{executablePath}\" \"{ScriptFilePath}\"' " +
                                      $"failed with exit code {startVEnvProcess.ExitCode}\n\n" +
                                      $"Stack Trace:\n{fullStackTrace}\n\n";

                    Errors.WriteErrorAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
                }
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message: $"BAM Manager (BAMM) was unable to execute:\n{ScriptFilePath}\n\n" +
                             $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                             $"Error log:\nCommand: '{executablePath} {scriptFileName}' failed.\n\n" +
                             $"Interpreter Response:\n{e.Message}",
                    status: 1
                );
            }
            return true;
        }
    }
}
