using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;
using System.Net;
using BrowserAutomationMaster.Managers.AppManager.OS;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers
{
    public class ConstantManager
    {
        public const string BASE_REPO_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/";
        public const string DOCUMENTATION_LINK = "https://static-codes.github.io/BrowserAutomationMaster/";
        public const string ISSUES_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/issues";
        public const string LATEST_VERSION_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest";
        public const string RELEASES_DOWNLOAD_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/download";
        public const string BROWSER_STACK_LINK = "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/AppData/browserstack.json";
        public const StringComparison CCIC = StringComparison.CurrentCultureIgnoreCase;
        public const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
    }

    public class UpdateManager()
    {
        public const string CurrentVersion = "v1.0.0A4";
        // Assuming current is latest until further checks are done.
        public static string LatestVersion { get; set; } = CurrentVersion; 
        public static async Task<bool> CheckForUpdate()
        {
            if (await UpdateAvailable())
            {
                Warning.Write(
                    $"BAM Manager (BAMM) has an available update.\n\n" +
                    $"Current Version: {CurrentVersion}\n" +
                    $"Latest Version: {LatestVersion}\n\n"
                );
                string response = Input.WriteTextAndReturnRawInput(
                    "Would you like to download the update now? [y/n]:\n"
                ) ?? "n";

                if (response.ToLower().Equals("y")) {
                    OpenLatestVersionInBrowser();
                    Environment.Exit(0);
                }
                return true;
                
            }
            else { 
                Success.WriteSuccessMessage(
                    $"BAM Manager (BAMM) is currently running the latest release ({LatestVersion})"
                );
                return true;
            }
        }

        private static async Task<string> GetLatestVersion()
        {
            HttpResponseMessage? response = new();
            try
            {
                bool uriCreated = Uri.TryCreate(
                    $"{LATEST_VERSION_LINK}", 
                    UriKind.Absolute, 
                    out Uri? uriResult
                );

                if (!uriCreated || uriResult == null) 
                    return string.Empty;

                RequestManager requestManager = RequestManager.Create(uriResult);
                response = await requestManager.GetAsync(followRedirects: false);
                
                if (response == null)
                    return string.Empty;

                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    Errors.WriteErrorAndContinue(
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
                Errors.WriteErrorAndReturnEmptyString(
                    $"BAM Manager (BAMM) was unable to check github for the latest version.\n" +
                    $"If this issue persists, " +
                    $"please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\n{e.Message}"
                );
            }

            // This should never actually be hit but the line below has a "Dereference of possibly null reference" error so im leaving this in"
            if (response == null)
                return string.Empty;

            string url = 
                response.Headers.Location != null ? response.Headers.Location.AbsoluteUri : string.Empty;

            int versionIndex = url.LastIndexOf('/');

            if (versionIndex == -1) { 
                Errors.WriteErrorAndContinue(
                    message: 
                    $"BAM Manager (BAMM) was unable to check github for the latest version, " +
                    $"if this issue persists, and you are positive your network connection is stable, " +
                    $"please make a bug report at {ISSUES_LINK}\n" +
                    $"Error log:\n\n" +
                    $"Unable to parse version from latest release response."
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
            string url = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => Path.Combine(currentReleaseUri, $"BAMM-{LatestVersion}-ARM64-Setup.exe"),
                Architecture.X64 => Path.Combine(currentReleaseUri, $"BAMM-{LatestVersion}-x64-Setup.exe"),
                _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-bypass flag.")
            };

            var psi = new ProcessStartInfo("cmd", $"/c start {url}")
            {
                CreateNoWindow = true
            };

            Process.Start(psi);
        }
        
        private static void OpenLatestForMacOS(string currentReleaseUri)
        {
            string url = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.Arm64 => Path.Combine(currentReleaseUri, "bamm-silicon"),
                Architecture.X64 => Path.Combine(currentReleaseUri, "bamm"),
                _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-bypass flag.")
            };
            Process.Start("open", url);
        }

        private static void OpenLatestForLinux(string currentReleaseUri)
        {
            string choice = Input.WriteListFromOptions(["Debian Based", "Fedora Based", "Other"], noun: "distro");

            string? uri = null;

            if (choice == "Debian Based")
            {
                uri = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.Arm64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-arm64.deb"),
                    Architecture.X64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-x64.deb"),
                    _ => throw new PlatformNotSupportedException("Unsupported CPU architecture, try running BAMM on linux with the --linux-cpu-bypass flag.")

                };
            }

            else if (choice == "Fedora Based")
            {
                uri = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.Arm64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-arm64.rpm"),
                    Architecture.X64 => Path.Combine(currentReleaseUri, $"bamm.{LatestVersion}.linux-x64.rpm"),
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

                if (Linux.CommandExists(openCMD))
                    Process.Start("xdg-open", uri);
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

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    OpenLatestForWindows(currentReleaseUri);

                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    OpenLatestForMacOS(currentReleaseUri);

                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    OpenLatestForLinux(currentReleaseUri);
            }
            catch (Exception e) { Errors.WriteErrorAndContinue(
                message:
                    $"BAM Manager (BAMM) was unable to check github for the latest version.\n" + 
                    "If this issue persists, and you are positive your network connection is stable, " + 
                    $"please make a bug report at:\n{ISSUES_LINK}\n" + 
                    $"Error log:\n\n" +
                    $"Unable to download latest release using the user's default browser.\n{e.Message}:" +
                ""); 
            }

        }

        private static async Task<bool> UpdateAvailable()
        {
            if (!HasNetworkConnection()) {
                Errors.WriteErrorAndContinue(
                    message:
                        "BAM Manager (BAMM) was unable to check for an update, " +
                        "this likely means your system doesn't currently have an internet connection."
                );

                string response = Input.WriteTextAndReturnRawInput("\nWould you like to continue? [y/n]:\n");

                if (response.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
                    Environment.Exit(1); 
                
                return false;
            }
            LatestVersion = await GetLatestVersion();

            if (string.IsNullOrEmpty(LatestVersion) || !LatestVersion.StartsWith('v')) {
                Errors.WriteErrorAndReturnBool(
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
