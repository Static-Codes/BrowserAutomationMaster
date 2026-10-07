using System;
using System.Threading;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Types;
using static BrowserAutomationMaster.Core.Helpers.UserAgentHelper;

namespace BrowserAutomationMaster.Core.Utilities;
public class UserAgentUtility
{
    private static string lastBrowserName = string.Empty; 
    private static bool lastMobileStatus = false;
    private static UserAgent[] UserAgentChoices = [];

    public static UserAgent? GetUserAgent(string browserName, bool isMobile)
    {
        // Only reassigning UserAgentChoices if a new browserName or mobileStatus is passed.
        if (lastBrowserName != browserName || lastMobileStatus != isMobile) 
        {
            Console.WriteLine();
            Warning.Write($"Updating user agents, please wait..");
            Thread.Sleep(100);

            UserAgentChoices = (browserName, isMobile) switch {
                ("chrome", false) => GetChromeDesktopUserAgents(),
                ("firefox", false) => GetFirefoxDesktopUserAgents(),
                ("safari", false) => GetSafariDesktopUserAgents(),
                ("safari", true) => GetSafariMobileUserAgents(),
                _ => throw new ArgumentException(
                    $"Invalid data passed to GetUserAgent -> GetUserAgent(browserName: {browserName}, isMobile: {isMobile})"
                )
            };

            Success.WriteSuccessMessage
            (
                string.Join("", [
                    "Operation successful, the current session will choose at random between ",
                    UserAgentChoices.Length,
                    "/",
                    TotalUserAgentsCount, 
                    " supported user agents."
                ])
            );

            // Updating state vars
            lastBrowserName = browserName;
            lastMobileStatus = isMobile;
        }

        if (UserAgentChoices.Length == 0) {
            Errors.Write("UserAgentChoices has a length of 0.");
            return null;
        }

        return UserAgentChoices[
            Random.Shared.Next(UserAgentChoices.Length)
        ];
    }
}