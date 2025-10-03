using BrowserAutomationMaster.Managers;
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
                UserScripts Dir: {DirectoryManager.GetUserScriptDirectory()}".Replace("                ", "");
        }
        
    }
}
