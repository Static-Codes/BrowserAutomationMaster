using System.Net;
using System.Threading.Tasks;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers 
{
    // This class will be used to parse the commands of
    // bamm extension "file://path/to/firefox/extension.xpi"
    // bamm extension "https://url/to/firefox/extension.xpi"
    // bamm extension "file://path/to/chrome/extension.crx"
    // bamm extension "https://url/to/chrome/extension.crx"
    public class ExtensionManager(string rawExtensionPath, string browserName)
    {
        public string RawExtensionPath { get; init; }  = rawExtensionPath;
        public string extensionPath { get; init; } = rawExtensionPath.Replace("file://", "");
        public bool IsLocalFile { get; init; } = rawExtensionPath.StartsWith("file://");
        public bool IsURL { get; init; } = rawExtensionPath.StartsWith("http://") || rawExtensionPath.StartsWith("https://");
        public async Task<bool> IsValidExtension() 
        {
            // A Chrome extension was passed but the specified browser is not Chrome
            if (extensionPath.EndsWith(".crx") && !browserName.Equals("chrome"))
            {
                return false;
            }

            // A Firefox extension was passed but the specified browser is not Firefox
            if (extensionPath.EndsWith(".xpi") && !browserName.Equals("firefox"))
            {
                return false;
            }

            if (IsLocalFile) 
            {
                return File.Exists(extensionPath);
            }

            if (IsURL) 
            {
                if (await RequestManager.SiteIsPingable(extensionPath)) {
                    return true;
                }

                Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
                Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
                return false;
            }

            Warning.Write("Unable to make contact with the website hosting the extension provided.");
            return false;
        }
    }
}