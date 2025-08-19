using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.Managers
{
    class DirectoryManager
    {
        public static string AppDataDirectory { get; private set; } = GetAppDataDirectory();
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
        public static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                try { Directory.CreateDirectory(path); }
                catch (Exception)
                {
                    Errors.WriteErrorAndContinue(
                        message: $"BAM Manager (BAMM) was unable to create the userScripts directory:\n{path}"
                    );
                }
            }
        }

        public static string GetBrowserStackDirectory()
        {
            return Path.Combine(AppDataDirectory, "browserstack");
        }

        public static string GetConfigDirectory() { return Path.Combine(AppDataDirectory, "config"); }
        public static string GetDesiredSaveDirectory() { return Path.Combine(AppDataDirectory, "compiled"); }
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
                Errors.WriteErrorAndExit(
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
                Errors.WriteErrorAndContinue(
                    message:
                        $"BAM Manager (BAMM) could not automatically determine the user's home directory\n" +
                        $"(UserProfile was empty)."
                );
                string username = Environment.UserName;
                if (string.IsNullOrEmpty(username))
                {
                    Errors.WriteErrorAndContinue(
                        message: "BAM Manager (BAMM) was also unable to determine the active user's username automatically."
                    );

                    string response = Input.WriteTextAndReturnRawInput(
                        "Would you like to manually enter the username? [y/n]: "
                    );

                    bool manuallyEntering = response.Equals("y");

                    if (manuallyEntering)
                    {
                        username = Input.WriteTextAndReturnRawInput(
                            "Please enter the exact username of the current active user: "
                        );

                        if (string.IsNullOrEmpty(username))
                        {
                            Errors.WriteErrorAndExit(
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
                        Errors.WriteErrorAndExit(
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetAppDataWindows(appName);

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetAppDataMacOS(appName);

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetAppDataLinux(appName);
            
            else
                throw new PlatformNotSupportedException($"Unsupported OS");
        }
    }
}
