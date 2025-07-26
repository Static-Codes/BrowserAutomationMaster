using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Messaging
{
    public class Success
    {
        public static void WriteSuccessMessage(string message) {
            WriteMessage(message, isSuccess: true);
        }
        public static void WriteSuccessMessageAndExit(string message, int exitCode) 
        {
            WriteMessage(message, isSuccess: true);
            Environment.Exit(exitCode);
        }
    }
}
