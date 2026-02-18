using BrowserAutomationMaster.Managers.Common;
using BrowserAutomationMaster.Managers.OS.Unix.Linux;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.DistroManager;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers
{
    public class UpdateManager()
    {
        public const string CurrentVersion = "v1.0.0A8";

        // These two are used in Publisher.Build
        public const string BaseVersion = "1.0.0";
        public const string VersionIdentifier = "alpha8";

        // Assuming current is latest until further checks are done.
        public static string LatestVersion { get; set; } = CurrentVersion; 
        public static async Task CheckForUpdate()
        {
            if (await UpdateAvailable())
            {
                Warning.Write(
                    $"BAM Manager (BAMM) has an available update.\n\n" +
                    $"Current Version: {CurrentVersion}\n" +
                    $"Latest Version: {LatestVersion}\n\n"
                );

                string response = Input.AskForInput("Would you like to download the update now? [y/n]:\n");

                if (response.ToLower().Equals("y")) {
                    OpenLatestVersionInBrowser();
                    Environment.Exit(0);
                }
                
            }
            else 
                WriteSuccessMessage($"BAM Manager (BAMM) is currently running the latest release ({LatestVersion})");
        }

        public static async Task<string> GetLatestVersion()
        {
            HttpResponseMessage? response = new();
            try
            {
                bool uriCreated = Uri.TryCreate($"{LATEST_VERSION_LINK}", UriKind.Absolute, out Uri? uriResult);

                if (!uriCreated || uriResult == null) 
                    return string.Empty;

                RequestManager requestManager = RequestManager.Create(uriResult);
                response = await requestManager.GetAsync(followRedirects: false);
                
                if (response == null) {
                    return string.Empty;
                }

                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    Write(
                        message:
                            $"BAM Manager (BAMM) was unable to check github for the latest version.\n" +
                            $"If this issue persists, " +
                            $"please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\n" +
                            $"The response for the version request didn't contain a redirect status code (302), " +
                            $"contains: {response.StatusCode}."
                    );
                }
            }
            
            catch (Exception e)
            {
                WriteErrorAndReturnEmptyString(
                    $"BAM Manager (BAMM) was unable to check github for the latest version.\n" +
                    $"If this issue persists, " +
                    $"please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\n{e.Message}"
                );
            }

            // This should never actually be hit but the line below has a "Dereference of possibly null reference" error so im leaving this in"
            if (response == null) {
                return string.Empty;
            }

            string url = 
                response.Headers.Location != null ? response.Headers.Location.AbsoluteUri : string.Empty;

            int versionIndex = url.LastIndexOf('/');

            if (versionIndex == -1) { 
                Write(
                    "BAM Manager (BAMM) was unable to check github for the latest version, " +
                    "if this issue persists, and you are positive your network connection is stable, " +
                    $"please make a bug report at {ISSUES_LINK}\n" +
                    $"Error log:{NLC}" +
                    "Unable to parse version from latest release response."
                ); 
            }
            else if (versionIndex < url.Length - 1) { 
                return url[(versionIndex + 1)..]; // returns vX.X.X
            }
            return string.Empty;
        }
        private static bool HasNetworkConnection()
        {
            try {
                using Ping pinger = new();
                return pinger.Send("8.8.8.8").Status == IPStatus.Success;
            }
            catch (PingException) { return false; }
        }

        private static void OpenLatestForWindows(string currentReleaseUri)
        {
            string url = Platforms.CurrentArchitecture switch
            {
                Arm64 => Path.Combine(currentReleaseUri, $"BAMM-{LatestVersion}-ARM64-Setup.exe"),
                X64 => Path.Combine(currentReleaseUri, $"BAMM-{LatestVersion}-x64-Setup.exe"),
                _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-bypass flag.")
            };

            var psi = new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true };
            using var Process = ProcessFactory.SpawnProcess(psi, "open new release page", runSync: true, timeout: 20).Result;

        }
        
        private static void OpenLatestForMacOS(string currentReleaseUri)
        {
            string url = RuntimeInformation.ProcessArchitecture switch
            {
                Arm64 => Path.Combine(currentReleaseUri, "bamm-silicon"),
                X64 => Path.Combine(currentReleaseUri, "bamm"),
                _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-bypass flag.")
            };

            var psi = new ProcessStartInfo("open", url);
            using var Process = ProcessFactory.SpawnProcess(psi, "open new release page", runSync: true, timeout: 20).Result;
        }

        private static void OpenLatestForLinux(string currentReleaseUri)
        {
            CheckLinuxDistro();
            
            var invalidDistro = Platforms.CurrentDistribution!.Equals(Distros.Unknown);

            if (invalidDistro) {
                WriteAndExit(
                    message: invalidDistroMessage, 
                    status: 1
                );
            }

            string? uri = null;

            // Handling Linux installations that are bundled as packages for the CurrentDistribution
            if (Platforms.CurrentDistribution.InstallationType.Equals(InstallationType.Package))
            {
                if (Platforms.CurrentDistribution.PackageType.Equals(PackageType.DEB)) 
                {
                    uri = RuntimeInformation.ProcessArchitecture switch
                    {
                        Arm64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-arm64.deb"),
                        X64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-x64.deb"),
                        _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-cpu-bypass flag.")

                    };
                }

                else if (Platforms.CurrentDistribution.PackageType.Equals(PackageType.RPM))
                {
                    uri = RuntimeInformation.ProcessArchitecture switch
                    {
                        Arm64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-arm64.rpm"),
                        X64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-x64.rpm"),
                        _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-cpu-bypass flag.")

                    };
                }
            }

            else 
            {
                uri = RuntimeInformation.ProcessArchitecture switch
                {
                    Arm64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-arm64"),
                    X64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-x64"),
                    _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-cpu-bypass flag.")

                };
            }

            string openCMD = "xdg-open";
            try
            {

                if (string.IsNullOrEmpty(uri))
                {
                    Warning.Write($"Unable to download latest BAMM release, please visit:\n{uri}");
                    return;
                }

                if (CommandExists(openCMD))
                {
                    var psi = new ProcessStartInfo("xdg-open", uri);
                    using var Process = ProcessFactory.SpawnProcess(psi, "open new release page", runSync: true, timeout: 20).Result;
                }
            }
            catch
            {
                Warning.Write($"Unable to download latest BAMM release, please visit:\n{uri}");
            }
        }

        // https://github.com/dotnet/runtime/issues/17938#issuecomment-
        private static void OpenLatestVersionInBrowser()
        {
            try
            {
                string currentReleaseUri = Path.Combine(RELEASES_DOWNLOAD_LINK, LatestVersion);

                if (Platforms.IsWindows) {
                    OpenLatestForWindows(currentReleaseUri);
                }

                else if (Platforms.IsMacOS) {
                    OpenLatestForMacOS(currentReleaseUri);
                }

                else if (Platforms.IsLinux) {
                    OpenLatestForLinux(currentReleaseUri);
                }
            }
            
            catch (Exception e) 
            { 
                Write(
                    string.Join(NLC, [
                        $"BAM Manager (BAMM) was unable to check github for the latest version.", 
                        "If this issue persists, and you are positive your network connection is stable:", 
                        $"Please make a bug report at: {ISSUES_LINK}", 
                        $"Error Log:",
                        NLC, 
                        e.Message 
                    ])
                ); 
            }

        }

        private static async Task<bool> UpdateAvailable()
        {
            if (!HasNetworkConnection()) 
            {
                Write(
                    string.Join(' ', [
                        "BAM Manager (BAMM) was unable to check for an update,", 
                        "this likely means your system doesn't currently have an internet connection."
                    ])
                );

                string response = Input.AskForInput("\nWould you like to continue? [y/n]:\n");

                if (Input.ConditionRejected(response)) {
                    Environment.Exit(1); 
                }
                
                return false;
            }

            LatestVersion = await GetLatestVersion();

            if (string.IsNullOrEmpty(LatestVersion) || !LatestVersion.StartsWith('v')) 
            {
                WriteErrorAndReturnBool(
                    message:
                        "BAM Manager (BAMM) was unable to determine the latest release version, please check:\n" +
                        LATEST_VERSION_LINK,
                    returnBool: false
                ); 
            }
            return !string.Equals(CurrentVersion, LatestVersion, CCIC);
        }

    }
}
