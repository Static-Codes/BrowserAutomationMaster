using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Parsing;
using System.Diagnostics;
using System.Runtime.Versioning;

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
                UserScripts Dir: {Parser.userScriptsDirectory}".Replace("                ", "");
        }
        
    }
}
