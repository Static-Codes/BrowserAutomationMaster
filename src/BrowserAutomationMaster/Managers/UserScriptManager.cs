using System.Runtime.InteropServices;
using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.DirectoryManager;

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
                Errors.WriteErrorAndExit(
                    message: "Path to userScripts directory could not be determined, " +
                    "if this continues please reinstall the application.", 
                    status: 1
                );
            }

            // Performs path validation 2/6 (Ensures the userScript directory exists)
            if (!Directory.Exists(userScriptDirectory)) {
                try {
                    Directory.CreateDirectory(userScriptDirectory);
                    Success.WriteSuccessMessage(
                        $"Successfully created userScripts directory.\n" +
                        $"Location: {userScriptDirectory}"
                    );
                }
                catch (Exception ex) {
                    Errors.WriteErrorAndExit(
                        message: "Failed to create userScripts directory.\n'" + $"{userScriptDirectory}'\nError: {ex.Message}", 
                        status: 1
                    );
                }
            }

            // Performs path validation 3/6 (Ensures filePath's value is not null or empty)
            if (string.IsNullOrWhiteSpace(filePath)) {
                Errors.WriteErrorAndExit(
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
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) encountered an invalid file path: {filePath}", 1);
                return;
            }

            // Performs path validation 5/6 (Validates file extension)
            if (!scriptPath.ToLower().Trim().EndsWith(".bamc")) { 
                Errors.WriteErrorAndExit(
                    message: "BAM Manager (BAMM) only works with .BAMC files.\n\n" +
                    "Please note: this file extension is not case sensitive, " +
                    "meaning '.bamc', '.BAMC', '.baMC', etc. will work!", 
                    status: 1
                );
            }

            // Performs path validation 6/6 (Locates the file within the userScript directory)
            if (!File.Exists(filePath)) {
                Errors.WriteErrorAndExit(
                    message: $"BAM Manager (BAMM) was unable to locate the source file: {filePath}, please check for typos.", 
                    status: 1
                );
            }

            HandleCLIArgs(method, filePath, scriptPath);
        }


        public void AddScript(string sourceFilePath, string fileName)
        {
            bool overwrite = false;

            if (File.Exists(scriptPath)) {
                string response = Input.WriteTextAndReturnRawInput(
                    $"\nThe file '{fileName}' already exists in the userScript directory. Overwrite? [y/n]:\n"
                );

                if (!response.Equals("y")) {
                    Errors.WriteErrorAndExit("Operation canceled by user, exiting...", 0);
                    return;
                }
                overwrite = true;
            }

            try {
                if (sourceFilePath != scriptPath)
                {
                    File.Copy(
                        sourceFilePath,
                        destFileName: scriptPath,
                        overwrite
                    );
                    Success.WriteSuccessMessage(
                        $"\nSuccessfully {(overwrite ? "overwritten" : "added")} '{fileName}' to the userScript directory.\n"
                    );
                    return;
                }
                Errors.WriteErrorAndExit($"\nBAM Manager (BAMM) was unable to overwrite {fileName}\nError log:\n\nThe Source Path is the same as the Destination Path.", status: 1);
            }
            catch (UnauthorizedAccessException ex) {
                Errors.WriteErrorAndExit(
                    message: $"\nBAM Manager (BAMM) was unable to continue, permission denied.\n" +
                    $"Source: {sourceFilePath}\nDestination: {scriptPath}\nError: {ex.Message}",
                    status: 1
                );
            }
            catch (IOException ex) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                        $"Source: {sourceFilePath}\n" +
                        $"Destination: {scriptPath}\n" +
                        $"Error: {ex.Message}",
                    status: 1
                );
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to " +
                        $"{(overwrite ? "overwrite" : "add")} " +
                        $"'{fileName}'.\nError: {ex.Message}",
                    status: 1
                );
            }
        }
        public void DeleteScript()
        {
            if (string.IsNullOrWhiteSpace(scriptPath)) { return; }
            if (!File.Exists(scriptPath)) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to locate:\n" +
                        $"{scriptPath}\n" +
                        $"Please ensure this directory exists.",
                    status: 1
                );
            }
            try
            {
                File.Delete(scriptPath);
                Success.WriteSuccessMessage(
                    message: $"BAM Manager (BAMM) successfully deleted file: {scriptPath}\n"
                );
            }
            catch (IOException) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue due to an I/O error.\n" +
                        $"File: {scriptPath}\n",
                    status: 1
                );
            }
            catch (UnauthorizedAccessException) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {scriptPath}\n",
                    status: 1
                );
            }
            catch (System.Security.SecurityException) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"\nBAM Manager (BAMM) was unable to continue, permission denied.\nFile: {scriptPath}\n", 
                    status: 1
                );
            }
            catch (ArgumentException) {
                Errors.WriteErrorAndExit(
                    message: $"Invalid argument for file path: '{scriptPath}'\n",
                    status: 1
                );
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"An unexpected error of type: '{ex.GetType().Name}' " +
                        $"occurred while trying to delete file: '{scriptPath}'\n", 
                    status: 1
                );
            }
        }
        
        private void HandleCLIArgs(string method, string filePath, string fileName)
        {
            switch (method.ToLower().Trim())
            {
                case "add":
                    AddScript(sourceFilePath: filePath, fileName);
                    break;
                case "compile": // Only compiles from .bamc files within the userScripts directory, this creates standardized behavior. 
                    if (!File.Exists(scriptPath))
                    {
                        Errors.WriteErrorAndExit(
                            message: $"BAM Manager (BAMM) was unable to compile: {filePath}\n" +
                            $"Please ensure you've added this script to the userScript directory and try again.",
                            status: 1);
                    }
                    Transpiler.New(filePath: scriptPath, args: []);
                    break;
                case "delete":
                    DeleteScript();
                    break;
                case "run":
                    RuntimeManager runtimeManager = new(scriptFilePath: scriptPath);
                    Action RunAction(RuntimeManager runtimeManager) => async () => { await runtimeManager.RunScript(); };
                    Task.Run(RunAction(runtimeManager));
                    break;

                default:
                    Errors.WriteErrorAndExit(
                        message: $"Unknown method: {method}. Please type:\nbamm help\n\nFor further instructions.",
                        status: 1
                    );
                    break;
            }
        }
    }

    public static class UserScriptExamples
    {
        public readonly static string EBayExample = @"browser ""chrome""
visit ""https://www.ebay.com/""
wait-for-seconds 1.5
fill-text ""#gh-ac"" ""Awesome deals""
wait-for-seconds 1
click ""#gh-search-btn""
wait-for-seconds 10
save-as-html ""ebay-search.html""";

        public readonly static string CodedpadExample = @"browser ""firefox""
visit ""https://www.codedpad.com/""
wait-for-seconds 2
fill-text ""#pad_code"" ""Thisisapasswordexamplethatisnotverysecure""
wait-for-seconds 1
click ""#home_submit_open""
wait-for-seconds 1.5
fill-text ""#pad_content"" ""If you are reading this then this script has worked for you""
wait-for-seconds .5
click ""#submit_save""";

        public readonly static string GoogleFillExample = @"browser ""firefox""
visit ""https://google.com""
fill-text ""#APjFqb"" ""This is a test""
wait-for-seconds .2
take-screenshot ""filename.png""
save-as-html ""filename.html""";

        public readonly static string GoogleGeminiExample = @"browser ""chrome""
visit ""https://gemini.google.com/app""
wait-for-seconds 3
start-javascript
document.querySelector('.ql-editor p').textContent = 'What is the perceived meaning of life?'
new Promise((resolve) => setTimeout(resolve, 1000));
end-javascript
click "".send-button""
wait-for-seconds 30
take-screenshot ""gemini-response.png""";

        public readonly static string GoogleMapsExample = @"visit ""https://www.google.com/maps/""
wait-for-seconds 1.5
fill-text ""#searchboxinput"" ""Topeka, KS""
click ""#searchbox-searchbutton""
wait-for-seconds 5
take-screenshot ""google-maps.png""";

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

        public readonly static string MarketplaceExample = @"browser ""chrome"" // this also works in firefox
visit ""https://www.facebook.com/marketplace/""
wait-for-seconds 1.5
start-javascript
var button = document.querySelector(""div[aria-label='Close']"");
if (button){
    button.click();
}
else{
    alert('Not Found');
}
end-javascript

wait-for-seconds 2

fill-text ""/html/body/div[1]/div/div[1]/div/div[3]/div/div/div[1]/div[1]/div[1]/div/div[2]/div/div/div/span/div/div/div/div/label/input"" ""free stuff""

wait-for-seconds 2

start-javascript
const enterEvent = new KeyboardEvent('keydown', {
  key: 'Enter',
  code: 'Enter',
  which: 13,
  keyCode: 13,
  bubbles: true,
  cancelable: true
});
var textbox = document.querySelector(""input[placeholder='Search Marketplace']"");
if (textbox){
    textbox.dispatchEvent(enterEvent);
}
else {
    alert('Unable to submit click event');
}
end-javascript

wait-for-seconds 15
take-screenshot ""marketplace-search.png""";

        public readonly static string SteamExample = @"browser ""chrome""
visit ""https://store.steampowered.com/""
wait-for-seconds 1.5

fill-text ""#store_nav_search_term"" ""Shooters""
wait-for-seconds 1
start-javascript		
document.getElementById(""searchform"").submit(); 
end-javascript

wait-for-seconds 10
save-as-html ""shooters.html""";

        public readonly static string YoutubeSearchExample = @"visit ""https://www.youtube.com/""
wait-for-seconds 1.5
fill-text "".ytSearchboxComponentInput"" ""This is a test, it works!""
wait-for-seconds 1.5
click "".ytSearchboxComponentSearchButton""
wait-for-seconds 5
take-screenshot ""youtube-feed.png""";

        public readonly static List<KeyValuePair<string, string>> AllExamples = [

            new KeyValuePair<string, string>("ebay.bamc", EBayExample),
            new KeyValuePair<string, string>("codedpad.bamc", CodedpadExample),
            new KeyValuePair<string, string>("google-gemini.bamc", GoogleGeminiExample),
            new KeyValuePair<string, string>("google-maps.bamc", GoogleMapsExample),
            new KeyValuePair<string, string>("fill-text-by-id.bamc", GoogleFillExample),
            new KeyValuePair<string, string>("js-embed.bamc", JSEmbedExample),
            new KeyValuePair<string, string>("marketplace.bamc", MarketplaceExample),
            new KeyValuePair<string, string>("steam.bamc", SteamExample),
            new KeyValuePair<string, string>("youtube-search.bamc", SteamExample),

        ];

        public static void WriteScriptExamples()
        {
            foreach (KeyValuePair<string, string> example in UserScriptExamples.AllExamples) {
                try
                {
                    string filename = example.Key;
                    string contents = example.Value;
                    if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(contents)) { continue; }
                    string filepath = Path.Combine(GetUserScriptDirectory(), filename);
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
