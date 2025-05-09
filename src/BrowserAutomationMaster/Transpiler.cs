using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
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
        static string selectedBrowser = "firefox"; // Defaults to firefox; brave, chrome, firefox, safari accepted.

        static bool browserPresent = false;
        static bool featurePresent = false;
        static bool otherPresent = false; // This might not be needed.

        static bool asyncEnabled = false; // Parser ensures both async and bypassCloudflare cannot both be true in a valid file.
        static bool bypassCloudflare = false;
        static bool disablePycache = false;

        static List<ApplicationNames> installedBrowsers = []; // Modified by VerifyInstallations();
        static List<ApplicationNames> installedPyVersions = []; // Modified by VerifyInstallations();
        static List<string> configLines = [];
        static List<string> featureLines = [];
        static List<string> importStatements = [];
        static List<string> scriptBody = [];
        static List<string> requirements = [];

        public static void New(string filePath)
        {
            SetScriptName(filePath);
            SetFileLines(filePath);
            AddBrowserImportsAndRequirements();
            Installations installations = InstallationCheck.Run();
            VerifyInstallations(installations);
            
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
                    //switch (selectedBrowser)
                    //{
                    //    case "firefox":
                    //        scriptBody.Add("driver = webdriver.")
                    //}
                    
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
            if (numberOfLines >= 1 && configLines[0].StartsWith("browser") && configLines[0].Contains(' ') && configLines[0].Split(' ').Length == 2) { browserPresent = true; }
            if (browserPresent) { selectedBrowser = configLines[0].Split(' ')[1].Replace('"', ' ').Trim(); }
            featureLines = [.. configLines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("feature"))];
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
            // GetUserAgent will exit in the event an invalid browserName is passed, thus the use of !
            if (browserPresent) { requestUserAgent = UserAgentManager.GetUserAgent(selectedBrowser)!; }
            if (asyncEnabled) { browserPackage = BrowserPackage.aiohttp; }
            if (bypassCloudflare) { browserPackage = BrowserPackage.tls_client; }
        }

        public static void HandleFeatureCmd()
        {
            if (featurePresent && featureLines.Count > 0)
            {
                foreach (string featureLine in featureLines)
                {
                    string firstArg = featureLine.Split(' ').First();
                    switch (firstArg){
                        case "":
                            break;
                    }
                }
            }
        }

        public static void HandlePythonVersionSelection(Installations installations)
        {
            List<string> foundVersions = [];
            Dictionary<ApplicationNames, string> versionMapping = new() {
                {ApplicationNames.Python3_9, "3.9" },
                {ApplicationNames.Python3_10, "3.10" },
                {ApplicationNames.Python3_11, "3.11" },
                {ApplicationNames.Python3_12, "3.12" },
                {ApplicationNames.Python3_13, "3.13" },
                {ApplicationNames.Python3_14, "3.14" },
            };
            string inputMessage = """
                Please select the number corresponding to the version of python to compile your BAMC file for:
            """;

            int iterationIndex = 0;
            foreach (ApplicationNames app in installations.InstalledApps){
                if (!versionMapping.TryGetValue(app, out string? version)){ continue; }
                foundVersions.Add(version);
                inputMessage += $"{iterationIndex}.     Python {version}\n";
                iterationIndex += 1;
            }
            while (true){
                Console.WriteLine(inputMessage);
                string? inputResponse = Console.ReadLine();
                if (int.TryParse(inputResponse, out int selection) && selection > 0 && selection <= iterationIndex)
                {
                    int elementIndex = selection - 1;
                    string value = foundVersions.ElementAt(elementIndex);
                    if (IsValidPyVersion(value))
                    {
                        pythonVersion = value;
                        break;
                    }
                }
            }
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
        public static void VerifyInstallations(Installations installations)
        {
            string pythonErrorMessage = """
            BAM Manager (BAMM) was unable to detect any valid versions of Python.

            BAMM was designed for Python 3.10 and 3.11.

            Please install one of these versions using the links below:

            Python 3.10: https://www.python.org/downloads/release/python-31011/
            Python 3.11: https://www.python.org/downloads/release/python-3119/
            """;


            string browserWarningMessage = """
            BAM Manager (BAMM) was unable to detect any supported browser installations.

            BAMM was designed mostly for browser automation, but has a few commands that can be used to automate raw requests (This is only for advanced users).
            
            Please install one of supported browsers below to fully utilize:

            Brave: https://brave.com/download/
            Chrome: https://google.com/chrome/
            Firefox: https://www.mozilla.org/en-US/firefox/
            """;

            string pythonWarningMessage = """
            BAM Manager (BAMM) detected multiple versions of Python.
            
            BAMM was designed for Python 3.10 and 3.11.
            
            While it's possible that scripts compiled with BAMM may run on 3.9, 3.12, 3.13, and 3.14, this should be avoided unless you intend to contribute to development.
            
            You will be prompted shortly to select a version to compile with, Please select either Python 3.10 or 3.11, unless you are explicitly testing other versions.
            """;




            installedBrowsers = [.. installations.InstalledApps.Where(x => InstallationCheck.BrowserApps.Contains(x))];
            installedPyVersions = [.. installations.InstalledApps.Where(x => InstallationCheck.PythonApps.Contains(x))];

            if (installedPyVersions.Count == 0) { Errors.WriteErrorAndExit(pythonErrorMessage, 1); }
            if (installedBrowsers.Count == 0) { Errors.WriteErrorAndContinue(browserWarningMessage); }

            List<string> problematicVersions = ["3.9", "3.12", "3.13", "3.14"];
            List<string> bestVersions = ["3.10", "3.11"];
            if (installedPyVersions.Any(x => problematicVersions.Contains(x.ToString()))) { 
                if (installedPyVersions.Any(x => bestVersions.Contains(x.ToString()))){
                    Errors.WriteErrorAndContinue(pythonWarningMessage);
                }
            }
        }
    }
}
