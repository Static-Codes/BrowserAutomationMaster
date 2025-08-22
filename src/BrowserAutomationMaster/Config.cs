using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;


public partial class ConfigRegex()
{
    public Regex BrowserRegex = BrowserRegexCompilation();
    [GeneratedRegex(@"^browser\s""(chrome|firefox)""$", RegexOptions.Compiled)]
    private static partial Regex BrowserRegexCompilation();

}

public class Config(string filePath)
{
	public string[] Lines = GetConfigLines(filePath);
    public string[] featureLines = [];
	public string desiredSaveDirectory = GetDesiredSaveDirectory();
	public bool browserPresent = false;
	public bool featurePresent = false;
	public bool otherPresent = false;
	public bool disablePycache = false;
	public bool disableSSL = false;
	public bool runHeadless = false;
	public string selectedBrowser = "firefox"; // Defaults to firefox, will be changed if needbe.

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

    public static bool OtherPresentFound(Config config)
    {

        if (config.Lines.Length == 0) 
            return false; 
        
        foreach (string line in config.Lines)
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

    public void Test(Config config)
	{
        int numberOfLines = config.Lines.Length;
        if (numberOfLines == 0)
        {
            Errors.WriteErrorAndExit(
                message:
                    "BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\n" +
                    "Press any key to exit...",
                status: 1
            );
        }

        browserPresent = 
            Lines[0].StartsWith("browser") &&
            Lines[0].Contains(' ') &&
            Lines[0].Split(' ').Length == 2;

        if (browserPresent)
            selectedBrowser = config.Lines[0].Split(' ')[1].Replace('"', ' ').Trim();

        config.featureLines = [..
            config.Lines
                .Select(line => line.Trim())
                .Where(line =>
                    !string.IsNullOrWhiteSpace(line)
                    && line.StartsWith("feature")
                )
        ];

        featurePresent = config.featureLines.Length > 0;

        disablePycache = featurePresent && featureLines.Any(line => line.Contains(" \"disable-pycache\""));
        disableSSL = featurePresent && featureLines.Any(line => line.Contains(" \"disable-ssl\""));
        runHeadless = featurePresent && featureLines.Any(line => line.StartsWith("feature \"run-headless\""));

        otherPresent = OtherPresentFound(config);
    }
}
