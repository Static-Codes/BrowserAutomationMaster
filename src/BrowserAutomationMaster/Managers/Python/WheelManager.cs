using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RequestManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Python
{
    public struct Wheel(string WheelName, string FileName)
    {
        public string Name { get; private set; } = WheelName;
        public string FileName { get; private set; } = FileName;
        public string PackageName { get; private set; } = SetPackageName(WheelName);
        private string DownloadLink { get; set; } = SetDownloadLink(FileName);

        public string DownloadLocation { get; set; } = SetDownloadLocation(FileName);
        public int InstallationStatus { get; private set; } = -1;
        public string InstallationResponse { get; private set; } = string.Empty;
        
        public async Task Download()
        {
            try
            {
                var baseDir = GetPythonWheelDirectory();
                var platformName = Platforms.IsARMhf ? "armhf" : "generic";
                var platformWheelDir = Path.Combine(baseDir, platformName);


                EnsureDirectoryExists(baseDir);
                EnsureDirectoryExists(platformWheelDir);

                var downloadPath = Path.Combine(platformWheelDir, FileName);

                DownloadLocation = downloadPath;


                if (File.Exists(DownloadLocation))
                    return;

                var responseStream = await NetworkClient.Instance.GetByteArrayAsync(DownloadLink);
                await File.WriteAllBytesAsync(downloadPath, responseStream);
            }
            catch (Exception ex)
            {
                WriteAndExit($"Unable to download: '{FileName}'\n\nError Log:\n{ex}", 1);
            }
        }

        private static string SetDownloadLink(string fileName)
        {
            var baseLink = Platforms.IsARMel ? BASE_ARMEL_WHEEL_LINK : BASE_ARMHF_WHEEL_LINK;
            return baseLink + fileName;
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
        private static Wheel BrotliARMv7 = new("BrotliPY for ARMv7", "brotlipy-0.7.0-cp311-cp311-linux_armv7l.whl");
        private static Wheel CFFIARMv7 = new("CFFI for ARMv7", "cffi-2.0.0-cp311-cp311-linux_armv7l.whl");
        private static Wheel PSUtilARMv7 = new("PSUtil for ARMhf", "psutil-7.1.2-cp36-abi3-linux_armv7l.whl");
        private static Wheel ZSTDARMv7 = new("ZSTD for ARMv7", "zstandard-0.25.0-cp311-cp311-linux_armv7l.whl");
        // private static Wheel PSUtilARMel = new Wheel

        private static Wheel BrotliARMhf = new("BrotliPY for ARMhf", "brotlipy-0.7.0-cp311-cp311-linux_armv7l.whl");
        private static Wheel CFFIARMhf = new("CFFI for ARMhf", "cffi-2.0.0-cp311-cp311-linux_armv7l.whl");
        private static Wheel PSUtilARMhf = new("PSUtil for ARMhf", "psutil-7.1.2-cp36-abi3-linux_armv7l.whl");
        private static Wheel ZSTDARMhf = new("ZSTD for ARMhf", "zstandard-0.25.0-cp311-cp311-linux_armv7l.whl");


        public static readonly Wheel[] ArmWheels = 
            Platforms.IsARMhf ? [PSUtilARMhf, BrotliARMhf, CFFIARMhf, ZSTDARMhf] 
            : [PSUtilARMv7, BrotliARMv7, CFFIARMv7, ZSTDARMv7, ];

        public static string[] GetRequirementStrings() 
        {
            string[] reqStrings = new string[ArmWheels.Length];

            for (int i = 0; i < ArmWheels.Length; i++)
                reqStrings[i] = ArmWheels[i].DownloadLocation;

            return reqStrings;
        }

        public static async Task DownloadWheels()
        {

            foreach (var wheel in ArmWheels)
            {
                try
                {
                    await wheel.Download();
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
    };

}
