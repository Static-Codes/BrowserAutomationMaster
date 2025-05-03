using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    public class Parser
    {
        public enum MenuOption
        {
            Compile,
            Compile_And_Verify,
            Help,
            Verify,
        }   
         

        readonly static string[] actionArgs = ["click", "click-button", "get-text", "fill-textbox", "save-as-html", "select-dropdown", "select-dropdown-element", "take-screenshot", "wait-for-seconds", "visit"];
        readonly static string[] featureArgs = ["bypass-cloudflare", "use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"];
        readonly static string[] validArgs = [.. actionArgs, .. featureArgs];
        readonly static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        readonly static string configDirectory = Path.Combine([baseDirectory, "config"]);
        
        public static void New()
        {
            bool configDirectoryExists = CreateConfigDirectory();
            if (!configDirectoryExists) {
                Console.WriteLine("An exception occured while attempting to create the BAMC directory.");
                return;
            }

            string noFilesFoundMessage = $"""
                BAM Manager (BAMM) was unable to find any .bamc files.
                
                Please ensure the config directory exists, and contains atleast one valid .bamc file!

                Config Directory: {configDirectory}

                If this directory wasn't already created please add a new folder named 'config' inside the same directory as this executable.

                Press any key to exit...
            """;

            string[] BAMCFiles = GetBAMCFiles();
            //Console.WriteLine(BAMCFiles.Length);
            string[] validFiles = ValidateBAMCFiles(BAMCFiles);
            if (validFiles.Length != 0)
            {
                Console.WriteLine($"{validFiles.Length} valid .BAMC files found!");
                Console.WriteLine("Press any key to exit..."); // Remove this line after debugging
                foreach (string BAMCFile in validFiles)
                {
                    Console.WriteLine(BAMCFile);
                }
            }
            else { Console.WriteLine(noFilesFoundMessage); }
            Console.ReadKey();

        }
    
       
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

        public static string[] GetBAMCFiles()
        {
            try {
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

        public static bool HandleLineValidation(string fileName, string line, int lineNumber)
        {
            string[] lineArgs = line.Split(" ");
            string firstArg = lineArgs[0];
            switch (firstArg)
            {
                case "click" or "click-button" or "get-text" or "fill-textbox" or "select-dropdown" or "select-dropdown-element" or "save-as-html" or "take-screenshot" or "visit":
                    string selectorString = "\"css-selector\""; // Defaults to "css-selector" for selector based actions
                    if (firstArg.Equals("save-as-html")) { selectorString = "filename.html"; }
                    if (firstArg.Equals("take-screenshot")) { selectorString = "filename.png"; }

                    if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        Console.WriteLine($"[File \"{fileName}\"]:\nInvalid Line [Line {lineNumber}]:\n{line}\n\nCValid Syntax:\n{firstArg} {selectorString}");
                        return false;
                    }
                    return true;

                case "wait-for-seconds":
                    if (lineArgs.Length != 2 || !int.TryParse(lineArgs[1], out int seconds) || seconds < 1)
                    {
                        Console.WriteLine($"[File \" {fileName} \"]:\nInvalid Line [Line {lineNumber}]:\n{line}\n\nValid Syntax:\n{firstArg} 5");
                        return false;
                    }
                    return true;

                case "feature":
                    if (lineArgs.Length != 2 || !featureArgs.Contains(lineArgs[1]) || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                    {
                        Console.WriteLine($"[File \" {fileName} \"]:\nInvalid Line [Line {lineNumber}]:\n{line}\n\nValid Syntax:\n{firstArg} \"feature-name\"");
                        return false;
                    }


                    return true;

                default:
                    Console.WriteLine("Implement Me");
                    return false;

            }
        }
        public static bool IsValidFile(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            try 
            {
                List<string> lines = [.. File.ReadAllLines(filePath).Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line))];
                bool featureBlockFinished = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];
                    if (line.StartsWith("feature"))
                    {
                        if (featureBlockFinished)
                        {
                            Console.WriteLine($"BAMC Validation Error:\n\nFile: \"{fileName}\"\nInvalid 'feature' command location on line {i}.\nAll 'feature' commands must be placed before any other command.");
                            return false;
                        }
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
            catch (FileNotFoundException)
            {
                Console.WriteLine($"BAMC Validation Error:\n\nError: File not found: '{fileName}'.\n");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"BAMC Validation Error:\n\nPermission was denied for '{fileName}'.\n");
                return false;
            }

            catch (IOException ex) // Handles locked files, network errors, etc.
            {
                Console.WriteLine($"BAMC Validation Error:\n\nAn IO Exception occurred while validating: '{fileName}'\nError: {ex.Message}\n");
                return false;
            }
            catch (Exception ex) // General catchall (LOG MORE SEVERLY IF HIT)
            {
                Console.WriteLine($"BAMC Validation Error:\n\nAn unexpected error occurred while validating:'{fileName}'\nError: {ex.Message}\n");
                return false;
            }
        }
        

        public static void Menu()
        {
            Dictionary<int, MenuOption> menuOptionsMapping = new()
            {
                { 1, MenuOption.Compile },
                { 2, MenuOption.Compile_And_Verify },
                { 3, MenuOption.Help },
                { 4, MenuOption.Verify },
            };
            string menuText = """
                Welcome To The BAM Manager (BAMM)!

                Please select the number correlating to your desired action from the menu options below:

                1. Compile BAMC File (With Verification)
                2. Compile BAMC File (No Verification)
                3. Verify BAMC File (No Compilation)
                4. Help


            """;
            string invalidChoiceText = "Invalid option please enter a number between 1 and 4.\n\n" + menuText;
            MenuOption selection;

            Console.WriteLine(menuText);
            while (true) {
                // ? Declares userChoice as a nullable value, as input cannot be verified without sanitization.
                bool validChoice = int.TryParse(Console.ReadLine(), out int optionNumber);
                if (validChoice && menuOptionsMapping.TryGetValue(optionNumber, out selection))
                { 
                    break; 
                }
                Console.WriteLine(invalidChoiceText);

            }
            switch (selection)
            {
                case MenuOption.Compile: break;
                case MenuOption.Compile_And_Verify: break;
                case MenuOption.Help: break;
                case MenuOption.Verify: break;
            }

        }

    }

    

    
}
