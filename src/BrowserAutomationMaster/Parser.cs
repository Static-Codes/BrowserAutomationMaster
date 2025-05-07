using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    public partial class Parser
    {
        public enum MenuOption
        {
            Compile,
            Compile_And_Validate,
            Help,
            Validate,
        }


        public readonly static string[] actionArgs = ["click", "click-button", "get-text", "fill-textbox", "save-as-html", "select-dropdown", "select-dropdown-element", "take-screenshot", "wait-for-seconds", "visit"];
        readonly static string[] proxyFeatureArgs = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] otherFeatureArgs = ["browser", "bypass-cloudflare", "disable-pycache"];
        readonly static string[] browserArgs = ["chrome", "chromium", "firefox", "safari", ];
        readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];
        readonly static string[] validArgs = [.. actionArgs, .. featureArgs];
        static string selectedFile = string.Empty;
        static List<string> validFiles = [];
        
        readonly static Dictionary<int, string> validFilesMapping = [];
        readonly static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        readonly static string userScriptsDirectory = Path.Combine([baseDirectory, "userScripts"]);

        static string noFilesFoundMessage = "";
        const string ProxyFormatPattern = @"^([^:]+):([^@]+)@([^:]+):(\d+)$";

        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.

        [GeneratedRegex(ProxyFormatPattern)]
        private static partial Regex PrecompiledProxyRegex();
        public static bool CreateUserScriptsDirectory()
        {
            if (userScriptsDirectory == null) { return false; }
            noFilesFoundMessage = $"""
            BAM Manager (BAMM) was unable to find any valid .bamc files.
                
            Please check the 'userScripts' directory and contains atleast one .bamc file!

            Location: {userScriptsDirectory}

            If this directory wasn't already created please rerun this application.

            Press any key to exit...
            """;

            if (Directory.Exists(userScriptsDirectory)) { return true; }
            else
            {
                try
                {
                    Directory.CreateDirectory(userScriptsDirectory);
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
                    Console.Write(uae.GetType().Name);
                    Console.WriteLine(uae.Message);
                    return false;
                }
                catch (PathTooLongException ptle)
                {
                    Console.WriteLine(ptle.GetType().Name);
                    Console.WriteLine(ptle.Message);
                    return false;
                }
                catch (IOException ie)
                {
                    Console.WriteLine(ie.GetType().Name);
                    Console.WriteLine(ie.Message);
                    return false;
                }
            }
        }

        public static void CreateValidFilesMapping(List<string> validFiles)
        {
            if (validFiles.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"BAM Manager (BAMM) located {validFiles.Count} valid .bamc files, please see below:\n");
                Console.ForegroundColor = ConsoleColor.White;
                for (int i = 0; i < validFiles.Count; i++)
                {
                    validFilesMapping.Add(i, validFiles[i]);
                }
            }
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

        public static bool IsValidProxyFormat(string proxyString)
        {
            if (string.IsNullOrWhiteSpace(proxyString)) { return false; }
            return PrecompiledProxyRegex().IsMatch(proxyString);
        }

        public static void HandleBAMCFileValidation(string[] BAMCFiles)
        {
            validFiles = [.. ValidateBAMCFiles(BAMCFiles)];
            if (validFiles.Count == 0)
            {
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }
            CreateValidFilesMapping(validFiles);
            if (validFilesMapping.Count == 0)
            {
                Errors.WriteErrorAndExit(noFilesFoundMessage, 1);
            }

        }
        
        public static bool HandleLineValidation(string fileName, string line, int lineNumber)
        {
            string[] lineArgs = line.Split(" ");
            string firstArg = lineArgs[0];
            string selectorString = "\"css-selector\""; // Defaults to "css-selector" for selector based actions
            switch (firstArg)
            {
                case "click" or "click-button" or "get-text" or "select-dropdown" or "select-dropdown-element" or "save-as-html" or "take-screenshot" or "visit":
                    if (firstArg.Equals("save-as-html")) { selectorString = "filename.html"; }
                    if (firstArg.Equals("take-screenshot")) { selectorString = "filename.png"; }

                    if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "fill-textbox":
                    if (lineArgs.Length != 3 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"') || !lineArgs[2].StartsWith('"') || !lineArgs[2].EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} \"value\"\n", false);
                    }
                    return true;

                case "wait-for-seconds":
                    selectorString = "5";
                    if (lineArgs.Length != 2 || !int.TryParse(lineArgs[1], out int seconds) || seconds < 1)
                    {
                        return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "browser":
                    if (lineArgs.Length != 2 || !browserArgs.Contains(lineArgs[1].Replace("\"", "")) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {"\"firefox\""}\n", false);
                    }
                    return true;

                case "feature":
                    if (lineArgs.Length != 2 && lineArgs.Length != 3 || !featureArgs.Contains(lineArgs[1]) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        selectorString = "\"feature-name\"";
                        return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    string[] proxyFeatures = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
                    if (proxyFeatures.Contains(lineArgs[1]))
                    {
                        if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                        {
                            selectorString = $"\"{lineArgs}\"";
                            return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
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
                    return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid command on line {lineNumber}.\nPlease check your spelling and try again.\n", false);


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
                bool featureBlockFinished = false;
                bool browserBlockFinished = false;

                for (int i = 0; i < lines.Count; i++)
                {
                    string selectorString = "value";
                    string line = lines[i];
                    string[] lineArgs = line.Split(" ");
                    if (lineArgs.Length == 0) { return false; }
                    string firstArg = lineArgs[0];
                    if (firstArg.Equals("browser"))
                    {
                        if (i != 0 || browserBlockFinished)
                        {
                            return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid 'browser' command location on line {i + 1}.\n'browser' command must be placed at the top of the file.\n", false);
                        }
                        browserBlockFinished = true;
                    }
                    else if (firstArg.Equals("feature"))
                    {
                        if (featureBlockFinished)
                        {
                            return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid 'feature' command location on line {i + 1}.\nAll 'feature' commands must be placed before any other command, except 'browser'.\n", false);
                        }
                        if (usedFeatures.Contains(line))
                        {
                            return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nDuplicate command on line {i + 1}:\n{line}\nAll 'feature' commands may only be defined once.\n", false);
                        }
                        string[] proxyFeatures = ["\"use-http-proxy\"", "\"use-https-proxy\"", "\"use-socks4-proxy\"", "\"use-socks5-proxy\""];
                        if (proxyFeatures.Contains(lineArgs[1]))
                        {
                            if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                            {
                                selectorString = lineArgs[1];
                                return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                            }

                            bool validProxy = IsValidProxyFormat(lineArgs[2]);
                            if (!validProxy)
                            {
                                return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                            }
                        }
                        usedFeatures.Add(line);
                    }

                    
                    else
                    {
                        bool validLine = HandleLineValidation(fileName, line, i);
                        if (!validLine) { return false; }
                        featureBlockFinished = true; // This flag will be used to ensure all feature commands are placed before all others.
                    }
                }
                return true;
            }
            catch (FileNotFoundException) { return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nError: File not found: '{fileName}'.\n", false);  }
            catch (UnauthorizedAccessException) {  return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nPermission was denied for '{fileName}'.\n", false); }

            // Handles locked files, network errors, etc.
            catch (IOException ex) { return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nAn IO Exception occurred while validating: '{fileName}'\nError: {ex.Message}\n", false); }
            
            // General catchall (LOG MORE SEVERLY IF HIT) 
            catch (Exception ex){ return Errors.WriteErrorAndReturnBool($"BAMC Validation Error:\n\nAn unexpected error occurred while validating:'{fileName}'\nError: {ex.Message}\n", false); }
        }
        
        public static MenuOption Menu()
        {
           Dictionary<int, MenuOption> menuOptionsMapping = new()
           {
                { 1, MenuOption.Compile },
                { 2, MenuOption.Compile_And_Validate },
                { 3, MenuOption.Validate },
                { 4, MenuOption.Help },
           };
            string menuText = """
            Welcome To The BAM Manager (BAMM)!

            Please select the number correlating to your desired action from the menu options below:

            1. Compile BAMC File (No Validation)
            2. Compile BAMC File (With Validation)
            3. Validate BAMC File (No Compilation)
            4. Help


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
        
        public static string New()
        {
            bool configDirectoryExists = CreateUserScriptsDirectory();
            if (!configDirectoryExists) { return Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage); }

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0) { return Errors.WriteErrorAndReturnEmptyString(noFilesFoundMessage); }

            MenuOption selection = Menu();
            int index;
            switch (selection)
            {
                case MenuOption.Compile:
                    return "";

                case MenuOption.Compile_And_Validate:
                    HandleBAMCFileValidation(BAMCFiles);
                    index = HandleUserSelection(validFilesMapping);
                    selectedFile = BAMCFiles[index];
                    return selectedFile;

                case MenuOption.Validate:
                    HandleBAMCFileValidation(BAMCFiles);
                    DisplayValidFiles();
                    return "";

                case MenuOption.Help:
                    return "";
            }

            return "This should never be triggered";
        }
        
    }

    
}
