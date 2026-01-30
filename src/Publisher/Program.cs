using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);