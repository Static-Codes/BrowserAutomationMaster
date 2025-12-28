using static Tests.Tests;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;


// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);


var data = new Dictionary<int, (object, object)>() {
    { 1, ( "A", "A" ) },
    { 2, ( "B", "B" ) },
    { 3, ( "C", "A" ) },
    { 4, ( "A", "A" ) },
};


var tests = CreateTests(data);
foreach (var test in tests) {
    RunTest(test.Key, test.Value);
}