using System.Text;
using BrowserAutomationMaster.Core;
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);
FileDialogHelper.RunTest();

// FileDialogHelper.RunTest();
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