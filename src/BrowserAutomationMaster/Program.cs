using System.Runtime.InteropServices;
using BrowserAutomationMaster;
using BrowserAutomationMaster.AppManager.OS;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;

string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { Windows.VerifyRootDrive(pArgs); } // Verify the user is running on their C: drive (assuming they're on windows)


List<string> validCLIArgs = ["add", "clear", "compile", "delete", "help", "run"];
// If pArgs contains any args from nonUserScriptArgs, Compatibility checks are skipped because the user is not attempting to compile or run any scripts.
List<string> nonUserScriptArgs = ["clear", "help", "uninstall"]; // These commands are handled within the program loop instead of in UserScriptManager.

Console.Title = $"BrowserAutomationMaster Manager (BAMM!) {UpdateManager.CurrentVersion}";

bool isRunning = true;
bool isCLI = false;

if (!pArgs.Any(arg => nonUserScriptArgs.Contains(arg))) { RuntimeManager.DoRuntimeCheck(); } // Set expectations regarding automation performance given the user's specs.
UpdateManager.CheckForUpdate(); // New releases are fun - Ghandi probably.

if (pArgs.Length == 2 && !nonUserScriptArgs.Contains(pArgs[0].ToLower())) { isCLI = true; } // Set CLI True if a validCLIArg is passed.


// Handles direct CLI cases
// -> bamm add "file.bamc"
// -> bamm compile "file.bamc" (if userScript directory contains file.bamc)
// -> bamm delete "file.bamc"
// -> bamm help --all
// -> bamm run "filename.py"
if (isCLI) {
    if (validCLIArgs.Contains(pArgs[0])) { var __ = new UserScriptManager(pArgs[1], pArgs[0]); }
}


// Handles cases where file is double clicked. (Functions the same as bamm add "file.bamc") The file is added to userScripts directory.
if (pArgs.Length == 1 && pArgs[0].ToLower().EndsWith(".bamc") && File.Exists(pArgs[0])) {
    var __ = new UserScriptManager(pArgs[0], "add");
    bool wantsToContinue = (Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ") ?? "n").ToLower().Trim().Equals("y");
    if (!wantsToContinue) { isRunning = false; }
}

// Handles bare 'bamm clear' command
else if (pArgs.Length == 1 && pArgs[0].Equals("clear", StringComparison.CurrentCultureIgnoreCase)) {
    Errors.WriteErrorAndContinue("Invalid 'clear' command.\n\nValid commands:\nbamm clear userScripts\nbamm clear compiled\n\nPress any key to continue...");
    Console.ReadKey();
}

// Handles 'bamm clear compiled' and 'bamm clear userScripts'
else if (pArgs.Length == 2 && pArgs[0].Equals("clear", StringComparison.CurrentCultureIgnoreCase)) {
    if (pArgs[1].Equals("userScripts", StringComparison.CurrentCultureIgnoreCase)) {
        if ((Input.WriteTextAndReturnRawInput("Are you sure you want to delete the 'userScripts' directory? [y/n]:") ?? "n").ToLower().Trim().Equals("y")) {
            DirectoryManager.DeleteDirectory(UserScriptManager.GetUserScriptDirectory());
        }
        else { isRunning = false; }
    }
    else if (pArgs[1].Equals("compiled", StringComparison.CurrentCultureIgnoreCase)) {
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

// Handles cases where no filename is provided to bamm run
else if (pArgs.Length == 1 && pArgs[0].Equals("run", StringComparison.CurrentCultureIgnoreCase)) {
    Errors.WriteErrorAndExit("Invalid command: 'bamm run'\n\nPlease provide the path to a python script you wish to run.\n\nValid Syntax:\n'bamm run \"path/to/a/python/file.py\"", 1);
}

// Handles bamm run "filename.py" -> ensures the file passed exists.
else if (pArgs.Length == 2 && pArgs[0].Equals("run", StringComparison.CurrentCultureIgnoreCase) && File.Exists(pArgs[1])) {
    Errors.WriteErrorAndExit("Invalid command: 'bamm run'\n\nPlease provide the path to a python script you wish to run.\n\nValid Syntax:\n'bamm run \"path/to/a/python/file.py\"", 1);
}

// Handles bamm uninstall
else if (pArgs.Length == 1 && pArgs[0].Equals("uninstall", StringComparison.CurrentCultureIgnoreCase)) { new UninstallationManager().Uninstall(); }



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

        case Parser.MenuOption.Run:
            RuntimeManager runtimeManager = new(parserResult.Value);
            runtimeManager.RunScript();
            break;

        case Parser.MenuOption.Help:
            Help.GetDescriptionOfCommand("bamm help --all");
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