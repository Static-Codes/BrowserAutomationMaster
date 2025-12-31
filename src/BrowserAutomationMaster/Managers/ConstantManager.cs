namespace BrowserAutomationMaster.Managers
{
    public class ConstantManager
    {
        public static readonly string NLC = Environment.NewLine; // This isn't a constant but for simplicity it will be placed here.
        public const string BASE_REPO_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/";
        public const string DOCUMENTATION_LINK = "https://static-codes.github.io/BAMM-Docs/";
        public const string ISSUES_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/issues";
        public const string LATEST_VERSION_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest";
        public const string RELEASES_DOWNLOAD_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/download";
        public const string BROWSER_STACK_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/AppData/browserstack.json";
        public const string BASE_ARMEL_WHEEL_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/raw/refs/heads/main/src/BrowserAutomationMaster/AppData/wheels/generic/";
        public const string BASE_ARMHF_WHEEL_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/raw/refs/heads/main/src/BrowserAutomationMaster/AppData/wheels/armhf/";
        public const string PACKAGES_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/AppData/packages.json";
        public const string USERAGENTS_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/AppData/useragents.json";
        public const string GUI_DAEMON_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/Helpers/UIDaemon.py";
        public const string GUI_ZIP_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/gui/gui.zip";
        public const StringComparison CCIC = StringComparison.CurrentCultureIgnoreCase;
        public const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
        public const char HORIZONTAL_TAB = '\t';

        // Used in ExtensionManager
        // Using ReadOnlySpan<byte> for constants to avoid heap allocations
        public static readonly ReadOnlyMemory<byte> XPIMagicBytes = new byte[4] { 0x50, 0x4B, 0x05, 0x06 };
        public static readonly ReadOnlyMemory<byte> recBytes = "mozilla-recommendation.json"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> coseManBytes = "META-INF/cose.manifest"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> coseSigBytes = "META-INF/cose.sig"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> manSfBytes = "META-INF/manifest.mf"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> mozRsaBytes = "META-INF/mozilla.rsa"u8.ToArray();
        public static readonly Dictionary<string, ReadOnlyMemory<byte>> XPIContentChecks = new () {
            { "'mozilla-recommendation.json'", recBytes },
            { "'META-INF/cose.manifest'", coseManBytes }, 
            { "'META-INF/cose.sig'", coseSigBytes },
            { "'META-INF/manifest.mf'", manSfBytes },
            { "'META-INF/mozilla.rsa'", mozRsaBytes }
        };
        //public const string CHROME_VERSION_HISTORY_URL = "https://versionhistory.googleapis.com/v1/chrome/platforms/win/channels/stable/versions";
        public const string CHROME_VERSION_URL = "https://chromiumdash.appspot.com/fetch_releases?channel=Extended&platform=Windows&num=1&offset=0";
        public static readonly ReadOnlyMemory<byte> manJsonBytes = "manifest.json"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> CRXMagicBytes = "Cr24"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> metadataBytes = "_metadata/"u8.ToArray();
        public static readonly ReadOnlyMemory<byte> verifContentsBytes = "_metadata/verified_contents.json"u8.ToArray();
        public static readonly Dictionary<string, ReadOnlyMemory<byte>> CRXContentChecks = new () {
            { "'manifest.json'", manJsonBytes },
            { "'Cr24'", CRXMagicBytes }, 
            { "'_metadata/'", metadataBytes },
            { "'_metadata/verified_contents.json'", verifContentsBytes },
        };

    }
}
