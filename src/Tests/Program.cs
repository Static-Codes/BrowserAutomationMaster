using static Tests.Runner;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using BrowserAutomationMaster.Managers;


// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

// var extensionManager = new ExtensionManager("file:///home/nerdy/Desktop/xpis/languagetool.xpi", "firefox");
var extensionManager = new ExtensionManager("https://chromewebstore.google.com/detail/volume-booster-sound-bass/ebpckmjdefimgaenaebngljijofojncm", "chrome");
// var extensionManager = new ExtensionManager("https://addons.mozilla.org/en-US/firefox/addon/youtube-screenshot-button/", "firefox");
// var extensionManager = new ExtensionManager("https://addons.mozilla.org/en-US/firefox/addon/languagetool/", "firefox");
await extensionManager.GetExtensionContents();
Environment.Exit(0);

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