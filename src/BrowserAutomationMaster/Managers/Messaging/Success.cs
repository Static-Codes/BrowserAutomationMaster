using static BrowserAutomationMaster.Managers.Common.ANSI;

namespace BrowserAutomationMaster.Managers.Messaging
{
    public class Success
    {
        public static void WriteSuccessMessage(string message, bool noNewLines = false) 
        {
            if (noNewLines) 
            {
                WriteMessageNoNewLines(message, isSuccess: true);
                return;
            }

            WriteMessage(message, isSuccess: true);
        }
        public static void WriteSuccessMessageAndExit(string message, int exitCode) 
        {
            WriteMessage(message, isSuccess: true);
            Environment.Exit(exitCode);
        }
    }
}
