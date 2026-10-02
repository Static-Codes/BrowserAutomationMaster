using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Parsing;
using BrowserAutomationMaster.Core.Utilities;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Utilities.ExtensionUtility;

namespace BrowserAutomationMaster.Core.Compilation
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

        // Adds any extensions specified using the `add-extension` feature command.
        public ExtensionUtility[] Extensions = [];

        // Disables the writing of .pyc files, if `disable-pycache` is specified.
        public bool disablePycache = false;

        // Disables SSL certificate authorization session wide, if `disable-ssl` is specified.
        public bool disableSSL = false;

        // Runs the browser in headless mode, if `--run-headless` is specified.
        public bool runHeadless = false;

        // Instructs the Transpiler to use a mobile user agent, if `use-mobile-user-agent` is specified.
        public bool useMobileUserAgent = false;

        // Defaults to firefox, will be changed if needbe. 
        // Accepted Values: 
        // "chrome"
        // "firefox"
        // "safari"
        // Note:
        // Specifying safari as the browser, does not instruct Selenium to use Safari for the session.
        // Instead Firefox is used with a Safari user agent.
        public string selectedBrowser = "firefox"; 

        public void CheckConfigLines()
        {
            int numberOfLines = Lines.Length;
            if (numberOfLines == 0)
            {
                WriteAndExit(
                    message:
                        "BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\n" +
                        "Press any key to exit...",
                    status: 1
                );
            }

            browserPresent = BrowserRegex.IsMatch(Lines[0]);

            // If browser is specified, otherwise it defaults to firefox.
            if (browserPresent)
            {
                selectedBrowser = Lines[0].Split(' ')[1].Replace('"', ' ').Trim();
            }

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
            useMobileUserAgent = featurePresent && featureLines.Any(line => line.StartsWith("\"use-mobile-user-agent\""));

            var extensionPaths = GetExtensionPaths(featureLines);
        
            Extensions = CreateExtensionArrayFromPaths(extensionPaths, selectedBrowser);

            otherPresent = OtherPresentFound();

            if (!otherPresent)
            {
                Warning.Write
                (
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
                    $"Unable to read the contents of the desired BAMC file.{NLC}" +
                    $"If this error persists, please make a bug report at {ISSUES_LINK}{NLC}" +
                    $"Error Log:{NLC}{e.Message}";
                Write(message);
                return [];
            }
        }

        public bool OtherPresentFound()
        {
            if (Lines.Length == 0) {
                return false;
            }

            foreach (string line in Lines)
            {
                if (string.IsNullOrWhiteSpace(line)) {
                    continue;
                }

                string trimmedLine = line.Trim();
                string firstArg;

                int spaceCharIndex = trimmedLine.IndexOf(' ');
                firstArg =  spaceCharIndex == -1 ? trimmedLine : trimmedLine[..spaceCharIndex];

                if (Parser.actionArgs.Contains(firstArg)) {
                    return true;
                }
            }
            return false;
        }

        private static string[] GetExtensionPaths(string[] featureLines) 
        {
            var paths = 
                featureLines
                .Where(
                    line => 
                    line.Contains("\"add-extension\"") && 
                    line.AsSpan().Count(' ') == 2
                )
                .Select(line => line.Split(' ')[2].Replace("\"", ""));

            if (paths == null) { 
                return [];
            }
            
            return [.. paths];
        }

    }
}