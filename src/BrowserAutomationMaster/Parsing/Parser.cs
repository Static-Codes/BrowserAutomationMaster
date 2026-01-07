using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.CommandManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static BrowserAutomationMaster.Parsing.LineValidation;

namespace BrowserAutomationMaster.Parsing
{
    public partial class Parser
    {
        public readonly static string[] actionArgs = [
            "add-header", "add-headers", "click", "click-at-position", "click-exp", "close-current-tab", 
            "end-javascript", "fill-text", "fill-text-exp", "get-text", "open-new-tab", "save-as-html", 
            "save-as-html-exp", "select-element", "select-option", "set-custom-useragent", "start-javascript", 
            "take-screenshot", "wait-for-seconds", "visit"
        ];

        public readonly static string[] proxyFeatureArgs = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] otherFeatureArgs = ["add-extension", "browser", "disable-pycache", "disable-ssl", "run-headless"];
        
        //readonly static string[] browserArgs = ["brave", "chrome", "firefox", "safari", ];
        public readonly static string[] browserArgs = ["chrome", "firefox", "safari", ];

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
        

        public static bool CreateUserScriptsDirectory() // Write more detailed error handling.
        {
            
            if (string.IsNullOrEmpty(userScriptsDirectory)) 
            { 
                return false; 
            }

            

            if (Directory.Exists(userScriptsDirectory)) 
            {
                UserScriptExamples.WriteScriptExamples();
                return true; 
            }
            
            else
            {
                try
                {
                    Directory.CreateDirectory(userScriptsDirectory);
                    UserScriptExamples.WriteScriptExamples();
                    return true;
                }
                catch (ArgumentNullException ane)
                {
                    Spectre.Console.AnsiConsole.Write(ane.GetType().Name);
                    Spectre.Console.AnsiConsole.Write(ane.Message);
                    return false;
                }
                catch (UnauthorizedAccessException uae)
                {
                    Spectre.Console.AnsiConsole.Write(uae.GetType().Name);
                    Spectre.Console.AnsiConsole.Write(uae.Message);
                    return false;
                }
                catch (PathTooLongException ptle)
                {
                    Spectre.Console.AnsiConsole.Write(ptle.GetType().Name);
                    Spectre.Console.AnsiConsole.Write(ptle.Message);
                    return false;
                }
                catch (DirectoryNotFoundException dnfe)
                {
                    Spectre.Console.AnsiConsole.Write(dnfe.GetType().Name);
                    Spectre.Console.AnsiConsole.Write(dnfe.Message);
                    return false;
                }
                catch (IOException ie)
                {
                    Spectre.Console.AnsiConsole.Write(ie.GetType().Name);
                    Spectre.Console.AnsiConsole.Write(ie.Message);
                    return false;
                }
                catch (Exception ex)
                {
                    Spectre.Console.AnsiConsole.Write($"An unexpected error occurred while creating userScript directory:\n{ex.GetType().Name}");
                    Spectre.Console.AnsiConsole.Write(ex.Message);
                    return false;
                }
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

            int commentIndex = line.IndexOf(" // ");

            // If no comment is found, commentIndex will equal -1, meaning the entire line is just code.
            if (commentIndex == -1) {                
                return line.Trim();
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
                
                if (rawFileName != null)
                    Spectre.Console.AnsiConsole.Write($"File {index} ----> {rawFileName}\n");
                
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
        
        public static string? GetFileNumber(string rawInput)
        {
            var builder = new StringBuilder();
            foreach (char c in rawInput)
            {
                if (char.IsWhiteSpace(c)) { continue; }
                if (!char.IsNumber(c))
                {
                    break;
                }
                builder.Append(c);
            }
            return builder.Length > 0 ? builder.ToString() : null;
        }

        public static string GetValidBrowserCommands()
        {
            var builder = new StringBuilder();
            builder.AppendLine(); // Empty line for formatting purposes.
            foreach (var browser in browserArgs)
            {
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
            if (validFiles.Count == 0)
            {
                WriteAndExit(noFilesFoundMessage, 1);
            }
            if (validFilesMapping.Count != validFiles.Count)
            {
                CreateValidFilesMapping(validFiles);
            }
            if (validFilesMapping.Count == 0)
            {
                WriteAndExit(noFilesFoundMessage, 1);
            }

        }
        
        public static void HandleHelpSelection()
        {
            while (true) 
            {
                string command = Input.WriteListFromOptions([.. CommandList.Select(cmd => cmd.Name), "Exit App"]);
                Help.ShowCommandDetails(command.Trim());

                string choice = Input.AskForInput(
                    "\nWould you like to continue learning more about BAM Manager (BAMM)? [y/n]:"
                );
                if (!choice.Equals("y")) {
                    Environment.Exit(1);
                }
            }
        }
        
        public static bool HandleLineValidation(string fileName, string line, int lineNumber)
        {
            string selectorString = "selector"; // Defaults to "selector" for selector based actions
            string trimmedLine = line.Trim();

            if (line.StartsWith(" //") || line.StartsWith("//"))
                return true; // This is assumed as a comment

            if (line.StartsWith("add-headers"))
            {
                AddHeaders(fileName, line, lineNumber, ref selectorString);
                return true;
            }


            string[] lineArgs;
            string[] lineArgSpecialCases = ["add-header",  "fill-text", "fill-text-exp", "set-custom-useragent"];

            // Special case to handle lineArgSpecialCases
            if (lineArgSpecialCases.Any(lineArg => line.StartsWith(lineArg)))
                lineArgs = line.Split(" \"");

            // Handles all others
            else
            {
                lineArgs = line.Split(" ");
            }

            // DEBUG ONLY
            // foreach (var lineArg in lineArgs ){
            //     Console.WriteLine(lineArg);
            // }

            string firstArg = lineArgs[0];

            return firstArg switch
            {
                "click" or "get-text" or "save-as-html" or "save-as-html-exp" or "select-element" or "take-screenshot" or "visit" => BasicCommands(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "add-header" => AddHeader(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "click-at-position" => ClickAtPosition(fileName, line, lineNumber, firstArg, ref selectorString),

                "click-exp" => ClickExp(fileName, line, lineNumber, firstArg, ref lineArgs, ref selectorString),

                "close-current-tab" => true,// No parsing is needed here

                "fill-text" => FillText(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "fill-text-exp" => FillTextExp(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "open-new-tab" => OpenNewTab(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "select-option" => SelectOption(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "set-custom-useragent" => SetCustomUserAgent(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "wait-for-seconds" => WaitForSeconds(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                "browser" => Browser(fileName, line, lineNumber, firstArg, lineArgs),

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

            if (mapping.Count == 0)
                WriteAndExit(noFilesFoundMessage, 1);

            int numberOfFilesFound = mapping.Count;
           
            //string inputText = string.Empty;
            string[] menuOptions = new string[numberOfFilesFound];

            for (int i = 0; i < menuOptions.Length; i++) 
            {
                string? rawFileName;
                try 
                { 
                    rawFileName = Path.GetFileName(mapping.Values.ElementAt(i)); 
                }
                catch 
                {
                    continue; // Silent continue is the intended be
                }
                if (rawFileName != null)
                    menuOptions[i] = $"{i + 1}.  {rawFileName}";
            }

            if (menuOptions.Length == 0) 
                WriteAndExit(noFilesFoundMessage, 1); 

            string panicText = 
                $"BAM Manager (BAMM) panicked due an invalid value provided as input.  " +
                $"Value must be between 1 and {numberOfFilesFound}";

            var rawInput = Input.WriteListFromOptions(menuOptions, "file");
            var input = GetFileNumber(rawInput);

            if (input == null)
                WriteAndExit(panicText, 1);

            if (!int.TryParse(input, out int fileNumber))
                WriteAndExit(panicText, 1);

            if (fileNumber < 1 || fileNumber > numberOfFilesFound)
                WriteAndExit(panicText, 1);

            return fileNumber - 1; // index = fileNumber - 1;
        }

        public static bool IsValidHeaderFormat(string headerString)
        {
            if (string.IsNullOrEmpty(headerString))
                return false;

            return PrecompiledHeaderRegex().IsMatch(headerString);
        }

        public static bool IsValidNumberFormat(string numberString)
        {
            if (string.IsNullOrEmpty(numberString))
                return false;

            return PrecompiledNumberRegex().IsMatch(numberString);
        }

        public static bool IsValidLinkFormat(string linkString)
        {
            if (string.IsNullOrWhiteSpace(linkString))
                return false;

            bool hasValidProtocol = false;

            foreach (string protocol in validProtocols)
            {
                if (linkString.StartsWith(protocol))
                {
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

        public static bool IsValidFile(string filePath)
        {
            List<string> usedFeatures = [];
            var fileName = Path.GetFileName(filePath);

            try
            {
                List<string> lines = [..
                    File.ReadAllLines(filePath)
                    .Select(line => DeleteCommentIfPresent(line.Trim()))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                ];

                var browserBlockFinished = false;
                var featureBlockFinished = false;
                var visitBlockFinished = false;
                var jsBlockFinished = true; // Starts off as true and will change below

                var currentJSBlockContent = string.Empty;
                var jsError = string.Empty;

                int lineCurrentJSBlockStarts = 0; // Will be modified assuming a javascript block is provided.

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];

                    if (!jsBlockFinished)
                    {
                        BuildJSBlock(fileName, line, lines, i, ref currentJSBlockContent, ref lineCurrentJSBlockStarts, ref jsBlockFinished, ref jsError);
                        continue;
                    }

                    string[] lineArgs = line.Split(" ");

                    if (lineArgs.Length == 0)
                    {
                        return false;
                    }

                    var firstArg = lineArgs[0];


                    #region Start of Browser Feature Check

                    // If a browser command is present in any line but the first line that contains characters.
                    if (firstArg.Equals("browser") && i != 0 && browserBlockFinished)
                    {
                        return WriteErrorAndReturnBool(
                            message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                                     $"File: \"{fileName}\"\n" +
                                     $"Invalid 'browser' command location on line {i + 1}.\n" +
                                     "'browser' command must be placed at the top of the file.\n",
                            returnBool: false
                        );
                    }

                    if (firstArg.Equals("browser") && !browserBlockFinished && !BrowserRegex.IsMatch(line))
                    {
                        // The error message here appears to be the same as the first one, 
                        // but the failure reason is different.
                        return WriteErrorAndReturnBool(
                            message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                                     $"File: \"{fileName}\"\n" +
                                     $"Invalid browser name on \"browser\" command on line {i + 1}.\n" +
                                     $"Valid Commands:\n{GetValidBrowserCommands()}",
                            returnBool: false
                        );
                    }

                    if (firstArg.Equals("browser") && BrowserRegex.IsMatch(line))
                    {
                        browserBlockFinished = true;
                        continue;
                    }

                    #endregion End of Browser Feature Check


                    #region Start of Invalid Feature Check

                    // If a feature name is provided after defining non feature actions
                    else if (firstArg.Equals("feature") && featureBlockFinished)
                    {
                        return WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid 'feature' command location on line {i + 1}.\n" +
                                $"All 'feature' commands must be placed before any other command, except 'browser'.\n",
                            returnBool: false
                        );
                    }

                    // If a duplicate feature name is provided -> feature "duplicate-name"
                    else if (firstArg.Equals("feature") && usedFeatures.Contains(line))
                    {
                        ExitOnDuplicateCommand(fileName, line, i);
                    }

                    // If an invalid feature name is provided -> feature "invalid-name"
                    else if (firstArg.Equals("feature") && !featureArgs.Any(arg => line.Contains(arg)))
                    {
                        return WriteErrorAndReturnBool(
                            message:
                                "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Unknown feature command on line {i + 1}:\n{line}\n\n" +
                                $"For more information please see, {DOCUMENTATION_LINK}",
                            returnBool: false
                        );
                    }

                    #endregion Start of Invalid Feature Check


                    # region Start of Proxy Feature Check

                    var invalidProxyFeatureMessage =
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {i + 1}\n" +
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
                        $"Invalid syntax on line {i + 1}\n" +
                        $"Line: {line}" + "\n\n" +
                        "If you were attempting to add a proxy to your .BAMC file, please run one of the following commands:\n" +
                        "bamm help use-http-proxy\n" +
                        "bamm help use-https-proxy\n" +
                        "bamm help use-socks4-proxy\n" +
                        "bamm help use-socks5-proxy\n";

                    // Loose check for a line containing -> feature use....-proxy
                    // This is not sanitized and is treated as such until IsValidProxyFormat() is called below.
                    var potentialProxyLine =
                        firstArg.Equals("feature") &&
                        lineArgs.Length == 3 &&
                        line.Contains("use") &&
                        line.Contains("-proxy");

                    // Checks if use-x-proxy is found where x can be one of the 4 proxy types from intendedToUseProxyMessage
                    var proxyFeatureFound =
                        lineArgs.Any(arg => proxyFeatureArgs.Contains(arg.Replace('"', ' ').Trim()));

                    if (potentialProxyLine && !proxyFeatureFound)
                    {
                        return WriteErrorAndReturnBool(intendedToUseProxyMessage, false);
                    }

                    Action AddValidatedProxy() => () =>
                    {
                        string proxyFeatureString = lineArgs[1].Replace('"', ' ').Trim();
                        string proxyString = lineArgs[2].Replace("\"", "");

                        // Only one Proxy feature command is permitted per script.
                        if (usedFeatures.Any(feature => feature.Contains(proxyFeatureString)))
                            ExitOnDuplicateCommand(fileName, line, i);

                        if (!IsValidProxyFormat(proxyString))
                            WriteAndExit(invalidProxyFeatureMessage, 1);

                        usedFeatures.Add(line);

                    };

                    if (potentialProxyLine && proxyFeatureFound)
                    {
                        AddValidatedProxy();
                        continue;
                    }

                    #endregion End of Proxy Feature Check


                    #region Start of Visit Feature Check
                    
                    if (firstArg.Equals("visit") && visitBlockFinished)
                        return true;

                    List<string> invalidLines = [];

                    if (firstArg.Equals("visit"))
                    {
                        List<string> passedLines = [.. lines.Take(i + 1)];
                        string[] availableCommands = ["browser", "feature", "visit"];

                        invalidLines = [..
                            passedLines.Where(
                                line => !availableCommands.Any(prefix => line.Trim().StartsWith(prefix)) &&
                                !line.Trim().StartsWith("//") // Ignores comments
                        
                            )
                        ];
                    }

                    if (invalidLines.Count > 0)
                        WriteAndExit(
                            message:
                                GenerateErrorMessage(fileName, line, i,
                                    issueText:
                                        $"A 'visit' command must be placed after 'browser' and 'feature' commands." +
                                        $"\n\nExample:\n\n" +
                                        "browser \"firefox\"\n" +
                                        "feature \"run-headless\"\n" +
                                        "feature \"disable-pycache\"\n" +
                                        "visit \"https://google.com\"\n"
                                ),
                            status: 1
                        );

                    #endregion End of Visit Feature Check


                    #region Start of JS Feature Check

                    else if (line.StartsWith("start-javascript"))
                        jsBlockFinished = false;

                    else if (line.StartsWith("end-javascript"))
                    {
                        jsBlockFinished = true;
                        currentJSBlockContent = string.Empty;
                    }

                    else if (!HandleLineValidation(fileName, line, i + 1))
                        return false;

                    // Ignores comments
                    if (!line.StartsWith("//"))
                        // This flag will be used to ensure all 'feature' commands are placed before all other commands, excluding 'browser'.
                        featureBlockFinished = true;

                    #endregion End of JS Feature Check

                }

                // Leaving this outside the for loop saves 1 execution cycle per valid line within a .BAMC file
                // Support for async and bypass-cloudflare were removed in BAMM v1.0.0A3
                // This will be uncommented if support is reintroduced.

                //if (
                //    usedFeatures.Any(x => x.Contains("async")) &&
                //    usedFeatures.Any(x => x.Contains("bypass-cloudflare"))
                //)
                //    return WriteErrorAndReturnBool(
                //        message:
                //            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                //            $"File: \"{fileName}\"\n\n" +
                //            $"Error: Script cannot contain both \"async\" and \"bypass-cloudflare\"\n",
                //        returnBool: false
                //    );

                return true;
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

            // Handles locked files, network errors, etc.
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

            // General catchall (LOG MORE SEVERLY IF HIT) 
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
        // This is extremely sloppy, I'm the first one to acknowledge so, 
        // I could create a temp file with the contents streamed in then call IsValidFile(), 
        // but im quite busy so this will have to do.
        public static bool IsValidFileContents(string[] lines)
        {
            List<string> usedFeatures = [];
            var fileName = "new_file.bamc";

            try
            {
                var browserBlockFinished = false;
                var featureBlockFinished = false;
                var visitBlockFinished = false;
                var jsBlockFinished = true; // Starts off as true and will change below

                var currentJSBlockContent = string.Empty;
                var jsError = string.Empty;

                int lineCurrentJSBlockStarts = 0; // Will be modified assuming a javascript block is provided.

                for (int i = 0; i < lines.Length; i++)
                {

                    var line = lines[i];

                    if (!jsBlockFinished)
                    {
                        BuildJSBlock(fileName, line, [.. lines], i, ref currentJSBlockContent, ref lineCurrentJSBlockStarts, ref jsBlockFinished, ref jsError);
                        continue;
                    }

                    string[] lineArgs = line.Split(" ");

                    if (lineArgs.Length == 0)
                    {
                        return false;
                    }

                    var firstArg = lineArgs[0];


                    #region Start of Browser Feature Check

                    // If a browser command is present in any line but the first line that contains characters.
                    if (firstArg.Equals("browser") && i != 0 && browserBlockFinished)
                    {
                        return WriteErrorAndReturnBool(
                            message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                                     $"File: \"{fileName}\"\n" +
                                     $"Invalid 'browser' command location on line {i + 1}.\n" +
                                     "'browser' command must be placed at the top of the file.\n",
                            returnBool: false
                        );
                    }

                    if (firstArg.Equals("browser") && !browserBlockFinished && !BrowserRegex.IsMatch(line))
                    {
                        // The error message here appears to be the same as the first one, 
                        // but the failure reason is different.
                        return WriteErrorAndReturnBool(
                            message: $"BAM Manager (BAMM) ran into a BAMC validation error:\n" +
                                     $"File: \"{fileName}\"\n" +
                                     $"Invalid browser name on \"browser\" command on line {i + 1}.\n" +
                                     $"Valid Commands:\n{GetValidBrowserCommands()}",
                            returnBool: false
                        );
                    }

                    if (firstArg.Equals("browser") && BrowserRegex.IsMatch(line))
                    {
                        browserBlockFinished = true;
                        continue;
                    }

                    #endregion End of Browser Feature Check


                    #region Start of Invalid Feature Check

                    // If a feature name is provided after defining non feature actions
                    else if (firstArg.Equals("feature") && featureBlockFinished)
                    {
                        return WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid 'feature' command location on line {i + 1}.\n" +
                                $"All 'feature' commands must be placed before any other command, except 'browser'.\n",
                            returnBool: false
                        );
                    }

                    // If a duplicate feature name is provided -> feature "duplicate-name"
                    else if (firstArg.Equals("feature") && usedFeatures.Contains(line)) {
                        ExitOnDuplicateCommand(fileName, line, i);
                    }


                    // If an invalid feature name is provided -> feature "invalid-name"
                    else if (firstArg.Equals("feature") && !featureArgs.Any(arg => line.Contains(arg.Replace('"', ' ').Trim())))
                    {
                        return WriteErrorAndReturnBool(
                            message:
                                "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Unknown feature command on line {i + 1}:\n{line}\n\n" +
                                $"For more information please see, {DOCUMENTATION_LINK}",
                            returnBool: false
                        );
                    }

                    #endregion Start of Invalid Feature Check


                    # region Start of Proxy Feature Check

                    var invalidProxyFeatureMessage =
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {i + 1}\n" +
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
                        $"Invalid syntax on line {i + 1}\n" +
                        $"Line: {line}" + "\n\n" +
                        "If you were attempting to add a proxy to your .BAMC file, please run one of the following commands:\n" +
                        "bamm help use-http-proxy\n" +
                        "bamm help use-https-proxy\n" +
                        "bamm help use-socks4-proxy\n" +
                        "bamm help use-socks5-proxy\n";

                    // Loose check for a line containing -> feature use....-proxy
                    // This is not sanitized and is treated as such until IsValidProxyFormat() is called below.
                    var potentialProxyLine =
                        firstArg.Equals("feature") &&
                        lineArgs.Length == 3 &&
                        line.Contains("use") &&
                        line.Contains("-proxy");

                    // Checks if use-x-proxy is found where x can be one of the 4 proxy types from intendedToUseProxyMessage
                    var proxyFeatureFound =
                        lineArgs.Any(arg => proxyFeatureArgs.Contains(arg.Replace('"', ' ').Trim()));

                    if (potentialProxyLine && !proxyFeatureFound)
                        return WriteErrorAndReturnBool(intendedToUseProxyMessage, false);


                    Action AddValidatedProxy() => () =>
                    {
                        string proxyFeatureString = lineArgs[1].Replace('"', ' ').Trim();
                        string proxyString = lineArgs[2].Replace("\"", "");

                        // Only one Proxy feature command is permitted per script.
                        if (usedFeatures.Any(feature => feature.Contains(proxyFeatureString)))
                        {
                            ExitOnDuplicateCommand(fileName, line, i);
                        }

                        if (!IsValidProxyFormat(proxyString))
                        {
                            WriteAndExit(invalidProxyFeatureMessage, 1);
                        }

                        usedFeatures.Add(line);

                    };

                    if (potentialProxyLine && proxyFeatureFound)
                    {
                        AddValidatedProxy();
                        continue;
                    }

                    #endregion End of Proxy Feature Check


                    #region Start of Visit Feature Check
                    
                    if (firstArg.Equals("visit") && visitBlockFinished)
                    {
                        return true;
                    }

                    List<string> invalidLines = [];

                    if (firstArg.Equals("visit"))
                    {
                        List<string> passedLines = [.. lines.Take(i + 1)];
                        string[] availableCommands = ["browser", "feature", "visit"];

                        invalidLines = [..
                            passedLines.Where(
                                line => !availableCommands.Any(prefix => line.Trim().StartsWith(prefix)) &&
                                !line.Trim().StartsWith("//") // Ignores comments
                        
                            )
                        ];
                    }

                    if (invalidLines.Count > 0)
                    {
                        WriteAndExit(
                            message:
                                GenerateErrorMessage(fileName, line, i,
                                    issueText:
                                        $"A 'visit' command must be placed after 'browser' and 'feature' commands." +
                                        $"\n\nExample:\n\n" +
                                        "browser \"firefox\"\n" +
                                        "feature \"run-headless\"\n" +
                                        "feature \"disable-pycache\"\n" +
                                        "visit \"https://google.com\"\n"
                                ),
                            status: 1
                        );
                    }

                    #endregion End of Visit Feature Check


                    #region Start of JS Feature Check

                    else if (line.StartsWith("start-javascript"))
                    {
                        jsBlockFinished = false;
                    }

                    else if (line.StartsWith("end-javascript"))
                    {
                        jsBlockFinished = true;
                        currentJSBlockContent = string.Empty;
                    }

                    else if (!HandleLineValidation(fileName, line, i + 1))
                    {
                        return false;
                    }

                    // Ignores comments
                    if (!line.StartsWith("//"))
                    {
                        // This flag will be used to ensure all 'feature' commands are placed before all other commands, excluding 'browser'.
                        featureBlockFinished = true;
                    }

                    #endregion End of JS Feature Check
                }

                // Leaving this outside the for loop saves 1 execution cycle per valid line within a .BAMC file
                // Support for async and bypass-cloudflare were removed in BAMM v1.0.0A3
                // This will be uncommented if support is reintroduced.

                //if (
                //    usedFeatures.Any(x => x.Contains("async")) &&
                //    usedFeatures.Any(x => x.Contains("bypass-cloudflare"))
                //)
                //    return WriteErrorAndReturnBool(
                //        message:
                //            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                //            $"File: \"{fileName}\"\n\n" +
                //            $"Error: Script cannot contain both \"async\" and \"bypass-cloudflare\"\n",
                //        returnBool: false
                //    );

                WriteSuccessMessage("Validated the provided file's contents, you can now click 'Export Script'");
                return true;
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

            // Handles locked files, network errors, etc.
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

            // General catchall (LOG MORE SEVERLY IF HIT) 
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
