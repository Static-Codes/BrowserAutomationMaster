using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    // Use https://github.com/bogdanfinn/tls-client
    enum BrowserPackage
    {
        aiohttp,
        selenium,
        tls_client
    }

    
    internal class Transpiler
    {
        readonly static string backupScriptFileName = "untitled-script";
        readonly static string desiredSaveDirectory = "compiled";
       
        static BrowserPackage browserPackage = BrowserPackage.selenium; // By default selenium is chosen, however aiohttp and tls-client as also possible options.
        static string pythonScriptFileName = "";  // Modified by SetScriptName();
        static string pythonVersion = "3.10";
        private static string requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"; // Default value if inhouse function fails.

        static bool browserPresent = false;
        static bool featurePresent = false;
        static bool otherPresent = false; // This might not be needed.

        static bool asyncEnabled = false; // Parser ensures both async and bypassCloudflare cannot both be true in a valid file.
        static bool bypassCloudflare = false;
        static bool disablePycache = false;

        static List<string> configLines = [];
        static List<string> importStatements = [];
        static List<string> scriptBody = [];
        static List<string> requirements = [];

        public static void New(string filePath)
        {
            SetScriptName(filePath);
            SetFileLines(filePath);
            AddBrowserImportsAndRequirements();
            foreach (string importStatement in importStatements)
            {
                Console.WriteLine(importStatement);
            }
        }

        public static void AddBrowserImportsAndRequirements()
        {
            HandleBrowserCmd();
            // This function will exit if a null value is reached so no worries about a null check here
            string version = PackageManager.New(browserPackage.ToString(), pythonVersion);
            requirements.Add($"{browserPackage}=={version}");
            switch (browserPackage)
            {
                case BrowserPackage.aiohttp:
                    
                    importStatements.Add("from aiohttp import ClientSession");
                    break;

                case BrowserPackage.tls_client:
                    importStatements.Add("from tls_client import Session");
                    break;

                case BrowserPackage.selenium:
                    importStatements.Add("from selenium import webdriver");
                    break;
            }

        }
        public static void CheckConfigLines()
        {
            int numberOfLines = configLines.Count;
            if (numberOfLines == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Errors.WriteErrorAndExit("BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\nPress any key to exit...", 1);
            }
            if (numberOfLines >= 1 && configLines[1].StartsWith("browser")) { browserPresent = true; }
            List<string> featureLines = [.. configLines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("feature"))];
            featurePresent = featureLines.Count > 0; // Roslyn recommend Any() over Count() > 0
            if (featurePresent ||  featureLines.Any(line => line.Contains(" \"disable-pycache\""))) { disablePycache = true; }
            otherPresent = CheckOtherPresent();
            if (!otherPresent) { Errors.WriteErrorAndContinue("BAM Manager (BAMM) was unable to find any requests logic, if this is intentional, you can safely ignore this warning."); }
            if (disablePycache) { importStatements.AddRange(["import sys", "sys.dont_write_byte_code"]); }
            if (featurePresent || featureLines.Any(line => line.Contains(" \"async\""))) { asyncEnabled = true; }
            if (featurePresent || featureLines.Any(line => line.Contains(" \"bypass-cloudflare\""))) { bypassCloudflare = true; }
            
        }

        

        public static bool CheckOtherPresent()
        {
            
            if (configLines.Count == 0) { return false; }
            foreach (string line in configLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string trimmedLine = line.Trim();
                string firstArg;
                int spaceCharIndex = trimmedLine.IndexOf(' ');
                if (spaceCharIndex == -1) { firstArg = trimmedLine; }
                else { firstArg = trimmedLine[..spaceCharIndex]; }
                if (Parser.actionArgs.Contains(firstArg)) {  return true; }
            }
            return false;
        }





        public static string GenerateBackupFilename()
        {
            string potentialFileName = $"{backupScriptFileName}.py";
            int index = 2;
            while (true)
            {
                if (!File.Exists(potentialFileName)) { return potentialFileName; }
                potentialFileName = $"{backupScriptFileName}({index}).py";
                index++;
            }
        }

        public static void HandleBrowserCmd()
        {
            if (browserPresent)
            {
                // Make a JSON file with browser name as key and a list of user agents as value (Dictionary<string, List<string>>)
                // Make a function that returns a random user agent if a valid key is provided, and throws an error if not.
                // Call said function here, if valid response, overwrite requestUserAgent; otherwise continue without.
            }
            if (asyncEnabled) { browserPackage = BrowserPackage.aiohttp; }
            if (bypassCloudflare) { browserPackage = BrowserPackage.tls_client; }
            
        }

        public static void HandleFeatureCmd()
        {

        }

        public static void HandleOtherCmds()
        {

        }

        public static bool IsValidPyVersion(string pyVersion)
        {
            if (string.IsNullOrWhiteSpace(pyVersion)) { return false; }
            string[] parts = pyVersion.Split('.');
            if (parts.Length != 2) { return false; }
            if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor)) { return false; }
            return major == 3 && minor >= 9 && minor <= 13;
        }

        public static void SetFileLines(string filePath)
        {
            string fileNotFoundMessage = $"BAM Manager (BAMM) was unable to find the file:\n\n{filePath}, Please ensure this file exists, then rerun bamm.exe.\n\nPress any key to exit...";
            if (!File.Exists(filePath)) { Errors.WriteErrorAndExit(fileNotFoundMessage, 1); }
            configLines = [.. File.ReadAllLines(filePath).Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line))];
            CheckConfigLines();
        }

        public static void SetScriptName(string filePath)
        {
            string failureMessage = $"""
            BAM Manager (BAMM) was unable to access:\n\n{filePath}\n\nPlease ensure this file was not deleted, and is not in use by any other program.\n\nPress any key to exit...
            """;
           
            try
            {
                string fileName = Path.GetFileName(filePath);
                // If null the function i wrote will exit, however the static compiler is unaware of this thus the requirement for !.
                if (fileName == null) { Errors.WriteErrorAndExit(failureMessage, 1); }

                if (!File.Exists(filePath))
                {
                    failureMessage = $"BAM Manager (BAMM) was unable to access:\n\n{fileName}\n\nPlease ensure this file was not deleted, and is not in use by any other program.\\n\\nPress any key to exit...";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Errors.WriteErrorAndExit(failureMessage, 1);
                }
                pythonScriptFileName = fileName!.Split(".")[0] + ".py"; // I hate c#'s static compiler i already ensured fileName cannot be null, yet i have to yell at the compiler using !
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); Errors.WriteErrorAndExit(failureMessage, 1); }
        }

    }
}
