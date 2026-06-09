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

﻿using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using BrowserAutomationMaster.Messaging;
using System.ComponentModel;
using System.Diagnostics;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.DistroManager;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers
{
    public class UninstallationManager()
    {
        public static async Task Uninstall()
        {
            Write("This will delete BAM Manager (BAMM) from your system.\n");

            var response = Input.AskForInput("Would you like to continue with the uninstallation process? [y/n]: ");
            var uninstallConfirmed = Input.ConditionAccepted(response);
            
            if (!uninstallConfirmed) {
                Environment.Exit(0);
            }
            
            string dataMessage = "This will delete all program files and associated data.\n" +
                "Please ensure you've backed up your data before continuing.\n\n" +
                "THIS CANNOT BE REVERSED!\n\n" +
                "To backup your data close BAMM and enter the following command:\n" +
                "bamm backup\n\n";


            response = Input.AskForInput("Do you want to remove all application data? [y/n]: ");
            var removeAppData = Input.ConditionAccepted(response);

            if (removeAppData)
            {
                Write(dataMessage);
                response = Input.AskForInput("Have you backed up your data? [y/n]: ");
                if (Input.ConditionAccepted(response)) {
                    DoAppDataDeletion();
                }
            }

            if (Platforms.IsWindows) {
                DoWindowsUninstall();
            }
            
            else if (Platforms.IsMacOS) {
                DoMacUninstall();
            }

            else if (Platforms.IsLinux) {
                await DoLinuxUninstall();
            }
            
            else {
                throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms");
            }
        }
        
        private static void DoWindowsUninstall()
        {
            string failureMessage = "BAM Manager (BAMM) was unable to determine the current directory, " +
                    "please uninstall this application by searching " +
                    "'Add or remove programs' in your Windows Searchbar.";
            string installationDirectory = AppContext.BaseDirectory;

            if (!Path.Exists(installationDirectory)) {
                WriteAndExit(message: failureMessage, status: 1);
            }
            
            string uninstallerPath = Path.Combine(installationDirectory, "unins000.exe");
            try
            {
                if (File.Exists(uninstallerPath))
                {
                    Process.Start(uninstallerPath);
                    WriteSuccessMessageAndExit(
                        message: "Started uninstaller, BAM Manager (BAMM) will now exit...",
                        exitCode: 0
                    );
                }
            }
            catch (FileNotFoundException notFound)
            {
                WriteAndExit(message: notFound.Message, status: 1);
            }
            catch (Win32Exception w32e)
            {
                WriteAndExit(message: w32e.Message, status: 1);
            }
            catch (ObjectDisposedException notDisposed)
            {
                WriteAndExit(message: notDisposed.Message, status: 1);
            }
            catch (Exception ex)
            {
                WriteAndExit(message: $"{ex.Message}", status: 1);
            }
        }
        private static void DoMacUninstall()
        {
            var message =
                "To uninstall BAM Manager (BAMM) on macOS:\n" +
                "   - 1. Locate the 'bamm' executable file (wherever you saved it)\n" +
                "   - 2. Drag the 'bamm' executable file to the Trash, or click 'Move To Trash'.";

            WriteMessage(message, isWarning: true);
            Environment.Exit(0);
        }
        private static async Task DoLinuxUninstall()
        {
            
            string binaryPath = "/usr/local/bin/bamm";

            string binaryNotFound = string.Join(NLC, [
                $"Unable to locate the the BAMM executable at expected path: {binaryPath}",
                "Please try executing:",
                "which bamm",
                NLC,
                "If the above command returns a path, please execute:",
                "sudo rm 'path/to/bamm'"
            ]);
            
            // Attempts to prompt the user for a distro choice if Platforms.CurrentDistribution is null.
            CheckLinuxDistro();

            // CheckLinuxDistro exits if Platform.CurrentDistribution is null.
            var invalidDistro = Platforms.CurrentDistribution!.Equals(Distros.Unknown);

            if (invalidDistro) {
                WriteAndExit(
                    message: invalidDistroMessage, 
                    status: 1
                );
            }

            if (!File.Exists(binaryPath)) 
            {
                WriteAndExit(
                    message: binaryNotFound,
                    status: 1
                );
            }

            
            var psi = new ProcessStartInfo() {
                FileName = Platforms.CurrentDistribution!.ShellPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
            };

            switch (Platforms.CurrentDistribution!.InstallationType)  {
                case InstallationType.Binary:
                    psi.Arguments = $"-c sudo rm {binaryPath}";
                    break;

                
                case InstallationType.Package:
                    psi.Arguments = $"-c {Platforms.CurrentDistribution.UninstallCommand}";
                    break;
                
                default:
                    Console.WriteLine("Default case not implemented in UninstallatioNManager.DoLinuxUninstall()");
                    break;
            };


            if (psi.Arguments.Contains("sudo")) {
                Warning.Write(
                    string.Join(NLC, [
                        $"Superuser privileges are required due to the location of the installed binary: {binaryPath}",
                        "Please enter your password when prompted for the following command to execute:",
                        $"{psi.FileName} {psi.Arguments}"
                    ])
                );
            }

            using var process = await ProcessFactory.SpawnProcess(psi, "attempting to uninstall", timeout: 60);
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);

            if (ExitCode != 0) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "An exception occured while attempting to uninstall the BAMM binary.",
                        "The command: ",
                        $"{psi.FileName} {psi.Arguments}",
                        $"returned a non zero exit code: {ExitCode}",
                        "Error Log:",
                        string.Join(NLC, STDErr)
                    ]),
                    status: 1
                );
            }

            var acknowledgementMessage = string.Join(NLC, [
                "Successfully uninstalled the BAMM binary.",
                "Thank you for trying BAMM, I greatly appreciate it! - Static"
            ]);

            WriteSuccessMessageAndExit(acknowledgementMessage, 0);
        }

        private static void DoAppDataDeletion()
        {
            try
            {
                DeleteDirectory(AppDataDirectory);
            }
            catch (Exception e)
            {
                {
                    var message =
                        "Unable to delete app data for BAM Manager (BAMM).\n" +
                        "Please remove this directory manually:\n" +
                        $"{AppDataDirectory}\n" +
                        $"Please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error Log:\n{e.Message}";
                    Write(message);
                }
            }
        }
    }
}
