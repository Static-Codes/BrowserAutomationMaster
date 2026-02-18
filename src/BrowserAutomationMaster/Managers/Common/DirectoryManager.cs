using BrowserAutomationMaster.Managers.Messaging;
using System.IO.Compression;
using System.Security;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.Messaging.Errors;
using static BrowserAutomationMaster.Managers.Messaging.Success;

namespace BrowserAutomationMaster.Managers.Common
{
    public class DirectoryManager
    {
        public static string AppDataDirectory { get; private set; } = GetAppDataDirectory();
        public static void ArchiveAppDataDirectory(string compression = "zip", string? outputPath = null)
        {

            var backupPath = GetDefaultBackupPath();
            if (File.Exists(backupPath))
            {
                var message = $"A backup of the AppData used by BAM Manager (BAMM) already exists at: {backupPath}\n";
                Warning.Write(message);

                var response = Input.AskForInput("Would you like to overwrite it? [y/n]: ");

                if (Input.ConditionRejected(response)) {
                    Environment.Exit(0);
                }

                DeleteFile(backupPath);
            }

            try
            {
                switch (compression)
                {
                    case "zip" when outputPath == null:
                        if (!Directory.Exists(AppDataDirectory))
                        {
                            WriteAndExit($"Unable to create backup file, directory doesn't exist at: {AppDataDirectory}", 1);
                        }

                        if (string.IsNullOrEmpty(backupPath))
                        {
                            var message = "Would you like to create a backup in the current directory? [y/n]: ";
                            var response = Input.AskForInput(message);
                            
                            if (Input.ConditionRejected(response)) {
                                Environment.Exit(0);
                            }

                            backupPath = Environment.CurrentDirectory;
                        }


                        ZipFile.CreateFromDirectory(AppDataDirectory, backupPath, CompressionLevel.Optimal, false);
                        WriteSuccessMessage($"Successfully created backup at: {backupPath}");
                        break;
                }
            }
            catch (Exception ex)
            {
                var message =
                    "Unable to create a backup file.\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}" +
                    $"Error Log:\n{ex.Message}";

                WriteAndExit(message, 1);
            }
        }

        public static void DeleteDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) 
            { 
                return; 
            }

            if (!Directory.Exists(directory))
            {
                WriteAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to locate:{NLC}{directory}{NLC}Please ensure this directory exists.",
                    status: 1
                );
            }
            try
            {
                Directory.Delete(directory, true);
                WriteSuccessMessage($"BAM Manager (BAMM) successfully deleted directory:{NLC}{directory}{NLC}");
            }

            catch (IOException e)
            {
                WriteAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.{NLC}" +
                             $"File: {directory}{NLC}Exception:{NLC}{e.Message}",
                    status: 1
                );
            }
            catch (UnauthorizedAccessException e)
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.{NLC}" +
                        $"File: {directory}\n\nException:\n\n{e.Message}",
                    status: 1
                );
            }
            catch (SecurityException e)
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                        $"File: {directory}\n\nException:\n\n{e.Message}",
                    status: 1
                );
            }
            catch (ArgumentException e)
            {
                WriteAndExit(
                    message:
                        $"Invalid argument for file path: '{directory}\n\n" +
                        $"Exception:\n\n {e.Message}",
                    status: 1
                );
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message:
                        $"An unexpected error of type: '{ex.GetType().Name}' occurred while trying to delete file: '{directory}'\n\n" +
                        $"Exception:\n\n{ex.Message}",
                    status: 1
                );
            }
        }

        public static void DeleteFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) { return; }
            if (!File.Exists(path))
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to locate:\n" +
                        $"{path}\n" +
                        $"Please ensure this directory exists.",
                    status: 1
                );
            }
            try
            {
                File.Delete(path);
                WriteSuccessMessage(
                    message: $"BAM Manager (BAMM) successfully deleted file: {path}\n"
                );
            }
            catch (IOException)
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                        $"File: {path}\n",
                    status: 1
                );
            }
            catch (UnauthorizedAccessException)
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {path}\n",
                    status: 1
                );
            }
            catch (SecurityException)
            {
                WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {path}\n",
                    status: 1
                );
            }
            catch (ArgumentException)
            {
                WriteAndExit(
                    message: $"Invalid argument for file path: '{path}'\n",
                    status: 1
                );
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message:
                        $"An unexpected error of type: '{ex.GetType().Name}' " +
                        $"occurred while trying to delete file: '{path}'\n",
                    status: 1
                );
            }
        }
        
        public static void EnsureDirectoryExists(string path)
        {
            if (Directory.Exists(path)) {
                return;
            }

            try { 
                Directory.CreateDirectory(path); 
            }
            catch (Exception)
            {
                Write
                (
                    message: string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to create the userScripts directory:",
                        path
                    ])
                );
            }
        }

        public static string GetAppDataDirectory()
        {

            string appName = "BrowserAutomationMaster";

            if (Platforms.IsWindows)
            {
                return GetAppDataWindows(appName);
            }

            else if (Platforms.IsMacOS)
            {
                return GetAppDataMacOS(appName);
            }

            else if (Platforms.IsLinux || Platforms.IsChromeOS || Platforms.IsRaspi)
            {
                return GetAppDataLinux(appName);
            }

            else {
                throw new PlatformNotSupportedException($"Unsupported OS");
            }
        }

        public static string GetBinariesDirectory() { return Path.Combine(AppDataDirectory, "binaries"); }
        
        public static string GetBrowserStackDirectory() { return Path.Combine(AppDataDirectory, "browserstack"); }

        public static string GetBrowserStackConfigPath() { return Path.Combine(GetBrowserStackDirectory(), "browserstack.yml"); }

        public static string GetBAMConfigDirectory() { return Path.Combine(AppDataDirectory, "config"); }

        public static string GetDesiredSaveDirectory() { return Path.Combine(AppDataDirectory, "compiled"); }

        private static string GetDefaultBackupPath(string compression = "zip")
        {
            if (Platforms.IsUnixLike && !HasDisplayVarSet())
            {
                return Path.Combine("~", $"BAMM-Backup.{compression}");
            }

            try
            {
                var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktopDir))
                {
                    return desktopDir;
                }

                var fileName = $"BAMM-Backup.{compression}";
                return Path.Combine(desktopDir, fileName);
            }
            catch
            {
                var message = "Unable to get the default backup directory, please ensure you have a Desktop Environment installed.";
                return WriteErrorAndReturnEmptyString(message);
            }
        }
    
        public static string GetExtensionsDirectory() { return Path.Combine(AppDataDirectory, "extensions"); }    
        public static string GetGUIDaemonPath() 
        { 
            return Path.Combine(AppDataDirectory, "guiDaemon.py"); 
        }
      
        public static string GetGUIDirectoryPath() 
        { 
            return Path.Combine(AppDataDirectory, "gui"); 
        }

        public static string GetGUIScriptsPath()
        {
            return Path.Join(GetGUIDirectoryPath(), "scripts", "/");
        }

        public static string GetGUIStylesPath()
        {
            return Path.Join(GetGUIDirectoryPath(), "styles", "/");
        }

        public static string GetGUISidebarCSSPath()
        {
            return Path.Combine(GetGUIStylesPath(), "sidebar.css");
        }

        public static string GetMainGUIPage(bool includeProtocol = false) 
        {
            if (includeProtocol)
            {
                // Opted for Join over Combine since a oot check is done to prevent protocol or file prefixes.
                return Path.Join("file://", GetGUIDirectoryPath(), "index.html");
            }
            else
            {
                return Path.Combine(GetGUIDirectoryPath(), "index.html");
            }
        }

        public static string GetGUIZipPath() 
        { 
            return Path.Combine(AppDataDirectory, "gui.zip"); 
        }
        
        public static string GetProjectRequirementsPath(string ParentDirectory)
        {
            return Path.Combine(ParentDirectory, "requirements.txt");
        }

        public static string GetProjectVEnvPath(string ParentDirectory) 
        { 
            return Path.Combine(ParentDirectory, "venv"); 
        }

        public static string GetProjectVEnvPythonPath(string ParentDirectory)
        {
            if (Platforms.IsWindows) {
                return Path.Combine(GetProjectVEnvPath(ParentDirectory), "Scripts", "python.exe");
            }

            if (Platforms.IsUnixLike) {
                return Path.Combine(GetProjectVEnvPath(ParentDirectory), "bin", "python3");
            }

            ThrowUnsupportedPlatformException();
            return ""; // This wont be returned however rosyln being static in nature, doesn't know this.
        }

        public static string GetProjectVEnvPipPath(string ParentDirectory)
        {
            if (Platforms.IsWindows) {
                return Path.Combine(GetProjectVEnvPath(ParentDirectory), "Scripts", "pip.exe");
            }

            if (Platforms.IsUnixLike) {
                return Path.Combine(GetProjectVEnvPath(ParentDirectory), "bin", "pip");
            }

            ThrowUnsupportedPlatformException();
            return ""; // This wont be returned however rosyln being static in nature, doesn't know this.
        }

        public static string GetPythonWheelDirectory() { return Path.Combine(AppDataDirectory, "wheels"); }
        
        public static string GetTemporaryNeofetchPath(){
            return Path.Combine(AppDataDirectory, "neofetch.tmp");
        }
        
        public static string GetSourceDirectory() 
        {
            // This will be modified if it does not resolve from DirectoryManager.
            var AppDataPath = AppDataDirectory;
                
            if (AppDataPath == null) {
                Write("DirectoryManager.AppDataDirectory could not be resolved.");
                AppDataPath = Input.AskForInput("Please enter the directory to save the BAMM codebase.");
            }

            if (!Directory.Exists(AppDataPath)) 
            {
                WriteAndExit("DirectoryManager.AppDataPath could not be resolved, please try another directory.", 1);
            }

            return Path.Join(AppDataPath, "source");
        }

        public static string GetSourceBuildsDirectory() 
        {
            var sourceDir = GetSourceDirectory();
            var sourceBuildsDir = Path.Join(sourceDir, "builds");
            EnsureDirectoryExists(sourceBuildsDir);
            return sourceBuildsDir;
        }
        
        // ~/.config/BrowserAutomationMaster
        private static string GetAppDataLinux(string appName)
        {
            string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(homeDirectory)) {
                homeDirectory = Environment.GetEnvironmentVariable("HOME");
            }

            // Fallback for second check
            if (string.IsNullOrEmpty(homeDirectory)) {
                WriteAndExit(
                    message:
                        "BAM Manager (BAMM) could not determine home directory on Linux.\n" +
                        "Press any key to exit...",
                    status: 1
                );
            }

            // Ensures compliance with XDG specs using $XDG_CONFIG_HOME or $HOME/.config
            string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome)) {
                configHome = Path.Combine(homeDirectory, ".config");
            }
            
            string appDataDirectory = Path.Combine(configHome, appName);
            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }

        // ~/Library/Application Support/BrowserAutomationMaster
        private static string GetAppDataMacOS(string appName)
        {
            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            if (string.IsNullOrEmpty(homeDirectory))
            {
                throw new InvalidOperationException(
                    "Could not determine the user's home directory (Environment.SpecialFolder.UserProfile was empty). " +
                    "Cannot construct application data path for macOS."
                );
            }

            string appDataDirectory = Path.Combine(
                homeDirectory,
                "Library",
                "Application Support",
                appName
            );

            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }

        // C:\Users\{username}\AppData\Roaming\BrowserAutomationMaster
        private static string GetAppDataWindows(string appName)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            if (string.IsNullOrEmpty(appDataFolder))
            {
                throw new InvalidOperationException(
                    "Could not determine the Windows Application Data folder (Environment.SpecialFolder.ApplicationData was empty)."
                );
            }

            string appDataDirectory = Path.Combine(appDataFolder, appName);

            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }

        private static void HandleRestoreConfirmation(string response, ref bool isConfirmed)
        {
            if (Input.ConditionAccepted(response))
            {
                isConfirmed = true;
            }

            if (!isConfirmed)
            {
                WriteAndExit("Unable to restore from backup due to a user cancellation.", 1);
            }
            
        }
        
        public static void RestoreFromBackup(string compression = "zip", string? backupFile = null, bool overwriteConfirmed = false)
        {
            if (compression is not "zip") {
                WriteAndExit($"Currently unsupported archive type: `{compression}` used in RestoreFromBackup()", 1);
            }

            // Compound assignment operator
            backupFile ??= GetDefaultBackupPath();

            if (!File.Exists(backupFile))
            {
                WriteAndExit("Unable to restore from backup, no backup file found.", 1);
            }

            if (AppDataDirectory == null)
            {
                WriteAndExit("Unable to restore from backup, AppDataDirectory returned null.", 1);
            }

            EnsureDirectoryExists(AppDataDirectory);

            var childDirectories = Directory.GetDirectories(AppDataDirectory) ?? [];
            var childFiles = Directory.GetFiles(AppDataDirectory) ?? [];

            bool directoryIsNotEmpty = childDirectories.Length != 0 || childFiles.Length != 0;
            bool finalOverwriteFlag = overwriteConfirmed; // Makes a copy as to not operate with a potentially stale or improper state

            if (directoryIsNotEmpty && !finalOverwriteFlag) 
            {
                var response = Input.AskForInput(string.Join(NLC, [
                    $"{AppDataDirectory} is not empty.",
                    "Would you like to overwrite its contents? [y/n]: "]
                ));

                // Prompts the user to confirm the overwrite if files already exist within the intended backup location.
                HandleRestoreConfirmation(response, ref finalOverwriteFlag);
            }

            var extension = Path.GetExtension(backupFile);
            if (extension is null || !extension.Equals(".zip", OIC)) 
            {
                WriteAndExit($"Currently unsupported archive type used in RestoreFromBackup()", 1);
            }

            try
            {
                ZipFile.ExtractToDirectory(backupFile, AppDataDirectory, finalOverwriteFlag);
            }
            catch (FileNotFoundException)
            {
                WriteAndExit("Unable to restore from backup, unable to locate backup.", 1);
            }
            catch (DirectoryNotFoundException)
            {
                WriteAndExit("Unable to restore from backup, the target directory was not found.", 1);
            }
            catch (IOException ex) when (ex.Message.Contains("already exists"))
            {
                WriteAndExit("Unable to restore from backup, the files already exist and cannot be overwritten.", 1);
            }
            catch (Exception ex)
            {
                WriteAndExit($"Unable to restore from backup, an unknown error occurred during backup restoration: {ex.Message}", 1);
            }


        }
    }
}
