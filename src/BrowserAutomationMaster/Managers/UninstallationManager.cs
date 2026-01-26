using BrowserAutomationMaster.Messaging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers
{
    public class UninstallationManager()
    {
        public static void Uninstall()
        {
            Write("This will delete BAM Manager (BAMM) from your system.\n");

            var response = Input.AskForInput("Would you like to continue with the uninstallation process? [y/n]: ");
            var uninstallConfirmed = Input.ConditionAccepted(response);
            
            if (!uninstallConfirmed) 
            {
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
                DoLinuxUninstall();
            }
            
            else 
                throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms");
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
        private static void DoLinuxUninstall()
        {

            string platform = Input.WriteListFromOptions(["Debian Based", "Fedora Based", "Other"], noun: "distro");

            string debianMessage =
                "To uninstall BAM Manager (BAMM) on Debian:\n" +
                "   - Run the following command:\nsudo apt-get remove --purge bamm -y\n\n" +
                "   - You may be prompted for your user password, enter it and press enter.";

            string fedoraMessage =
                "To uninstall BAM Manager (BAMM) on Fedora:\n" +
                "   - Run the following command" +
                "   - sudo dnf remove bamm -y";

            var message = platform switch
            {
                "Debian Based" => debianMessage,
                "Fedora Based" => fedoraMessage,
                "Other" => "Unsupported, please manually uninstall",
                _ => "Invalid choice"
            };

            WriteMessage(message);
            Environment.Exit(0);
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
