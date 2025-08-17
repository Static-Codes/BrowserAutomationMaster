using BrowserAutomationMaster.Messaging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.BrowserVersionManager;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    public struct BrowserStackConfig(string BuildName, string ProjectName)
    {
        public required string UserName;
        public required string AccessKey;
        public required BrowserStackPlatform[] Platforms;
        public bool BrowserStackLocal = true;
        public string BuildName = BuildName; // Python script filename (without the extension)
        public string ProjectName = ProjectName; // projectDirectory (Current timestamp)
        public bool Debug { get; private set; } = true;
        public string ConsoleLogs { get; private set; } = "info";
        public string Framework { get; private set; } = "python";
    }

    public struct BrowserStackPlatform()
    {
        public required string OSName;
        public required string OSVersion;
        public required string BrowserName;
        public required string BrowserVersion;
        public string? DeviceName = null;
        public DeviceOrientation? DeviceOrientation = null;
    }

    public enum DeviceOrientation
    {
        Landscape,
        Portrait
    }


    public class InstanceManager
    {
        private readonly static string browserStackDirectory = GetBrowserStackDirectory();
        public readonly string browserStackConfig = Path.Combine(browserStackDirectory, "browserstack.yml");
        
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

        private void BuildConfig(string userName, string accessKey)
        {
            var rawOSName = Input.WriteListFromOptions(["Android", "iOS", "MacOS", "Windows"], noun: "operating system");
            var OSName = SanitizeOSName(rawOSName);

            var versions = GetVersionsOfOS(rawOSName);
            var rawOSVersion = Input.WriteListFromOptions(versions, noun: "version");
            var OSVersion = SanitizeOSVersion(rawOSVersion, rawOSName, versions);

            // Will be used for defining DeviceName and DeviceOrientation if mobile
            // If not mobile, browserVersion must be specified.
            var isMobile = rawOSName switch
            {
                "Android" or "iOS" => true,
                _ => false,
            };

            var browserName = GetDesiredBrowser(rawOSName);

            // Add a flag isManualBrowserVersion
            //var browserVersion = GetDesiredBrowserVersion(rawOSName, browserName);

            var browserVersion = "latest";

            

            string? deviceOrientation = null;
            //if (isMobile)
            //    deviceOrientation = Input.WriteListFromOptions(DeviceOrientation.Portrait, DeviceOrientation.Landscape);

            







        }

        private void WriteConfig(BrowserStackPlatform[] stackPlatforms)
        {

            //var stackPlatform = new BrowserStackPlatform()
            //{
            //    OSName = "",
            //    OSVersion = "",
            //    BrowserName = "",
            //    BrowserVersion = "",
            //    DeviceName = "",
            //    DeviceOrientation = null,
            //};

            BrowserStackConfig config = new(BuildName: "", ProjectName: "")
            {
                UserName = Input.WriteTextAndReturnRawInput("Please enter your BrowserStack Username: "),
                AccessKey = Input.WriteTextAndReturnRawInput("Please enter your BrowserStack Access Key: "),
                Platforms = stackPlatforms,
            };
        }
    }
}
