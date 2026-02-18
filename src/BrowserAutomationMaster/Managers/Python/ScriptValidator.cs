using System.Diagnostics;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Messaging.Errors;


namespace BrowserAutomationMaster.Managers.Python
{
    // A struct is easier to maintain than an inline tuple regarding ScriptValidator.ValidateSyntaxAsync.
    public readonly struct ValidationResult(bool isValid, string output, string errors, int exitCode)
    {
        public bool IsValid { get; } = isValid;
        public string Output { get; } = output;
        public string Errors { get; } = errors;
        public int ExitCode { get; } = exitCode;
    }


    // Validates a script using py_compile 
    // (Built in, already cross platform, and lightweight since it compiles directly to bytecode)
    public static class ScriptValidator
    {
        public static ValidationResult ValidateSyntax(string pythonExecutablePath, string scriptPath)
        {
            if (string.IsNullOrEmpty(pythonExecutablePath))
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to determine the path of the installed python instance.",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}."
                    ]),
                    status: 1
                );
            }

            if (!File.Exists(scriptPath)) {
                WriteErrorAndReturnBool("BAM Manager (BAMM) was unable to locate the specified file, please try again.", false);
            }

            try
            {
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


                process.Start();
                process.WaitForExit();
                bool isValid = process.ExitCode == 0;
                
                if (isValid) {
                    return new ValidationResult(
                        isValid,
                        process.StandardOutput.ReadToEnd(), 
                        "No errors detected", 
                        process.ExitCode
                    );
                }

                return new ValidationResult(
                    isValid: false, 
                    "No output detected.",
                    process.StandardError.ReadToEnd(),
                    process.ExitCode
                );
            }
            catch (Exception ex)
            {
                return new ValidationResult(
                    isValid: false,
                    output: "No output detected",
                    errors: $"Unable to validate selected file:\n{ex.Message}\nExecutable Path: {pythonExecutablePath}",
                    exitCode: -1
                );
            }
        }
    }
}
