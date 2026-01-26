using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

// var stream = EmbeddedResourceManager.GetEmbeddedResource("browserstack.json", "BrowserAutomationMaster.AppData.browserstack.json");
// var stream = EmbeddedResourceManager.GetEmbeddedResource("browserstack.json", "BrowserAutomationMaster.AppData.colors.json");
// var stream = EmbeddedResourceManager.GetEmbeddedResource("browserstack.json", "BrowserAutomationMaster.AppData.packages.json");
// var stream = EmbeddedResourceManager.GetEmbeddedResource("browserstack.json", "BrowserAutomationMaster.AppData.useragents.json");

// Console.WriteLine(stream.Length);

Console.WriteLine(DistroManager.DetermineDistroFromID().Name);

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