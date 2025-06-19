using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster
{
    public partial class Parser
    {
        public enum MenuOption
        {
            Add,
            Compile,
            Help,
            Exit,
            Invalid
        }


        public readonly static string[] actionArgs = [
            "click", "click-exp", "end-javascript", "fill-text", "get-text", "save-as-html", "save-as-html-exp", "select-element", "select-option",
            "set-custom-useragent", "start-javascript", "take-screenshot", "wait-for-seconds", "visit"
        ];
        readonly static string[] proxyFeatureArgs = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] otherFeatureArgs = ["async", "browser", "bypass-cloudflare", "disable-pycache", "no-ssl"];
        //readonly static string[] browserArgs = ["brave", "chrome", "firefox", "safari", ];
        readonly static string[] browserArgs = ["chrome", "firefox", "safari", ];

        readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];
        //readonly static string[] validArgs = [.. actionArgs, .. featureArgs];
        static string selectedFile = string.Empty;
        static List<string> validFiles = [];
        
        readonly static Dictionary<int, string> validFilesMapping = [];

        // This needs to be modified to properly support cross platform file structures 
        //readonly static string userScriptsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BrowserAutomationMaster",  "userScripts");
        readonly static string userScriptsDirectory = UserScriptManager.GetUserScriptDirectory();

        static string noFilesFoundMessage = "";
        const string LinkFormatPattern = @"(?i)\b(https?://(?:(?:(?:[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?\.)*(?:[a-z\u00a1-\uffff]{2,}|[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?)\.?)|(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|\[(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?:(?::[0-9a-fA-F]{1,4}){1,6})|:(?:(?::[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(?::[0-9a-fA-F]{0,4}){0,4}%[a-zA-Z0-9._~%-]+|::(?:ffff(?::0{1,4}){0,1}:){0,1}(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|(?:[0-9a-fA-F]{1,4}:){1,4}:(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d))\]))(?::\d{2,5})?(?:[/?#][^\s<>""']*)?\b";

        const string ProxyFormatPattern = @"^([^:]+):([^@]+)@([^:]+):(\d+)$";
        const string NumberFormatPattern = @"^(?:\d+(?:\.\d{1,3})?|\.\d{1,3})$";
        const string UserAgentFormatPattern = "^[^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?(?:[ ][^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?)*$";


        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.

        [GeneratedRegex(ProxyFormatPattern)]
        private static partial Regex PrecompiledProxyRegex();

        [GeneratedRegex(LinkFormatPattern)]
        private static partial Regex PrecompiledLinkRegex();

        [GeneratedRegex(NumberFormatPattern)]
        private static partial Regex PrecompiledNumberRegex();
        
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
                    Console.WriteLine(ane.GetType().Name);
                    Console.WriteLine(ane.Message);
                    return false;
                }
                catch (UnauthorizedAccessException uae)
                {
                    Console.WriteLine(uae.GetType().Name);
                    Console.WriteLine(uae.Message);
                    return false;
                }
                catch (PathTooLongException ptle)
                {
                    Console.WriteLine(ptle.GetType().Name);
                    Console.WriteLine(ptle.Message);
                    return false;
                }
                catch (DirectoryNotFoundException dnfe)
                {
                    Console.WriteLine(dnfe.GetType().Name);
                    Console.WriteLine(dnfe.Message);
                    return false;
                }
                catch (IOException ie)
                {
                    Console.WriteLine(ie.GetType().Name);
                    Console.WriteLine(ie.Message);
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An unexpected error occurred while creating userScript directory:\n{ex.GetType().Name}");
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }
        public static void CreateValidFilesMapping(List<string> validFiles)
        {
            if (validFiles.Count != 0)
            {
                Success.WriteSuccessMessage($"BAM Manager (BAMM) located {validFiles.Count} valid .bamc files, please see below:\n");
                for (int i = 0; i < validFiles.Count; i++)
                {
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
                    Console.WriteLine($"File {index} ----> {rawFileName}\n");
                }
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
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
                Console.WriteLine(ex.GetType().Name);
                Console.WriteLine(ex.Message);
                return [];
            }
        }
        public static string[] ValidateBAMCFiles(string[] BAMCFiles)
        {
            return [.. BAMCFiles.Where(file => IsValidFile(file))];
        }
        public static bool IsValidNumberFormat(string numberString) {
            if (string.IsNullOrEmpty(numberString)) { return false; }
            return PrecompiledNumberRegex().IsMatch(numberString);
        }
        public static bool IsValidLinkFormat(string emailString) {
            if (string.IsNullOrWhiteSpace(emailString)) { return false; }
            return PrecompiledLinkRegex().IsMatch(emailString);
        }
        public static bool IsValidProxyFormat(string proxyString) {
            if (string.IsNullOrWhiteSpace(proxyString)) { return false; }
            return PrecompiledProxyRegex().IsMatch(proxyString);
        }
        public static bool IsValidUserAgentFormat(string userAgentString) {
            if (string.IsNullOrEmpty(userAgentString)) { return false; }
            return PrecompiledNumberRegex().IsMatch(userAgentString);
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
            bool isContinuing = true;
            while (isContinuing) {
                isContinuing = false;
                Help.DisplayAvailableCommands();
                string? command = Input.WriteTextAndReturnRawInput("Please find the command you wish to learn more about, then type it below.") ?? "Not Found";
                Help.ShowCommandDetails(command.Trim());
                isContinuing = (Input.WriteTextAndReturnRawInput("\nWould you like to continue learning more about BAM Manager (BAMM)? [y/n]:") ?? "n").ToLower().Trim().Equals("y");
            }
        }
        public static bool HandleLineValidation(string fileName, string line, int lineNumber)
        {
            if (line.Trim().StartsWith(" //") || line.Trim().StartsWith("//")) { return true; } // This is assumed as a comment
            string[] lineArgs;
            if (line.StartsWith("fill-text")) { lineArgs = line.Trim().Split(" \""); } // Special case to handle fill-text
            else { lineArgs = line.Trim().Split(" "); } // Handle all others
            string firstArg = lineArgs[0];
            string selectorString = "selector"; // Defaults to "selector" for selector based actions
            switch (firstArg)
            {
                case "click" or "get-text" or "save-as-html" or "save-as-html-exp" or "select-element" or "take-screenshot" or "visit":
                    if (firstArg.Contains("save-as-html")) { selectorString = "filename.html"; }
                    if (firstArg.Equals("take-screenshot")) { selectorString = "filename.png"; }
                    if (firstArg.Equals("select-option")) { selectorString = "option-selector"; }

                    if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"')) { 
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} \"{selectorString}\"\n", false);
                    }
                    if (lineArgs[0].Equals("visit") && !IsValidLinkFormat(lineArgs[1].Replace('"', ' ').Trim())) {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid url format on line {lineNumber}\nLine: {line}\n", false);
                    }
                    return true;

                case "click-exp":
                    lineArgs = line.Trim().Split(" '");
                    selectorString = "'selector'";
                    if (lineArgs.Length != 2 || !lineArgs[1].EndsWith('\''))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "fill-text":
                    if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} \"{selectorString}\" \"value\"\n", false);
                    }
                    return true;

                case "select-option":
                    if (lineArgs.Length != 3 || !lineArgs[1].StartsWith('"') || !lineArgs[1].Trim().EndsWith('"') || !int.TryParse(lineArgs[2], out int parsedInt)) {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} \"{selectorString}\" index\n", false);
                    }
                    return true;

                case "set-custom-useragent":
                    selectorString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0";
                    if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].Trim().EndsWith('"')) {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} \"{selectorString}\"\n", false);
                    }
                    else if (!IsValidUserAgentFormat(lineArgs[1].Trim())) {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid useragent on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} \"{selectorString}\"\n", false);
                    }
                    return true;

                case "wait-for-seconds":
                    selectorString = "5";
                    if (!IsValidNumberFormat(lineArgs[1].Trim())) {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid url format on line {lineNumber}\nLine: {line}\\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "browser":
                    if (lineArgs.Length != 2 || !browserArgs.Contains(lineArgs[1].Replace("\"", "")) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {"\"firefox\""}\n", false);
                    }
                    return true;

                case "feature":
                    if (lineArgs.Length != 2 && lineArgs.Length != 3 || !featureArgs.Contains(lineArgs[1]) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        selectorString = "\"feature-name\"";
                        return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    if (proxyFeatureArgs.Contains(lineArgs[1]))
                    {
                        selectorString = $"\"{lineArgs[1]}\"";
                        if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                        {
                            return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                        }

                        lineArgs[2] = lineArgs[2].Replace('"', ' ').Trim();
                        bool validProxy = IsValidProxyFormat(lineArgs[2]);
                        if (!validProxy)
                        {
                            return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                        }
                    }
                    return true;

                default:
                    return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid command on line {lineNumber}.\nPlease check your spelling and try again.\n", false);


            }
        }
        public static int HandleUserSelection(Dictionary<int, string> mapping)
        {
            Type desiredType = typeof(int);

            if (mapping.Count == 0)
            {
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }

            int numberOfFilesFound = mapping.Count;
           
            string inputText = string.Empty;
            foreach (KeyValuePair<int, string> pair in mapping)
            {
                int index = pair.Key + 1;
                string? rawFileName = null;
                try { rawFileName = Path.GetFileName(pair.Value); }
                catch { rawFileName = null; }
                if (rawFileName != null)
                {
                    inputText += $"{index}. {rawFileName}\n";
                }
            }

            if (inputText == string.Empty) { Errors.WriteErrorAndExit(noFilesFoundMessage, 1); }

            inputText = $"{inputText}\n\nPlease enter the number corresponding to your desired file [Between 1-{numberOfFilesFound}]: ";
            string panicText = $"BAM Manager (BAMM) panicked due an invalid value provided as input.  Value must be between 1 and {numberOfFilesFound}\n\n{inputText}";
            

            while (true)
            {
                object? rawInput = Input.WriteTextAndReturnInputType(inputText, panicText, desiredType, true); // This will run until valid input is provided.
                if (rawInput != null && rawInput.GetType() == desiredType) 
                {
                    int fileNumber = (int) rawInput; //
                    if (fileNumber < 1 || fileNumber > numberOfFilesFound) { inputText = panicText; continue; } // Continue until valid input or user exit.
                    return fileNumber - 1; // This returns the index 
                }
            }
        }
        
        public static bool IsValidFile(string filePath)
        {
            List<string> usedFeatures = [];
            string fileName = Path.GetFileName(filePath);
            try
            {
                List<string> lines = [.. File.ReadAllLines(filePath).Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line))];
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
                    string trimmedLine = DeleteCommentIfPresent(line);

                    if (!jsBlockFinished) {
                        if (trimmedLine.StartsWith("end-javascript")) { jsBlockFinished = true; }
                        else if (trimmedLine.StartsWith("start-javascript")) {
                            lineCurrentJSBlockStarts = i + 1;
                            return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\n\nError: Attempted to create a second JavaScript block on line {lineCurrentJSBlockStarts} while the previous block has not been closed.\n\nPlease ensure end-javascript is placed at or before line {i}.", false);
                        }
                        else {
                            currentJSBlockContent += $"{line}\n";
                        }
                    }
                    else
                    {
                        string[] lineArgs = trimmedLine.Split(" ");
                        if (lineArgs.Length == 0) { return false; }
                        string firstArg = lineArgs[0];
                        if (firstArg.Equals("browser"))
                        {
                            if (i != 0 || browserBlockFinished)
                            {
                                return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid 'browser' command location on line {i + 1}.\n'browser' command must be placed at the top of the file.\n", false);
                            }
                            browserBlockFinished = true;
                        }
                        else if (firstArg.Equals("feature"))
                        {
                            if (featureBlockFinished)
                            {
                                return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid 'feature' command location on line {i + 1}.\nAll 'feature' commands must be placed before any other command, except 'browser'.\n", false);
                            }
                            if (usedFeatures.Contains(line))
                            {
                                return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nDuplicate command on line {i + 1}:\n{line}\nAll 'feature' commands may only be defined once.\n", false);
                            }
                            string[] proxyFeatures = ["\"use-http-proxy\"", "\"use-https-proxy\"", "\"use-socks4-proxy\"", "\"use-socks5-proxy\""];
                            if (proxyFeatures.Contains(lineArgs[1]))
                            {
                                if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                                {
                                    selectorString = lineArgs[1];
                                    return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                                }

                                bool validProxy = IsValidProxyFormat(lineArgs[2].Replace("\"", ""));
                                if (!validProxy)
                                {
                                    return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
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
                                    line.Trim().StartsWith(prefix)) && !line.Trim().StartsWith("//") // Ignores comments
                                
                            )
                            ];
                            if (invalidLines.Count > 0)
                            {
                                Errors.WriteErrorAndExit(
                                    Errors.GenerateErrorMessage(fileName, line, i, 
                                    $"A 'visit' command must be placed after 'browser' commands in addition to 'feature' commands (if used)."), 
                                1);
                            }
                        }
                        else if (trimmedLine.StartsWith("start-javascript")){ jsBlockFinished = false; }
                        else if (trimmedLine.StartsWith("end-javascript")) {
                            jsBlockFinished = true;
                            currentJSBlockContent = string.Empty;
                        }
                        else {
                            bool validLine = HandleLineValidation(fileName, trimmedLine, i + 1);
                            if (!validLine) { return false; }
                            if (!trimmedLine.StartsWith("//")){ // Ignores comments
                                featureBlockFinished = true; // This flag will be used to ensure all feature commands are placed before all others.
                            }
                        }
                    }
                }
                if (usedFeatures.Any(x=>x.Contains("async")) && usedFeatures.Any(x=>x.Contains("bypass-cloudflare"))) {
                    return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nFile: \"{fileName}\"\n\nError: Script cannot contain both \"async\" and \"bypass-cloudflare\"\n", false);
                }
                return true;
            }
            catch (FileNotFoundException) { return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nError: File not found: '{fileName}'.\n", false);  }
            catch (UnauthorizedAccessException) {  return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nPermission was denied for '{fileName}'.\n", false); }

            // Handles locked files, network errors, etc.
            catch (IOException ex) { return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nAn IO Exception occurred while validating: '{fileName}'\nError: {ex.Message}\n", false); }
            
            // General catchall (LOG MORE SEVERLY IF HIT) 
            catch (Exception ex){ return Errors.WriteErrorAndReturnBool($"BAM Manager (BAMM) ran into a BAMC validation error:\n\nA fatal error occurred while validating:'{fileName}'\nError: {ex.Message}\n", false); }
        }
        
        public static MenuOption Menu()
        {
            Dictionary<int, MenuOption> menuOptionsMapping = new()
            {
                { 1, MenuOption.Add },
                { 2, MenuOption.Compile },
                { 3, MenuOption.Help },
                { 4, MenuOption.Exit },
            };
            string menuText = """
            
            Welcome To The BAM Manager (BAMM)!

            Please select the number correlating to your desired action from the menu options below:

            1. Add local .BAMC File to userScripts Directory
            2. Compile .BAMC File from userScripts Directory
            3. Help
            4. Exit


            """;
            string invalidChoiceText = "Invalid option please enter a number between 1 and 4.\n\n" + menuText;

            Console.WriteLine(menuText);
            while (true)
            {
                // ? Declares userChoice as a nullable value, as input cannot be verified without sanitization.
                bool validChoice = int.TryParse(Console.ReadLine(), out int optionNumber);
                if (validChoice && menuOptionsMapping.TryGetValue(optionNumber, out MenuOption selection)) {
                    Console.Clear(); // Clears Terminal prior to proceeding.
                    return selection;
                }
                Console.WriteLine(invalidChoiceText);
            }
        }
        
        public static KeyValuePair<MenuOption, string> New()
        {
            bool userScriptDirExists = CreateUserScriptsDirectory();
            if (!userScriptDirExists) { return KeyValuePair.Create(MenuOption.Invalid, Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage)); }

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0) { return KeyValuePair.Create(MenuOption.Invalid, Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage)); }

            MenuOption selection = Menu();
            int index;
            switch (selection)
            {
                case MenuOption.Add:
                    string input = Input.WriteTextAndReturnRawInput("Drag and drop the file you wish to add to the userScripts directory, or enter 'exit'.\n\n") ?? "exit";
                    if (input.Equals("exit")) { Errors.WriteErrorAndExit("Operation cancelled by user, BAM Manager (BAMM) will exit now.", 1); }
                    if (!File.Exists(input)) { Errors.WriteErrorAndExit($"BAMM Manager (BAMM) was unable to find the provided file, please ensure the file below exists:\n{input}", 1); }
                    UserScriptManager _ = new(input, "add");
                    return KeyValuePair.Create(MenuOption.Add, input);

                case MenuOption.Compile:
                    HandleBAMCFileValidation(BAMCFiles);
                    index = HandleUserSelection(validFilesMapping);
                    selectedFile = BAMCFiles[index];
                    //return KeyValuePair.Create(MenuOption.Compile, Path.Combine(UserScriptManager.GetUserScriptDirectory(), selectedFile));
                    return KeyValuePair.Create(MenuOption.Compile, Path.Combine(AppContext.BaseDirectory, "userScripts", selectedFile));
                
                case MenuOption.Help:
                    HandleHelpSelection();
                    return KeyValuePair.Create(MenuOption.Help, ""); // This just needs to passthrough, action will be taken back in program.cs 

                case MenuOption.Exit:
                    Environment.Exit(0);
                    break; // Stupid requirement for c#'s static compiler
            }

            return KeyValuePair.Create(MenuOption.Help, "This should never be triggered");
        }
        
    }

    
}
