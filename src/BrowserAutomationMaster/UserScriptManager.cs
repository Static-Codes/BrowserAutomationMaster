using System.Diagnostics;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster
{
    public class UserScriptManager
    {
        readonly string scriptPath = string.Empty;
        readonly string userScriptDirectory;

        public UserScriptManager(string filePath, string method)
        {
            // Performs path validation 1/6 (Ensures userScriptDirectory's value is not null or empty)
            this.userScriptDirectory = Parser.GetUserScriptDirectory();
            if (string.IsNullOrEmpty(this.userScriptDirectory)) {
                Errors.WriteErrorAndExit("Path to userScript directory could not be determined, if this continues please reinstall the application.", 1);
            }

            // Performs path validation 2/6 (Ensures the userScript directory exists)
            if (!Directory.Exists(this.userScriptDirectory)) {
                try {
                    Directory.CreateDirectory(this.userScriptDirectory);
                    Success.WriteSuccessMessage($"Successfully created userScript directory.\nLocation: {this.userScriptDirectory}");
                }
                catch (Exception ex) {
                    Errors.WriteErrorAndExit($"Failed to create userScript directory.\n'{this.userScriptDirectory}'\nError: {ex.Message}", 1);
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
                this.scriptPath = Path.Combine(this.userScriptDirectory, fileName); // this is the full path to the userScript/fileName.bamc
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) encountered an invalid file path: {filePath}", 1);
                return;
            }

            // Performs path validation 5/6 (Validates file extension)
            if (!this.scriptPath.ToLower().Trim().EndsWith(".bamc")) { 
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
                    if (!File.Exists(this.scriptPath)) {
                        Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to compile: {filePath}\nPlease ensure you've added this script to the userScript directory and try again.", 1);
                    }
                    Transpiler.New(this.scriptPath, []); 
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

            if (File.Exists(this.scriptPath)) {
                string response = Input.WriteTextAndReturnRawInput($"\nThe file '{fileName}' already exists in the userScript directory. Overwrite? [y/n]:\n") ?? "n";
                if (!response.ToLower().Trim().Equals("y")) {
                    Errors.WriteErrorAndExit("Operation canceled by user, exiting...", 0);
                    return;
                }
                overwrite = true;
            }

            try {
                File.Copy(sourceFilePath, this.scriptPath, overwrite);
                Success.WriteSuccessMessage($"\nSuccessfully {(overwrite ? "overwritten" : "added")} '{fileName}' to the userScript directory.\n");
            }
            catch (UnauthorizedAccessException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nSource: {sourceFilePath}\nDestination: {this.scriptPath}\nError: {ex.Message}", 1);
            }
            catch (IOException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nSource: {sourceFilePath}\nDestination: {this.scriptPath}\nError: {ex.Message}", 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to {(overwrite ? "overwrite" : "add")} '{fileName}'.\nError: {ex.Message}", 1);
            }
        }
        public void DeleteScript()
        {
            if (string.IsNullOrWhiteSpace(this.scriptPath)) { return; }
            if (!File.Exists(this.scriptPath)) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to locate:\n{this.scriptPath}\nPlease ensure this directory exists.", 1);
            }
            try
            {
                File.Delete(this.scriptPath);
                Success.WriteSuccessMessage($"BAM Manager (BAMM) successfully deleted file: {this.scriptPath}\n");
            }
            catch (IOException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nFile: {this.scriptPath}\n", 1);
            }
            catch (UnauthorizedAccessException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {this.scriptPath}\n", 1);
            }
            catch (System.Security.SecurityException) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {this.scriptPath}\n", 1);
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit($"Invalid argument for file path: '{this.scriptPath}'\n", 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"An unexpected error of type: '{ex.GetType().Name}' occurred while trying to delete file: '{this.scriptPath}'\n", 1);
            }
        }

    }
}
