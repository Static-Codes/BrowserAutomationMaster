// See https://aka.ms/new-console-template for more information
using BrowserAutomationMaster;

Console.Title = "BrowserAutomationMaster Manager (BAMM!)";
//Parser.New();


//Transpiler.New("C:\\Users\\Nerdy\\Documents\\GitHub\\BrowserAutomationMaster\\BrowserAutomationMaster\\src\\BrowserAutomationMaster\\bin\\Release\\net8.0\\win-x86\\publish\\config\\with-features.BAMC");
//Console.ReadKey();

PackageManager.New("selenium", 3.10);
Console.ReadKey();