using System.Runtime.InteropServices;
using BrowserAutomationMaster;
using BrowserAutomationMaster.AppManager.OS;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;

bool isRunning = true;
string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Windows.VerifyRootDrive(pArgs); }


string testMessage = @$"OS Version: {Environment.OSVersion}
Platform: {Environment.OSVersion.Platform}
Current Dir: {Environment.CurrentDirectory}
Base Dir: {AppContext.BaseDirectory}
UserScripts Dir: {UserScriptManager.GetUserScriptDirectory()}";
Debug.WriteTestMessage(testMessage);

Console.Title = "BrowserAutomationMaster Manager (BAMM!)"; // Dont waste memory if the system isn't compatible.

List<string> validCLIArgs = ["add", "clear", "compile", "delete", "help", "uninstall"];
List<string> nonUserScriptArgs = ["clear", "help", "uninstall"]; // These commands are handled within the program loop instead of in UserScriptManager


bool isCLI = false;
if (pArgs.Length == 2 && !nonUserScriptArgs.Contains(pArgs[0].ToLower())) { isCLI = true; }


// Handles direct CLI cases
// -> bamm add "file.bamc"
// -> bamm compile "file.bamc" (if userScript directory contains file.bamc)
// -> bamm delete "file.bamc"
// -> bamm help --all
if (isCLI) {
    if (validCLIArgs.Contains(pArgs[0])) { var __ = new UserScriptManager(pArgs[1], pArgs[0]); }
}


// Handles cases where file is double clicked. (Functions the same as bamm add "file.bamc")
// The file is added to userScripts directory.
// (Logic to compile automatically is commented out as to not be intrusive, opting for user confirmation.)
if (pArgs.Length == 1 && pArgs[0].ToLower().EndsWith(".bamc") && File.Exists(pArgs[0]))
{
    var __ = new UserScriptManager(pArgs[0], "add");
    bool wantsToContinue = (Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ") ?? "n").ToLower().Trim().Equals("y");
    if (!wantsToContinue) { isRunning = false; }
}

else if (pArgs.Length == 1 && pArgs[0].Equals("clear", StringComparison.CurrentCultureIgnoreCase)) {
    Errors.WriteErrorAndContinue("Invalid 'clear' command.\n\nValid commands:\nbamm clear userScripts\nbamm clear compiled\n\nPress any key to continue...");
    Console.ReadKey();
}

else if (pArgs.Length == 2 && pArgs[0].Equals("clear", StringComparison.CurrentCultureIgnoreCase)) {
    if (pArgs[1].Equals("userScripts", StringComparison.CurrentCultureIgnoreCase)){
        if ((Input.WriteTextAndReturnRawInput("Are you sure you want to delete the 'userScripts' directory? [y/n]:") ?? "n").ToLower().Trim().Equals("y")) {
            DirectoryManager.DeleteDirectory(UserScriptManager.GetUserScriptDirectory());
        }
        else { isRunning = false; }
    }
    else if (pArgs[1].Equals("compiled", StringComparison.CurrentCultureIgnoreCase)){
        if ((Input.WriteTextAndReturnRawInput("Are you sure you want to delete the 'compiled' directory? [y/n]:") ?? "n").ToLower().Trim().Equals("y")) {
            DirectoryManager.DeleteDirectory(DirectoryManager.GetDesiredSaveDirectory());
        }
        else { isRunning = false; }
    }
    else {
        Errors.WriteErrorAndContinue("Invalid 'clear' command.\n\nValid commands:\nbamm clear userScripts\nbamm clear compiled\n\nPress any key to continue...");
        Console.ReadKey();
    }

}

// Handles cases where only bare "bamm help" command is supplied
else if (pArgs.Length == 1 && pArgs[0].Equals("help", StringComparison.CurrentCultureIgnoreCase)) {
    Errors.WriteErrorAndContinue("Invalid command: 'bamm help'\n\nTo see available entries for the 'help' command please type: 'bamm help --all'\n\nPress any key to continue.");
    Console.ReadKey();
}

// Handles bamm help "command-name"
else if (pArgs.Length == 2 && pArgs[0].Equals("help", StringComparison.CurrentCultureIgnoreCase)) { Help.ShowCommandDetails(pArgs[1]); }




while (isRunning)
{
    KeyValuePair<Parser.MenuOption, string> parserResult = Parser.New(); // The value of this KeyValuePair is the filepath of the selected file.
    switch (parserResult.Key)
    {
        case Parser.MenuOption.Add:
            bool overwriteConfirmation = (Input.WriteTextAndReturnRawInput("Would you like to compile the newly added file? [y/n]:") ?? "n").ToLower().Trim().Equals("y");
            if (overwriteConfirmation) { Transpiler.New(parserResult.Value, args); }
            break;
        case Parser.MenuOption.Compile:
            Transpiler.New(parserResult.Value, args);
            break;

        case Parser.MenuOption.Help:
            break;

        case Parser.MenuOption.Invalid:
            isRunning = false;
            break;
    }
    bool exitConfirmation = (Input.WriteTextAndReturnRawInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:") ?? "n").ToLower().Trim().Equals("y");
    if (exitConfirmation) { isRunning = false; }
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();