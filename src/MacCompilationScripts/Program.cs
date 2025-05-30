using MacCompilationScripts;
using MacCompilationScripts.Messaging;

string username = Environment.UserName;

if (username == null) { Errors.WriteErrorAndExit("Unable to determine the active user's username, if this persists, it is almost definitely a bug and NOT an issue with your system.", 1); }

string macAppBundlePath = @$"C:\Users\{username}\Documents\GitHub\BrowserAutomationMaster\BrowserAutomationMaster\src\BrowserAutomationMaster\bin\Release\net8.0\osx-x64\publish\bamm";

if (!Path.Exists(macAppBundlePath)) { 
    Errors.WriteErrorAndExit($"Unable to find the Mac .app Bundle Path:\n\nThis should be located at:\n{macAppBundlePath}", 1); 
}

bool argsProvided = args.Length > 0;
//Success.WriteSuccessMessage(macAppBundlePath);

if (!argsProvided) {
    Warning.Write("This script can be run with arguments.  If you are an advanced user who is comfortable with Command Line arguments, please run this script with the -a flag, otherwise you may continue using the onscreen prompts.");
}

string versionFile = "nextBuildVersion.txt";
int currentVersionNumber = 1; // Default if file doesn't exist.
if (File.Exists(versionFile))
{
    try {
        string fileContent = File.ReadAllText(versionFile);
        if (int.TryParse(fileContent, out int parsedVersion)) { 
            currentVersionNumber = parsedVersion; 
        }
    }
    catch { }
    
}
else {  File.WriteAllText(versionFile, "1"); }

int nextVersionNumber = currentVersionNumber + 1;
try { File.WriteAllText(versionFile, nextVersionNumber.ToString()); }
catch {
    Errors.WriteErrorAndExit($"Unable to manually update build number in nextBuildVersion.txt, please ensure this file contains the number: {nextVersionNumber}", 1);
}
Structure.CompileBuild(macAppBundlePath, currentVersionNumber.ToString());
Console.WriteLine($"Version after increment and write: {nextVersionNumber}");


