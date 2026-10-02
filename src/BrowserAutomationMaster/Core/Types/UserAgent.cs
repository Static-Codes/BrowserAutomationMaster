namespace BrowserAutomationMaster.Core.Types 
{
    public class UserAgent(string browserName, string userAgentString, bool isMobileDevice) 
    {
        public string browserName = browserName;
        public string userAgentString = userAgentString;
        public bool isMobileDevice = isMobileDevice;
    }
}