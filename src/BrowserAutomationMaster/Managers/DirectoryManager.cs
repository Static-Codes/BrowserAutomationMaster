using System.IO.Compression;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers
{
    class DirectoryManager
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
                
                if (Input.ConditionRejected(response))
                    Environment.Exit(0);
                
                DeleteFile(backupPath);
            }

            try
            {
                switch (compression)
                {
                    case "zip" when outputPath == null:
                        if (!Directory.Exists(AppDataDirectory))
                            Errors.WriteAndExit($"Unable to create backup file, directory doesn't exist at: {AppDataDirectory}", 1);

                        
                        if (string.IsNullOrEmpty(backupPath))
                        {
                            var message = "Would you like to create a backup in the current directory? [y/n]: ";
                            var response = Input.AskForInput(message);
                            if (Input.ConditionRejected(response))
                                Environment.Exit(0);

                            backupPath = Environment.CurrentDirectory;
                        }


                        ZipFile.CreateFromDirectory(AppDataDirectory, backupPath, CompressionLevel.Optimal, false);
                        Success.WriteSuccessMessage($"Successfully created backup at: {backupPath}");
                        break;
                }
            }
            catch (Exception ex)
            {
                var message =
                    "Unable to create a backup file.\n" +
                    $"If this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}" +
                    $"Error Log:\n{ex.Message}";

                Errors.WriteAndExit(message, 1);
            }
        }
        
        public static void DeleteDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) { return; }
            if (!Directory.Exists(directory)) {
                Errors.WriteAndExit(
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
                Errors.WriteAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                             $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (UnauthorizedAccessException e) {
                Errors.WriteAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                        $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (System.Security.SecurityException e) {
                Errors.WriteAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                        $"File: {directory}\n\nException:\n\n{e.Message}", 
                    status: 1
                );
            }
            catch (ArgumentException e) {
                Errors.WriteAndExit(
                    message: 
                        $"Invalid argument for file path: '{directory}\n\n" +
                        $"Exception:\n\n {e.Message}", 
                    status: 1
                );
            }
            catch (Exception ex)
            {
                Errors.WriteAndExit(
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
                Errors.WriteAndExit(
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
                Success.WriteSuccessMessage(
                    message: $"BAM Manager (BAMM) successfully deleted file: {path}\n"
                );
            }
            catch (IOException)
            {
                Errors.WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                        $"File: {path}\n",
                    status: 1
                );
            }
            catch (UnauthorizedAccessException)
            {
                Errors.WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {path}\n",
                    status: 1
                );
            }
            catch (System.Security.SecurityException)
            {
                Errors.WriteAndExit(
                    message:
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {path}\n",
                    status: 1
                );
            }
            catch (ArgumentException)
            {
                Errors.WriteAndExit(
                    message: $"Invalid argument for file path: '{path}'\n",
                    status: 1
                );
            }
            catch (Exception ex)
            {
                Errors.WriteAndExit(
                    message:
                        $"An unexpected error of type: '{ex.GetType().Name}' " +
                        $"occurred while trying to delete file: '{path}'\n",
                    status: 1
                );
            }
        }
        
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception)
                {
                    Errors.Write(
                        message: $"BAM Manager (BAMM) was unable to create the userScripts directory:\n{path}"
                    );
                }
            }
        }
        
        private static string GetDefaultBackupPath(string compression = "zip")
        {
            try
            {
                var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                if (string.IsNullOrEmpty(desktopDir))
                    return desktopDir;

                var fileName = $"BAMM-Backup.{compression}";
                return Path.Combine(desktopDir, fileName);
            }
            catch
            {
                var message = "Unable to get the default backup directory, please ensure you have a Desktop Environment installed.";
                return Errors.WriteErrorAndReturnEmptyString(message);
            }
        }
        
        public static string GetBrowserStackDirectory() { return Path.Combine(AppDataDirectory, "browserstack"); }
        
        public static string GetBrowserStackConfigPath() { return Path.Combine(GetBrowserStackDirectory(), "browserstack.yml"); }
        
        public static string GetBAMConfigDirectory() { return Path.Combine(AppDataDirectory, "config"); }
        
        public static string GetDesiredSaveDirectory() { return Path.Combine(AppDataDirectory, "compiled"); }
        
        public static string GetGlobalVEnvPath() { return Path.Combine(AppDataDirectory, "globalVEnv"); }

        public static string GetGlobalVEnvPythonPath()
        {
            if (IsWindows)
                return Path.Combine(GetGlobalVEnvPath(), "Scripts", "python.exe");

            if (IsUnixLike)
                return Path.Combine(GetGlobalVEnvPath(), "bin", "python3");

            Errors.ThrowUnsupportedPlatformException();
            return ""; // This wont be returned however rosyln being static in nature, doesn't know this.
        }

        public static string GetGlobalVEnvPipPath()
        {
            if (IsWindows)
                return Path.Combine(GetGlobalVEnvPath(), "Scripts", "pip");

            if (IsUnixLike)
                return Path.Combine(GetGlobalVEnvPath(), "bin", "pip");

            Errors.ThrowUnsupportedPlatformException();
            return ""; // This wont be returned however rosyln being static in nature, doesn't know this.
        }

        public static string GetPackagesPath() { return Path.Combine(AppDataDirectory, "packages.json"); }

        public static string GetUserAgentsPath() { return Path.Combine(AppDataDirectory, "useragents.json"); }
        
        public static string GetUserScriptDirectory() { return Path.Combine(AppDataDirectory, "userScripts"); }
        
        private static string GetAppDataLinux(string appName)
        {
            string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(homeDirectory))
                homeDirectory = Environment.GetEnvironmentVariable("HOME");

            // Fallback for second check
            if (string.IsNullOrEmpty(homeDirectory))
            {
                Errors.WriteAndExit(
                    message:
                        "BAM Manager (BAMM) could not determine home directory on Linux.\n" +
                        "Press any key to exit...",
                    status: 1
                );
            }

            // Ensures compliance with XDG specs using $XDG_CONFIG_HOME or $HOME/.config
            string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (string.IsNullOrEmpty(configHome))
                configHome = Path.Combine(homeDirectory, ".config");

            string appDataDirectory = Path.Combine(configHome, appName);
            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }
        
        private static string GetAppDataMacOS(string appName)
        {
            string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string appDataDirectory;

            if (!string.IsNullOrEmpty(homeDirectory))
            {
                appDataDirectory = Path.Combine(
                    homeDirectory,
                    "Library",
                    "Application Support",
                    appName
                );

            }

            else
            {
                Errors.Write(
                    message:
                        $"BAM Manager (BAMM) could not automatically determine the user's home directory\n" +
                        $"(UserProfile was empty)."
                );
                string username = Environment.UserName;
                if (string.IsNullOrEmpty(username))
                {
                    Errors.Write(
                        message: "BAM Manager (BAMM) was also unable to determine the active user's username automatically."
                    );

                    string response = Input.AskForInput(
                        "Would you like to manually enter the username? [y/n]: "
                    );

                    bool manuallyEntering = response.Equals("y");

                    if (manuallyEntering)
                    {
                        username = Input.AskForInput(
                            "Please enter the exact username of the current active user: "
                        );

                        if (string.IsNullOrEmpty(username))
                        {
                            Errors.WriteAndExit(
                                message:
                                    "Invalid username provided. " +
                                    "BAM Manager (BAMM) will now exit. " +
                                    "Press any key to exit...",
                                status: 1
                            );
                        }
                    }
                    else
                    {
                        Errors.WriteAndExit(
                            message:
                                "Username not provided. Press any key to exit...",
                            status: 1
                        );
                    }
                }
                // Assuming username is a non null value, created using /Users/{username} structure
                homeDirectory = $"/Users/{username}";
                appDataDirectory = Path.Combine(
                    homeDirectory,
                    "Library",
                    "Application Support",
                    appName
                );
            }

            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }
        
        private static string GetAppDataWindows(string appName)
        {
            string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataDirectory = Path.Combine(appDataFolder, appName);
            EnsureDirectoryExists(appDataDirectory);
            return appDataDirectory;
        }
        
        public static string GetAppDataDirectory()
        {
            string appName = "BrowserAutomationMaster";

            if (IsWindows)
                return GetAppDataWindows(appName);

            else if (IsOSX)
                return GetAppDataMacOS(appName);

            else if (IsLinux)
                return GetAppDataLinux(appName);
            
            else
                throw new PlatformNotSupportedException($"Unsupported OS");
        }
    }
}
