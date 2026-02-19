using BrowserAutomationMaster.Managers.Types;
using static BrowserAutomationMaster.Managers.Utilities.UserAgentUtility;

namespace BrowserAutomationMaster.Managers.Helpers 
{
    public static class UserAgentHelper 
    {
        public static UserAgent[] GetChromeDesktopUserAgents() 
        {
            return [.. FullList
                   .Where(userAgent => userAgent.browserName.Equals("chrome"))
                   .Where(userAgent => !userAgent.isMobileDevice)];
        }

        public static UserAgent[] GetFirefoxDesktopUserAgents() 
        {
            return [.. FullList
                   .Where(userAgent => userAgent.browserName.Equals("firefox"))
                   .Where(userAgent => !userAgent.isMobileDevice)];
        }
        public static UserAgent[] GetSafariDesktopUserAgents() 
        {
            return [.. FullList
                   .Where(userAgent => userAgent.browserName.Equals("safari"))
                   .Where(userAgent => !userAgent.isMobileDevice)];
        }

        public static UserAgent[] GetSafariMobileUserAgents() 
        {
            return [.. FullList
                   .Where(userAgent => userAgent.browserName.Equals("safari"))
                   .Where(userAgent => userAgent.isMobileDevice)];
        }
    }
}