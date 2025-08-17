using BrowserAutomationMaster.Messaging;
using System.Text.Json.Serialization;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    internal class DeviceManager
    {
        public static DeviceTypes? Devices { get; set; } = null;
        // Commented out versions don't support the latest version of a specific browser.
        public static string[] WindowsVersions = ["10", "11"]; //["XP", "7", "8", "8.1", "10", "11"];
        public static string[] MacOSVersions =
        [
            
            //"10.6 (Snow Leopard)",
            //"10.7 (Lion)",
            //"10.8 (Mountain Lion)",
            //"10.9 (Mavericks)",
            //"10.10 (Yosemite)",
            //"10.11 (El Capitan)",
            //"10.12 (Sierra)",
            //"10.13 (High Sierra)",
            //"10.14 (Mojave)",
            //"10.15 (Catalina)",
            //"11 (Big Sur)",
            "12 (Monterey)",
            "13 (Ventura)",
            "14 (Sonoma)",
            "15 (Sequoia)",
            "26 (Tahoe)"
        ];

        public static string[] iOSVersions = ["11", "12", "13", "14", "15", "16", "17", "18", "26 Beta"];
        public static string[] AndroidVersions =
        [
            "8 (Oreo)",
            "9 (Pie)",
            "10 (Quince Tart)",
            "11 (Red Velvet Cake)",
            "12 (Snow Cone)",
            "13 (Tiramisu)",
            "14 (Upside Down Cake)",
            "15 (Vanilla Ice Cream)",
            "16 (Baklava)"
        ];


        public static string SanitizeOSName(string rawOSName)
        {
            return rawOSName switch
            {
                "Android" => "android",
                "iOS" => "ios",
                "MacOS" => "OS X",
                "Windows" => "Windows",
                _ => "Windows" // Uses windows as default.
            };
        }
        public static string SanitizeOSVersion(string rawOSVersion, string rawOSName, string[] versions)
        {
            var OSVersion = GetVersionNumber(rawOSVersion);

            if (OSVersion == "Not Found")
            {
                Warning.Write($"No version number was provided, using the most recent version of {rawOSName}.");
                return versions[^1]; // Last element (Most recent version)
            }
            return OSVersion;
        }

        public static string GetDesiredBrowser(string rawOSName)
        {
            static string GetBrowser(string[] browsers)
            {
                return Input.WriteListFromOptions(browsers, noun: "browser");
            }
            return rawOSName switch
            {
                "Android" => "Chrome",
                "iOS" => GetBrowser(["Chromium", "Safari"]),
                "MacOS" or "Windows" or _ => GetBrowser(["Chrome", "Firefox"]),
            };
        }

        public static string[] GetVersionsOfOS(string rawOSName)
        {
            return rawOSName switch
            {
                "Android" => AndroidVersions,
                "iOS" => iOSVersions,
                "MacOS" => MacOSVersions,
                "Windows" => WindowsVersions,
                _ => WindowsVersions  // Uses windows as default.
            };
        }

        public static string GetVersionNumber(string versionString)
        {
            var chars = versionString.AsSpan();
            int index = -1;

            // Gets the index of the first non-numeric char
            for (int i = 0; i < chars.Length; i++) 
            {
                if (!char.IsNumber(chars[i]) && chars[i] != '.')
                {
                    index = i;
                    break;
                }
            }
            // Returns the raw version number or "Not Found"
            return index != -1 ? chars[..index].ToString() : "Not Found";
        }

        public bool IsValidDevice(BrowserStackConfig config)
        {
            return true;
        }

        public void PopulateDevices()
        {
            RequestManager.NetworkClient.Instance.GetStringAsync(BROWSER_STACK_LINK);
        }

    }

    public class DeviceTypes
    {
        [JsonPropertyName("desktop")]
        public required List<Desktop> Desktop { get; set; }

        [JsonPropertyName("mobile")]
        public required List<Mobile> Mobile { get; set; }
    }
    public class Desktop
    {
        [JsonPropertyName("os")]
        public required string OS { get; set; }

        [JsonPropertyName("os_version")]
        public string OSVersion { get; set; } = "latest";

        [JsonPropertyName("os_display_name")]
        public string OSDisplayName { get; set; } = ""; // Not used in config

        public bool RealMobile { get; init; } = false;

        [JsonPropertyName("browsers")]
        public required List<Browser> Browsers { get; set; }
    }
    public class Mobile
    {
        [JsonPropertyName("device")]
        public required string Device { get; set; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = ""; // Not used in config

        [JsonPropertyName("os_version")]
        public required string OSVersion { get; set; }

        [JsonPropertyName("real_mobile")]
        public required bool RealMobile { get; set; }

        [JsonPropertyName("browser")]
        public required string Browser { get; set; }

        [JsonPropertyName("browsers")]
        public required List<Browser> Browsers { get; set; }
    }
    public class Browser
    {
        [JsonPropertyName("browser")]
        public required string BrowserName { get; set; }

        [JsonPropertyName("browser_version")]
        public string BrowserVersion { get; set; } = "latest";

        // This isn't needed but i've had issues without it.
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; } = ""; 
    }



}
