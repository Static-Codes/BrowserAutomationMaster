namespace BrowserAutomationMaster
{
    public class UserScriptManager
    {
        readonly string userScriptDirectory;

        public UserScriptManager(string filePath, string method)
        {
            this.userScriptDirectory = Parser.GetUserScriptDirectory();
            if (string.IsNullOrEmpty(this.userScriptDirectory)) {
                Errors.WriteErrorAndExit("User script directory path could not be determined.", 1);
            }

            if (!Directory.Exists(this.userScriptDirectory)) {
                try {
                    Directory.CreateDirectory(this.userScriptDirectory);
                    Success.WriteSuccessMessage($"User script directory created: {this.userScriptDirectory}");
                }
                catch (Exception ex)
                {
                    Errors.WriteErrorAndExit($"Failed to create user script directory '{this.userScriptDirectory}': {ex.Message}", 1);
                }
            }

            if (string.IsNullOrWhiteSpace(filePath)) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM): File path cannot be empty.", 1);
            }

            string fileName;
            try {
                fileName = Path.GetFileName(filePath);
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) encountered an invalid file path: {filePath}", 1);
                return;
            }

            if (!filePath.ToLower().Trim().EndsWith(".bamc")) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) only works with .BAMC files.\n\nPlease note: this file extension is not case sensitive, meaning '.bamc', '.BAMC', '.baMC', etc. will work!", 1);
            }

            if (!File.Exists(filePath)) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to locate the source file: {filePath}, please check for typos.", 1);
            }

            switch (method.ToLower().Trim())
            {
                case "add":
                    AddScript(filePath, fileName);
                    break;
                default:
                    Errors.WriteErrorAndExit($"Unknown method: {method}. Supported methods are:\n\n'add'\n'delete'\n'duplicate'", 1);
                    break;
            }
        }

        public void AddScript(string sourceFilePath, string fileName)
        {
            bool overwrite = false;
            string destinationScriptFile = Path.Combine(this.userScriptDirectory, fileName);

            if (File.Exists(destinationScriptFile)) {
                string response = Input.WriteTextAndReturnRawInput($"\nThe file '{fileName}' already exists in the userScript directory. Overwrite? [y/n]:\n") ?? "n";
                if (!response.ToLower().Trim().Equals("y")) {
                    Errors.WriteErrorAndExit("Operation canceled by user, exiting...", 0);
                    return;
                }
                overwrite = true;
            }

            try {
                File.Copy(sourceFilePath, destinationScriptFile, overwrite);
                Success.WriteSuccessMessage($"\nSuccessfully {(overwrite ? "overwritten" : "added")} '{fileName}' to the userScript directory.\n");
            }
            catch (UnauthorizedAccessException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue, permission denied.\nSource: {sourceFilePath}\nDestination: {destinationScriptFile}\nError: {ex.Message}", 1);
            }
            catch (IOException ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\nSource: {sourceFilePath}\nDestination: {destinationScriptFile}\nError: {ex.Message}", 1);
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to {(overwrite ? "overwrite" : "add")} '{fileName}'.\nError: {ex.Message}", 1);
            }
        }
    }
}
