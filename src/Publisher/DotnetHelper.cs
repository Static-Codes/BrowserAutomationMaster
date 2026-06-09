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

using System.Diagnostics;
using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace Publisher 
{
    public class DotnetHelper 
    {
        public static async Task<bool> DotnetIsInstalled() 
        {
            var psi = new ProcessStartInfo()
            {
                FileName = GetShellPath(),
                Arguments = $"{GetShellArg()} \"{GetWhichCommand()} {GetDotnetBinaryName()}\"",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            using var process = await ProcessFactory.SpawnProcess(psi, "checking for the dotnet SDK binary", timeout: 20);
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);

            if (ExitCode != 0) 
            {
                var errorLog = (STDErr != null) switch {
                    true => string.Join(NLC, STDErr),
                    false => $"the {GetWhichCommand()} returned a non zero status code: {ExitCode}"
                };

                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to locate a dotnet SDK binary in your system path.",
                        "Please ensure the dotnet SDK is installed, and is added to your system path.",
                        "Error Log:",
                        errorLog
                    ]),
                    status: 1
                );
            }
            
            if (STDOut.Count == 1 && STDOut[0].Contains("dotnet")) 
            {
                return true;
            }

            return false;
            
        }

        /// <summary>
        /// <returns>Returns the "which" or "where" command assuming it's in the system path.</returns>
        /// </summary>
        public static string GetWhichCommand()
        {
            return Platforms.IsWindows switch {
                true => "where.exe",
                false => "which"
            };
        }

        /// <summary>
        /// <returns>Returns the name of the dotnet SDK binary assuming it's in the system path.</returns>
        /// </summary>
        public static string GetDotnetBinaryName()
        {
            return Platforms.IsWindows switch {
                true => "dotnet.exe",
                false => "dotnet"
            };
        }


        /// <summary>
        /// <returns>Returns the path to the system's shell.</returns>
        /// </summary>
        public static string GetShellPath() 
        {
            return Platforms.IsWindows switch {
                true => "cmd.exe",
                false => "/bin/bash"
            };
        }

        /// <summary>
        /// <returns>Returns the argument for the system's shell to interpret the following text as commands.</returns>
        /// </summary>
        public static string GetShellArg() 
        {
            return Platforms.IsWindows switch {
                true => "/c",
                false => "-c",
            };
        }
    }
}