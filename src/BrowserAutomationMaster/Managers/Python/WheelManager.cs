using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RequestManager;

namespace BrowserAutomationMaster.Managers.Python
{
    public struct Wheel(string WheelName, string FileName)
    {
        public string Name { get; private set; } = WheelName;
        public string FileName { get; private set; } = FileName;
        private string DownloadLink { get; set; } = SetDownloadLink(FileName);

        public string DownloadLocation { get; set; } = string.Empty;
        public int InstallationStatus { get; private set; } = -1;
        public string InstallationResponse { get; private set; } = string.Empty;

        public async Task Download()
        {
            try
            {
                var baseDir = GetPythonWheelDirectory();
                var platformName = Platforms.IsARMel ? "armel" : "armhf";
                var platformWheelDir = Path.Combine(baseDir, platformName);


                EnsureDirectoryExists(baseDir);
                EnsureDirectoryExists(platformWheelDir);

                var downloadPath = Path.Combine(platformWheelDir, FileName);
                DownloadLocation = downloadPath;

                var responseStream = await NetworkClient.Instance.GetByteArrayAsync(DownloadLink);
                await File.WriteAllBytesAsync(downloadPath, responseStream);
            }
            catch (Exception ex)
            {
                Errors.WriteAndExit($"Unable to download: '{FileName}'\n\nError Log:\n{ex}", 1);
            }
        }
        private static string SetDownloadLink(string fileName)
        {
            var baseLink = Platforms.IsARMel ? BASE_ARMEL_WHEEL_LINK : BASE_ARMHF_WHEEL_LINK;
            return baseLink + fileName;
        }

        public void UpdateResponse(string response) { InstallationResponse = response; }
        public void UpdateStatus(int newStatus) { InstallationStatus = newStatus; }

    };

    public static class WheelManager
    {
        private static Wheel BrotliARMel = new("BrotliPY for ARMel", "brotlipy-0.7.0-cp39-cp39-linux_armel.whl");
        private static Wheel CFFIARMel = new("CFFI for ARMel", "cffi-2.0.0-cp39-cp39-linux_armel.whl");
        private static Wheel ZSTDARMel = new("ZSTD for ARMel", "zstandard-0.25.0-cp39-cp39-linux_armel.whl");

        private static Wheel BrotliARMhf = new("BrotliPY for ARMhf", "brotlipy-0.7.0-cp38-cp38-linux_armhf.whl");
        private static Wheel CFFIARMhf = new("CFFI for ARMhf", "cffi-1.17.1-cp38-cp38-linux_armhf.whl");
        private static Wheel ZSTDARMhf = new("ZSTD for ARMhf", "zstandard-0.23.0-cp38-cp38-linux_armhf.whl");

        private static readonly Wheel[] wheels = 
            Platforms.IsARMel?[BrotliARMel, CFFIARMel, ZSTDARMel] 
            : [BrotliARMhf, CFFIARMhf, ZSTDARMhf];

        public static async Task DownloadWheels()
        {

            foreach (var wheel in wheels)
            {
                try
                {
                    await wheel.Download();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static async Task InstallWheels(string InterpreterPath)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = InterpreterPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            foreach (var wheel in wheels)
            {
                psi.Arguments = $"install {wheel.DownloadLocation}";
                using var process = await ProcessFactory.SpawnProcess(psi, $"installing {wheel.FileName}");
                (var ExitCode, var STDOut, var STDErr) = await ProcessFactory.GetProcessResponse(process);
                wheel.UpdateStatus(ExitCode);
                wheel.UpdateResponse("ADD LOGIC HERE");
            }
        }
    };

}
