// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

﻿using System;
using System.Diagnostics;
using System.IO;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Messaging.Errors;

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
                WriteAndExit(
                    message:
                        "BAM Manager (BAMM) was unable to determine the path of the installed python instance, " +
                        "if this continues, please make an issue on github.", 
                    status: 1
                );
            }

            if (!File.Exists(scriptPath)) {
                WriteErrorAndReturnBool("BAM Manager (BAMM) was unable to locate the specified file, please try again.", false);
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

            try
            {
                process.Start();
                process.WaitForExit();
                bool isValid = process.ExitCode == 0;
                
                if (isValid) {
                    return new PythonValidationResult(
                        isValid,
                        process.StandardOutput.ReadToEnd(), 
                        "No errors detected", 
                        process.ExitCode
                    );
                }

                return new PythonValidationResult(
                    isValid: false, 
                    "No output detected.",
                    process.StandardError.ReadToEnd(),
                    process.ExitCode
                );
            }
            catch (Exception ex)
            {
                return new PythonValidationResult(
                    isValid: false,
                    output: "No output detected",
                    errors: $"Unable to validate selected file:\n{ex.Message}\nExecutable Path: {pythonExecutablePath}",
                    exitCode: -1
                );
            }
        }
    }
}
