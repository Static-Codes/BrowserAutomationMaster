using BrowserAutomationMaster;


string[] programArgs = args;
SysCheck _ = new(programArgs); // Runs a system compatibility check.
Console.Title = "BrowserAutomationMaster Manager (BAMM!)";

KeyValuePair<Parser.MenuOption, string> parserResult = Parser.New(); // value is
switch (parserResult.Key)
{
    case Parser.MenuOption.Compile:
        Transpiler.New(parserResult.Value, programArgs);
        break;

    case Parser.MenuOption.Help:
        break;

    case Parser.MenuOption.Invalid:
        break;
}


//ParsedSelector parsedSelector = SelectorParser.Parse("#container"); // Parse css/xpath selector
//Console.WriteLine(parsedSelector);


//string path = Path.Combine(AppContext.BaseDirectory, "userScripts", "with-features.BAMC");
//Transpiler.New(path);
//Console.ReadKey();


//Console.WriteLine(UserAgentManager.GetUserAgent("firefox")); // Get User Agent Example
Console.ReadKey();