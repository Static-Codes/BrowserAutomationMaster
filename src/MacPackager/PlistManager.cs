using static BrowserAutomationMaster.Managers.UpdateManager;

namespace MacPackager
{
    public static class PlistManager
    {
        public static async Task<string> GetPlistContent() 
        {

            var rawVersionTag = await GetLatestVersion();

            if (string.IsNullOrEmpty(rawVersionTag))
            {
                Console.WriteLine("[ERROR]: The BAMM for macOS Packager was unable to determine the latest version of BAMM, please try again.");
                Environment.Exit(1);
            }

            var latestVersion = rawVersionTag.Replace("v", "");

            var indexOfAChar = latestVersion.IndexOf('A');
            
            var shortVersion = latestVersion[..indexOfAChar];


            var InfoPlist = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
    <!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
    <plist version=""1.0"">
    <dict>
        <key>CFBundleExecutable</key>
        <string>BAMM</string> <key>CFBundleIdentifier</key>
        <string>com.static-codes.bamm</string> <key>CFBundlePackageType</key>
        <string>APPL</string>
        <key>CFBundleShortVersionString</key>
        <string>{shortVersion}</string> 
        <key>CFBundleVersion</key>
        <string>{latestVersion}</string> 
        <key>CFBundleName</key>
        <string>BAMM</string>
        <key>CFBundleDisplayName</key>
        <string>BAMM</string>
        <key>CFBundleIconFile</key>
        <string>AppIcon</string> 
        <key>NSHighResolutionCapable</key>
        <true/> 
        <key>LSApplicationCategoryType</key>
        <string>public.app-category.developer-tools</string> </dict>
    </plist>";

            return InfoPlist;

        }
    }
}