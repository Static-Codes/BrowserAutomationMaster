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


        readonly static string[] actionArgs = ["click", "click-button", "get-text", "fill-textbox", "save-as-html", "select-dropdown", "select-dropdown-element", "take-screenshot", "wait-for-seconds", "visit"];
        readonly static string[] proxyFeatureArgs = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] otherFeatureArgs = ["bypass-cloudflare"];
        readonly static string[] featureArgs = [.. proxyFeatureArgs, .. otherFeatureArgs];
        readonly static string[] validArgs = [.. actionArgs, .. featureArgs];
        static string selectedFile = string.Empty;
        static List<string> validFiles = [];
        

        readonly static Dictionary<int, string> validFilesMapping = [];
        readonly static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        readonly static string configDirectory = Path.Combine([baseDirectory, "config"]);
        const string ProxyFormatPattern = @"^([^:]+):([^@]+)@([^:]+):(\d+)$";

        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.

        [GeneratedRegex(ProxyFormatPattern)]
        private static partial Regex PrecompiledProxyRegex();


        
        

        public static bool CreateConfigDirectory()
        {
            if (configDirectory == null)
            {
                return false;
            }
            if (Directory.Exists(configDirectory))
            {
                return true;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(configDirectory);
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
                int index = pair.Key; // Remove this comment after this is done; Remember to lower the parsed integer value by one before returning the file
                string? rawFileName = null;
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
                return [.. Directory.GetFiles(configDirectory).Where(x => x.ToLower().EndsWith(".bamc"))];
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
            string noFilesFoundMessage = $"""
                BAM Manager (BAMM) was unable to find any valid .bamc files.
                
                Please check the 'config' directory and contains atleast one .bamc file!

                Config Directory: {configDirectory}

                If this directory wasn't already created please add a new folder named 'config' inside the same directory as this executable.

                Press any key to exit...
            """;

            validFiles = [.. ValidateBAMCFiles(BAMCFiles)];
            if (validFiles.Count == 0)
            {
                WriteErrorAndExit(noFilesFoundMessage, 1);
            }
            CreateValidFilesMapping(validFiles);
            if (validFilesMapping.Count == 0)
            {
                WriteErrorAndExit(noFilesFoundMessage, 1);
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
                        return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "fill-textbox":
                    if (lineArgs.Length != 3 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"') || !lineArgs[2].StartsWith('"') || !lineArgs[2].EndsWith('"'))
                    {
                        return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} \"value\"\n", false);
                    }
                    return true;

                case "wait-for-seconds":
                    selectorString = "5";
                    if (lineArgs.Length != 2 || !int.TryParse(lineArgs[1], out int seconds) || seconds < 1)
                    {
                        return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    return true;

                case "feature":
                    if (lineArgs.Length != 2 && lineArgs.Length != 3 || !featureArgs.Contains(lineArgs[1]) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        selectorString = "\"feature-name\"";
                        return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString}\n", false);
                    }
                    string[] proxyFeatures = ["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
                    if (proxyFeatures.Contains(lineArgs[1]))
                    {
                        if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                        {
                            selectorString = $"\"{lineArgs}\"";
                            return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                        }

                        lineArgs[2] = lineArgs[2].Replace('"', ' ').Trim();
                        bool validProxy = IsValidProxyFormat(lineArgs[2]);
                        if (!validProxy)
                        {
                            return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {lineNumber}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                        }


                    }

                    return true;

                default:
                    return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid command on line {lineNumber}.\nPlease check your spelling and try again.\n", false);


            }
        }

        public static int HandleUserSelection(Dictionary<int, string> mapping)
        {
            Type desiredType = typeof(int);
            string noFilesFoundMessage = $"""
                BAM Manager (BAMM) was unable to find any valid .bamc files.
                
                Please check the 'config' directory and contains atleast one .bamc file!

                Config Directory: {configDirectory}

                If this directory wasn't already created please add a new folder named 'config' inside the same directory as this executable.

                Press any key to exit...
            """.Trim();

            if (mapping.Count == 0)
            {
                WriteErrorAndExit(noFilesFoundMessage, 1);
            }

            int numberOfFilesFound = mapping.Count;
           
            string inputText = string.Empty;
            foreach (KeyValuePair<int, string> pair in mapping)
            {
                int index = pair.Key; // Remove this comment after this is done; Remember to lower the parsed integer value by one before returning the file
                string? rawFileName = null;
                try { rawFileName = Path.GetFileName(pair.Value); }
                catch { rawFileName = null; }
                if (rawFileName != null)
                {
                    inputText += $"{index}. {rawFileName}\n";
                }
            }

            if (inputText == string.Empty) { WriteErrorAndExit(noFilesFoundMessage, 1); }

            inputText = $"BAM Manager (BAMM) was able to locate {numberOfFilesFound} valid .bamc files!\n\n{inputText}\n\nPlease enter the number corresponding to your desired file [Between 1-{numberOfFilesFound}]: ";
            string panicText = $"BAM Manager (BAMM) panicked due an invalid value provided as input.  Value must be between 1 and {numberOfFilesFound}\n\n{inputText}";
            

            while (true)
            {
                object? rawInput = WriteTextAndReturnInputType(inputText, panicText, desiredType, true); // This will run until valid input is provided.
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
                for (int i = 0; i < lines.Count; i++)
                {
                    string selectorString = "value";
                    string line = lines[i];
                    string[] lineArgs = line.Split(" ");
                    if (lineArgs.Length == 0) { return false; }
                    string firstArg = lineArgs[0];

                    if (firstArg.Equals("feature"))
                    {
                        if (featureBlockFinished)
                        {
                            return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid 'feature' command location on line {i + 1}.\nAll 'feature' commands must be placed before any other command.\n", false);
                        }
                        if (usedFeatures.Contains(line))
                        {
                            return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nDuplicate command on line {i + 1}:\n{line}\nAll 'feature' commands may only be defined once.\n", false);
                        }
                        string[] proxyFeatures = ["\"use-http-proxy\"", "\"use-https-proxy\"", "\"use-socks4-proxy\"", "\"use-socks5-proxy\""];
                        if (proxyFeatures.Contains(lineArgs[1]))
                        {
                            if (lineArgs.Length != 3 || lineArgs[2].Count(c => (c == ':')) != 2 || lineArgs[2].Count(c => (c == '@')) != 1)
                            {
                                selectorString = lineArgs[1];
                                return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
                            }

                            bool validProxy = IsValidProxyFormat(lineArgs[2]);
                            if (!validProxy)
                            {
                                return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid syntax on line {i + 1}\nLine: {line}\nValid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\nIf no authentication is required: NULL:NULL@IP:PORT\n", false);
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
            catch (FileNotFoundException) { return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nError: File not found: '{fileName}'.\n", false);  }
            catch (UnauthorizedAccessException) {  return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nPermission was denied for '{fileName}'.\n", false); }

            // Handles locked files, network errors, etc.
            catch (IOException ex) { return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nAn IO Exception occurred while validating: '{fileName}'\nError: {ex.Message}\n", false); }
            
            // General catchall (LOG MORE SEVERLY IF HIT) 
            catch (Exception ex){ return WriteErrorAndReturnBool($"BAMC Validation Error:\n\nAn unexpected error occurred while validating:'{fileName}'\nError: {ex.Message}\n", false); }
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

            1. Compile BAMC File (With Validation)
            2. Compile BAMC File (No Validation)
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
        public static void New()
        {
            bool configDirectoryExists = CreateConfigDirectory();
            if (!configDirectoryExists)
            {
                Console.WriteLine("An exception occured while attempting to create the BAMC directory.");
                return;
            }

            string noFilesFoundMessage = $"""
                BAM Manager (BAMM) was unable to find any .bamc files.
                
                Please check the 'config' directory and contains atleast one .bamc file!

                Config Directory: {configDirectory}

                If this directory wasn't already created please add a new folder named 'config' inside the same directory as this executable.

                Press any key to exit...
            """;

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0)
            {
                WriteErrorAndExit(noFilesFoundMessage, 1);
            }

            MenuOption selection = Menu();
            int index;
            switch (selection)
            {
                case MenuOption.Compile:
                    break;

                case MenuOption.Compile_And_Validate:
                    HandleBAMCFileValidation(BAMCFiles);
                    index = HandleUserSelection(validFilesMapping);
                    selectedFile = BAMCFiles[index];
                    break;

                case MenuOption.Validate:
                    HandleBAMCFileValidation(BAMCFiles);
                    DisplayValidFiles();
                    break;

                case MenuOption.Help:
                    break;
            }
        }

        public static void WriteErrorAndExit(string message, int status)
        {
            Console.WriteLine(message);
            Console.ReadKey();
            Environment.Exit(status);
        }
        public static bool WriteErrorAndReturnBool(string message, bool returnBool)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            return returnBool;
        }
        public static string? WriteTextAndReturnRawInput(string inputMessage)
        {
            Console.WriteLine(inputMessage);
            return Console.ReadLine();
        }
        public static object? WriteTextAndReturnInputType(string inputMessage, string panicMessage, Type desiredType, bool repeatUntilValid = false)
        {
            string? rawInputString;
            if (desiredType == null)
            {
                WriteErrorAndExit($"Invalid type provided to WriteTextAndReturnInputType(.., .., {desiredType}).\nIf you are seeing this there is invalid code written and it should be addressed immediately ", 1);
            }

            while (true)
            {
                rawInputString = WriteTextAndReturnRawInput(inputMessage);

                if (rawInputString != null)
                {
                    if (desiredType == typeof(int))
                    {
                        try { return Convert.ToInt32(rawInputString); }
                        catch {
                            if (!repeatUntilValid)
                            {
                                return null;
                            }
                        }
                    }
                    else if (desiredType == typeof(string))
                    {
                        try { return rawInputString; }
                        catch {
                            if (!repeatUntilValid)
                            {
                                return null;
                            }
                        }
                    }

                }
                if (!repeatUntilValid)
                {
                    return null;
                }
                inputMessage = panicMessage; // Starts writing the panic message instead of the initial input message.
            }

        }
    }

    
}
