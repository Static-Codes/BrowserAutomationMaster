using BrowserAutomationMaster.Core.Types;
using UserAgents;

namespace BrowserAutomationMaster.Core.Helpers;
public static class UserAgentHelper 
{
    // Shared selector instantiated once to prevent parsing the internal JSON repeatedly
    private static readonly UserAgentSelector Selector = new();
    
    // Cache the total count so we don't recalculate it every time GetUserAgent is called
    private static int? _totalCount;
    public static int TotalUserAgentsCount => _totalCount ??= Selector.GetAllMatching(new UserAgentFilter()).Count();

    public static UserAgent[] GetChromeDesktopUserAgents() 
    {
        var filter = new UserAgentFilter { UserAgentPattern = @"Chrome/" };
        
        return [.. Selector.GetAllMatching(filter)
            .Where(ua => ua.UserAgentString.Contains("Windows NT") || 
                            ua.UserAgentString.Contains("Macintosh") || 
                            ua.UserAgentString.Contains("X11"))
            // Ensure we don't accidentally capture Edge, Opera, or Android Chrome
            .Where(ua => !ua.UserAgentString.Contains("Mobile") && 
                            !ua.UserAgentString.Contains("Android") && 
                            !ua.UserAgentString.Contains("Edg") && 
                            !ua.UserAgentString.Contains("OPR"))
            .Select(ua => new UserAgent("chrome", ua.UserAgentString, false))];
    }

    public static UserAgent[] GetFirefoxDesktopUserAgents() 
    {
        var filter = new UserAgentFilter { UserAgentPattern = @"Firefox/" };
        
        return [.. Selector.GetAllMatching(filter)
            .Where(ua => ua.UserAgentString.Contains("Windows NT") || 
                            ua.UserAgentString.Contains("Macintosh") || 
                            ua.UserAgentString.Contains("X11"))
            .Where(ua => !ua.UserAgentString.Contains("Mobile") && 
                            !ua.UserAgentString.Contains("Android"))
            .Select(ua => new UserAgent("firefox", ua.UserAgentString, false))];
    }

    public static UserAgent[] GetSafariDesktopUserAgents() 
    {
        var filter = new UserAgentFilter { UserAgentPattern = @"Safari/" };
        
        return [.. Selector.GetAllMatching(filter)
            .Where(ua => ua.UserAgentString.Contains("Macintosh") && 
                            ua.UserAgentString.Contains("Version/"))
            // Chrome's UA includes "Safari", so we must explicitly exclude Chrome
            .Where(ua => !ua.UserAgentString.Contains("Chrome"))
            .Select(ua => new UserAgent("safari", ua.UserAgentString, false))];
    }

    public static UserAgent[] GetSafariMobileUserAgents() 
    {
        var filter = new UserAgentFilter { UserAgentPattern = @"Safari/" };
        
        return [.. Selector.GetAllMatching(filter)
            .Where(ua => (ua.UserAgentString.Contains("iPhone") || 
                            ua.UserAgentString.Contains("iPad")) && 
                            ua.UserAgentString.Contains("Version/"))
            .Select(ua => new UserAgent("safari", ua.UserAgentString, true))];
    }
}