
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;
using System.Net;

namespace BrowserAutomationMaster.Managers
{
    public class ConstantManager
    {
        public const string ISSUES_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/issues";
        public const string LATEST_VERSION_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest";
        public const string RELEASES_DOWNLOAD_LINK = "https://github.com/Static-Codes/BrowserAutomationMaster/releases/download";
    }

    public class UpdateManager()
    {
        public const string CurrentVersion = "v1.0.0A4";
        public static string LatestVersion { get; set; } = CurrentVersion; // Assuming current is latest until further checks are done.
        public static void CheckForUpdate()
        {
            if (UpdateAvailable())
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
                
            }
            else { 
                Success.WriteSuccessMessage(
                    $"BAM Manager (BAMM) is currently running the latest release ({LatestVersion})"
                ); 
            }
        }

        private static string GetLatestVersion()
        {
            HttpResponseMessage response = new();
            try
            {
                if (!Uri.TryCreate("https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest", UriKind.Absolute, out Uri? uriResult) || uriResult == null) 
                { 
                    return string.Empty; 
                }

                RequestManager requestManager = RequestManager.Create(uriResult);
                Task<HttpResponseMessage> responseTask = requestManager.GetAsync(followRedirects: false);
                response = responseTask.GetAwaiter().GetResult();
                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to check github for the latest version, if this issue persists, and you are positive your network connection is stable, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\nError log:\n\nThe response for the version request didn't contain a redirect status code (302), contains: {response.StatusCode}.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}");
                }
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to check github for the latest version, if this issue persists, and you are positive your network connection is stable, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\nError log:\n{e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}");
            }
            string url = response.Headers.Location != null ? response.Headers.Location.AbsoluteUri : string.Empty;
            int versionIndex = url.LastIndexOf('/');

            if (versionIndex == -1) { Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to check github for the latest version, if this issue persists, and you are positive your network connection is stable, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\nError log:\n\nUnable to parse version from latest release response.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}"); }
            else if (versionIndex < url.Length - 1) { return url[(versionIndex + 1)..]; }
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

        // https://github.com/dotnet/runtime/issues/17938#issuecomment-
        private static void OpenLatestVersionInBrowser()
        {
            try
            {
                string currentReleasePath = Path.Combine(ConstantManager.RELEASES_DOWNLOAD_LINK, LatestVersion);
                string url = string.Empty;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = Path.Combine(currentReleasePath, $"BAMM-{LatestVersion}-x64-Setup.exe");
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = Path.Combine(currentReleasePath, $"BAMM-{LatestVersion}-ARM64-Setup.exe");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                    }
                    else { throw new PlatformNotSupportedException("Invalid OS"); }
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = Path.Combine(currentReleasePath, "bamm");
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = Path.Combine(currentReleasePath, "bamm-silicon");
                    }
                    else { throw new PlatformNotSupportedException("Invalid OS"); }
                    Process.Start("open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = Path.Combine(currentReleasePath, $"bamm.{LatestVersion}.linux-x64.deb");
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = Path.Combine(currentReleasePath, $"bamm.{LatestVersion}.linux-arm64.deb");
                    }
                    Process.Start("xdg-open", url);
                }
            }
            catch (Exception e) { Errors.WriteErrorAndContinue(
                $"BAM Manager (BAMM) was unable to check github for the latest version.\n" + 
                "If this issue persists, and you are positive your network connection is stable, " + 
                $"please make a bug report at:\n{ConstantManager.ISSUES_LINK}\n" + 
                $"Error log:\n\n" +
                $"Unable to download latest release using the user's default browser.\n{e.Message}:" +
                "\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}"); }

        }

        private static bool UpdateAvailable()
        {
            if (!HasNetworkConnection()) {
                Errors.WriteErrorAndContinue(
                    "BAM Manager (BAMM) was unable to check for an update, " +
                    "this likely means your system doesn't currently have an internet connection."
                );
                string response = Input.WriteTextAndReturnRawInput("\nWould you like to continue? [y/n]:\n") ?? "n";
                if (response.ToLower().Equals("y")) { Environment.Exit(1); }
                return false;
            }
            LatestVersion = GetLatestVersion();
            if (string.IsNullOrEmpty(LatestVersion) || !LatestVersion.StartsWith('v')) {
                Errors.WriteErrorAndReturnBool(
                    "BAM Manager (BAMM) was unable to determine the latest release version, please check:" +
                    ConstantManager.LATEST_VERSION_LINK,
                    false
                ); 
            }
            return !string.Equals(CurrentVersion, LatestVersion, StringComparison.CurrentCultureIgnoreCase);
        }

    }
}
