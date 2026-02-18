using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Python
{
    public struct Wheel(string WheelName, string FileName, string PlatformType)
    {
        public string Name { get; private set; } = WheelName;
        public string FileName { get; private set; } = FileName;
        public string PlatformType { get; private set; } = PlatformType;
        public string PackageName { get; private set; } = SetPackageName(WheelName);
        public string DownloadLocation { get; set; } = SetDownloadLocation(FileName);
        public int InstallationStatus { get; private set; } = -1;
        public string InstallationResponse { get; private set; } = string.Empty;
        
        public async Task Download()
        {
            string[] validPlatformTypes = ["armhf", "generic"];
            try
            {
                var baseDir = GetPythonWheelDirectory();
                var platformName = PlatformType;
                var platformWheelDir = Path.Combine(baseDir, platformName);


                EnsureDirectoryExists(baseDir);
                EnsureDirectoryExists(platformWheelDir);

                var downloadPath = Path.Combine(platformWheelDir, FileName);

                DownloadLocation = downloadPath;


                if (File.Exists(DownloadLocation)) {
                    return;
                }
                

                if (!validPlatformTypes.Contains(PlatformType)) {
                    throw new Exception($"Invalid PlatformType provided: {PlatformType}, expected 'armhf' or 'generic'");
                }

                // Dynamically creating the path to each embedded resource.
                var ResourcePattern = string.Format("BrowserAutomationMaster.Resources.wheels.{0}.{1}", PlatformType, FileName);
                
                // Retrieving the contents of the resource.
                var responseStream = EmbeddedResourceManager.GetEmbeddedResource(FileName, ResourcePattern);
                
                // Writing the contents to disk.
                await EmbeddedResourceManager.WriteEmbeddedResourceToDisk(FileName, ResourcePattern, downloadPath);
            }
            catch (Exception ex)
            {
                WriteAndExit($"Unable to download: '{FileName}'\n\nError Log:\n{ex}", 1);
            }
        }

        private static string SetDownloadLocation(string fileName)
        {

            var baseDir = GetPythonWheelDirectory();
            var platformName = Platforms.IsARMhf ? "armhf" : "generic";
            var platformWheelDir = Path.Combine(baseDir, platformName);


            EnsureDirectoryExists(baseDir);
            EnsureDirectoryExists(platformWheelDir);

            return Path.Combine(platformWheelDir, fileName);

        }

        private static string SetPackageName(string friendlyName)
        {
            string? packageName = null;
            try
            {
                packageName = friendlyName.Split(' ')[0].ToLower();
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    string.Join(NLC, [
                        $"BAMM ran into a fatal error while attempting to download wheel: {friendlyName}",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                        $"\nError Log:\n\n{ex.Message}"
                    ]), status: 1
                );
            }
            return packageName;
        }

        public void UpdateResponse(string response) { InstallationResponse = response; }
        public void UpdateStatus(int newStatus) { InstallationStatus = newStatus; }

    };

    public static class WheelManager
    {
        // Generic ARMv7 Wheels
        private static Wheel BrotliARMv7 = new("BrotliPY for ARMv7", "brotlipy-0.7.0-cp311-cp311-linux_armv7l.whl", "generic");
        private static Wheel CFFIARMv7 = new("CFFI for ARMv7", "cffi-2.0.0-cp311-cp311-linux_armv7l.whl", "generic");
        private static Wheel ZSTDARMv7 = new("ZSTD for ARMv7", "zstandard-0.25.0-cp311-cp311-linux_armv7l.whl", "generic");

        // Specific ARMhf PSUtil Wheel
        private static Wheel PSUtilARMv7 = new("PSUtil for ARMv7", "psutil-7.1.2-cp36-abi3-linux_armv7l.whl", "armhf");

        // Generic ARMv7 PSUtil Wheel
        private static Wheel PSUtilARMhf = new("PSUtil for ARMhf", "psutil-7.1.3-cp36-abi3-linux_armv7l.whl", "generic");
        
        // The first 3 wheels are downloaded for both generic ARMv7 and ARMhf
        // The PSUtil wheel differs between platforms.
        public static readonly Wheel[] ArmWheels = [
            BrotliARMv7,
            CFFIARMv7,
            ZSTDARMv7,
            Platforms.IsARMhf ? PSUtilARMhf : PSUtilARMv7,
        ];

        public static async Task DownloadWheels()
        {

            foreach (var wheel in ArmWheels)
            {
                try
                {
                    // Only download if the file doesn't already exist.
                    if (!File.Exists(wheel.DownloadLocation)) {
                        await wheel.Download();
                    }
                }
                catch (Exception ex)
                {
                    WriteAndExit(
                        message: string.Join(NLC, [
                            "BAMM ran into a fatal error while attempting to download precompiled binaries for selenium.",
                            $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                            $"\nError Log:\n{ex.Message}"
                        ]), status: 1
                    );
                }
            }
        }
    
        public static string[] GetRequirementStrings() 
        {
            string[] reqStrings = new string[ArmWheels.Length];

            for (int i = 0; i < ArmWheels.Length; i++) {
                reqStrings[i] = ArmWheels[i].DownloadLocation;
            }
            
            return reqStrings;
        }

        
    };

}
