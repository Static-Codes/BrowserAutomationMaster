using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers.Python
{
    // Takes in the path to the Virtual Environment and can start and stop it as needed.
    internal class VEnvManager(string InterpreterPath, string ScriptFilePath)
    {
        string VEnvPath { get; set; } = string.Empty;
        private string? ParentDirectory = null;
        private bool VEnvExists()
        {
            try
            {
                ParentDirectory = Path.GetDirectoryName(ScriptFilePath);
                if (ParentDirectory == null) { Environment.Exit(1); }
                VEnvPath = Path.Combine(ParentDirectory, "venv");
                return Directory.Exists(VEnvPath);
            }
            catch (Exception e) { 
                Errors.WriteErrorAndExit(e.Message, 1); 
                return false; 
            }
        }

        public void CreateVEnv()
        {
            if (VEnvExists()) { return; }
            ProcessStartInfo createVEnvStartInfo = new()
            {
                FileName = InterpreterPath,
                Arguments = $"-m venv \"{VEnvPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            try
            {
                Process createVEnvProcess = new() { StartInfo = createVEnvStartInfo };
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
