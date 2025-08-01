using System.Text.RegularExpressions;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Managers.CommandManager;
using System.Text;


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
        readonly static string[] proxyFeatureArgs = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] otherFeatureArgs = ["browser", "disable-pycache", "disable-ssl", "run-headless"];
        //readonly static string[] browserArgs = ["brave", "chrome", "firefox", "safari", ];
        readonly static string[] browserArgs = ["chrome", "firefox", "safari", ];

        readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];
        //readonly static string[] validArgs = [.. actionArgs, .. featureArgs];
        static string selectedFile = string.Empty;

        static List<string> validFiles = [];
        public readonly static string[] validProtocols = ["http://", "https://", "file://"];

        readonly static Dictionary<int, string> validFilesMapping = [];

        // This needs to be modified to properly support cross platform file structures 
        //readonly static string userScriptsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserAutomationMaster",  "userScripts");
        readonly static string userScriptsDirectory = UserScriptManager.GetUserScriptDirectory();

        static string noFilesFoundMessage = "";
        const string HeaderFormatPattern = @"^add-headers\s*(?<json>\{\s*(?:""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*""(?:\s*,\s*""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*"")*)?\s*\})$";
        const string LinkFormatPattern = @"(?i)\b(https?://(?:(?:(?:[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?\.)*(?:[a-z\u00a1-\uffff]{2,}|[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?)\.?)|(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|\[(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?:(?::[0-9a-fA-F]{1,4}){1,6})|:(?:(?::[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(?::[0-9a-fA-F]{0,4}){0,4}%[a-zA-Z0-9._~%-]+|::(?:ffff(?::0{1,4}){0,1}:){0,1}(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|(?:[0-9a-fA-F]{1,4}:){1,4}:(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d))\]))(?::\d{2,5})?(?:[/?#][^\s<>""']*)?\b";

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
        public static string[] ValidateBAMCFiles(string[] BAMCFiles)
        {
            return [.. BAMCFiles.Where(file => IsValidFile(file))];
        }
        public static bool IsValidHeaderFormat(string headerString) {
            if (string.IsNullOrEmpty(headerString)) { return false; }
            return PrecompiledHeaderRegex().IsMatch(headerString);
        }
        public static bool IsValidNumberFormat(string numberString) {
            if (string.IsNullOrEmpty(numberString)) { return false; }
            return PrecompiledNumberRegex().IsMatch(numberString);
        }
        public static bool IsValidLinkFormat(string linkString) {
            if (string.IsNullOrWhiteSpace(linkString)) { return false; }
            bool hasValidProtocol = false;
            foreach (string protocol in validProtocols){ 
                if (linkString.StartsWith(protocol)) { 
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
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }
            if (validFilesMapping.Count != validFiles.Count)
            {
                CreateValidFilesMapping(validFiles);
            }
            if (validFilesMapping.Count == 0)
            {
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }

        }
        public static void HandleHelpSelection()
        {
            while (true) {
                string command = Input.WriteListFromOptions([.. CommandList.Select(cmd => cmd.Name), "Exit App"]);
                Help.ShowCommandDetails(command.Trim());

                string choice = Input.WriteTextAndReturnRawInput(
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
            if (line.StartsWith(" //") || line.StartsWith("//")) { return true; } // This is assumed as a comment

            if (line.StartsWith("add-headers")) {
                selectorString = $"{{\"header-name\": \"header-value\", \"header-name2\": \"header-value2\"}}";
                if (!IsValidHeaderFormat(line)) {
                    return Errors.WriteErrorAndReturnBool(
                        message:
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n" +
                            $"Invalid header format on line {lineNumber}\n" +
                            $"Line: {line}\n" +
                            $"Valid Syntax: add-headers {selectorString}\n", 
                        returnBool: false
                     );
                }
                return true;
            }

            string[] lineArgs;
            string[] lineArgSpecialCases = ["add-header",  "fill-text", "fill-text-exp", "set-custom-useragent"];

            // Special case to handle lineArgSpecialCases
            if (lineArgSpecialCases.Any(lineArg => line.StartsWith(lineArg))) { lineArgs = line.Split(" \""); } 

            else { lineArgs = line.Split(" "); } // Handle all others

            string firstArg = lineArgs[0];
            switch (firstArg)
            {
                case "click":
                case "get-text":
                case "save-as-html":
                case "save-as-html-exp":
                case "select-element":
                case "take-screenshot":
                case "visit":
                    if (firstArg.Contains("save-as-html")) { selectorString = "filename.html"; }
                    if (firstArg.Equals("take-screenshot")) { selectorString = "filename.png"; }
                    if (firstArg.Equals("select-option")) { selectorString = "option-selector"; }

                    if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"')) { 
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\nValid Syntax: {firstArg} \"{selectorString}\"\n", 
                            returnBool: false
                        );
                    }
                    if (lineArgs[0].Equals("visit") && !IsValidLinkFormat(lineArgs[1].Replace('"', ' ').Trim())) {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\nInvalid url format on line {lineNumber}\n" +
                                $"Line: {line}\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "add-header":
                    selectorString = "\"header-name\" \"header-value\"";
                    if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].EndsWith('"')) {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n",
                            returnBool: false
                        );
                    }
                    return true;

                case "click-at-position":
                    lineArgs = line.Trim().Split(" ");
                    selectorString = "\"x-coordinate\" \"y-coordinate\"";
                    if (lineArgs.Length != 3) { 
                        return Errors.WriteErrorAndReturnBool(
                            message: 
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    
                    string[] positionArgs = [lineArgs[1], lineArgs[2]];
                    foreach (var arg in positionArgs) 
                    {
                        bool notQuoted = !arg.StartsWith('"') || !arg.EndsWith('"');
                        bool notParsable = !int.TryParse(
                            arg.Replace('"', ' ').Trim(), 
                            out int posArg
                        );
                        if (notQuoted || notParsable) {
                            return Errors.WriteErrorAndReturnBool(
                                message:
                                    $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" + 
                                    $"File: \"{fileName}\"\n" +
                                    $"Invalid syntax on line {lineNumber}\n" +
                                    $"Line: {line}\n" +
                                    $"Valid Syntax: {firstArg} {selectorString}\n", 
                                returnBool: false
                            );
                        }
                    }
                    return true;

                case "click-exp":
                    lineArgs = line.Trim().Split(" '");
                    selectorString = "'selector'";
                    if (lineArgs.Length != 2 || !lineArgs[1].EndsWith('\'')) {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" + 
                                $"File: \"{fileName}\"\n" + 
                                $"Invalid syntax on line {lineNumber}\n" + 
                                $"Line: {line}\n" + 
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "close-current-tab":
                    // Add a check here ensuring theres only one element
                    return true; // No parsing is needed here

                case "fill-text":
                    selectorString = "\"selector\" \"Desired value to input\"";
                    if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} \"{selectorString}\" \"value\"\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "fill-text-exp":
                    selectorString = "\"selector\" \"Desired value to input\"";
                    if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} \"{selectorString}\" \"value\"\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "open-new-tab":
                    selectorString = "\"x-coordinate\" \"y-coordinate\"";
                    if (lineArgs.Length != 3) { // Invalid # of args
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    for (int i = 0; i < lineArgs.Length; i++) { lineArgs[i] = lineArgs[i].Replace('"', ' ').Trim(); } // Removing double quotes.
                    if (!IsValidLinkFormat(lineArgs[1])) { // Invalid url format
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Issue: Invalid link format for link: '{lineArgs[1]}'\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }

                    if (!int.TryParse(lineArgs[2], out int waitTime)) { // Invalid timeout
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Issue: Invalid timeout argument: '{lineArgs[2]}'\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "select-option":
                    if (lineArgs.Length != 3 || 
                        !lineArgs[1].StartsWith('"') || 
                        !lineArgs[1].Trim().EndsWith('"') || 
                        !int.TryParse(lineArgs[2], out int parsedInt)) 
                    {
                        return Errors.WriteErrorAndReturnBool(
                            message: 
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} \"{selectorString}\" index\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "set-custom-useragent":
                    selectorString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0";
                    if (lineArgs.Length != 2 || 
                        !lineArgs[1].Trim().EndsWith('"')) 
                    {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} \"{selectorString}\"\n", 
                            returnBool: false
                        );
                    }
                    else if (!IsValidUserAgentFormat(lineArgs[1].Trim())) {
                        return Errors.WriteErrorAndReturnBool(
                            message: 
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid useragent on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} \"{selectorString}\"\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "wait-for-seconds":
                    selectorString = "5";
                    if (!IsValidNumberFormat(lineArgs[1].Trim())) {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid url format on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "browser":
                    if (
                        lineArgs.Length != 2 || 
                        !browserArgs.Contains(lineArgs[1].Replace("\"", "")) || 
                        !lineArgs[1].StartsWith('"') || 
                        !lineArgs[1].EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                $"File: \"{fileName}\"\n" +
                                $"Invalid syntax on line {lineNumber}\n" +
                                $"Line: {line}\n" +
                                $"Valid Syntax: {firstArg} {"\"firefox\""}\n", 
                            returnBool: false
                        );
                    }
                    return true;

                case "feature":
                    if (lineArgs.Length != 2 && lineArgs.Length != 3 || 
                        !featureArgs.Contains(lineArgs[1]) || 
                        !lineArgs[1].StartsWith('"') || 
                        !lineArgs[1].EndsWith('"'))
                    {
                        selectorString = "\"feature-name\"";
                        return Errors.WriteErrorAndReturnBool(
                            message:
                                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" + 
                                $"File: \"{fileName}\"\n" + 
                                $"Invalid syntax on line {lineNumber}\n" + 
                                $"Line: {line}\n" + 
                                $"Valid Syntax: {firstArg} {selectorString}\n", 
                            returnBool: false
                        );
                    }
                    if (proxyFeatureArgs.Contains(lineArgs[1]))
                    {
                        selectorString = $"\"{lineArgs[1]}\"";
                        if (lineArgs.Length != 3 || 
                            lineArgs[2].Count(c => (c == ':')) != 2 || 
                            lineArgs[2].Count(c => (c == '@')) != 1)
                        {
                            return Errors.WriteErrorAndReturnBool(
                                message:
                                    $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                    $"File: \"{fileName}\"\n" +
                                    $"Invalid syntax on line {lineNumber}\n" +
                                    $"Line: {line}\n" +
                                    $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                                    $"If no authentication is required: NULL:NULL@IP:PORT\n", 
                                returnBool: false
                            );
                        }

                        lineArgs[2] = lineArgs[2].Replace('"', ' ').Trim();
                        bool validProxy = IsValidProxyFormat(lineArgs[2]);
                        if (!validProxy)
                        {
                            return Errors.WriteErrorAndReturnBool(
                                message:
                                    $"BAMC Validation Error:\n\n" +
                                    $"File: \"{fileName}\"\n" +
                                    $"Invalid syntax on line {lineNumber}\n" +
                                    $"Line: {line}\n" +
                                    $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                                    $"If no authentication is required: NULL:NULL@IP:PORT\n", 
                                returnBool: false
                            );
                        }
                    }
                    return true;

                default:
                    return Errors.WriteErrorAndReturnBool(
                        message:
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n" +
                            $"Invalid command on line {lineNumber}.\n" +
                            $"Please check your spelling and try again.\n", 
                        returnBool: false
                    );


            }
        }
        private static int HandleUserSelection(Dictionary<int, string> mapping)
        {

            if (mapping.Count == 0)
            {
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }

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
                {
                    menuOptions[i] = $"{i + 1}.  {rawFileName}"; 
                }
            }

            if (menuOptions.Length == 0) 
            { 
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1); 
            }

            string panicText = 
                $"BAM Manager (BAMM) panicked due an invalid value provided as input.  " +
                $"Value must be between 1 and {numberOfFilesFound}";

            var rawInput = Input.WriteListFromOptions(menuOptions, "file");
            var input = GetFileNumber(rawInput);
            if (input == null)
            {
                Errors.WriteErrorAndExit(panicText, 1);
            }

            if (!int.TryParse(input, out int fileNumber))
            {
                Errors.WriteErrorAndExit(panicText, 1);
            }

            if (fileNumber < 1 || fileNumber > numberOfFilesFound)
            {
                Errors.WriteErrorAndExit(panicText, 1);
            }
            return fileNumber - 1; // index = fileNumber - 1;
        }
        private static bool IsValidFile(string filePath)
        {
            List<string> usedFeatures = [];
            string fileName = Path.GetFileName(filePath);
            try
            {
                List<string> lines = [.. 
                    File.ReadAllLines(filePath)
                    .Select(line => DeleteCommentIfPresent(line.Trim()))
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                ];
                string currentJSBlockContent = string.Empty;
                int lineCurrentJSBlockStarts = 0; // Will be modified assuming a javascript block is provided.
                bool browserBlockFinished = false;
                bool featureBlockFinished = false;
                bool visitBlockFinished = false;
                bool jsBlockFinished = true; // Starts off as true and will change below

                for (int i = 0; i < lines.Count; i++)
                {
                    string selectorString = "value";
                    string line = lines[i];

                    if (!jsBlockFinished) {

                        // At this point the following are true:
                        // There are atleast 3 lines in the file (visit, start-javascript, and end-javascript)
                        // Its also possible browser is defined at the top of the file.
                        if (line.StartsWith("end-javascript")) {
                            if (!JavaScript.IsValidSyntax(currentJSBlockContent, out string? jsError)) {
                                string surroundingLines = 
                                    $"Line {i-2} -> {lines[i - 2]}\n" +
                                    $"Line {i - 1} -> {lines[i - 1]}\n" +
                                    $"Line {i} -> {line} <-- This is the line that's causing the issue.\n";

                                return Errors.WriteErrorAndReturnBool(
                                    message:
                                        $"BAM Manager (BAMM) ran into a BAMC validation error on line {i} of \"{fileName}\".\n\n" +
                                        $"Error log:\n{surroundingLines}\n" +
                                        $"Compiler error:\n" +
                                        $"In the current block, on {jsError}\n\n" +
                                        $"Please correct this and recompile.",
                                    returnBool: false
                                );
                            }
                            jsBlockFinished = true; 
                        }
                        else if (line.StartsWith("start-javascript")) {
                            lineCurrentJSBlockStarts = i + 1;
                            return Errors.WriteErrorAndReturnBool(
                                message:
                                    $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                    $"File: \"{fileName}\"\n\n" +
                                    $"Error: Attempted to create a second JavaScript block on line {lineCurrentJSBlockStarts} " +
                                    $"while the previous block has not been closed.\n\n" +
                                    $"Please ensure end-javascript is placed at or before line {i}.", 
                                returnBool: false
                            );
                        }
                        else {
                            currentJSBlockContent += $"{line}\n";
                        }
                    }
                    else
                    {
                        string[] lineArgs = line.Split(" ");
                        if (lineArgs.Length == 0) { return false; }
                        string firstArg = lineArgs[0];
                        if (firstArg.Equals("browser"))
                        {
                            if (i != 0 || browserBlockFinished)
                            {
                                return Errors.WriteErrorAndReturnBool(
                                    message:
                                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                        $"File: \"{fileName}\"\n" +
                                        $"Invalid 'browser' command location on line {i + 1}.\n" +
                                        $"'browser' command must be placed at the top of the file.\n",
                                    returnBool: false
                                );
                            }
                            browserBlockFinished = true;
                        }
                        else if (firstArg.Equals("feature"))
                        {
                            if (featureBlockFinished)
                            {
                                return Errors.WriteErrorAndReturnBool(
                                    message:
                                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                        $"File: \"{fileName}\"\n" +
                                        $"Invalid 'feature' command location on line {i + 1}.\n" +
                                        $"All 'feature' commands must be placed before any other command, except 'browser'.\n", 
                                    returnBool: false
                                );
                            }
                            if (usedFeatures.Contains(line))
                            {
                                return Errors.WriteErrorAndReturnBool(
                                    message:
                                        "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                        $"File: \"{fileName}\"\n" +
                                        $"Duplicate command on line {i + 1}:\n{line}\n" +
                                        "All 'feature' commands may only be defined once.\n", 
                                    returnBool: false
                                );
                            }
                            if (!featureArgs.Any(arg => line.Contains(arg)))
                            {
                                return Errors.WriteErrorAndReturnBool(
                                    message:
                                        "BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                        $"File: \"{fileName}\"\n" +
                                        $"Unknown feature command on line {i + 1}:\n{line}\n\n" +
                                        $"For more information please see, {ConstantManager.DOCUMENTATION_LINK}",
                                    returnBool: false
                                );
                            }

                            string[] proxyFeatures = [
                                "\"use-http-proxy\"", 
                                "\"use-https-proxy\"", 
                                "\"use-socks4-proxy\"", 
                                "\"use-socks5-proxy\""
                            ];
                            if (proxyFeatures.Contains(lineArgs[1]))
                            {
                                if (lineArgs.Length != 3 || 
                                    lineArgs[2].Count(c => (c == ':')) != 2 || 
                                    lineArgs[2].Count(c => (c == '@')) != 1)
                                {
                                    selectorString = lineArgs[1];
                                    return Errors.WriteErrorAndReturnBool(
                                        message:
                                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                            $"File: \"{fileName}\"\nInvalid syntax on line {i + 1}\n" +
                                            $"Line: {line}\n" +
                                            $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                                            $"If no authentication is required: NULL:NULL@IP:PORT\n", 
                                        returnBool: false
                                    );
                                }

                                bool validProxy = IsValidProxyFormat(lineArgs[2].Replace("\"", ""));
                                if (!validProxy)
                                {
                                    return Errors.WriteErrorAndReturnBool(
                                        message:
                                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                                            $"File: \"{fileName}\"\n" +
                                            $"Invalid syntax on line {i + 1}\nLine: {line}\n" +
                                            $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                                            $"If no authentication is required: NULL:NULL@IP:PORT\n", 
                                        returnBool: false
                                    );
                                }
                            }
                            usedFeatures.Add(line);
                        }
                        else if (firstArg.Equals("visit"))
                        {
                            if (visitBlockFinished) { return true; }

                            List<string> passedLines = [.. lines.Take(i + 1)];
                            List<string> availableCommands = ["browser", "feature", "visit"];
                            List<string> invalidLines = [..
                            passedLines.Where(line =>
                                !availableCommands.Any(prefix => 
                                    line.Trim().StartsWith(prefix)) && 
                                    !line.Trim().StartsWith("//") // Ignores comments
                                
                                )
                            ];

                            if (invalidLines.Count > 0)
                            {
                                Errors.WriteErrorAndExit(
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
                            }
                        }
                        else if (line.StartsWith("start-javascript")){ jsBlockFinished = false; }
                        else if (line.StartsWith("end-javascript")) {
                            jsBlockFinished = true;
                            currentJSBlockContent = string.Empty;
                        }
                        else {
                            bool validLine = HandleLineValidation(fileName, line, i + 1);
                            if (!validLine) { return false; }
                            if (!line.StartsWith("//")){ // Ignores comments
                                // This flag will be used to ensure all 'feature' commands are placed before all other commands, excluding 'browser'.
                                featureBlockFinished = true;
                            }
                        }
                    }
                }
                if (
                    usedFeatures.Any(x=>x.Contains("async")) && 
                    usedFeatures.Any(x=>x.Contains("bypass-cloudflare"))
                ) 
                {
                    return Errors.WriteErrorAndReturnBool(
                        message: 
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n\n" +
                            $"Error: Script cannot contain both \"async\" and \"bypass-cloudflare\"\n", 
                        returnBool: false
                    );
                }
                return true;
            }
            catch (FileNotFoundException) { 
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAMC Validation Error:\n\n" +
                        $"Error: File not found: '{fileName}'.\n", 
                    returnBool: false
                );  
            }
            catch (UnauthorizedAccessException) {  
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"Permission was denied for '{fileName}'.\n", 
                    false
                ); 
            }

            // Handles locked files, network errors, etc.
            catch (IOException ex) { 
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"An IO Exception occurred while validating: '{fileName}'\n" +
                        $"Error: {ex.Message}\n", 
                    returnBool: false
                ); 
            }
            
            // General catchall (LOG MORE SEVERLY IF HIT) 
            catch (Exception ex){ 
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"A fatal error occurred while validating:'{fileName}'\n" +
                        $"Error: {ex.Message}\n", 
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
                    string input = Input.WriteListFromOptions(
                        options: ["Select a File", "Exit"]
                    );

                    if (input.Equals("Exit")) { 
                        Errors.WriteErrorAndExit("Operation cancelled by user, BAM Manager (BAMM) will exit now.", 1); 
                    }
                    string path = Input.WriteTextAndReturnRawInput("Path: ");
                    if (!File.Exists(path))
                    {
                        Errors.WriteErrorAndExit(
                            message:
                                "BAMM Manager (BAMM) was unable to find the provided file, " +
                                $"please ensure the file below exists:\n{path}",
                            status: 1
                        );
                    }
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
                $"Please make a bug report {ConstantManager.ISSUES_LINK}"
            );
        }
        
    }

    
}
