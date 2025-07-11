using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers
{
    class DirectoryManager
    {
        public static void DeleteDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) { return; }
            if (!Directory.Exists(directory)) {
                Errors.WriteErrorAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to locate:\n{directory}\nPlease ensure this directory exists.", 
                    status: 1
                );
            }
            try {
                Directory.Delete(directory, true);
                Success.WriteSuccessMessage(
                    message: $"BAM Manager (BAMM) successfully deleted directory:\n{directory}\n"
                );
            }
            catch (IOException e) {
                Errors.WriteErrorAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                             $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (UnauthorizedAccessException e) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                        $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (System.Security.SecurityException e) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                        $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (ArgumentException e) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"Invalid argument for file path: '{directory}\n\n" +
                        $"Exception:\n\n {e.Message}", 
                    status: 1
                );
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
                    message: 
                        $"An unexpected error of type: '{ex.GetType().Name}' occurred while trying to delete file: '{directory}'\n\n" +
                        $"Exception:\n\n{ex.Message}", 
                    status: 1
                );
            }
        }

        public static string GetDesiredSaveDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "BrowserAutomationMaster", 
                    "compiled"
                );
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string userScriptDirectory = UserScriptManager.GetUserScriptDirectory();
                string parentDirectory = Path.GetDirectoryName(userScriptDirectory) ?? Environment.CurrentDirectory;
                return Path.Combine(parentDirectory, "compiled");
            }

            else { throw new PlatformNotSupportedException("Unsupported OS."); }
        }
    }
}
