using System.Xml;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static MacPackager.PlistHelperFunctions;

namespace MacPackager
{
    public static class PlistManager
    {
        public static async Task<MemoryStream> GetPlistContent() 
        {
            var applicationName = "BAMM";
            var bundleIdentifier = "com.static-codes.bamm";
            var categoryType = "public.app-category.developer-tools";

            var rawVersionTag = await GetLatestVersion();

            if (string.IsNullOrEmpty(rawVersionTag))
            {
                Console.WriteLine("[ERROR]: The BAMM for macOS Packager was unable to determine the latest version of BAMM, please try again.");
                Environment.Exit(1);
            }

            var latestVersion = rawVersionTag.Replace("v", "");
            var indexOfAChar = latestVersion.IndexOf('A');
            var shortVersion = latestVersion[..indexOfAChar];

            var InfoPlist = await CreateInfoPlistStreamAsync(applicationName, bundleIdentifier, shortVersion, latestVersion, categoryType);

            return InfoPlist;

        }

        private static async Task<MemoryStream> CreateInfoPlistStreamAsync(
            string applicationName, string bundleIdentifier, string shortVersion, string latestVersion, string categoryType
        )
        { 
            // MemoryStream is chosen over Stream due to the increased performance it provides through direct buffer writes to the machine's RAM.
            var memoryStream = new MemoryStream();

            XmlWriterSettings settings = new() {
                Async = true,
                CloseOutput = false, // Preventing the stream from premature disposal
                Indent = true,
                IndentChars = "^I" // Horizontal tab, can also be written as \t
            };

            using var writer = XmlWriter.Create(memoryStream, settings);

            # region ".plist Doc Header"
            await writer.WriteStartDocumentAsync(standalone: true); // Setting standalone to true might cause issues later, will address if needbe.

            var docTypeName = "plist"; // File Type
            var docTypeVersion = "1.0"; // Version of plist used on the installer.
            var docTypePubID = "-//Apple//DTD PLIST 1.0//EN"; // Apple's Publisher ID

            // Document Type Definition (DTD) -> https://en.wikipedia.org/wiki/Document_type_definition
            var docTypeSysID = "https://www.apple.com/DTDs/PropertyList-1.0.dtd";

            // Not sure what this does yet, but it has the option to pass a null value, so im going to ignore it.
            string? docTypeSubset = null; 

            await writer.WriteDocTypeAsync(docTypeName, docTypePubID, docTypeSysID, docTypeSubset);
            
            // Writes the <plist version="1.0"> tag
            await writer.WriteStartElementAsync(null, docTypeName, null);
            await writer.WriteAttributeStringAsync(null, "version", null, docTypeVersion);

            # endregion

            # region "Writing Key/Value Pairs"
            // Writes the starting <dict> tag.
            await writer.WriteStartElementAsync(null, "dict", null);

            // Writes the required key/value pairs
            await WriteStringEntryAsync(writer, "CFBundleExecutable", applicationName);
            await WriteStringEntryAsync(writer, "CFBundleIdentifier", bundleIdentifier);
            await WriteStringEntryAsync(writer, "CFBundlePackageType", "APPL");
            await WriteStringEntryAsync(writer, "CFBundleShortVersionString", shortVersion);
            await WriteStringEntryAsync(writer, "CFBundleVersion", latestVersion);
            await WriteStringEntryAsync(writer, "CFBundleName", applicationName);
            await WriteStringEntryAsync(writer, "CFBundleDisplayName", applicationName);
            await WriteStringEntryAsync(writer, "CFBundleIconFile", "AppIcon");


            await WriteBoolEntryAsync(writer, "NSHighResolutionCapable", true);
            await WriteStringEntryAsync(writer, "LSApplicationCategoryType", categoryType);
            
            // Writes the ending </dict> tag.
            await writer.WriteEndElementAsync();

            // Writes the ending </plist> tag.
            await writer.WriteEndElementAsync();

            #endregion

            # region "Flushing and Return"
            // I SPENT SO LONG ON THIS, ITS NOT DOCUMENTED
            await writer.FlushAsync(); // Flushes the contents of writer to memoryStream
            
            // By default, the MemoryStream's position is the index of the last byte in the Stream.
            // A reset is required to return the correct value.
            memoryStream.Position = 0; 

            // FOR THE LOVE OF GOD PLEASE DON'T INTRODUCE A MEMORYLEAK BY FORGETTING TO DISPOSE OF THIS, YOU HAVE ONE JOB, KEEP IT CLEAN!
            return memoryStream; 

            #endregion
        }
    }

    public static class PlistHelperFunctions 
    {
        public static async Task WriteEntryKeyAsync(XmlWriter writer, string key)
        {
            await writer.WriteStartElementAsync(prefix: null, localName: "key", ns: null); // Writes the opening <key> tag.
            await writer.WriteStringAsync(key); // Writes the entry's key name.
            await writer.WriteEndElementAsync(); // Writes the ending </key> tag.
        }

        public static async Task WriteSelfClosingTagAsync(XmlWriter writer, string tagName)
        {
            await writer.WriteStartElementAsync(prefix: null, localName: tagName, ns: null);
            await writer.WriteEndElementAsync();
        }

        public static async Task WriteStringEntryAsync(XmlWriter writer, string key, string value)
        {
            await WriteEntryKeyAsync(writer, key);

            await writer.WriteStartElementAsync(prefix: null, localName: "string", ns: null); // Writes the starting <string> tag.
            await writer.WriteStringAsync(value); // Writes the entry's value.
            await writer.WriteEndElementAsync(); // Writes the ending </string> tag.
        }

        public static async Task WriteBoolEntryAsync(XmlWriter writer, string key, bool value)
        {
            await WriteEntryKeyAsync(writer, key);
            var boolCast = value ? "true" : "false";
            await WriteSelfClosingTagAsync(writer, boolCast);
        }
    }
}