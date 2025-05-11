// See https://aka.ms/new-console-template for more information
using BrowserAutomationMaster;



Console.Title = "BrowserAutomationMaster Manager (BAMM!)";

if (!Environment.Is64BitOperatingSystem) {
    Errors.WriteErrorAndExit("BAM Manager (BAMM) was designed for 64 bit windows operating systems.\n\nIf you're reading this message, your current system is incompatible with this application.\n\nFor more information please visit the link below:\n\nhttps://answers.microsoft.com/en-us/windows/forum/all/what-is-the-advantage-of-going-to-64-biti-have-32/dea5fbb6-4b53-4c66-a0e6-70e76f934d79", 1);
}
//Parser.New();


//ParsedSelector parsedSelector = SelectorParser.Parse("#container");
//Console.WriteLine(parsedSelector);
Transpiler.New("C:\\Users\\Nerdy\\Documents\\GitHub\\BrowserAutomationMaster\\BrowserAutomationMaster\\src\\BrowserAutomationMaster\\bin\\Release\\net8.0\\win-x86\\publish\\userScripts\\with-features.BAMC");
//Console.ReadKey();


//Console.WriteLine(UserAgentManager.GetUserAgent("firefox"));
Console.ReadKey();