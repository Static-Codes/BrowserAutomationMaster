using BrowserAutomationMaster.Messaging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static BrowserAutomationMaster.Helpers.EnumHelper;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager.DeviceHelper;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    public struct BrowserStackConfig()
    {
        public required string UserName;
        public required string AccessKey;
        public required BrowserStackPlatform[] Platforms;
        public required bool BrowserStackLocal;
        public required string BuildName; // Python script filename (without the extension)
        public required string ProjectName; // projectDirectory (Current timestamp)
        public required bool Debug;
        public required string ConsoleLogs;
        public required string Framework;
    }

    public struct BrowserStackPlatform()
    {
        public required string osName;
        public required string osVersion;
        public required string BrowserName;
        public required string BrowserVersion;
        public string? DeviceName = null;
        public string? DeviceOrientation = null;
    }

    public enum DeviceOrientation
    {
        Landscape,
        Portrait
    }


    public class InstanceManager
    {
        public readonly static string browserStackDirectory = GetBrowserStackDirectory();
        public readonly static string browserStackConfig = Path.Combine(browserStackDirectory, "browserstack.yml");
        
        public static BrowserStackConfig StackConfig { get; private set; }


        private BrowserStackConfig? LoadConfig()
        {
            if (!File.Exists(browserStackConfig))
                return null;

            try 
            {
                var fileText = File.ReadAllText(browserStackConfig);
            
                var deserializer = 
                    new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                return deserializer.Deserialize<BrowserStackConfig>(fileText);
            }
            catch {
                return null;
            }
        }

        public static void WriteConfig(string userName, string accessKey, string projectName, string scriptName)
        {
            try
            {
                var rawOSName = Input.WriteListFromOptions(OSNames, noun: "Operating System", pageSize: 4);
                var osName = SanitizeOSName(rawOSName);
                var browserName = GetDesiredBrowser(rawOSName);

                var osVersions = GetVersionsOfOS(osName);
                var rawOSVersion = Input.WriteListFromOptions(osVersions, noun: $"Version of {rawOSName}");
                var osVersion = SanitizeOSVersion(rawOSVersion, rawOSName, osVersions);

                var versions = GetBrowserVersionsSupported(browserName, osName);

                var description = $"version of {rawOSName} that supports {browserName}";
                if (versions == null)
                    Errors.WriteErrorAndExit($"Unable to find a {description}, please try a different combination.", 1);

                // Will be used for defining DeviceName and DeviceOrientation if mobile
                // If not mobile, browserVersion must be specified.
                var isMobile = osName switch
                {
                    "android" or "ios" => true,
                    _ => false,
                };

                string browserVersion = "";
                if (osName != "android") // BrowserStack doesn't allow you to specify the browserVersion on android.
                    browserVersion = GetDesiredBrowerVersion(browserName, osName, osVersion);

                string[] devices;
                string? device = null;
                string? deviceOrientation = null;

                if (isMobile)
                {
                    devices = osName switch
                    {
                        "android" => GetAndroidDeviceNames(osVersion, browserName),
                        "ios" => GetiOSDeviceNames(osVersion, browserName),
                        _ => []
                    };

                    if (devices.Length == 0)
                        Errors.WriteErrorAndExit("Unable to find device supported by BrowserStack that fits your requirements.", status: 1);

                    device = Input.WriteListFromOptions(devices, noun: "device");

                    var reprs = GetStringReprs(typeof(DeviceOrientation));
                    deviceOrientation = Input.WriteListFromOptions(reprs, noun: "orientation");
                }


                // Currently only one platform is supported at a time but plans are to implement multiple if desired.
                var platform = new BrowserStackPlatform[]
                {
                    new()
                    {
                        osName = osName,
                        osVersion = osVersion,
                        BrowserName = browserName,
                        BrowserVersion = browserVersion,
                        DeviceName = device,
                        DeviceOrientation = deviceOrientation
                    }
                };

                var config = new BrowserStackConfig()
                {
                    AccessKey = accessKey,
                    UserName = userName,
                    Platforms = platform,
                    Debug = true,
                    BrowserStackLocal = true,
                    BuildName = scriptName,
                    ProjectName = projectName,
                    ConsoleLogs = "disabled",
                    Framework = "python",
                };

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                var yaml = serializer.Serialize(config);
                if (yaml == null)
                    Errors.WriteErrorAndExit("Unable to generate browserstack.yml using the selected information, please try again.", 1);
                EnsureDirectoryExists(browserStackDirectory);
                File.WriteAllText(browserStackConfig, yaml);

            }

            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    "Unable to generate browserstack.yml using the selected information, please try again.\n\n" +
                    $"Error Log:\n{e.Message} in WriteConfig()", 
                    status: 1
                );
            }

        }

        //private void WriteConfig(BrowserStackPlatform[] stackPlatforms)
        //{

        //    //var stackPlatform = new BrowserStackPlatform()
        //    //{
        //    //    osName = "",
        //    //    osVersion = "",
        //    //    BrowserName = "",
        //    //    BrowserVersion = "",
        //    //    DeviceName = "",
        //    //    DeviceOrientation = null,
        //    //};

        //    BrowserStackConfig config = new()
        //    {
        //        UserName = Input.WriteTextAndReturnRawInput("Please enter your BrowserStack Username: "),
        //        AccessKey = Input.WriteTextAndReturnRawInput("Please enter your BrowserStack Access Key: "),
        //        Platforms = stackPlatforms,
        //    };
        //}
    }
}
