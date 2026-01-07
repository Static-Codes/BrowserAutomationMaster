using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

var extensionManager = new ExtensionManager("file:///home/nerdy/Desktop/xpis/languagetool.xpi", "firefox");
// var extensionManager = new ExtensionManager("https://chromewebstore.google.com/detail/volume-booster-sound-bass/ebpckmjdefimgaenaebngljijofojncm", "chrome");
// var extensionManager = new ExtensionManager("https://addons.mozilla.org/en-US/firefox/addon/youtube-screenshot-button/", "firefox");
// var extensionManager = new ExtensionManager("https://addons.mozilla.org/en-US/firefox/addon/languagetool/", "firefox");



var contents = await extensionManager.GetExtensionContents();

var outputPath = await extensionManager.WriteExtensionContents(contents);

if (outputPath == null) {
    
}



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