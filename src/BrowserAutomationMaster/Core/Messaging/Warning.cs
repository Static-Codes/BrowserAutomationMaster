using static BrowserAutomationMaster.Core.Common.ANSI;

namespace BrowserAutomationMaster.Core.Messaging
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
