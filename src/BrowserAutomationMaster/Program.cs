using BrowserAutomationMaster;

string[] pArgs = args.Length > 0 ? args : []; // By default args doesn't include the executable.

SysCheck _ = new(pArgs); // Runs a system compatibility check.
Console.Title = "BrowserAutomationMaster Manager (BAMM!)";

bool isCLI = false;
if (pArgs.Length == 2) { isCLI = true; }


if (isCLI) {
    UserScriptManager usm = new(pArgs[1], pArgs[0]);
}

KeyValuePair<Parser.MenuOption, string> parserResult = Parser.New(); // value is the filepath of the selected file.
switch (parserResult.Key)
{
    case Parser.MenuOption.Compile:
        Transpiler.New(parserResult.Value, args);
        break;

    case Parser.MenuOption.Help:
        break;

    case Parser.MenuOption.Invalid:
        break;
}


Console.WriteLine("Press any key to exit..");
Console.ReadKey();