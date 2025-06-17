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
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to locate:\n{directory}\nPlease ensure this directory exists.", 1);
            }
            try {
                Directory.Delete(directory, true);
                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully deleted directory:\n{directory}\n");
            }
            catch (IOException e) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nFile: {directory}\n\nException:\n\n{e.Message}", 1);
            }
            catch (UnauthorizedAccessException e) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {directory}\n\nException:\n\n{e.Message}", 1);
            }
            catch (System.Security.SecurityException e) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {directory}\n\nException:\n\n{e.Message}", 1);
            }
            catch (ArgumentException e) {
                Errors.WriteErrorAndExit($"Invalid argument for file path: '{directory}\n\nException:\n\n {e.Message}", 1);
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit($"An unexpected error of type: '{ex.GetType().Name}' occurred while trying to delete file: '{directory}'\n\nException:\n\n{ex.Message}", 1);
            }
        }

        public static string GetDesiredSaveDirectory()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserAutomationMaster", "compiled");
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
