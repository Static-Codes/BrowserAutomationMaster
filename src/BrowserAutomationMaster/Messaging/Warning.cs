using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Messaging
{
    public class Warning
    {

        public static void Write(string message, bool noNewLines = false)
        {
            if (noNewLines) {
                WriteMessageNoNewLines(message, isWarning: true);
                return;
            }

            WriteMessage(message, isWarning: true);
        }
    }
}
