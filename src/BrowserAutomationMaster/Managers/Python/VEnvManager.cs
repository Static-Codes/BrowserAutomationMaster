using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BrowserAutomationMaster.Messaging;

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
            catch (Exception e) { Errors.WriteErrorAndExit(e.Message, 1); return false; }
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
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n{InterpreterPath}.\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nCommand: '{InterpreterPath} -m venv {VEnvPath}' failed with exit code {createVEnvProcess.ExitCode}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }
            }
            catch (Exception e) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to create a virtual environment for the interpreter:\n{InterpreterPath}.\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nCommand: '{InterpreterPath} -m venv {VEnvPath}' failed.\n\nInterpreter Response:\n{e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }
        }
        public bool RunScriptInVEnv()
        {
            CreateVEnv();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return RunScriptOnWindows(); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return RunScriptOnMacOS(); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return RunScriptOnLinux(); }
            else { throw new PlatformNotSupportedException("Invalid OS."); }
        }

        public bool RunScriptOnWindows() {
            string executablePath;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { executablePath = Path.Combine(VEnvPath, "bin", InterpreterPath); }
            else { executablePath = Path.Combine(VEnvPath, "Scripts", "python.exe"); }
            string scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(scriptFileName)) { scriptFileName = ScriptFilePath; }
            try
            {

                if (!File.Exists(executablePath))
                {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to run '{scriptFileName}', if this issue persists.please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nUnable to find python executable in virtual environment.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }
                var outputLines = new List<string>();
                var errorLines = new List<string>();

                ProcessStartInfo startVEnvStartInfo = new()
                {
                    FileName = $"\"{executablePath}\"",
                    Arguments = $"\"{ScriptFilePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8, // Proactively preventing any encoding issues caused by crossplatform development
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = ParentDirectory,
                };

                using (Process startVEnvProcess = new() { StartInfo = startVEnvStartInfo })
                {
                    startVEnvProcess.EnableRaisingEvents = true; // Enabling events to be reported to the handlers below.

                    // Declaring required event handlers
                    startVEnvProcess.OutputDataReceived += (sender, args) => { if (args.Data != null) { 
                            outputLines.Add(args.Data);  
                            Success.WriteSuccessMessage(args.Data);
                        }
                    };
                    startVEnvProcess.ErrorDataReceived += (sender, args) => { if (args.Data != null)
                        {
                            errorLines.Add(args.Data);
                            //Errors.WriteErrorAndContinue(args.Data);
                        }
                    };

                    startVEnvProcess.Start();
                    startVEnvProcess.BeginOutputReadLine();
                    startVEnvProcess.BeginErrorReadLine();
                    startVEnvProcess.WaitForExit(); // Reminder to add a timeout if scripts start hanging (startVEnvProcess.WaitForExit(60000) // 60 second timeout)

                    if (startVEnvProcess.ExitCode != 0)
                    {
                        string fullStackTrace = string.Join("\n", errorLines);
                        string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                        string userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                                     "If this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues";

                        string detailedLog = $"Error log:\nCommand: '\"{executablePath}\" \"{ScriptFilePath}\"' failed with exit code {startVEnvProcess.ExitCode}\n\n" +
                                             $"Stack Trace:\n{fullStackTrace}\n\n" +
                                             $"{Messaging.Debug.GetPlatformInfoForErrorLog()}";

                        Errors.WriteErrorAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
                    }
                }

                string output = string.Join("\n", outputLines);
                Success.WriteSuccessMessage($"Script Output:\n\n{output}");
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to execute:\n{ScriptFilePath}\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nCommand: '{executablePath} {scriptFileName}' failed.\n\nInterpreter Response:\n{e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }
            return true;
        }

        public bool RunScriptOnLinux()
        {
            return true;
        }

        public bool RunScriptOnMacOS()
        {
            string executablePath = Path.Combine(VEnvPath, "bin", InterpreterPath);
            string scriptFileName = Path.GetFileName(ScriptFilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(scriptFileName)) { scriptFileName = ScriptFilePath; }
            try
            { 
                if (!File.Exists(executablePath)) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to run '{scriptFileName}', if this issue persists.please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nUnable to find python executable in virtual environment.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }
                var outputLines = new List<string>();
                var errorLines = new List<string>();

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash", 
                    // The shell will receive: source "/path/to/venv/bin/activate" && "/path/to/python" "/path/to/script.py"
                    Arguments = $"-c \"source \\\"{ParentDirectory}/venv/bin/activate\\\" && \\\"{executablePath}\\\" \\\"{ScriptFilePath}\\\"\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = ParentDirectory,
                };

                using (Process startVEnvProcess = new() { StartInfo = startInfo })
                {
                    startVEnvProcess.EnableRaisingEvents = true; // Enabling events to be reported to the handlers below.

                    // Declaring required event handlers
                    startVEnvProcess.OutputDataReceived += (sender, args) => { if (args.Data != null) { outputLines.Add(args.Data); } };
                    startVEnvProcess.ErrorDataReceived += (sender, args) => { if (args.Data != null) { errorLines.Add(args.Data); } };

                    startVEnvProcess.Start();
                    startVEnvProcess.BeginOutputReadLine();
                    startVEnvProcess.BeginErrorReadLine();
                    startVEnvProcess.WaitForExit(); // Reminder to add a timeout if scripts start hanging (startVEnvProcess.WaitForExit(60000) // 60 second timeout)

                    if (startVEnvProcess.ExitCode != 0)
                    {
                        string fullStackTrace = string.Join("\n", errorLines);
                        string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                        string userFriendlyMessage = $"BAM Manager (BAMM) was unable to start the virtual environment for runtime.\n\n" +
                                                     "If this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues";

                        string detailedLog = $"Error log:\nCommand: '\"{executablePath}\" {ScriptFilePath}\"' failed with exit code {startVEnvProcess.ExitCode}\n\n" +
                                             $"Stack Trace:\n{fullStackTrace}\n\n" +
                                             $"{Messaging.Debug.GetPlatformInfoForErrorLog()}";

                        Errors.WriteErrorAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
                    }
                }

                string output = string.Join("\n", outputLines);
                Success.WriteSuccessMessage($"Script Output:\n\n{output}");
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to execute:\n{ScriptFilePath}\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nCommand: '{executablePath} {scriptFileName}' failed.\n\nInterpreter Response:\n{e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }
            return true;
        }
    }
}
