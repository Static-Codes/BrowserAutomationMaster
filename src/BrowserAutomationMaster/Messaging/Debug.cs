using System.Diagnostics;
using BrowserAutomationMaster.Managers;

namespace BrowserAutomationMaster.Messaging
{
    public static class Debug
    {

        public static string GetPlatformInfoForErrorLog()
        {
            // Make this a part of the Debug class and implement bamm info
            return @$"---------------- PLATFORM DEBUG INFO ----------------
                OS Version: {Environment.OSVersion}
                Platform: {Environment.OSVersion.Platform}
                Current Dir: {Environment.CurrentDirectory}
                Installation Dir: {AppContext.BaseDirectory}
                UserScripts Dir: {UserScriptManager.GetUserScriptDirectory()}".Replace("                ", "");
        }
        public static void WriteTestMessage(string message) {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
        public static void DisplayNumberOfCodeLinesInProject()
        {
            string cmd = @"(Get-ChildItem -Path ""C:\Users\Nerdy\Documents\GitHub\BrowserAutomationMaster\BrowserAutomationMaster\src"" -Include *.cs -Recurse | Where-Object { $_.FullName -notmatch '\\(bin|obj|Properties|My Project|Designer\.cs|g\.cs|AssemblyInfo\.cs|TemporaryGeneratedFile_.*\.cs|Resources\.Designer\.cs|Settings\.Designer\.cs)\\' } | Get-Content | Measure-Object -Line | Select-Object -ExpandProperty Lines)";
            ProcessStartInfo processStartInfo = new()
            {
                FileName = "powershell.exe",
                Arguments = $"/c {cmd}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using Process process = new() { StartInfo = processStartInfo };
            process.Start();
            process.WaitForExit();
            string output = process.StandardOutput.ReadToEnd();
            if (string.IsNullOrEmpty(output)) { Errors.WriteErrorAndExit("Unable to query the total number of non whitespace code lines in the current project.", 1); }
            Success.WriteSuccessMessageAndExit($"Found {output.Replace("\n", " ").Trim()} lines of valid c# code in the current project.", 0);

        }
        
    }
}
