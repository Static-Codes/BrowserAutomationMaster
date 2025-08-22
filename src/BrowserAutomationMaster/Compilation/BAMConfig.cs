using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Compilation
{
    public partial class BAMConfig(string filePath)
    {
        public string[] Lines = GetConfigLines(filePath);
        
        // Lines containing feature commands
        public string[] featureLines = [];

        public string Name = Path.GetFileName(filePath);

        // Not to be confused with noBrowsersFound, this is a flag only for the command 'browser'
        public bool browserPresent = false;
        public bool featurePresent = false;
        public bool otherPresent = false;

        // Disables the writing of .pyc files.
        public bool disablePycache = false;

        // Disables SSL certificate authorization session wide, if specified.
        public bool disableSSL = false;

        // Runs the browser in headless mode, if specified.
        public bool runHeadless = false;

        public string selectedBrowser = "firefox"; // Defaults to firefox, will be changed if needbe. Accepted Values: "chrome" or "firefox"


        // Used for browserPresent
        private readonly static Regex BrowserRegex = BrowserRegexCompilation();
        [GeneratedRegex(@"^browser\s""(chrome|firefox)""$", RegexOptions.Compiled)]
        private static partial Regex BrowserRegexCompilation();

        public void CheckConfigLines()
        {
            int numberOfLines = Lines.Length;
            if (numberOfLines == 0)
            {
                Errors.WriteErrorAndExit(
                    message:
                        "BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\n" +
                        "Press any key to exit...",
                    status: 1
                );
            }

            browserPresent = BrowserRegex.IsMatch(Lines[0]);

            if (browserPresent)
                selectedBrowser = Lines[0].Split(' ')[1].Replace('"', ' ').Trim();

            featureLines = [.. Lines
            .Select(line => line.Trim())
            .Where(line =>
                !string.IsNullOrWhiteSpace(line)
                && line.StartsWith("feature")
            )
            ];

            featurePresent = featureLines.Length > 0;

            disablePycache = featurePresent && featureLines.Any(line => line.Contains("\"disable-pycache\""));
            disableSSL = featurePresent && featureLines.Any(line => line.Contains("\"disable-ssl\""));
            runHeadless = featurePresent && featureLines.Any(line => line.StartsWith("\"run-headless\""));

            otherPresent = OtherPresentFound();

            if (!otherPresent)
            {
                Warning.Write(
                    message:
                        "BAM Manager (BAMM) was unable to find any requests logic, " +
                        "if this is intentional, you can safely ignore this warning."
                );
            }
        }

        private static string[] GetConfigLines(string filePath)
        {
            try
            {
                return File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                var message =
                    "Unable to read the contents of the desired BAMC file.\n" +
                    $"If this error persists, please make a bug report at {ISSUES_LINK}\n" +
                    $"Error Log:\n\n{e.Message}";
                Errors.Write(message);
                return [];
            }
        }

        public bool OtherPresentFound()
        {
            if (Lines.Length == 0)
                return false;

            foreach (string line in Lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmedLine = line.Trim();
                string firstArg;

                int spaceCharIndex = trimmedLine.IndexOf(' ');
                if (spaceCharIndex == -1)
                    firstArg = trimmedLine;

                else
                    firstArg = trimmedLine[..spaceCharIndex];

                if (Parser.actionArgs.Contains(firstArg))
                    return true;
            }
            return false;
        }



    }
}