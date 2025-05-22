using BrowserAutomationMaster;

bool isRunning = true;
string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.

SysCheck _ = new(pArgs); // Runs a system compatibility check.
Console.Title = "BrowserAutomationMaster Manager (BAMM!)"; // Dont waste memory if the system isn't compatible.


bool isCLI = false;
if (pArgs.Length == 2) { isCLI = true; }

List<string> validCLIArgs = ["add", "compile", "delete"];

// Handles direct CLI cases
// -> bamm add "file.bamc"
// -> bamm compile "file.bamc" (if userScript directory contains file.bamc)
// -> bamm delete "file.bamc"
if (isCLI) {
    if (validCLIArgs.Contains(pArgs[0])) { var __ = new UserScriptManager(pArgs[1], pArgs[0]); }
}


// Handles cases where file is double clicked. (Functions the same as bamm add "file.bamc")
// The file is added to userScripts directory.
// (Logic to compile automatically is commented out as to not be intrusive, opting for user confirmation.)
if (pArgs.Length == 1 && pArgs[0].ToLower().EndsWith(".bamc") && File.Exists(pArgs[0])) { 
    var __ = new UserScriptManager(pArgs[0], "add");
    bool wantsToContinue = (Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ") ?? "n").ToLower().Trim().Equals("y");
    if (!wantsToContinue) { isRunning = false; }
    //Transpiler.New(pArgs[0], pArgs);
    //isRunning = false;
}



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