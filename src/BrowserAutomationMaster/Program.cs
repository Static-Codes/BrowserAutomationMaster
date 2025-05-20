using BrowserAutomationMaster;

bool isRunning = true;
string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.

SysCheck _ = new(pArgs); // Runs a system compatibility check.
Console.Title = "BrowserAutomationMaster Manager (BAMM!)";

bool isCLI = false;
if (pArgs.Length == 2) { isCLI = true; }


if (isCLI) {
    if (pArgs[0].Equals("add")) { new UserScriptManager(pArgs[1], pArgs[0]); }
}

if (pArgs.Length == 1 && pArgs[0].ToLower().EndsWith(".bamc") && File.Exists(pArgs[0])) {
    Transpiler.New(pArgs[0], pArgs);
    isRunning = false;
}



while (isRunning)
{
    KeyValuePair<Parser.MenuOption, string> parserResult = Parser.New(); // value is the filepath of the selected file.
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