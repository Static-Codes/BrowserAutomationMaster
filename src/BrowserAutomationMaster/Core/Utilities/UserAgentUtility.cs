using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Types;
using static BrowserAutomationMaster.Core.Helpers.UserAgentHelper;

namespace BrowserAutomationMaster.Core.Utilities
{
    public class UserAgentUtility
    {
        private static string lastBrowserName = string.Empty; 
        private static bool lastMobileStatus = false;
        private static UserAgent[] UserAgentChoices = [];
        public readonly static List<UserAgent> FullList = 
        [
            // -------------------------------
            // Chrome on Win10/11 (x64)
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/133.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/127.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on Win10/Win11 (ARM64)
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on Intel Macs (x64)
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 12_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 13_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/139.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 14_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on ChromeOS (x64)
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (X11; CrOS x86_64 15699.0.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/138.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on Generic Linux
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on Fedora Based Linux
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (X11; Fedora; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/140.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Chrome on Ubuntu Based Linux
            // -------------------------------
            AddChromeUserAgent("Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/132.0.0.0 Safari/537.36", isMobileDevice: false),
            AddChromeUserAgent("Mozilla/5.0 (X11; Ubuntu; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36", isMobileDevice: false),


            // -------------------------------
            // Firefox User Agents (Win10/Win11 x64)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:147.0) Gecko/20100101 Firefox/147.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:146.0) Gecko/20100101 Firefox/146.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:145.0) Gecko/20100101 Firefox/145.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:144.0) Gecko/20100101 Firefox/144.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:142.0) Gecko/20100101 Firefox/142.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:140.0) Gecko/20100101 Firefox/140.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:133.0) Gecko/20100101 Firefox/133.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:130.0) Gecko/20100101 Firefox/130.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:128.0) Gecko/20100101 Firefox/128.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:115.0) Gecko/20100101 Firefox/115.0", isMobileDevice: false),


            // -------------------------------
            // Firefox User Agents (Win10/Win11 ARM64)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (Windows NT 10.0; WOW64; rv:139.0) Gecko/20100101 Firefox/139.0", isMobileDevice: false),


            // -------------------------------
            // Firefox User Agents (Intel Mac)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:147.0) Gecko/20100101 Firefox/147.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 14.7; rv:145.0) Gecko/20100101 Firefox/145.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:143.0) Gecko/20100101 Firefox/143.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 13.6; rv:141.0) Gecko/20100101 Firefox/141.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:138.0) Gecko/20100101 Firefox/138.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 12.7; rv:135.0) Gecko/20100101 Firefox/135.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:132.0) Gecko/20100101 Firefox/132.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:129.0) Gecko/20100101 Firefox/129.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:128.0) Gecko/20100101 Firefox/128.0", isMobileDevice: false),


            // -------------------------------
            // Firefox on Generic Linux (x64)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Linux x86_64; rv:147.0) Gecko/20100101 Firefox/147.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Linux x86_64; rv:143.0) Gecko/20100101 Firefox/143.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Linux x86_64; rv:128.0) Gecko/20100101 Firefox/128.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Linux x86_64; rv:137.0) Gecko/20100101 Firefox/137.0", isMobileDevice: false),
            

            // -------------------------------
            // Firefox on Fedora Based Linux (x64)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:144.0) Gecko/20100101 Firefox/144.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Fedora; Linux x86_64; rv:134.0) Gecko/20100101 Firefox/134.0", isMobileDevice: false),


            // -------------------------------
            // Firefox on Ubuntu Based Linux (x64)
            // -------------------------------
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:146.0) Gecko/20100101 Firefox/146.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0", isMobileDevice: false),
            AddFirefoxUserAgent("Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:131.0) Gecko/20100101 Firefox/131.0", isMobileDevice: false),


            // -------------------------------
            // Safari User Agents (iPhone)
            // -------------------------------
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 19_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/19.3 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 19_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/19.2 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 19_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/19.0 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.2 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_1 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.1 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 17_7 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            
            // -------------------------------
            // Safari User Agents (iPad)
            // -------------------------------
            AddSafariUserAgent("Mozilla/5.0 (iPad; CPU OS 19_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/19.3 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPad; CPU OS 18_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.6 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPad; CPU OS 18_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.3 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPad; CPU OS 17_6 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Mobile/15E148 Safari/604.1", isMobileDevice: true),
            AddSafariUserAgent("Mozilla/5.0 (iPad; CPU OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1", isMobileDevice: true),

            // -------------------------------
            // Safari User Agents (Intel Macs)
            // -------------------------------
            AddSafariUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 14_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Safari/605.1.15", isMobileDevice: false),
            AddSafariUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.5 Safari/605.1.15", isMobileDevice: false),
            AddSafariUserAgent("Mozilla/5.0 (Macintosh; Intel Mac OS X 13_6) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.6 Safari/605.1.15", isMobileDevice: false),

        ];
    
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
                        FullList.Count, 
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
                Random.Shared.Next(0, UserAgentChoices.Length - 1)
            ];
        }
        
        private static UserAgent AddChromeUserAgent(string userAgentString, bool isMobileDevice) {
            return GenerateUserAgent("chrome", userAgentString, isMobileDevice);
        }

        private static UserAgent AddFirefoxUserAgent(string userAgentString, bool isMobileDevice) {
            return GenerateUserAgent("firefox", userAgentString, isMobileDevice);
        }

        private static UserAgent AddSafariUserAgent(string userAgentString, bool isMobileDevice) {
            return GenerateUserAgent("safari", userAgentString, isMobileDevice);
        }

        private static UserAgent GenerateUserAgent(string browserName, string userAgentString, bool isMobileDevice) {
            return new UserAgent(browserName, userAgentString, isMobileDevice);
        }
    }
}
