using System.Text;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

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
        public string ExtensionPath { get; init; } = rawExtensionPath.Replace("file://", "");
        public bool IsLocalFile { get; init; } = rawExtensionPath.StartsWith("file://");
        public bool IsURL { get; init; } = rawExtensionPath.StartsWith("http://") || rawExtensionPath.StartsWith("https://");
        public bool IsValidFileExtension() 
        {
            return 
                ExtensionPath.EndsWith(".crx") && browserName.Equals("chrome") ||
                ExtensionPath.EndsWith(".xpi") && browserName.Equals("firefox");
        }

        public async Task<bool> ExtensionExists() 
        {
            if (IsLocalFile) 
            {
                return File.Exists(ExtensionPath);
            }

            if (IsURL) 
            {
                if (await RequestManager.SiteIsPingable(ExtensionPath)) {
                    return true;
                }

                Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
                Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
                return false;
            }

            Warning.Write("Unable to make contact with the website hosting the extension provided.");
            return false;
        }

        public async Task<MemoryStream?> GetExtensionContents() 
        {
            if (IsURL) 
            {
                try 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var contents = await RequestManager.NetworkClient.Instance.GetByteArrayAsync(RawExtensionPath, cts.Token);

                    if (contents == null) {
                        return null;
                    }

                    using MemoryStream memoryStream = new(contents);
                    return memoryStream;
                }

                catch (Exception ex) 
                {
                    Errors.WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                            "Error Log:",
                            ex.Message
                        ]),
                        status: 1, 
                        writePlatformDebugInfo: true
                    );
                }
            }
            // if (IsLocalFile) 
            // {
                if (RawExtensionPath.EndsWith(".xpi"))
                {

                    using var reader = new StreamReader(RawExtensionPath);
                    var magicChars = new char[4];

                    try 
                    {
                        reader.ReadBlock(magicChars, 0, 4);
                        var magicBytes = Encoding.UTF8.GetBytes(magicChars, 0, 4);
                        byte[] validBytes = [ 0x06, 0x05, 0x4B, 0x50 ];

                        Console.WriteLine(validBytes.Length);
                        Console.WriteLine(magicBytes.Length);
                        Console.WriteLine(validBytes.SequenceEqual(magicBytes));

                        using MemoryStream memoryStream = new(magicBytes);
                        return memoryStream;
                    }

                    catch (Exception ex) 
                    {
                        Errors.WriteAndExit
                        (
                            message: string.Join(NLC, [
                                $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                                "Error Log:",
                                ex.Message
                            ]),
                            status: 1, 
                            writePlatformDebugInfo: true
                        );
                    }
                }
            // }
            return null;
        }
    }
}