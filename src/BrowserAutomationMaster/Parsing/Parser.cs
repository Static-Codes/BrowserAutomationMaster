using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Managers.CommandManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Parsing.LineValidation;
using System.Diagnostics.CodeAnalysis;


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
        readonly static string[] otherFeatureArgs = ["browser", "disable-pycache", "disable-ssl", "run-headless"];
        
        //readonly static string[] browserArgs = ["brave", "chrome", "firefox", "safari", ];
        public readonly static string[] browserArgs = ["chrome", "firefox", "safari", ];

        public readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];

        public readonly static string[] validProtocols = ["http://", "https://", "file://"];

        readonly static string userScriptsDirectory = DirectoryManager.GetUserScriptDirectory();

        static string selectedFile = string.Empty;

        static List<string> validFiles = [];

        readonly static Dictionary<int, string> validFilesMapping = [];

        static string noFilesFoundMessage = "";
        const string HeaderFormatPattern = @"^add-headers\s*(?<json>\{\s*(?:""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*""(?:\s*,\s*""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*"")*)?\s*\})$";
        const string LinkFormatPattern = @"(?i)\b(http|https|file|ftp?://(?:(?:(?:[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?\.)*(?:[a-z\u00a1-\uffff]{2,}|[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?)\.?)|(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|\[(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?:(?::[0-9a-fA-F]{1,4}){1,6})|:(?:(?::[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(?::[0-9a-fA-F]{0,4}){0,4}%[a-zA-Z0-9._~%-]+|::(?:ffff(?::0{1,4}){0,1}:){0,1}(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|(?:[0-9a-fA-F]{1,4}:){1,4}:(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d))\]))(?::\d{2,5})?(?:[/?#][^\s<>""']*)?\b";

        const string ProxyFormatPattern = @"^([^:]+):([^@]+)@([^:]+):(\d+)$";
        const string NumberFormatPattern = @"^(?:\d+(?:\.\d{1,3})?|\.\d{1,3})$";
        const string UserAgentFormatPattern = "^[^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?(?:[ ][^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?)*$";


        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.

        [GeneratedRegex(HeaderFormatPattern)]
        public static partial Regex PrecompiledHeaderRegex(); // Public declaration required for usage in Transpiler.HandleCompilation

        [GeneratedRegex(LinkFormatPattern)]
        private static partial Regex PrecompiledLinkRegex();
        
        [GeneratedRegex(NumberFormatPattern)]
        private static partial Regex PrecompiledNumberRegex();

        [GeneratedRegex(ProxyFormatPattern)]
        private static partial Regex PrecompiledProxyRegex();

        [GeneratedRegex(UserAgentFormatPattern)]
        private static partial Regex PrecompiledUserAgentRegex();

        public static bool CreateUserScriptsDirectory() // Write more detailed error handling.
        {
            
            if (string.IsNullOrEmpty(userScriptsDirectory)) { return false; }
            noFilesFoundMessage = $"""
            BAM Manager (BAMM) was unable to find any valid .bamc files.
            
            Please check the 'userScripts' directory and contains atleast one .bamc file!

            Location: {userScriptsDirectory}

            If this directory wasn't already created please rerun this application.
            """;

            if (Directory.Exists(userScriptsDirectory)) {
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
                Success.WriteSuccessMessage(
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
                string? rawFileName;
                try { rawFileName = Path.GetFileName(pair.Value); }
                catch { rawFileName = null; }
                if (rawFileName != null)
                {
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
        
        [DoesNotReturn]
        public static void ExitOnDuplicateCommand(string fileName, string line, int i)
        {
            Errors.WriteAndExit(
                message:
                    "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                    $"File: \"{fileName}\"\n" +
                    $"Duplicate command on line {i + 1}:\n{line}\n" +
                    "All 'feature' commands may only be defined once.\n",
                status: 1
            );
        }
        public static string[] ValidateBAMCFiles(string[] BAMCFiles)
        {
            return [.. BAMCFiles.Where(file => IsValidFile(file))];
        }

        public static bool IsValidHeaderFormat(string headerString) {
            if (string.IsNullOrEmpty(headerString))
                return false; 

            return PrecompiledHeaderRegex().IsMatch(headerString);
        }
        public static bool IsValidNumberFormat(string numberString) {
            if (string.IsNullOrEmpty(numberString))
                return false;

            return PrecompiledNumberRegex().IsMatch(numberString);
        }

        public static bool IsValidLinkFormat(string linkString) {
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
        public static bool IsValidProxyFormat(string proxyString) {
            if (string.IsNullOrWhiteSpace(proxyString)) { return false; }
            return PrecompiledProxyRegex().IsMatch(proxyString);
        }
        public static bool IsValidUserAgentFormat(string userAgentString) {
            if (string.IsNullOrEmpty(userAgentString)) { return false; }
            return PrecompiledUserAgentRegex().IsMatch(userAgentString);
        }
        public static void HandleBAMCFileValidation(string[] BAMCFiles)
        {
            validFiles = [.. ValidateBAMCFiles(BAMCFiles)];
            if (validFiles.Count == 0)
            {
                Errors.WriteAndExit(noFilesFoundMessage, 1);
            }
            if (validFilesMapping.Count != validFiles.Count)
            {
                CreateValidFilesMapping(validFiles);
            }
            if (validFilesMapping.Count == 0)
            {
                Errors.WriteAndExit(noFilesFoundMessage, 1);
            }

        }
        public static void HandleHelpSelection()
        {
            while (true) {
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
        private static bool HandleLineValidation(string fileName, string line, int lineNumber)
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

            // Handle all others
            else
                lineArgs = line.Split(" ");

            string firstArg = lineArgs[0];

            return firstArg switch
            {
                "click" or "get-text" or "save-as-html" or "save-as-html-exp" or "select-element" or "take-screenshot" or "visit" => BasicCommands(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

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

                "browser" => Browser(fileName, line, lineNumber, firstArg, lineArgs),

                "feature" => Feature(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString),

                _ => Errors.WriteErrorAndReturnBool(
                        message:
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n" +
                            $"Invalid command on line {lineNumber}.\n" +
                            $"Please check your spelling and try again.\n",
                        returnBool: false
                    ),
            };

        
        }
        private static int HandleUserSelection(Dictionary<int, string> mapping)
        {

            if (mapping.Count == 0)
                Errors.WriteAndExit(noFilesFoundMessage, 1);

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
                Errors.WriteAndExit(noFilesFoundMessage, 1); 

            string panicText = 
                $"BAM Manager (BAMM) panicked due an invalid value provided as input.  " +
                $"Value must be between 1 and {numberOfFilesFound}";

            var rawInput = Input.WriteListFromOptions(menuOptions, "file");
            var input = GetFileNumber(rawInput);

            if (input == null)
                Errors.WriteAndExit(panicText, 1);

            if (!int.TryParse(input, out int fileNumber))
                Errors.WriteAndExit(panicText, 1);

            if (fileNumber < 1 || fileNumber > numberOfFilesFound)
                Errors.WriteAndExit(panicText, 1);

            return fileNumber - 1; // index = fileNumber - 1;
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
                        return false;

                    var firstArg = lineArgs[0];


                    #region Start of Browser Feature Check

                    // If a browser command is present in any line but the first line that contains characters.
                    if (firstArg.Equals("browser") && i != 0 && browserBlockFinished)
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid 'browser' command location on line {i + 1}.\n" +
                                $"'browser' command must be placed at the top of the file.\n",
                            returnBool: false
                        );


                    if (firstArg.Equals("browser") && !browserBlockFinished)
                    {
                        browserBlockFinished = true;
                        continue;
                    }

                    #endregion End of Browser Feature Check


                    #region Start of Invalid Feature Check

                    // If a feature name is provided after defining non feature actions
                    else if (firstArg.Equals("feature") && featureBlockFinished)
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid 'feature' command location on line {i + 1}.\n" +
                                $"All 'feature' commands must be placed before any other command, except 'browser'.\n",
                            returnBool: false
                        );

                    // If a duplicate feature name is provided -> feature "duplicate-name"
                    else if (firstArg.Equals("feature") && usedFeatures.Contains(line))
                        ExitOnDuplicateCommand(fileName, line, i);


                    // If an invalid feature name is provided -> feature "invalid-name"
                    else if (firstArg.Equals("feature") && !featureArgs.Any(arg => line.Contains(arg)))
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Unknown feature command on line {i + 1}:\n{line}\n\n" +
                                $"For more information please see, {DOCUMENTATION_LINK}",
                            returnBool: false
                        );

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
                        return Errors.WriteErrorAndReturnBool(intendedToUseProxyMessage, false);


                    Action AddValidatedProxy() => () =>
                    {
                        string proxyFeatureString = lineArgs[1].Replace('"', ' ').Trim();
                        string proxyString = lineArgs[2].Replace("\"", "");

                        // Only one Proxy feature command is permitted per script.
                        if (usedFeatures.Any(feature => feature.Contains(proxyFeatureString)))
                            ExitOnDuplicateCommand(fileName, line, i);

                        if (!IsValidProxyFormat(proxyString))
                            Errors.WriteAndExit(invalidProxyFeatureMessage, 1);

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
                        Errors.WriteAndExit(
                            message:
                                Errors.GenerateErrorMessage(fileName, line, i,
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
                //    return Errors.WriteErrorAndReturnBool(
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
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAMC Validation Error:\n\n" +
                        $"Error: File not found: '{fileName}'.\n",
                    returnBool: false
                );
            }

            catch (UnauthorizedAccessException)
            {
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"Permission was denied for '{fileName}'.\n",
                    false
                );
            }

            // Handles locked files, network errors, etc.
            catch (IOException ex)
            {
                return Errors.WriteErrorAndReturnBool(
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
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"A fatal error occurred while validating:'{fileName}'\n" +
                        $"Error: {ex}\n",
                    returnBool: false
                );
            }
        }
        public static KeyValuePair<MenuOption, string> New()
        {
            bool userScriptDirExists = CreateUserScriptsDirectory();
            if (!userScriptDirExists) { 
                return KeyValuePair.Create(
                    MenuOption.Invalid, 
                    Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage)
                ); 
            }

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0) { 
                return KeyValuePair.Create(
                    MenuOption.Invalid, 
                    Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage)
                ); 
            }

            MenuOption selection = Menu.New();
            int index;
            switch (selection)
            {
                case MenuOption.Add:

                    string input = Input.WriteListFromOptions(["Select a File", "Exit"]);

                    if (input.Equals("Exit"))
                        Errors.WriteAndExit("Operation cancelled by user, BAM Manager (BAMM) will exit now.", 1); 
                    

                    string path = Input.AskForInput("Path: ");
                    
                    if (!File.Exists(path))
                        Errors.WriteAndExit(
                            message:
                                "BAMM Manager (BAMM) was unable to find the provided file, " +
                                $"please ensure the file below exists:\n{path}",
                            status: 1
                        );

                    UserScriptManager _ = new(path, "add");
                    return KeyValuePair.Create(MenuOption.Add, path);

                case MenuOption.Compile:
                    HandleBAMCFileValidation(BAMCFiles);

                    index = HandleUserSelection(validFilesMapping);
                    selectedFile = BAMCFiles[index];
                    
                    return KeyValuePair.Create(
                        MenuOption.Compile, 
                        Path.Combine(
                            AppContext.BaseDirectory, 
                            "userScripts", 
                            selectedFile
                        )
                    );

                case MenuOption.Run:
                    selectedFile = RuntimeManager.HandleUserScriptChoice();
                    return KeyValuePair.Create(
                        MenuOption.Run, 
                        selectedFile
                    );

                // Add functionality to return back to the main menu after a completed action
                case MenuOption.Help:
                    HandleHelpSelection();
                    return KeyValuePair.Create(
                        MenuOption.Help,
                        string.Empty
                    );

                case MenuOption.Exit:
                    Environment.Exit(0);
                    break; // Stupid requirement for c#'s static compiler
            }

            return KeyValuePair.Create(
                MenuOption.Help, 
                "If you're reading this a menu option was incorrectly handled.\n\n" +
                $"Please make a bug report {ISSUES_LINK}"
            );
        }
    }
}
