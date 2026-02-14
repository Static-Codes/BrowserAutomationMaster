using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using static BrowserAutomationMaster.Managers.Common.RequestManager.NetworkClient;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.Build.BuildInfo;

namespace Publisher 
{
    public class DotnetHelper 
    {
        public static async Task<bool> DotnetIsInstalled() 
        {
            var psi = new ProcessStartInfo()
            {
                FileName = GetShellPath(),
                Arguments = $"{GetShellArg()} \"{GetWhichCommand()} {GetDotnetBinaryName()}\"",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            using var process = await ProcessFactory.SpawnProcess(psi, "checking for the dotnet SDK binary", timeout: 20, writeSTDInOut: false);
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);

            if (ExitCode != 0) 
            {
                var errorLog = (STDErr != null) switch {
                    true => string.Join(NLC, STDErr),
                    false => $"the {GetWhichCommand()} returned a non zero status code: {ExitCode}"
                };

                Console.WriteLine("Unable to locate a .NET SDK binary in your system path.");
                var result = Input.AskForInput("Would you like to install it now? [y/n]: ");

                if (Input.ConditionRejected(result)) {
                    WriteAndExit("Build operation cancelled, please ensure the .NET SDK is installed.", 1);
                }
            }
            
            if (STDOut.Count == 1 && STDOut[0].Contains("dotnet")) {
                return true;
            }

            return false;
            
        }


        // <summary>
        // Downloads the latest version of the .NET SDK for linux-x64
        // </summary>
        public static async Task<bool> DownloadLatestDotnetSDK() 
        {
            // ------------------------------------------
            // Start of pre-download platform validation.
            // ------------------------------------------
            if (Platforms.CurrentDistribution == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to determine the current machine's Distribution information.",
                        "Error Log:",
                        "Platforms.CurrentDistribution is null in DownloadLatestDotnetSDK()"
                    ]),
                    status: 1
                );
            }

            if (!Platforms.IsLinux) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "The current .NET SDK download logic only supports Linux-based distributions.",
                        "Error Log:",
                        "Platforms.IsLinux is false in DownloadLatestDotnetSDK()"
                    ]),
                    status: 1
                );
            }

            // ------------------------------------------
            // End of pre-download platform validation.
            // ------------------------------------------



            // ------------------------------------------
            // Start of ~/.dotnet Directory Creation.
            // ------------------------------------------
            
            string dotnetDir = string.Empty;
            try 
            {
                var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                dotnetDir = Path.Combine(userDir, ".dotnet");
            }

            catch (Exception ex)  
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to locate the current user's directory.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }

            if (!Directory.Exists(dotnetDir)) {
                EnsureDirectoryExists(dotnetDir);
            }

            // ------------------------------------------
            // End of ~/.dotnet Directory Creation.
            // ------------------------------------------



            // ------------------------------------------
            // Start of latest release determination.
            // ------------------------------------------

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            Console.WriteLine("Determining latest supported version of the .NET SDK.");
            var latestRelease = await GetLatestDotnetRelease(cts);

            if (latestRelease == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to retrieve the manifest contents for .NET releases, please try again.",
                        "Error Log:",
                        "latestSDKVersion is null"
                    ]),
                    status: 1
                );
            }

            Success.WriteSuccessMessage("Operation successful.");
            Success.WriteSuccessMessage($"Latest SDK Release Found: {latestRelease.LatestSDKVersion}");

            // ------------------------------------------
            // End of latest release determination.
            // ------------------------------------------



            // ------------------------------------------
            // Start of variable declaration
            // ------------------------------------------

            var baseDomain = string.Concat([
                "https://builds.dotnet.microsoft.com/dotnet/Sdk/",
                latestRelease.LatestSDKVersion,
                "/"
            ]);

            var sdkFileName = string.Concat([
                "dotnet-sdk-",
                latestRelease.LatestSDKVersion,
                "-linux-x64.tar.gz"
            ]);

            // Link to the latest version of the .NET SDK supported by BAMM
            var directDownloadLink = $"{baseDomain}{sdkFileName}";
            var prettyFileName = $".NET SDK {latestRelease.LatestSDKVersion}";

            // ------------------------------------------
            // End of variable declaration
            // ------------------------------------------
            


            try 
            {
                // ------------------------------------------
                // Start of SDK content retrieval.
                // ------------------------------------------

                Console.WriteLine($"Downloading the {prettyFileName}");
                Console.WriteLine($"File Location: {directDownloadLink}");

                using var tarballStream = await Instance.GetStreamAsync(directDownloadLink, cts.Token);
            
                if (tarballStream == null) 
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"Unable to retrieve the tarball contents for the {prettyFileName}, please try again.",
                            "Error Log:",
                            "tarballStream is null"
                        ]),
                        status: 1
                    );
                }
                Success.WriteSuccessMessage($"Retrieved the contents for the {prettyFileName}");

                // ------------------------------------------
                // End of SDK content retrieval.
                // ------------------------------------------



                // ------------------------------------------
                // Start of Stream to FileStream Conversion
                // ------------------------------------------

                Console.WriteLine("Converting the generic Stream object to the more versatile FileStream object.");
                Warning.Write("This file is ~200MB, this may take a few minutes.");
                var tempFile = Path.Combine(dotnetDir, sdkFileName);
                using var outputStream = new FileStream(tempFile, FileMode.Create, FileAccess.ReadWrite);
                await tarballStream.CopyToAsync(outputStream);
                Success.WriteSuccessMessage("Conversion successful.");

                // ------------------------------------------
                // End of Stream to FileStream Conversion
                // ------------------------------------------



                // ------------------------------------------
                // Start of expected hash retrieval.
                // ------------------------------------------

                Console.WriteLine("Retrieving the SHA512 hash provided by Microsoft for the downloaded archive.");
                var latestManifestLink = latestRelease.ReleaseManifestUri;
                var SDKHash = await GetOfficialReleaseHash(latestRelease, cts, directDownloadLink);

                if (SDKHash == null) 
                {
                    var releaseString = $".NET {latestRelease.LatestSDKVersion}";
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"Unable to retrieve the SHA512 Hash for the for {releaseString} SDK, please try again.",
                            "Error Log:",
                            "SDKHash is null"
                        ]),
                        status: 1
                    );
                }
                Success.WriteSuccessMessage("Operation successful.");
                
                // ------------------------------------------
                // End of expected hash retrieval.
                // ------------------------------------------



                // ------------------------------------------
                // Start of archive hash calculation.
                // ------------------------------------------

                Console.WriteLine("Calculating the SHA512 hash of the downloaded archive for comparison.");
                (var calculatedHash, _) = await CalculateSHA512HashOfFile(outputStream);
                Success.WriteSuccessMessage("Operation successful.");
                Thread.Sleep(300);

                // ------------------------------------------
                // End of archive hash calculation.
                // ------------------------------------------



                // ------------------------------------------
                // Start of validation and sdk extraction.
                // ------------------------------------------

                Console.WriteLine("Validating the calculated hash against the expected hash.");
                Console.WriteLine();
                Thread.Sleep(100);

                Warning.Write($"Expected: {SDKHash}");

                if (SDKHash.Equals(calculatedHash)) {
                    Success.WriteSuccessMessage($"Received: {calculatedHash}");
                    Thread.Sleep(200);
                    Success.WriteSuccessMessage("The downloaded archive has passed hash validation.");

                    Console.WriteLine("Extracting archive to: ~/.dotnet/");
                    return ArchiveManager.UnarchiveTarball(outputStream, dotnetDir);
                }

                Write($"Received: {calculatedHash}"); // Displaying red to indicate an issue is present.
                Thread.Sleep(200);
                Console.WriteLine();
                    
                Warning.Write("The calculated hash did not match the expected hash, as such the downloaded archive will be deleted.");

                Console.WriteLine("Deleting the downloaded archive.");
                Thread.Sleep(200);
                File.Delete(tempFile);
                Success.WriteSuccessMessage("Operation successful.");

                Console.WriteLine("Please run the following commands to download the latest version of the .NET SDK:");
                Console.WriteLine();

                Console.WriteLine("wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh");
                Console.WriteLine("chmod +x ./dotnet-install.sh");
                Console.WriteLine("./dotnet-install.sh --channel 10.0");
                Console.WriteLine("rm dotnet-install.sh");

                // ------------------------------------------
                // End of hash validation.
                // ------------------------------------------

            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to download the latest .NET release, please try again.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }

            return false;
        }

        /// <summary>
        /// <returns>Returns the name of the .NET SDK binary assuming it's in the system path.</returns>
        /// </summary>
        public static string GetDotnetBinaryName()
        {
            return Platforms.IsWindows switch {
                true => "dotnet.exe",
                false => "dotnet"
            };
        }
        

        /// <summary>
        /// <returns>
        /// Returns:
        /// Returns a DotnetRelease object of the latest .NET SDK for the currently supported version.
        /// </returns>
        /// </summary>
        private static async Task<DotnetRelease?> GetLatestDotnetRelease(CancellationTokenSource cts) 
        {
            var releaseManifestUri = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json";
            DotnetReleaseInfo? DotnetReleases = null;
            
            try {
                DotnetReleases = await Instance.GetFromJsonAsync<DotnetReleaseInfo>(releaseManifestUri, cts.Token);
            }
            catch (Exception ex) {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Operation timed out while attempting to determine the latest version of the .NET SDK, please try again.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }

            if (DotnetReleases == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to retrieve the manifest contents for .NET releases, please try again.",
                        "Error Log:",
                        "DotnetReleases is null"
                    ]),
                    status: 1
                );
            }

            var targetChannel = "10.0"; // .NET SDK 10.X
            var supportPhase = "active"; // Ensuring only an active release is targetted.

            // Returning the latest version of the targetChannel above, a null check is required after execution.
            return 
                DotnetReleases.Releases
                .FirstOrDefault(
                    release => 
                    release.ChannelVersion.Equals(targetChannel) &&
                    release.SupportPhase.Equals(supportPhase)
                );
            
        }
        
        /// <summary>
        /// <returns>
        /// Returns:
        /// Returns a DotnetRelease object of the latest .NET SDK for the currently supported version.
        /// </returns>
        /// </summary>
        private static async Task<string?> GetOfficialReleaseHash(DotnetRelease DotnetRelease, CancellationTokenSource cts, string SDKDownloadUri) 
        {
            DotnetReleaseManifest? releaseManifest = null;
            var expectedHash = string.Empty;
            
            try {
                releaseManifest = await Instance.GetFromJsonAsync<DotnetReleaseManifest>(
                    DotnetRelease.ReleaseManifestUri, 
                    cts.Token
                );
            }
            catch (Exception ex) 
            {
                Write
                (
                    string.Join(NLC, [
                        "An exception occured while attempting to retrive the SHA512 hash of the downloaded .NET SDK, please try again.",
                        "Error Log:",
                        ex.Message
                    ])
                );
            }

            if (releaseManifest == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to retrieve the SHA512 hash of the downloaded .NET SDK, please try again.",
                        "Error Log:",
                        "releaseManifest is null"
                    ]),
                    status: 1
                );
            }

            // Returning the latest version of the targetChannel above, a null check is required after execution.
            var latestRelease = releaseManifest.Releases.FirstOrDefault();

            if (latestRelease == null) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to retrieve the SHA512 hash of the downloaded .NET SDK, please try again.",
                        "Error Log:",
                        "releaseManifest.Releases is null"
                    ]),
                    status: 1
                );
            }

            // Finding the file that matches the link of the archive previously downloaded.
            var SDKArchiveFile = 
                latestRelease.Sdk.Files
                .FirstOrDefault(file => file.SDKUrl.Equals(SDKDownloadUri));
            
            return SDKArchiveFile?.SDKHash;

            
        }

        /// <summary>
        /// <returns>Returns the path to the system's shell.</returns>
        /// </summary>
        public static string GetShellPath() 
        {
            return Platforms.IsWindows switch {
                true => "cmd.exe",
                false => "/bin/bash"
            };
        }


        /// <summary>
        /// <returns>Returns the argument for the system's shell to interpret the following text as commands.</returns>
        /// </summary>
        public static string GetShellArg() 
        {
            return Platforms.IsWindows switch {
                true => "/c",
                false => "-c",
            };
        }


        /// <summary>
        /// <returns>Returns the "which" or "where" command assuming it's in the system path.</returns>
        /// </summary>
        public static string GetWhichCommand()
        {
            return Platforms.IsWindows switch {
                true => "where.exe",
                false => "which"
            };
        }

        
    }

    public class DotnetReleaseInfo
    {
        [JsonPropertyName("releases-index")]
        public required List<DotnetRelease> Releases { get; set; }
    }

    public class DotnetRelease
    {
        [JsonPropertyName("channel-version")]
        public required string ChannelVersion { get; set; }

        [JsonPropertyName("latest-release")]
        public required string LatestReleaseVersion { get; set; }
        
        public bool Security { get; set; }

        [JsonPropertyName("latest-runtime")]
        public required string LatestRuntimeVersion { get; set; }

        [JsonPropertyName("latest-sdk")]
        public required string LatestSDKVersion { get; set; }

        [JsonPropertyName("support-phase")]
        public required string SupportPhase { get; set; }

        [JsonPropertyName("releases.json")]
        public required string ReleaseManifestUri { get; set; }
    }

    public class DotnetReleaseManifest
    {
        [JsonPropertyName("releases")]
        public required List<ReleaseInfo> Releases { get; set; }
    }

    public class ReleaseInfo
    {
        [JsonPropertyName("sdk")]
        public required SDKObject Sdk { get; set; }
    }

    public class SDKObject
    {
        [JsonPropertyName("version")]
        public required string Version { get; set; }

        [JsonPropertyName("files")]
        public required List<SDKInfo> Files { get; set; }
    }

    public class SDKInfo
    {
        [JsonPropertyName("rid")]
        public required string RID { get; set; } 

        [JsonPropertyName("url")]
        public required string SDKUrl { get; set; }

        [JsonPropertyName("hash")]
        public required string SDKHash { get; set; }
    }
}