using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;

namespace BrowserAutomationMaster.Managers
{
    public class UninstallationManager()
    {
        public static void Uninstall()
        {
            Errors.Write("This will delete BAM Manager (BAMM) from your system.\n");

            var response = Input.WriteTextAndReturnRawInput("Would you like to continue with the uninstallation process? [y/n]: ");
            var uninstallConfirmed = Input.ConditionAccepted(response);
            
            if (!uninstallConfirmed) 
                Environment.Exit(0);

            string dataMessage = "This will delete all program files and associated data.\n" +
                "Please ensure you've backed up your data before continuing.\n\n" +
                "THIS CANNOT BE REVERSED!\n\n" +
                "To backup your data close BAMM and enter the following command:\n" +
                "bamm backup\n\n";


            response = Input.WriteTextAndReturnRawInput("Do you want to remove all application data? [y/n]: ");
            var removeAppData = Input.ConditionAccepted(response);

            if (removeAppData)
            {
                Errors.Write(dataMessage);
                response = Input.WriteTextAndReturnRawInput("Have you backed up your data? [y/n]: ");
                if (Input.ConditionAccepted(response))
                    DoAppDataDeletion();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DoWindowsUninstall();
            
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                DoMacUninstall();

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DoLinuxUninstall();
            
            else 
                throw new PlatformNotSupportedException("Unsupported OS.");
        }
        private static void DoWindowsUninstall()
        {
            string failureMessage = "BAM Manager (BAMM) was unable to determine the current directory, " +
                    "please uninstall this application by searching " +
                    "'Add or remove programs' in your Windows Searchbar.";
            string installationDirectory = AppContext.BaseDirectory;

            if (!Path.Exists(installationDirectory))
                Errors.WriteErrorAndExit(message: failureMessage, status: 1);

            string uninstallerPath = Path.Combine(installationDirectory, "unins000.exe");
            try
            {
                if (File.Exists(uninstallerPath))
                {
                    Process.Start(uninstallerPath);
                    Success.WriteSuccessMessageAndExit(
                        message: "Started uninstaller, BAM Manager (BAMM) will now exit...",
                        exitCode: 0
                    );
                }
            }
            catch (FileNotFoundException notFound)
            {
                Errors.WriteErrorAndExit(message: notFound.Message, status: 1);
            }
            catch (Win32Exception w32e)
            {
                Errors.WriteErrorAndExit(message: w32e.Message, status: 1);
            }
            catch (ObjectDisposedException notDisposed)
            {
                Errors.WriteErrorAndExit(message: notDisposed.Message, status: 1);
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(message: $"{ex.Message}", status: 1);
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
                    Errors.Write(message);
                }
            }
        }
    }
}
