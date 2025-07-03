using System.Diagnostics;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers.Python
{
    // A struct is easier to maintain than an inline tuple regarding ScriptValidator.ValidateSyntaxAsync.
    public readonly struct PythonValidationResult(bool isValid, string output, string errors, int exitCode)
    {
        public bool IsValid { get; } = isValid;
        public string Output { get; } = output;
        public string Errors { get; } = errors;
        public int ExitCode { get; } = exitCode;
    }


    // Validates a script using py_compile (Built in, already cross platform, and lightweight since it compiles directly to bytecode)
    public static class ScriptValidationManager
    {
        public static PythonValidationResult ValidateSyntax(string pythonExecutablePath, string scriptPath)
        {
            if (string.IsNullOrEmpty(pythonExecutablePath))
            {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to determine the path of the installed python instance, if this continues, please make an issue on github.", 1);
            }

            if (!File.Exists(scriptPath))
            {
                Errors.WriteErrorAndReturnBool("BAM Manager (BAMM) was unable to locate the specified file, please try again.", false);
            }


            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExecutablePath,
                    Arguments = $"-m py_compile \"{scriptPath}\"",
                    RedirectStandardOutput = true, // Only STDErr/STDOut are required.
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(scriptPath)) ?? string.Empty
                }
            };

            //Console.WriteLine($"{pythonExecutablePath} -m py_compile \"{scriptPath}\"");
            //string outputBuilder = string.Empty;
            //string errorBuilder = string.Empty;
            //process.EnableRaisingEvents = true;
            //process.OutputDataReceived += (sender, e) => { if (e.Data != null) outputBuilder += $"{e.Data}\n"; };
            //process.ErrorDataReceived += (sender, e) => { if (e.Data != null) errorBuilder += $"{e.Data}\n"; };

            try
            {
                process.Start();
                process.WaitForExit();
                bool isValid = process.ExitCode == 0;
                
                if (isValid) {
                    string output = process.StandardOutput.ReadToEnd();
                    return new PythonValidationResult(isValid, output, "No errors detected", process.ExitCode);
                }
                string errors = process.StandardError.ReadToEnd();
                return new PythonValidationResult(false, "No output detected.", errors, process.ExitCode);
            }
            catch (Exception ex)
            {
                return new PythonValidationResult(
                    false,
                    "No output detected",
                    $"Unable to validate selected file:\n{ex.Message}\nExecutable Path: {pythonExecutablePath}",
                    -1
                );
            }
        }
    }
}
