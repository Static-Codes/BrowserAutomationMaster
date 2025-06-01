using System.Diagnostics;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers
{
    public class UserScriptManager
    {
        readonly string scriptPath = string.Empty;
        readonly string userScriptDirectory;

        public UserScriptManager(string filePath, string method)
        {
            // Performs path validation 1/6 (Ensures userScriptDirectory's value is not null or empty)
            userScriptDirectory = GetUserScriptDirectory();
            if (string.IsNullOrEmpty(userScriptDirectory)) {
                Errors.WriteErrorAndExit("Path to userScripts directory could not be determined, if this continues please reinstall the application.", 1);
            }

            // Performs path validation 2/6 (Ensures the userScript directory exists)
            if (!Directory.Exists(userScriptDirectory)) {
                try {
                    Directory.CreateDirectory(userScriptDirectory);
                    Success.WriteSuccessMessage($"Successfully created userScripts directory.\nLocation: {userScriptDirectory}");
                }
                catch (Exception ex) {
                    Errors.WriteErrorAndExit($"Failed to create userScripts directory.\n'{userScriptDirectory}'\nError: {ex.Message}", 1);
                }
            }

            // Performs path validation 3/6 (Ensures filePath's value is not null or empty)
            if (string.IsNullOrWhiteSpace(filePath)) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM): File path cannot be empty.", 1);
            }

            // Performs path validation 4/6 ()
            string fileName;
            try {
                fileName = Path.GetFileName(filePath);
                scriptPath = Path.Combine(userScriptDirectory, fileName); // this is the full path to the userScript/fileName.bamc
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) encountered an invalid file path: {filePath}", 1);
                return;
            }

            // Performs path validation 5/6 (Validates file extension)
            if (!scriptPath.ToLower().Trim().EndsWith(".bamc")) { 
                Errors.WriteErrorAndExit("BAM Manager (BAMM) only works with .BAMC files.\n\nPlease note: this file extension is not case sensitive, meaning '.bamc', '.BAMC', '.baMC', etc. will work!", 1);
            }

            // Performs path validation 6/6 (Locates the file within the userScript directory)
            if (!File.Exists(filePath))
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to locate the source file: {filePath}, please check for typos.", 1);
            }

            // Handles CLI args
            switch (method.ToLower().Trim())
            {
                case "add":
                    AddScript(filePath, fileName);
                    break;
                case "compile": // Only compiles from .bamc files within the userScripts directory, this creates standardized behavior. 
                    if (!File.Exists(scriptPath)) {
                        Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to compile: {filePath}\nPlease ensure you've added this script to the userScript directory and try again.", 1);
                    }
                    Transpiler.New(scriptPath, []); 
                    break;
                case "delete":
                    DeleteScript();
                    break;
                default:
                    Errors.WriteErrorAndExit($"Unknown method: {method}. Please type:\nbamm help\n\nFor further instructions.", 1);
                    break;
            }
        }


        public void AddScript(string sourceFilePath, string fileName)
        {
            bool overwrite = false;

            if (File.Exists(scriptPath)) {
                string response = Input.WriteTextAndReturnRawInput($"\nThe file '{fileName}' already exists in the userScript directory. Overwrite? [y/n]:\n") ?? "n";
                if (!response.ToLower().Trim().Equals("y")) {
                    Errors.WriteErrorAndExit("Operation canceled by user, exiting...", 0);
                    return;
                }
                overwrite = true;
            }

            try {
                File.Copy(sourceFilePath, scriptPath, overwrite);
                Success.WriteSuccessMessage($"\nSuccessfully {(overwrite ? "overwritten" : "added")} '{fileName}' to the userScript directory.\n");
            }
            catch (UnauthorizedAccessException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nSource: {sourceFilePath}\nDestination: {scriptPath}\nError: {ex.Message}", 1);
            }
            catch (IOException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nSource: {sourceFilePath}\nDestination: {scriptPath}\nError: {ex.Message}", 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to {(overwrite ? "overwrite" : "add")} '{fileName}'.\nError: {ex.Message}", 1);
            }
        }
        public void DeleteScript()
        {
            if (string.IsNullOrWhiteSpace(scriptPath)) { return; }
            if (!File.Exists(scriptPath)) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to locate:\n{scriptPath}\nPlease ensure this directory exists.", 1);
            }
            try
            {
                File.Delete(scriptPath);
                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully deleted file: {scriptPath}\n");
            }
            catch (IOException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nFile: {scriptPath}\n", 1);
            }
            catch (UnauthorizedAccessException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {scriptPath}\n", 1);
            }
            catch (System.Security.SecurityException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {scriptPath}\n", 1);
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit($"Invalid argument for file path: '{scriptPath}'\n", 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"An unexpected error of type: '{ex.GetType().Name}' occurred while trying to delete file: '{scriptPath}'\n", 1);
            }
        }

        public static string GetUserScriptDirectory()
        {
            string appName = "BrowserAutomationMaster";
            string userScriptsFolderName = "userScripts";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string userScriptsPath = Path.Combine(appDataPath, appName, userScriptsFolderName);
                EnsureDirectoryExists(userScriptsPath, "Windows");
                return userScriptsPath;
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string userScriptsPath;

                if (string.IsNullOrEmpty(homeDirectory))
                {
                    Errors.WriteErrorAndContinue($"BAM Manager (BAMM) could not automatically determine the user's home directory (UserProfile was empty).");
                    string? username = Environment.UserName; // Try Environment.UserName as a fallback
                    if (string.IsNullOrEmpty(username))
                    {
                        Errors.WriteErrorAndContinue("BAM Manager (BAMM) was also unable to determine the active user's username automatically.");
                        bool manuallyEntering = (Input.WriteTextAndReturnRawInput("Would you like to manually enter the username? [y/n]: ") ?? "n").ToLower().Equals("y");

                        if (manuallyEntering)
                        {
                            username = Input.WriteTextAndReturnRawInput("Please enter the exact username of the current active user: ") ?? string.Empty;
                            if (string.IsNullOrEmpty(username))
                            {
                                Errors.WriteErrorAndExit("Invalid username provided. BAM Manager (BAMM) will now exit. Press any key to exit...", 1);
                                return ""; // Should not be reached due to exit
                            }
                        }
                        else
                        {
                            Errors.WriteErrorAndExit("Username not provided. Press any key to exit...", 1);
                            return ""; // Should not be reached due to exit
                        }
                    }
                    // Assuming username is a non null value, created using /Users/{username} structure
                    homeDirectory = $"/Users/{username}"; // Use this as the base for the Application Support path
                    userScriptsPath = Path.Combine(homeDirectory, "Library", "Application Support", appName, userScriptsFolderName);
                }
                else
                {
                    // Fallback if user profile is available
                    userScriptsPath = Path.Combine(homeDirectory, "Library", "Application Support", appName, userScriptsFolderName);
                }

                EnsureDirectoryExists(userScriptsPath, "macOS");
                return userScriptsPath;
            }

            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                string? homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(homeDirectory)) { 
                    homeDirectory = Environment.GetEnvironmentVariable("HOME"); 
                }

                if (string.IsNullOrEmpty(homeDirectory)) {
                    Errors.WriteErrorAndExit("BAM Manager (BAMM) could not determine home directory on Linux.\nPress any key to exit...", 1);
                    return ""; // Should not be reached
                }

                // Ensures compliance with XDG specs using $XDG_CONFIG_HOME or $HOME/.config
                string? configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (string.IsNullOrEmpty(configHome)) {
                    configHome = Path.Combine(homeDirectory, ".config");
                }

                string userScriptsPath = Path.Combine(configHome, appName, userScriptsFolderName);
                EnsureDirectoryExists(userScriptsPath, "Linux");
                return userScriptsPath;
            }
            else
            {
                throw new PlatformNotSupportedException($"Unsupported OS ({RuntimeInformation.OSDescription}) or developmental flaw in UserScriptManager.GetUserScriptDirectory();");
            }
        }
        static void EnsureDirectoryExists(string path, string osHint) {
            if (!Directory.Exists(path)) {
                try { Directory.CreateDirectory(path); }
                catch (Exception) { Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to create the userScripts directory:\n{path}"); }
            }
        }


        //public static string GetUserScriptDirectory()
        //{
        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
        //        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserAutomationMaster", "userScripts");
        //    }

        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
        //        string username = Environment.UserName;
        //        if (username == null)
        //        {
        //            Errors.WriteErrorAndContinue("BAM Manager (BAMM) was unable to determine the active user's username.");
        //            bool manuallyEntering = (Input.WriteTextAndReturnRawInput("Would you like to manually enter this? [y/n]: ") ?? "n").ToLower().Equals("y");

        //            if (manuallyEntering) {
        //                username = Input.WriteTextAndReturnRawInput("Please enter the exact username of the current active user: ") ?? string.Empty;
        //                if (string.IsNullOrEmpty(username)) { Errors.WriteErrorAndExit("Invalid username provided, BAM Manager (BAMM) will now exit, press any key to exit...", 1); }
        //            }

        //            else { Errors.WriteErrorAndExit("Press any key to exit...", 1); return ""; }
        //            string directory = $"/Users/{username}/Library/Application Support/BrowserAutomationMaster/userScripts";
        //            if (!Directory.Exists(directory)) {
        //                Errors.WriteErrorAndContinue("BAM Manager (BAMM) was unable to determine the path to the userScripts directory, if this issue persists, its likely not a system fault but instead a developmental flaw.");
        //            }
        //            return directory;
        //        }
        //    }

        //    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        //    {
        //        string? userName = Environment.GetEnvironmentVariable("USER");
        //        if (!string.IsNullOrEmpty(userName)) { return userName; }
        //        userName = Environment.GetEnvironmentVariable("LOGNAME");
        //        if (!string.IsNullOrEmpty(userName)) { return userName; }
        //        return Environment.UserName;
        //    }
        //    else
        //    {
        //        throw new PlatformNotSupportedException("Unsupported OS or developmental flaw in UserScriptManager.GetUserScriptDirectory();");
        //    }

        //}
    }

    public static class UserScriptExamples
    {
        public readonly static string AllCmdsExample = @"browser ""firefox""
visit ""https://google.com""
click ""#id-selector""
click-exp 'ytd-popup-container.style-scope' 
get-text ""example-selector""
fill-text ""textbox-selector"" ""value""
select-option ""dropdown-selector"" 2
select-element ""dropdown-element-selector""
save-as-html ""filename.html""
save-as-html-exp ""filename2.html""
take-screenshot ""filename.png""
wait-for-seconds 5";



        public readonly static string LocalFileExample = @"browser ""firefox""
visit ""file:///C:/Users/TestEnv/Desktop/test.html""
wait-for-seconds .2
select-option ""#cars"" 2
wait-for-seconds .2
take-screenshot ""filename.png""";



        public readonly static string GoogleFillExample = @"browser ""firefox""
visit ""https://google.com""
fill-text ""#APjFqb"" ""This is a test""
wait-for-seconds .2
take-screenshot ""filename.png""
save-as-html ""filename.html""";



        public readonly static string JSEmbedExample = @"browser ""firefox""
visit ""https://google.com""
start-javascript
// Single and double quotes
let singleQuote = 'This is a single-quoted string';
let doubleQuote = ""This is a double-quoted string"";
let mixedQuotes = 'He said, ""It\'s a great day!""';

// Escaped characters
let path = ""C:\\Users\\Test\\Desktop"";
let escapeTest = ""Line1\\nLine2\\tTabbed\\\""Quote\\\"""";

// Template literal with interpolation
let name = ""Alice"";
let greeting = `Hello, ${name}! Today is ${new Date().toDateString()}.`;

// Multiline string using newline characters
let multiline = ""This is line one.\nThis is line two.\nThis is line three."";

// Tabs in string
let tabbed = ""Item1\tItem2\tItem3"";

// Regular expression literal
let regex = /^[A-Z]+\s\d+$/gm;

// Unicode and special characters
let unicode = ""Emoji: 😀 — Symbols: ≈ ≤ ∑"";

// Mixed quote escaping
let tricky = 'He said, ""Don\'t forget to escape backslashes: \\\\""';

// JavaScript comment examples
// This is a single-line comment
/*
This is a
multi-line comment
*/

function complexFunction() {
    let inner = ""Nested \""quotes\"" and 'single quotes' with tabs\tand newlines\n."";
    console.log(inner);
}

console.log(""All tests executed."");
end-javascript";

        public readonly static List<KeyValuePair<string, string>> AllExamples = [

            new KeyValuePair<string, string>("all-commands.bamc", AllCmdsExample),
            new KeyValuePair<string, string>("local-file.bamc", LocalFileExample),
            new KeyValuePair<string, string>("google-example.bamc", GoogleFillExample),
            new KeyValuePair<string, string>("js-embed-example.bamc", JSEmbedExample),
        ];

        public static void WriteScriptExamples()
        {
            foreach (KeyValuePair<string, string> example in UserScriptExamples.AllExamples) {
                try
                {
                    string filename = example.Key;
                    string contents = example.Value;
                    if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(contents)) { continue; }
                    string filepath = Path.Combine(UserScriptManager.GetUserScriptDirectory(), filename);
                    if (File.Exists(filepath)) { continue; } // This is an unnecessary check but i felt the need to include it
                    File.WriteAllText(filepath, contents); // Writes the actual contents
                }
                catch {
                    Warning.Write($"Unable to write example file: {example.Key}");  continue; 
                }
            }
        }

    }
}
