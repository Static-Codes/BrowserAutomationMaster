using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.OS.Unix.Linux;
using System.Diagnostics;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Common.ProcessFactory;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Input;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.DistroManager;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.Functions;

namespace BrowserAutomationMaster.Core.Utilities
{
    public class AppRemovalUtility()
    {
        private static void DoAppDataDeletion()
        {
            try
            {
                DeleteDirectory(AppDataDirectory);
            }
            catch (Exception e)
            {
                Write
                (
                    string.Join(NLC, [
                        "Unable to delete app data for BAM Manager (BAMM).",
                        "Please remove this directory manually:",
                        AppDataDirectory,
                        $"Please make a bug report at {ISSUES_LINK}",
                        "", // Creates the double newline before the error log
                        "Error Log:",
                        e.Message
                    ])
                );
                
            }
        }
        private static async Task DoLinuxUninstall()
        {
            (string symLinkPath, _) = RunCommand("which", "bamm");
            symLinkPath = symLinkPath.Replace("\n", "");

            // string binaryPath = "/usr/local/bin/bamm";

            string symLinkNotFound = $"Unable to locate the symlink to the BAMM executable at: {symLinkPath}";
            
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

            // Debug Only
            // Console.WriteLine($"symLinkPath: {symLinkPath}");

            var binaryPath = GetAbsolutePathOfSymLink(symLinkPath);

            // Debug Only
            // Console.WriteLine($"binaryPath: {binaryPath}");
            // Console.WriteLine($"File.Exists(binaryPath): {File.Exists(binaryPath)}");
            // Console.WriteLine($"Path.Exists(binaryPath): {Path.Exists(binaryPath)}");
            
            
            if (!File.Exists(symLinkPath)) 
            {
                WriteAndExit(
                    message: symLinkNotFound,
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
                    psi.Arguments = $"-c \"sudo rm {binaryPath}\"";
                    break;

                
                case InstallationType.Package:
                    psi.Arguments = string.Join(' ', [
                        "-c",
                        "\"sudo", 
                        $"{Platforms.CurrentDistribution.PackageManager}",
                        $"{Platforms.CurrentDistribution.UninstallCommand}",
                        "bamm\""
                    ]);
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

            using var process = await SpawnProcess(psi, "attempting to uninstall", timeout: 60);
            var (ExitCode, STDOut, STDErr) = await GetProcessResponse(process);

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
        private static void DoMacUninstall()
        {
            var message = string.Join(NLC, [
                "To uninstall BAM Manager (BAMM) on macOS:",
                "   - 1. Locate the 'bamm' executable file (wherever you saved it)",
                "   - 2. Drag the 'bamm' executable file to the Trash, or click 'Move To Trash'."
            ]);

            Warning.Write(message);
            Environment.Exit(0);
        }
        private static void DoWindowsUninstall()
        {
            string failureMessage = string.Join(" ", [
                "BAM Manager (BAMM) was unable to determine the current directory,",
                "please uninstall this application by searching",
                "'Add or remove programs' in your Windows Searchbar."
            ]);

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
            catch (Exception ex)
            {
                WriteAndExit(message: $"{ex.Message}", status: 1);
            }
        }
        public static async Task Uninstall()
        {
            Write
            (
                string.Concat([
                    "This will delete BAM Manager (BAMM) from your system.", 
                    NLC
                ])
            );

            var response = AskForInput("Would you like to continue with the uninstallation process? [y/n]: ");
            var uninstallConfirmed = ConditionAccepted(response);
            
            if (!uninstallConfirmed) {
                Environment.Exit(0);
            }
            
            string dataMessage = "This will delete all program files and associated data.\n" +
                "Please ensure you've backed up your data before continuing.\n\n" +
                "THIS CANNOT BE REVERSED!\n\n" +
                "To backup your data close BAMM and enter the following command:\n" +
                "bamm backup\n\n";


            response = AskForInput("Do you want to remove all application data? [y/n]: ");
            var removeAppData = ConditionAccepted(response);

            if (removeAppData)
            {
                Write(dataMessage);
                response = AskForInput("Have you backed up your data? [y/n]: ");

                if (ConditionRejected(response)) {
                    Write("Performing backup, please wait.");
                    ArchiveAppDataDirectory();
                }

                DoAppDataDeletion();
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
                throw new PlatformNotSupportedException("Failed to set all values for members in PlatformInfo.Platforms");
            }
        }
    }
}
