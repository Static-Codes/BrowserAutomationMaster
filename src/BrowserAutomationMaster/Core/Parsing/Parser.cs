using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Utilities;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static BrowserAutomationMaster.Core.Common.ANSI;
using static BrowserAutomationMaster.Core.Common.Commands;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.Parsing.LineValidation;

namespace BrowserAutomationMaster.Core.Parsing
{
    public partial class Parser
    {
        public readonly static string[] actionArgs = [
            "add-cookie", "add-header", "add-headers", "click", "click-at-position", "click-exp", "close-current-tab", 
            "end-javascript", "fill-text", "fill-text-exp", "get-text", "open-new-tab", "save-as-html", 
            "save-as-html-exp", "select-element", "select-option", "set-custom-useragent", "start-javascript", 
            "take-screenshot", "wait-for-seconds", "visit"
        ];

        public readonly static string[] proxyFeatureArgs = [
            "use-http-proxy", "use-https-proxy", 
            "use-socks4-proxy", "use-socks5-proxy"
        ];
        
        public readonly static string[] otherFeatureArgs = [
            "add-extension", "browser", "disable-pycache", 
            "disable-ssl", "run-headless", "use-mobile-user-agent"
        ];
        
        //readonly static string[] browserArgs = ["brave", "chrome", "firefox", "safari", ];
        public readonly static string[] browserArgs = ["chrome", "firefox"];

        public readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];

        public readonly static string[] validProtocols = ["http://", "https://", "file://"];

        public readonly static string userScriptsDirectory = Path.Combine(DirectoryManager.AppDataDirectory, "userScripts");


        static List<string> validFiles = [];

        public readonly static Dictionary<int, string> validFilesMapping = [];

        public readonly static string noFilesFoundMessage = $"""
            BAM Manager (BAMM) was unable to find any valid .bamc files.

            Please check the 'userScripts' directory and contains atleast one .bamc file!
            
            Location: {userScriptsDirectory}
            
            If this directory wasn't already created please rerun this application.
            """;
        

        public static async Task<bool> CreateUserScriptsDirectory() // Write more detailed error handling.
        {
            
            if (string.IsNullOrEmpty(userScriptsDirectory)) 
            { 
                return false; 
            }

            try 
            {
                if (!Directory.Exists(userScriptsDirectory)) {
                    Directory.CreateDirectory(userScriptsDirectory);
                }
                
                await UserScriptUtility.WriteScriptExamples();
                return true;
            }

            catch (Exception ex)
            {
                // Spectre.Console.AnsiConsole is chosen over Errors.WriteAndReturnFalse due to:
                // The potential for Platforms not to be set properly prior to this execution.
                Spectre.Console.AnsiConsole.Write(
                    string.Join(Environment.NewLine, [
                        "An unexpected error occurred while creating userScript directory:",
                        ex.GetType().Name,
                        ex.Message
                    ])
                );
                return false;
            }
        }
        
        public static void CreateValidFilesMapping(List<string> validFiles)
        {
            if (validFiles.Count != 0)
            {
                WriteSuccessMessage(
                    message: $"BAM Manager (BAMM) located {validFiles.Count} valid .bamc files, please see below:\n"
                );
                for (int i = 0; i < validFiles.Count; i++) {
                    validFilesMapping.Add(i, validFiles[i]);
                }
            }
        }
        
        public static string DeleteCommentIfPresent(string line)
        {
            if (string.IsNullOrEmpty(line)) {
                return string.Empty;
            }

            string trimmedLine = line.Trim();

            // A comment can be the whole remainder of the line with nothing after it, as in
            // 'visit "https://example.com" //'. The IndexOf below cannot see that, because it
            // requires a trailing space. Requiring whitespace before the slashes keeps a URL's
            // 'https://' intact, since that is always preceded by a colon.
            if (trimmedLine.EndsWith("//", StringComparison.Ordinal)
                && trimmedLine.Length > 2
                && char.IsWhiteSpace(trimmedLine[^3]))
            {
                return trimmedLine[..^2].Trim();
            }

            int commentIndex = line.IndexOf(" // ");

            // If no comment is found, commentIndex will equal -1, meaning the entire line is just code.
            if (commentIndex == -1) {
                return trimmedLine;
            }
            
            // If a comment is found, it gets removed since comments aren't valid commands.
            string codePart = line[..commentIndex];

            // Trim whitespace from the code part and return it
            return codePart.Trim();
        }

        public static void DisplayValidFiles()
        {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            foreach (KeyValuePair<int, string> pair in validFilesMapping)
            {
                int index = pair.Key + 1;
                string? rawFileName = null;

                try { 
                    rawFileName = Path.GetFileName(pair.Value); 
                }
                catch { 

                }
                
                if (rawFileName != null) {
                    Spectre.Console.AnsiConsole.Write($"File {index} ----> {rawFileName}\n");
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
            Spectre.Console.AnsiConsole.Write("\n\nPress any key to exit...");
            ReadKey();
        }
        
        public static string[] GetBAMCFiles()
        {
            try
            {
                if (userScriptsDirectory == null) { return []; }
                return [.. Directory.GetFiles(userScriptsDirectory).Where(x => x.ToLower().EndsWith(".bamc"))];
            }
            catch (Exception ex)
            {
                Spectre.Console.AnsiConsole.Write(ex.GetType().Name);
                Spectre.Console.AnsiConsole.Write(ex.Message);
                return [];
            }
        }

        /// <summary> Iterates through the rawChoice string and returns the number associated with the menu choice. </summary>
        public static string? GetFileNumber(string rawInput)
        {
            var builder = new StringBuilder();
            foreach (char c in rawInput)
            {
                if (char.IsWhiteSpace(c)) { continue; }
                if (!char.IsNumber(c)) { break; }
                builder.Append(c);
            }
            return builder.Length > 0 ? builder.ToString() : null;
        }

        public static string GetValidBrowserCommands()
        {
            var builder = new StringBuilder();
            builder.AppendLine(); // Empty line for formatting purposes.
            foreach (var browser in browserArgs) {
                builder.AppendLine($"-> browser \"{browser}\"");
            }
            return builder.ToString(); 
        }

        [DoesNotReturn]
        public static void ExitOnDuplicateCommand(string fileName, string line, int i)
        {
            WriteAndExit(
                message:
                    "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                    $"File: \"{fileName}\"\n" +
                    $"Duplicate command on line {i + 1}:\n{line}\n" +
                    "All 'feature' commands may only be defined once.\n",
                status: 1
            );
        }
        

        public static void HandleBAMCFileValidation(string[] BAMCFiles)
        {
            validFiles = [.. ValidateBAMCFiles(BAMCFiles)];
            if (validFiles.Count == 0) { WriteAndExit(noFilesFoundMessage, 1); }

            if (validFilesMapping.Count != validFiles.Count) { CreateValidFilesMapping(validFiles); }

            if (validFilesMapping.Count == 0) { WriteAndExit(noFilesFoundMessage, 1); }

        }
        
        public static void HandleHelpSelection()
        {
            while (true) 
            {
                string command = Input.WriteListFromOptions([.. CommandList.Select(cmd => cmd.Name), "Exit App"]);
                Help.ShowCommandDetails(command.Trim());

                string choice = Input.AskForInput(
                    $"{Environment.NewLine}Would you like to continue learning more about BAM Manager (BAMM)? [y/n]:"
                );

                if (!choice.Equals("y")) { Environment.Exit(1); }
            }
        }
        
        public static bool HandleLineValidation(string fileName, string line, int lineNumber)
        {
            string selectorString = "selector"; // Defaults to "selector" for selector based actions
            string trimmedLine = line.Trim();

            // This is assumed as a comment
            if (line.StartsWith(" //") || line.StartsWith("//")) { return true; }

            if (line.StartsWith("add-headers"))
            {
                AddHeaders(fileName, line, lineNumber, ref selectorString);
                return true;
            }


            string[] lineArgs;
            string[] lineArgSpecialCases = ["add-header",  "fill-text", "fill-text-exp", "set-custom-useragent"];

            // Special case to handle lineArgSpecialCases
            if (lineArgSpecialCases.Any(lineArg => line.StartsWith(lineArg))) {
                lineArgs = line.Split(" \"");
            }

            // Handles all others
            else { lineArgs = line.Split(" "); }

            // DEBUG ONLY
            // foreach (var lineArg in lineArgs ){
            //     Console.WriteLine(lineArg);
            // }

            string firstArg = lineArgs[0];

            return firstArg switch
            {
                "click" or 
                "get-text" or 
                "save-as-html" or 
                "save-as-html-exp" or 
                "select-element" or 
                "take-screenshot" or 
                "visit" => BasicCommands(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "add-cookie" => AddCookie(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),
                
                "add-header" => AddHeader(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "click-at-position" => ClickAtPosition(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "click-exp" => ClickExp(fileName, line, lineNumber, firstArg, ref lineArgs, ref selectorString),

                "close-current-tab" => true,// No parsing is needed here

                "fill-text" => FillText(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "fill-text-exp" => FillTextExp(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "open-new-tab" => OpenNewTab(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "select-option" => SelectOption(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "set-custom-useragent" => SetCustomUserAgent(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "wait-for-seconds" => WaitForSeconds(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "browser" => Browser(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "feature" => Feature(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                _ => WriteErrorAndReturnBool(
                        message:
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n" +
                            $"Invalid command on line {lineNumber}.\n" +
                            $"Please check your spelling and try again.\n",
                        returnBool: false
                    ),
            };

        
        }
        
        public static int HandleUserSelection(Dictionary<int, string> mapping)
        {

            if (mapping.Count == 0) { WriteAndExit(noFilesFoundMessage, 1); }

            int numberOfFilesFound = mapping.Count;
           
            //string inputText = string.Empty;
            string[] menuOptions = new string[numberOfFilesFound];

            for (int i = 0; i < menuOptions.Length; i++) 
            {
                string? rawFileName;
                try { rawFileName = Path.GetFileName(mapping.Values.ElementAt(i)); }

                // A Silent failure is the intended behavior
                catch { continue; }

                if (rawFileName != null) { menuOptions[i] = $"{i + 1}.  {rawFileName}"; }
            }

            if (menuOptions.Length == 0) { WriteAndExit(noFilesFoundMessage, 1); }
            
            string panicText = 
                $"BAM Manager (BAMM) panicked due an invalid value provided as input.  " +
                $"Value must be between 1 and {numberOfFilesFound}";

            var rawInput = Input.WriteListFromOptions(menuOptions, "file");
            var input = GetFileNumber(rawInput);

            if (input == null) {
                WriteAndExit(panicText, 1);
            }

            if (!int.TryParse(input, out int fileNumber)) {
                WriteAndExit(panicText, 1);
            }

            if (fileNumber < 1 || fileNumber > numberOfFilesFound) {
                WriteAndExit(panicText, 1);
            }

            return fileNumber - 1; // index = fileNumber - 1;
        }

        public static bool IsValidHeaderFormat(string headerString)
        {
            if (string.IsNullOrEmpty(headerString)) { return false; }

            return PrecompiledHeaderRegex().IsMatch(headerString);
        }

        public static bool IsValidNumberFormat(string numberString)
        {
            if (string.IsNullOrEmpty(numberString)) { return false; }

            return PrecompiledNumberRegex().IsMatch(numberString);
        }

        public static bool IsValidLinkFormat(string linkString)
        {
            if (string.IsNullOrWhiteSpace(linkString)) { return false; }

            bool hasValidProtocol = false;

            foreach (string protocol in validProtocols)
            {
                if (linkString.StartsWith(protocol)) {
                    hasValidProtocol = true;
                    break;
                }
            }
            return hasValidProtocol && PrecompiledLinkRegex().IsMatch(linkString);
        }

        public static bool IsValidProxyFormat(string proxyString)
        {
            if (string.IsNullOrWhiteSpace(proxyString)) { return false; }
            return PrecompiledProxyRegex().IsMatch(proxyString);
        }

        public static bool IsValidUserAgentFormat(string userAgentString)
        {
            if (string.IsNullOrEmpty(userAgentString)) { return false; }
            return PrecompiledUserAgentRegex().IsMatch(userAgentString);
        }

        private class BAMCValidationState
        {
            public bool BrowserBlockFinished;
            public bool FeatureBlockFinished;
            public bool VisitBlockFinished;
            public bool JSBlockFinished = true;
            public string CurrentJSBlockContent = string.Empty;
            public string JSError = string.Empty;
            public int LineCurrentJSBlockStarts;
            public List<string> UsedFeatures = [];
        }

        private static string[] ReadAndCleanLines(string filePath)
        {
            return [..
                File.ReadAllLines(filePath)
                .Select(line => DeleteCommentIfPresent(line.Trim()))
                .Where(line => !string.IsNullOrWhiteSpace(line))
            ];
        }

        private static bool ValidateBrowserCommand(string fileName, string line, int lineIndex, string firstArg, BAMCValidationState state)
        {
            if (!firstArg.Equals("browser")) return false;

            if (lineIndex != 0 && state.BrowserBlockFinished)
            {
                return WriteErrorAndReturnBool(
                    message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                             $"File: \"{fileName}\"\n" +
                             $"Invalid 'browser' command location on line {lineIndex + 1}.\n" +
                             "'browser' command must be placed at the top of the file.\n",
                    returnBool: false
                );
            }

            if (!state.BrowserBlockFinished && !BrowserRegex.IsMatch(line))
            {
                return WriteErrorAndReturnBool(
                    message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                             $"File: \"{fileName}\"\n" +
                             $"Invalid browser name on \"browser\" command on line {lineIndex + 1}.\n" +
                             $"Valid Commands:\n{GetValidBrowserCommands()}",
                    returnBool: false
                );
            }

            state.BrowserBlockFinished = true;
            return true;
        }

        private static bool ValidateFeatureCommand(string fileName, string line, int lineIndex, string firstArg, string[] lineArgs, BAMCValidationState state)
        {
            if (!firstArg.Equals("feature")) return false;

            if (state.FeatureBlockFinished)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid 'feature' command location on line {lineIndex + 1}.\n" +
                        $"All 'feature' commands must be placed before any other command, except 'browser'.\n",
                    returnBool: false
                );
            }

            if (state.UsedFeatures.Contains(line))
            {
                ExitOnDuplicateCommand(fileName, line, lineIndex);
            }

            string normalizedLine = line.Replace('"', ' ').Trim();
            if (!featureArgs.Any(arg => normalizedLine.Contains(arg)))
            {
                return WriteErrorAndReturnBool(
                    message:
                        "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Unknown feature command on line {lineIndex + 1}:\n{line}\n\n" +
                        $"For more information please see, {DOCUMENTATION_LINK}",
                    returnBool: false
                );
            }

            bool isProxyFeature = lineArgs.Length == 3 &&
                                  line.Contains("use") &&
                                  line.Contains("-proxy") &&
                                  lineArgs.Any(arg => proxyFeatureArgs.Contains(arg.Replace('"', ' ').Trim()));

            if (!isProxyFeature)
            {
                state.UsedFeatures.Add(line);
            }

            return true;
        }

        private static bool ValidateProxyFeature(string fileName, string line, int lineIndex, string firstArg, string[] lineArgs, BAMCValidationState state)
        {
            if (!firstArg.Equals("feature")) return false;

            var invalidProxyFeatureMessage =
                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                $"File: \"{fileName}\"\n" +
                $"Invalid syntax on line {lineIndex + 1}\n" +
                $"Line: {line}\n" +
                $"Valid Syntax: {firstArg} \"use-x-proxy\" USER:PASS@IP:PORT\n" +
                "Replace x is one of the following:\n" +
                "   -> http\n" +
                "   -> https\n" +
                "   -> socks4\n" +
                "   -> socks5\n" +
                $"If no authentication is required: NULL:NULL@IP:PORT\n";

            var intendedToUseProxyMessage =
                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                $"File: \"{fileName}\"\n" +
                $"Invalid syntax on line {lineIndex + 1}\n" +
                $"Line: {line}" + "\n\n" +
                "If you were attempting to add a proxy to your .BAMC file, please run one of the following commands:\n" +
                "bamm help use-http-proxy\n" +
                "bamm help use-https-proxy\n" +
                "bamm help use-socks4-proxy\n" +
                "bamm help use-socks5-proxy\n";

            var potentialProxyLine =
                firstArg.Equals("feature") &&
                lineArgs.Length == 3 &&
                line.Contains("use") &&
                line.Contains("-proxy");

            var proxyFeatureFound =
                lineArgs.Any(arg => proxyFeatureArgs.Contains(arg.Replace('"', ' ').Trim()));

            if (potentialProxyLine && !proxyFeatureFound)
            {
                return WriteErrorAndReturnBool(intendedToUseProxyMessage, false);
            }

            if (potentialProxyLine && proxyFeatureFound)
            {
                string proxyFeatureString = lineArgs[1].Replace('"', ' ').Trim();
                string proxyString = lineArgs[2].Replace("\"", "");

                if (state.UsedFeatures.Any(feature => feature.Contains(proxyFeatureString)))
                {
                    ExitOnDuplicateCommand(fileName, line, lineIndex);
                }

                if (!IsValidProxyFormat(proxyString))
                {
                    WriteAndExit(invalidProxyFeatureMessage, 1);
                }

                state.UsedFeatures.Add(line);
                return true;
            }

            return false;
        }

        private static bool ValidateVisitCommand(string fileName, string line, int lineIndex, string[] lines, BAMCValidationState state)
        {
            if (!line.StartsWith("visit")) return false;

            List<string> invalidLines = [];
            List<string> passedLines = [.. lines.Take(lineIndex + 1)];
            string[] availableCommands = ["browser", "feature", "visit"];

            invalidLines = [..
                passedLines.Where(
                    l => !availableCommands.Any(prefix => l.Trim().StartsWith(prefix)) &&
                         !l.Trim().StartsWith("//")
                )
            ];

            if (invalidLines.Count > 0)
            {
                WriteAndExit(
                    message:
                        GenerateErrorMessage(fileName, line, lineIndex + 1,
                            issueText:
                                "A 'visit' command must be placed after 'browser' and 'feature' commands." +
                                $"{Environment.NewLine}{Environment.NewLine}Example:{Environment.NewLine}{Environment.NewLine}" +
                                $"browser \"firefox\"{Environment.NewLine}" +
                                $"feature \"run-headless\"{Environment.NewLine}" +
                                $"feature \"disable-pycache\"{Environment.NewLine}" +
                                $"visit \"https://google.com\"{Environment.NewLine}"
                        ),
                    status: 1
                );
            }

            state.VisitBlockFinished = true;
            return true;
        }

        private static bool ValidateJSBlockCommand(string line, BAMCValidationState state)
        {
            if (line.StartsWith("start-javascript"))
            {
                state.JSBlockFinished = false;
                return true;
            }

            if (line.StartsWith("end-javascript"))
            {
                state.JSBlockFinished = true;
                state.CurrentJSBlockContent = string.Empty;
                return true;
            }

            return false;
        }

        private static bool ValidateBAMCLines(string[] lines, string fileName, bool writeSuccessOnEnd)
        {
            var state = new BAMCValidationState();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (!state.JSBlockFinished)
                {
                    BuildJSBlock(
                        fileName, 
                        line, 
                        [..lines], 
                        i, 
                        ref state.CurrentJSBlockContent, 
                        ref state.LineCurrentJSBlockStarts, 
                        ref state.JSBlockFinished, 
                        ref state.JSError
                    );
                    continue;
                }

                string[] lineArgs = line.Split(" ");
                var firstArg = lineArgs[0];

                if (ValidateBrowserCommand(fileName, line, i, firstArg, state))
                    continue;

                if (ValidateFeatureCommand(fileName, line, i, firstArg, lineArgs, state))
                    continue;

                if (ValidateProxyFeature(fileName, line, i, firstArg, lineArgs, state))
                    continue;

                if (ValidateVisitCommand(fileName, line, i, lines, state))
                    continue;

                if (ValidateJSBlockCommand(line, state))
                    continue;

                if (!HandleLineValidation(fileName, line, i + 1))
                    return false;

                if (!line.StartsWith("//") && !firstArg.Equals("feature"))
                {
                    state.FeatureBlockFinished = true;
                }
            }

            if (writeSuccessOnEnd)
            {
                WriteSuccessMessage("Validated the provided file's contents, you can now click 'Export Script'");
            }

            return true;
        }

        public static bool IsValidFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);

            try
            {
                string[] lines = ReadAndCleanLines(filePath);
                return ValidateBAMCLines(lines, fileName, writeSuccessOnEnd: false);
            }

            catch (FileNotFoundException)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAMC Validation Error:\n\n" +
                        $"Error: File not found: '{fileName}'.\n",
                    returnBool: false
                );
            }

            catch (UnauthorizedAccessException)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"Permission was denied for '{fileName}'.\n",
                    false
                );
            }

            catch (IOException ex)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"An IO Exception occurred while validating: '{fileName}'\n" +
                        $"Error: {ex.Message}\n",
                    returnBool: false
                );
            }

            catch (Exception ex)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"A fatal error occurred while validating:'{fileName}'\n" +
                        $"Error: {ex}\n",
                    returnBool: false
                );
            }
        }

        // Used in EndpointFunctions.Validate
        //
        // This can be optimized by:
        //
        // - Creating a temp file with the contents streamed in then call IsValidFile(), 
        public static bool IsValidFileContents(string[] lines)
        {
            var fileName = "new_file.bamc";

            try
            {
                return ValidateBAMCLines(lines, fileName, writeSuccessOnEnd: true);
            }

            catch (Exception ex)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"A fatal error occurred while validating:'{fileName}'\n" +
                        $"Error: {ex}\n",
                    returnBool: false
                );
            }
        }
        

        

        public static string[] ValidateBAMCFiles(string[] BAMCFiles)
        {
            return [.. BAMCFiles.Where(file => IsValidFile(file))];
        }
    }
}
