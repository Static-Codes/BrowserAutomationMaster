using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.ProgramFunctions;

Console.Title = $"BrowserAutomationMaster Manager (BAMM!) {CurrentVersion}";
await InitializeAsync(args);

foreach (var editor in EditorManager.GetSupportedLinuxEditors())
{
    Console.WriteLine("{0}: {1}", editor.Key, editor.Value);
}

Environment.Exit(1);

bool shouldExit = await HandleCLIArguments(args);

if (!shouldExit)
    await RunMenuLoop(args);