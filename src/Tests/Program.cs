using System.Text;
using BrowserAutomationMaster.Core;
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.OS.Unix.Linux;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);


var actions = Commands.GetCommandNamesByType(CommandType.Action);
var features = Commands.GetCommandNamesByType(CommandType.Feature);

static void addLines(StringBuilder stringBuilder, string[] contents) {
    foreach (var content in contents) { 
        stringBuilder.AppendLine(content); 
    }
}

var stringBuilder = new StringBuilder();

stringBuilder.AppendLine("---------------- Actions ----------------");
addLines(stringBuilder, actions);
stringBuilder.AppendLine(Environment.NewLine);

stringBuilder.AppendLine("---------------- Features ----------------");
addLines(stringBuilder, features);

var chars = new char[stringBuilder.Length];
stringBuilder.CopyTo(0, chars, 0, chars.Length);

var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

await File.WriteAllBytesAsync(    
    "CommandList.txt",
    Encoding.UTF8.GetBytes(chars),
    cts.Token
);


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