using static BrowserAutomationMaster.Managers.Utilities.AppUpdateUtility;
using static BrowserAutomationMaster.ProgramFunctions;

Console.Title = $"BrowserAutomationMaster Manager (BAMM!) {CurrentVersion}";

await InitializeAsync(args);
bool shouldExit = await HandleCLIArguments(args);

if (!shouldExit)
{
    await RunMenuLoop(args);
}