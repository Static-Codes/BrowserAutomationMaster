using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    // Use https://github.com/bogdanfinn/tls-client
    internal class Transpiler
    {
        readonly static string backupScriptFileName = "untitled-script";
        readonly static string desiredSaveDirectory = "compiled";
        static string pythonScriptFileName = "";
        static bool browserPresent = false;
        static bool featurePresent = false;
        static bool otherPresent = false;
        static List<string> configLines = [];
        static List<string> importStatements = [];
        static List<string> scriptBody = [];
        static List<string> requirements = [];


        //readonly static Dictionary<string, string> featureToPackageMapping = new()
        //{
        //    {"bypass-cloudflare", "tls-client" },
        //    {"disable-pycache", "" },
        //};

        public static void New(string filePath)
        {
            SetScriptName(filePath);
            SetFileLines(filePath);
            
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
            featurePresent = configLines.Select(line => line.Trim()).Any(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("feature")); // Roslyn recommend Any() over Count() > 0
            otherPresent = CheckOtherPresent();

            // if (numberOfLines >= 1 && co)

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
                
            }
            else
            {

            }
        }

        public static void HandleFeatureCmd()
        {

        }

        public static void HandleOtherCmds()
        {

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
