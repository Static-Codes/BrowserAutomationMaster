using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers
{
    public class UninstallationManager()
    {
        private bool ActionConfirmed { get; set; } = false;
        public void Uninstall()
        {
            ActionConfirmed = (Input.WriteTextAndReturnRawInput("Are you sure you want to uninstall BAM Manager (BAMM)? [y/n]: ") ?? "n").ToLower().Trim().Equals("y");
            if (!ActionConfirmed) { Environment.Exit(0); }

            ActionConfirmed = (Input.WriteTextAndReturnRawInput("This will delete all program files and associated data, please backup before accepting this.  Would you like to continue with the uninstallation process? [y/n]: ") ?? "n").ToLower().Trim().Equals("y");
            if (!ActionConfirmed) { Environment.Exit(0); }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { DoWindowsUninstall(); }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { DoMacUninstall(); }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { DoLinuxUninstall(); }
            else { throw new PlatformNotSupportedException("Unsupported OS."); }
        }
        private static void DoWindowsUninstall() 
        {
            string InstallationDirectory = AppContext.BaseDirectory;
            if (!Path.Exists(InstallationDirectory)) { Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to determine the current directory, please uninstall this application by searching 'Add or remove programs' in your Windows Searchbar.", 1); }
            string UninstallerPath = Path.Combine(InstallationDirectory, "unins000.exe");
            try {
                if (File.Exists(UninstallerPath)) {
                    Process.Start(UninstallerPath);
                    Success.WriteSuccessMessageAndExit("Started uninstaller, BAM Manager (BAMM) will now exit...", 0);
                }
            }
            catch (FileNotFoundException notFound) {
                Errors.WriteErrorAndExit(notFound.Message, 1);
            }
            catch (Win32Exception w32e) {
                Errors.WriteErrorAndExit(w32e.Message, 1);
            }
            catch (ObjectDisposedException notDisposed) {
                Errors.WriteErrorAndExit(notDisposed.Message, 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"{ex.Message}", 1);
            }
        }
        private static void DoMacUninstall() 
        {
            Console.WriteLine(@"To uninstall BAM Manager (BAMM) on macOS:

1.  Delete the BAM Manager executable file:
    	1A. Locate the 'bamm' executable file (wherever you saved it, whether in your 'Downloads' folder, 'Desktop', or 'Applications' folder).
    	1B. Drag the 'bamm' executable file to the Trash, or click 'Move To Trash'.

2.  BAM Manager stores its data (including 'userScripts' and 'compiled' directories) in your user's Library folder. To remove this:
        2A. Open 'Finder'.
        2B. In the menu bar at the top of the screen, click 'Go'.
        2C. Hold down the 'Option (⌥) key' on your keyboard, and a ""Library"" option will appear in the ""Go"" menu. 
	2D. Click 'Library'. (This folder is hidden by default.)
        2E. Navigate to the 'Application Support' folder within Library.
        2F. Locate and drag the 'BrowserAutomationMaster' folder to the Trash. This folder should contain your 'userScripts' and 'compiled' directories.

3.  Empty the Trash:
    	3A. Right-click (or Control-click) on the Trash icon in your Dock and select ""Empty Trash"" to permanently remove the files.");
            Environment.Exit(0);
        }
        private static void DoLinuxUninstall() {
            Console.WriteLine(@"To uninstall BAM Manager (BAMM) on Linux:
    - Run the following command: sudo apt-get remove --purge bamm
    - You may be prompted for your user password. Enter it and press Enter.
    - Confirm any prompts from apt-get to proceed with the uninstallation.");
            Environment.Exit(0);
        }
    }
}
