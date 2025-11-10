using BrowserAutomationMaster.Messaging;
using System.Text.Json;
using System.Text.Json.Serialization;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    internal class DeviceManager
    {
        public static DeviceTypes? Devices { get; set; } = null;
        // Commented out versions don't support the latest version of a specific browser.
        public static string[] OSNames = ["Android", "iOS", "MacOS", "Windows"];
        public static string[] WindowsVersions = ["11", "10"];
        public static string[] iOSVersions = ["26 Beta", "18", "17", "16", "15", "14", "13", "12", "11"];
        public static string[] MacOSVersions = [
            "26 (Tahoe)",
            "15 (Sequoia)",
            "14 (Sonoma)",
            "13 (Ventura)",
            "12 (Monterey)"
        ];
        public static string[] AndroidVersions =
        [
            //"16 (Baklava)", // Not currently supported as of 08/17/2025
            "15 (Vanilla Ice Cream)",
            "14 (Upside Down Cake)",
            "13 (Tiramisu)",
            "12 (Snow Cone)",
            "11 (Red Velvet Cake)",
            "10 (Quince Tart)",
            "9 (Pie)",
            "8 (Oreo)"
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
            if (rawOSVersion.EndsWith("Beta"))
                return rawOSVersion;

            bool isAndroid = rawOSName == "Android";
            var osVersion = GetVersionNumber(rawOSVersion, isAndroid);

            if (osVersion == "Not Found")
            {
                Warning.Write($"No version number was provided, using the most recent version of {rawOSName}.");
                return versions[^1]; // Last element (Most recent version)
            }
            return osVersion;
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
                "MacOS" => GetBrowser(["Chrome", "Firefox", "Safari"]),
                "Windows" or _ => GetBrowser(["Chrome", "Firefox"]),
            };
        }
        public static string GetVersionNumber(string versionString, bool isAndroid = false)
        {
            var chars = versionString.AsSpan();
            int index = 0;
            
            // Gets the index of the first non-numeric char
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsNumber(chars[i]) && chars[i] != '.')
                {
                    index = i;
                    break;
                }
                index++;
            }

            if (index == 0)
                return "Not Found";

            // Returns the raw version number or "Not Found"
            var version = chars[..index].ToString();

            // Conditionally append ".0" for Android Versioning 
            return isAndroid ? version + ".0" : version;
        }


        public static async Task<bool> PopulateDevices()
        {
            var msg = "Unable to populate supported devices for browserstack integration, any use of browserstack will throw an error.";
            try
            {
                var devicesJSON = await RequestManager.NetworkClient.Instance.GetStringAsync(BROWSER_STACK_LINK);

                if (devicesJSON is null)
                    return WriteErrorAndReturnBool(msg, false);

                Devices = JsonSerializer.Deserialize<DeviceTypes>(devicesJSON);
                return Devices != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return WriteErrorAndReturnBool(msg, false);
            }

        }

        public class DeviceTypes
        {
            [JsonPropertyName("desktop")]
            public required List<Desktop> Desktop { get; set; }

            [JsonPropertyName("mobile")]
            public required List<MobileOS> Mobile { get; set; }
        }

        public class Desktop
        {
            [JsonPropertyName("os")]
            public string? OS { get; set; }

            [JsonPropertyName("os_version")]
#pragma warning disable IDE1006 // Naming Styles
            public string osVersion { get; set; } = "latest";
#pragma warning restore IDE1006 // Naming Styles

            [JsonPropertyName("os_display_name")]
            public string OSDisplayName { get; set; } = ""; // Not used in config

            public bool RealMobile { get; init; } = false;

            [JsonPropertyName("browsers")]
            public List<Browser>? Browsers { get; set; }
        }
        public class MobileOS
        {
            [JsonPropertyName("os")]
            public string? OS { get; set; }

            [JsonPropertyName("os_display_name")]
            public string? OSDisplayName { get; set; }

            [JsonPropertyName("devices")]
            public required List<Mobile> Devices { get; set; }
        }
        public class Mobile
        {
            [JsonPropertyName("device")]
            public string? Device { get; set; }

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = ""; // Not used in config

            [JsonPropertyName("os_version")]
#pragma warning disable IDE1006 // Naming Styles
            public string? osVersion { get; set; }
#pragma warning restore IDE1006 // Naming Styles

            [JsonPropertyName("real_mobile")]
            public bool RealMobile { get; set; }

            [JsonPropertyName("browser")]
            public string? Browser { get; set; }

            [JsonPropertyName("browsers")]
            public List<Browser>? Browsers { get; set; }
        }

        public class Browser
        {
            [JsonPropertyName("browser")]
            public string? BrowserName { get; set; }

            [JsonPropertyName("browser_version")]
            public string BrowserVersion { get; set; } = "latest";

            // This isn't needed but i've had issues without it.
            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; } = "";
        }

        public class DeviceHelper()
        {
            public static string[] GetMobileDeviceNames()
            {
                if (Devices is null || Devices.Mobile is null)
                    return [];

                var devices = Devices?.Mobile?
                    .SelectMany(mobile => mobile.Devices)
                    .Select(device => device.Device)
                    .Where(device => !string.IsNullOrEmpty(device))
                    .Distinct()
                    .ToArray();

                if (devices is null || devices.Length == 0)
                    return [];

                return devices!;

            }
            public static string[] GetAndroidDeviceNames(string osVersion, string browserName)
            {
                if (Devices is null || Devices.Mobile is null)
                    return [];

                // OIC = OrdinalIgnoreCase (imported via ConstantManager)
                return [.. Devices.Mobile
                    .Where(m => m.OS?.Equals("android", OIC) ?? false)
                    .SelectMany(m => m.Devices)
                    .Where(m => m.osVersion?.Equals(osVersion, OIC) ?? false)
                    .Where(m => m.Browsers != null &&
                        m.Browsers.Any(b =>
                            b.DisplayName.Equals(browserName, OIC)
                        )
                    )
                    .Where(d => !string.IsNullOrEmpty(d.Device))
                    .Select(d => d.Device!)
                    .Distinct()];
            }

            public static string[] GetiOSDeviceNames(string osVersion, string browserName)
            {
                if (Devices is null || Devices.Mobile is null)
                    return [];

                // OIC = OrdinalIgnoreCase (imported via ConstantManager)
                return Devices.Mobile?
                    .Where(m => m.OS?.Equals("ios", OIC) ?? false)

                    .SelectMany(m => m.Devices)
                    .Where(m => m.osVersion?.Equals(osVersion, OIC) ?? false)
                    .Where(m => m.Browsers != null &&
                        m.Browsers.Any(b =>
                            b.DisplayName.Equals(browserName, OIC)
                        )
                    )
                    .Where(d => !string.IsNullOrEmpty(d.Device))
                    .Select(d => d.Device!) // Null check was executed with the previous clause
                    .Distinct()
                    .ToArray() ?? [];
            }

            public static string[] GetVersionsOfOS(string osName)
            {
                return osName switch
                {
                    "android" => AndroidVersions,
                    "ios" => iOSVersions,
                    "OS X" => MacOSVersions,
                    "Windows" => WindowsVersions,
                    _ => []
                };
            }
            public static string[] GetBrowserVersionsSupported(string browserName, string osName)
            {
                if (Devices is null || Devices.Mobile is null)
                    return [];

                if (osName == "OS X" || osName == "Windows")
                {
                    return [.. Devices.Desktop
                        .Where(d => d.OS?.Equals(osName, OIC) ?? false)
                        .SelectMany(d => d.Browsers ?? [])
                        .Where(d => d.BrowserName?.Equals(browserName, OIC) ?? false)
                        .Select(d => d.BrowserVersion)
                        .OrderDescending()
                    ];
                }
                // OIC = OrdinalIgnoreCase (imported via ConstantManager)
                return [.. Devices.Mobile
                    .Where(m => m.OS?.Equals(osName, OIC) ?? false)
                    .SelectMany(m => m.Devices)
                    .Where(d => d.Browsers != null &&
                        d.Browsers.Any(
                            b => b.DisplayName.Equals(browserName, OIC)
                        )
                     )
                    .Select(d => d.osVersion!)
                    .Distinct()
                    .OrderDescending()
                ];
            }

            public static string GetDesiredBrowerVersion(string browserName, string osName, string osVersion)
            {
                if (browserName == "Safari" && osName == "OS X")
                    return GetDesiredSafariVersion(osVersion);
                else if (browserName == "Safari" && osName == "ios")
                    return "";
                else
                    return "latest";
            }

            private static string GetDesiredSafariVersion(string osVersion)
            {
                return osVersion switch
                {
                        "12" => "Monterey",
                        "13" => "Venura",
                        "14" => "Sonoma",
                        "15" => "Sequoia",
                        "26" => "Tahoe",
                        _ => ""
                };


            }
        }

    }

}
