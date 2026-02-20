using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Helpers;
using BrowserAutomationMaster.Managers.OS.Unix.Linux;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

FileDialogHelper.RunTest();
// var data = new Dictionary<int, (object, object)>() {
//     { 1, ( "A", "A" ) },
//     { 2, ( "B", "B" ) },
//     { 3, ( "C", "A" ) },
//     { 4, ( "A", "A" ) },
// };


// var tests = CreateTests(data);
// foreach (var test in tests) {
//     RunTest(test.Key, test.Value);
// }