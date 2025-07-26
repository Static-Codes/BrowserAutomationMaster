using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Messaging
{
    public class Warning
    {

        public static void Write(string message)
        {
            WriteMessage(message, isWarning: true);
        }
    }
}
