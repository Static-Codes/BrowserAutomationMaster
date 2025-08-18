
using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Managers.Python.BrowserStack;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.ProcessManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.BrowserVersionManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Parsing.Parser;



// Populate DeviceManager.Devices
var isPopulated = await PopulateDevices();
if (!isPopulated)
    Environment.Exit(0);

// Populates BrowserVersionManager.browserVersions
SetBrowserVersions(await GetLatestVersionInfo());
var versions = GetBrowserVersion();

// Null check on BrowserVersionManager.browserVersions
if (versions == null)
{
    Warning.Write(
        "Unable to get most browser versions, please ensure you have an active internet connection.\n" +
        $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n"
    );
}

// Working Example of BrowserStack Config Generation.
//InstanceManager.WriteConfig(
//    userName: "test", 
//    accessKey: "test", 
//    projectName: "08-17-2025_5-30PM", 
//    scriptName: "scriptName"
//);
//Environment.Exit(0);

Linux.ChromeOSCheck();
CheckForMultipleInstances();
GlobalConfig = LoadConfig();

string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.

if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240)) { 
    Win.VerifyRootDrive(pArgs); 
}


List<string> validCLIArgs = ["add", "clear", "compile", "delete", "help", "run", "validate"];

// If pArgs contains any args from nonUserScriptArgs, Compatibility checks are skipped because the user is not attempting to compile or run any scripts.
// These commands are handled within the program loop instead of in UserScriptManager.
List<string> nonUserScriptArgs = ["clear", "help", "uninstall", "validate"];

Console.Title = $"BrowserAutomationMaster Manager (BAMM!) {CurrentVersion}";

bool isRunning = true;
bool isCLI = false;



if (!pArgs.Any(arg => nonUserScriptArgs.Contains(arg)))
{
    if (!pArgs.Contains("--vlinux-bypass")) // Crude solution because its 10:38PM and i want to test before sleeping.
    { 
        RuntimeManager.DoRuntimeCheck();  // Set expectations regarding automation performance given the user's specs.
        if (GlobalConfig.ShowUpdateCheck) {
            await CheckForUpdate(); // New releases are fun - Ghandi probably.
        }
    }
}

// Set CLI True if a validCLIArg is passed.
if (pArgs.Length == 2 && !nonUserScriptArgs.Contains(pArgs[0].ToLower())) { 
    isCLI = true; 
}


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
    string input = Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ");
    bool wantsToContinue = input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
    if (!wantsToContinue) { isRunning = false; }
}

// Handles bare 'bamm clear' command
else if (pArgs.Length == 1 && pArgs[0].Equals("clear", CCIC)) {
    Errors.WriteErrorAndContinue(
        "Invalid 'clear' command.\n\n" +
        "Valid commands:\n" +
        "bamm clear userScripts\n" +
        "bamm clear compiled\n\n" +
        "Press any key to continue..."
    );
    ReadKey();
}

// Handles 'bamm clear compiled' and 'bamm clear userScripts'
else if (pArgs.Length == 2 && pArgs[0].Equals("clear", CCIC)) {
    if (pArgs[1].Equals("userScripts", CCIC)) {

        string deleteInput = Input.WriteTextAndReturnRawInput(
            "Are you sure you want to delete the 'userScripts' directory? [y/n]:\n"
        );

        if (deleteInput.Equals("y"))
            DeleteDirectory(GetUserScriptDirectory());
        else
            isRunning = false;

    }
    else if (pArgs[1].Equals("compiled", CCIC)) {
        string input = Input.WriteTextAndReturnRawInput(
            "Are you sure you want to delete the 'compiled' directory? [y/n]:\n"
        );

        if (input.Equals("y"))
            DeleteDirectory(GetDesiredSaveDirectory());
        else
            isRunning = false;
    }
    else if (pArgs[1].Equals("config", CCIC))
    {
        string input = Input.WriteTextAndReturnRawInput(
            "Are you sure you want to delete the 'config' directory? [y/n]:\n"
        );

        if (input.Equals("y"))
            DeleteDirectory(GetConfigDirectory());
        else
            isRunning = false;
    }
    else {
        Errors.WriteErrorAndContinue(
            "Invalid 'clear' command.\n\n" +
            "Valid commands:\n" +
            "bamm clear compiled\n\n" +
            "bamm clear config\n\n" +
            "bamm clear userScripts\n" +
            "Press any key to continue..."
        );
        ReadKey();
    }

}

// Handles cases where only bare "bamm help" command is supplied
else if (pArgs.Length == 1 && pArgs[0].Equals("help", CCIC)) {
    Errors.WriteErrorAndContinue(
        "Invalid command: 'bamm help'\n\n" +
        "To see available entries for the 'help' command," +
        "run bamm without arguments then select the Help tab.\n\n"
    );
    ReadKey();
}

// Handles bamm help "command-name"
else if (pArgs.Length == 2 && pArgs[0].Equals("help", CCIC)) { 
    Help.ShowCommandDetails(pArgs[1]); 
}

// Handles cases where no filename is provided to bamm run
else if (pArgs.Length == 1 && pArgs[0].Equals("run", CCIC)) {
    Errors.WriteErrorAndExit(
        message:
            "Invalid command: 'bamm run'\n\n" +
            "Please provide the path to a python script you wish to run.\n\n" +
            "Valid Syntax:\n'" +
            "bamm run \"path/to/a/python/file.py\"", 
        status: 1
    );
}

// Handles bamm run "filename.py" -> ensures the file passed exists.
else if (pArgs.Length == 2 && pArgs[0].Equals("run", CCIC) && File.Exists(pArgs[1])) {
    Errors.WriteErrorAndExit(
        message:
            "Invalid command: 'bamm run'\n\n" +
            "Please provide the path to a python script you wish to run.\n\n" +
            "Valid Syntax:\n" +
            "'bamm run \"path/to/a/python/file.py\"", 
        status: 1
    );
}

// Handles bamm uninstall
else if (pArgs.Length == 1 && pArgs[0].Equals("uninstall", CCIC)) { 
    new UninstallationManager().Uninstall(); 
}

// Handles bamm validate
else if (pArgs.Length == 1 && pArgs[0].Equals("validate", CCIC))
{
    Errors.WriteErrorAndExit(
        "Invalid 'validate' command.\n\n" +
        "Valid Syntax:\n" +
        "bamm validate \"path/to/file.bamc\"\n",
        status: 1
    );
}

// Handles bamm 
else if (pArgs.Length == 2 && pArgs[0].Equals("validate", CCIC))
{
    if (IsValidFile(pArgs[1]))
    {
        Success.WriteSuccessMessageAndExit("Selected file has valid syntax.", 0);
    }
    Errors.WriteErrorAndExit("Select file has invalid syntax.", 1);
}


while (isRunning)
{
    KeyValuePair<MenuOption, string> parserResult = Parser.New(); // The value of this KeyValuePair is the filepath of the selected file.
    switch (parserResult.Key)
    {
        case MenuOption.Add:
            string compileInput = Input.WriteTextAndReturnRawInput("Would you like to compile the newly added file? [y/n]:");
            bool overwriteConfirmation = compileInput.Trim().Equals("y", StringComparison.OrdinalIgnoreCase);
            if (overwriteConfirmation)
            {
                Transpiler.New(parserResult.Value, args);
            }
            break;

        case MenuOption.Compile:
            Transpiler.New(parserResult.Value, args);
            break;

        case MenuOption.Run:
            RuntimeManager runtimeManager = new(parserResult.Value);
            await runtimeManager.RunScript();
            break;

        case MenuOption.Help:
            break;

        case MenuOption.Invalid:
            isRunning = false;
            break;
    }
    string input = Input.WriteTextAndReturnRawInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:") ?? "n";
    bool exitConfirmation = input.Trim().Equals("y");
    if (exitConfirmation) { isRunning = false; }
}

Spectre.Console.AnsiConsole.Write("\nPress any key to exit...");
ReadKey();