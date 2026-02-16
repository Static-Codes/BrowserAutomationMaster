using BrowserAutomationMaster.Managers.Compilation;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers
{
    public class UserScriptManager
    {
        readonly string scriptPath = string.Empty;
        readonly string userScriptDirectory;

        public UserScriptManager(string filePath, string method)
        {
            // Performs path validation 1/6 (Ensures userScriptDirectory's value is not null or empty)
            userScriptDirectory = Parser.userScriptsDirectory;
            if (string.IsNullOrEmpty(userScriptDirectory)) {
                WriteAndExit(
                    message: "Path to userScripts directory could not be determined, " +
                    "if this continues please reinstall the application.", 
                    status: 1
                );
            }

            // Performs path validation 2/6 (Ensures the userScript directory exists)
            if (!Directory.Exists(userScriptDirectory)) {
                try {
                    Directory.CreateDirectory(userScriptDirectory);
                    WriteSuccessMessage(
                        $"Successfully created userScripts directory.\n" +
                        $"Location: {userScriptDirectory}"
                    );
                }
                catch (Exception ex) {
                    WriteAndExit(
                        message: "Failed to create userScripts directory.\n'" + $"{userScriptDirectory}'\nError: {ex.Message}", 
                        status: 1
                    );
                }
            }

            // Performs path validation 3/6 (Ensures filePath's value is not null or empty)
            if (string.IsNullOrWhiteSpace(filePath)) {
                WriteAndExit(
                    message: "BAM Manager (BAMM): File path cannot be empty.", 
                    status: 1
                );
            }

            // Performs path validation 4/6 (Sets the value of scriptPath
            string fileName;
            try {
                fileName = Path.GetFileName(filePath);
                scriptPath = Path.Combine(userScriptDirectory, fileName); // this is the full path to the userScript/fileName.bamc
            }
            catch (ArgumentException) {
                WriteAndExit($"BAM Manager (BAMM) encountered an invalid file path: {filePath}", 1);
                return;
            }

            // Performs path validation 5/6 (Validates file extension)
            if (!scriptPath.ToLower().Trim().EndsWith(".bamc")) { 
                WriteAndExit(
                    message: "BAM Manager (BAMM) only works with .BAMC files.\n\n" +
                    "Please note: this file extension is not case sensitive, " +
                    "meaning '.bamc', '.BAMC', '.baMC', etc. will work!", 
                    status: 1
                );
            }

            // Performs path validation 6/6 (Locates the file within the userScript directory)
            if (!File.Exists(scriptPath)) {
                WriteAndExit(
                    message: $"BAM Manager (BAMM) was unable to locate the source file: {filePath}, please check for typos.", 
                    status: 1
                );
            }
            
            HandleCLIArgs(method, filePath, scriptPath).GetAwaiter().GetResult();
            Console.WriteLine("Test Complete");
        }

        public void AddScript(string sourceFilePath, string fileName)
        {
            bool overwrite = false;

            if (File.Exists(scriptPath)) 
            {
                string response = Input.AskForInput(
                    $"{NLC}The file '{fileName}' already exists in the userScript directory. Overwrite? [y/n]:{NLC}"
                );

                if (!response.Equals("y")) {
                    WriteAndExit("Operation canceled by user, exiting...", 0);
                    return;
                }
                overwrite = true;
            }

            try 
            {
                if (sourceFilePath != scriptPath)
                {
                    File.Copy(sourceFilePath, scriptPath, overwrite);
                    WriteSuccessMessage(
                        $"\nSuccessfully {(overwrite ? "overwritten" : "added")} '{fileName}' to the userScript directory.{NLC}"
                    );
                    return;
                }

                WriteAndExit
                (
                    string.Join(NLC, [
                        NLC, 
                        $"BAM Manager (BAMM) was unable to overwrite {fileName}.",
                        NLC,
                        "Error Log:",
                        "The Source Path is the same as the Destination Path." 
                    ]),
                    status: 1
                );
            }

            catch (UnauthorizedAccessException ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        NLC, 
                        $"BAM Manager (BAMM) was unable to continue, permission denied.",
                        NLC,
                        $"Source: {sourceFilePath}",
                        $"Destination: {scriptPath}",
                        $"Error: {ex.Message}",
                    ]),
                    status: 1
                );
            }
            
            catch (IOException ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        NLC,
                        "BAM Manager (BAMM) was unable to continue due to an I/O error.",
                        NLC,
                        $"Source: {sourceFilePath}",
                        $"Destination: {scriptPath}",
                        $"Error: {ex.Message}"
                    ]),
                    status: 1
                );
            }
            
            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        NLC,
                        $"BAM Manager (BAMM) was unable to {(overwrite ? "overwrite" : "add")} {fileName}.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
        }
        
        private async Task HandleCLIArgs(string method, string filePath, string fileName)
        {
            switch (method.ToLower().Trim())
            {
                case "add":
                    AddScript(sourceFilePath: filePath, fileName);
                    break;

                case "compile": // Only compiles from .bamc files within the userScripts directory, this creates standardized behavior. 
                    if (!File.Exists(scriptPath))
                    {
                        WriteAndExit(
                            message: $"BAM Manager (BAMM) was unable to compile: {filePath}\n" +
                            $"Please ensure you've added this script to the userScript directory and try again.",
                            status: 1);
                    }
                    await Transpiler.New(filePath: scriptPath, args: ["compile"]);
                    break;

                case "run":
                    Runtime runtimeManager = new(scriptFilePath: scriptPath);
                    await runtimeManager.RunScript(Transpiler.GetBrowserStackStatus());
                    break;

                default:
                    WriteAndExit(
                        message: $"Unknown method: {method}. Please type:\nbamm help\n\nFor further instructions.",
                        status: 1
                    );
                    break;
            }
        }

    }

    public static class UserScriptExamples
    {
        public readonly static List<string> ExampleFileNames = [
            "codedpad.bamc",
            "ebay.bamc",
            "google-gemini.bamc",
            "google-maps.bamc",
            "google-search.bamc",
            "js-embed.bamc",
            "marketplace.bamc",
            "steam.bamc",
            "youtube-search.bamc",
        ];

        public static async Task WriteScriptExamples()
        {
            foreach (var exampleFileName in ExampleFileNames) 
            {

                string filePath = Path.Combine(Parser.userScriptsDirectory, exampleFileName);
                try
                {
                    
                    if (File.Exists(filePath)) { 
                        continue; 
                    }

                    string resourcePattern = string.Format("BrowserAutomationMaster.userScripts.{0}", exampleFileName);

                    // Retrieves and writes the contents of the embedded resource to disk.
                    await EmbeddedResourceManager.WriteEmbeddedResourceToDisk(exampleFileName, resourcePattern, filePath);
                }

                catch (Exception ex) 
                {
                    Warning.Write(
                        string.Join(NLC, [ 
                            $"Unable to write example file: {filePath}",
                            "Error Log:",
                            ex.Message
                        ])
                    );
                }
            }
        }

    }
}
