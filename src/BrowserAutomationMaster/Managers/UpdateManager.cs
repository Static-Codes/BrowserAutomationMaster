
using System.Diagnostics;
using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices.Marshalling;
using System.Net;

namespace BrowserAutomationMaster.Managers
{
    public class UpdateManager()
    {
        public const string CurrentVersion = "v1.0.0A3";
        public static string LatestVersion { get; set; } = CurrentVersion; // Assuming current is latest until further checks are done.
        public static void CheckForUpdate()
        {

            if (UpdateAvailable())
            {
                Warning.Write($"BAM Manager (BAMM) has an available update.\n\nCurrent Version: {CurrentVersion}\nLatest Version: {LatestVersion}\n\n");
                if ((Input.WriteTextAndReturnRawInput("Would you like to download the update now? [y/n]:\n") ?? "n").ToLower().Equals("y")){
                    OpenLatestVersionInBrowser();
                    Environment.Exit(0);
                }
            }
            else { Success.WriteSuccessMessage($"BAM Manager (BAMM) is currently running the latest release ({LatestVersion})"); }
        }

        private static string GetLatestVersion()
        {
            HttpResponseMessage response = new();
            try {
                HttpClientHandler handler = new() {
                    AllowAutoRedirect = false
                };
                using (HttpClient client = new(handler)) {
                    client.Timeout = TimeSpan.FromSeconds(20);
                    HttpRequestMessage request = new(HttpMethod.Get, "https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest");
                    //response = client.Send(request);
                    response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult(); // Only redirect header is needed.
                }
                if (response.StatusCode != HttpStatusCode.Redirect) { Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to check github for the latest version, if this issue persists, and you are positive your network connection is stable, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\nError log:\n\nThe response for the version request didn't contain a redirect status code (302), contains: {response.StatusCode}.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}"); }
                //Console.WriteLine(response.StatusCode.ToString());
            }
            catch (Exception e) {
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
            #pragma warning disable IDE0063
            #pragma warning disable IDE0079
            try { using (Ping pinger = new()) { return pinger.Send("8.8.8.8").Status == IPStatus.Success; } }
            catch (PingException) { return false; }
            #pragma warning restore IDE0063
            #pragma warning restore IDE0079
        }

        // https://github.com/dotnet/runtime/issues/17938#issuecomment-
        private static void OpenLatestVersionInBrowser()
        {
            try
            {
                string url = string.Empty;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{LatestVersion}/BAMM-{LatestVersion}-x64-Setup.exe";
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{LatestVersion}/BAMM-{LatestVersion}-ARM64-Setup.exe";
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{LatestVersion}/bamm";
                        Process.Start("open", url);
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{LatestVersion}/bamm-silicon";
                        Process.Start("open", url);
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    if (RuntimeInformation.ProcessArchitecture == Architecture.X64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/bamm.{LatestVersion}.linux-x64.deb";
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
                        url = $"https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/bamm.{LatestVersion}.linux-arm64.deb";
                        Process.Start("xdg-open", url);
                    }
                }
            }
            catch (Exception e) { Errors.WriteErrorAndContinue($"BAM Manager (BAMM) was unable to check github for the latest version, if this issue persists, and you are positive your network connection is stable, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\nError log:\n\nUnable to download latest release using the user's default browser.\n{e.Message}:\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}"); }

        }

        private static bool UpdateAvailable()
        {
            if (!HasNetworkConnection()) {
                Errors.WriteErrorAndContinue("BAM Manager (BAMM) was unable to check for an update, this likely means your system doesn't currently have an internet connection.");
                bool continuing = (Input.WriteTextAndReturnRawInput("\nWould you like to continue? [y/n]:\n") ?? "n").ToLower().Equals("y");
                if (!continuing) { Environment.Exit(1); }
                return false;
            }
            LatestVersion = GetLatestVersion();
            if (string.IsNullOrEmpty(LatestVersion)) { Errors.WriteErrorAndReturnBool("BAM Manager (BAMM) was unable to determine the latest release version, please check https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest", false); }
            return !string.Equals(CurrentVersion, LatestVersion, StringComparison.CurrentCultureIgnoreCase);
        }

    }
}
