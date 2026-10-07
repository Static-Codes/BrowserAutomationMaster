using System.Diagnostics;
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Types.Linux;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using static WhichDistroSharp.WhichDistroSharp;

namespace BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux
{
    public class DistroManager() 
    {
        public readonly static Distro[] distroObjects = [.. ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true)];
        private readonly static IEnumerable<string> alternativeCommands = distroObjects.Select(d => $"{d.BackupReleaseCmd} {d.BackupReleaseCmdArgs}");

        /// <summary>
        /// The upstream distribution each supported entry claims. Built once, so the many to one
        /// collapse (Debian answers for Raspbian, openSUSE for Leap and Tumbleweed) is one lookup.
        /// </summary>
        private readonly static Dictionary<WhichDistro, Distro> distroMapping = BuildDistroMapping();

        public readonly static string invalidDistroMessage = string.Join(Environment.NewLine, [
            "Currently unable to determine the current Distribution in use.",
            "As such, BAMM does not know how to execute it's uninstallation.",
            $"Please make a bug report at: {ISSUES_LINK}"
        ]);

        private static Dictionary<WhichDistro, Distro> BuildDistroMapping()
        {
            var lookup = new Dictionary<WhichDistro, Distro>();

            foreach (var distro in distroObjects)
            {
                foreach (var supported in distro.SupportedDistros)
                {
                    if (!lookup.TryAdd(supported, distro))
                    {
                        // Execution fails here, as the host platform was unable to be detected.
                        throw new InvalidOperationException(
                            $"The distro '{supported}' is claimed by both '{lookup[supported].Name}' and " +
                            $"'{distro.Name}'. Each supported distro may belong to only one entry."
                        );
                    }
                }
            }

            return lookup;
        }

        public static void CheckLinuxDistro() 
        {
            if (GlobalUserInfo.PlatformInfo.CurrentDistribution != null) {
                return;
            }
            
            GlobalUserInfo.PlatformInfo.CurrentDistribution = GetUserDistroChoice(GlobalUserInfo.PlatformInfo.CurrentPlatform);
        }

        /// <summary>
        /// Resolves an upstream distro to a supported BAMM entry, without querying the active fs.
        /// </summary>
        /// <param name="detectedDetected">The distribution reported by WhichDistroSharp.</param>
        /// <returns>
        /// The associated entry, or null if BAMM does not declare support for it, in the form of an entry to .
        /// </returns>
        public static Distro? Resolve(WhichDistro detectedDetected)
        {
            return distroMapping.TryGetValue(detectedDetected, out var distro) ? distro : null;
        }

        public static Distro DetermineDistro() 
        {
            // Detect() and DetectPlatform() read the same file and agree on the result. 
            // The first call "which distro", the second "which distro, and what else does it say".
            var platform = DetectPlatform();

            GlobalUserInfo.PlatformInfo.CurrentPlatform = platform;

            var detected = Detect();

            if (!detected.WasFound()) {
                Warning.Write(
                    string.Join(Environment.NewLine, [
                        "Unable to determine the specific Linux distribution in use.",
                        "Falling back to the alternative detection methods.",
                        Environment.NewLine,
                        "Warning: No matching ID field was found in /etc/os-release or /usr/lib/os-release"
                    ])
                );
                return TryAlternativeCommands() ?? Distros.Unknown;
            }

            return Resolve(detected) ?? GetUserDistroChoice(platform);
        }

        public static Distro GetDistroByName(string name) 
        {
            var distro = distroObjects.Where(distro => distro.Name.Equals(name)).FirstOrDefault();
            return distro ?? Distros.Unknown;
        }


        /// <summary>
        /// Checks if the provided package is installed on the current distro<br/>
        /// <param name="packageName">The package to check</param><br/>
        /// <returns>
        /// Returns:
        /// status: A boolean representing the installation status, true means installed.<br/>
        /// ExitCode: An integer representing the exit code returned by the process invoked.<br/>
        /// STDOut: A list of strings representing the lines from standard output.<br/>
        /// STDErr: A list of strings representing the lines from standard error.<br/>
        /// </returns>
        /// </summary>
        public static async Task<(bool status, int ExitCode, List<string> STDOut, List<string> STDErr)> GetPackageStatus(string packageName) 
        {
            try 
            {
                var psi = new ProcessStartInfo() 
                {
                    FileName = GlobalUserInfo.PlatformInfo.CurrentDistribution!.QueryCommand,
                    Arguments = $"{GlobalUserInfo.PlatformInfo.CurrentDistribution.QueryArguments} {packageName}",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = await ProcessFactory.SpawnProcess(
                    psi, 
                    processAction: $"check the installation status of the package: {packageName}", 
                    timeout: 30,
                    writeSTDInOut: false
                );

                var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);
                
                bool status = ExitCode == 0;
                

                // If a keyword check is required.
                if (status && !string.IsNullOrWhiteSpace(GlobalUserInfo.PlatformInfo.CurrentDistribution!.InstallationKeyword)) {
                    status = STDOut.Any(line => line.Contains(GlobalUserInfo.PlatformInfo.CurrentDistribution!.InstallationKeyword));
                }

                return (status, ExitCode, STDOut, STDErr);
                
            }

            catch (Exception ex) 
            {
                Warning.Write(
                    string.Join(Environment.NewLine, [
                        $"A non fatal exception occured while querying the installation status of the package: {packageName}",
                        "Error Log:",
                        ex.Message
                    ])
                );
            }

            return (
                status: false, 
                ExitCode: -1, 
                STDOut: [], 
                STDErr: []
            );
        }

        private static string[] GetSupportedDistroNames() {
            return [..distroObjects.Select(a => a.Name)];
        }

        public static Distro GetUserDistroChoice(IPlatform? platform = null) 
        {
            var distroNames = GetSupportedDistroNames();

            if (platform != null) {
                var detected = string.Join(' ', [
                    platform.PrettyName,
                    string.IsNullOrWhiteSpace(platform.VersionId) ? "" : platform.VersionId,
                    string.IsNullOrWhiteSpace(platform.Id) ? "" : $"(ID: {platform.Id})"
                ]).Trim();

                Warning.Write(
                    string.Join(Environment.NewLine, [
                        $"Detected: {detected}",
                        "BAMM has no support entry for this distribution. Select the closest base below:",
                        Environment.NewLine
                    ])
                );
            }

            var userDistroChoice = Input.WriteListFromOptions(
                distroNames, 
                "distro", 
                pageSize: distroNames.Length
            );

            return GetDistroByName(userDistroChoice);
        }
        
        public static async Task<HashSet<string>> FindMissingPackages(string[] packageNames)
        {
            var missingPackages = new HashSet<string>();
            foreach (var packageName in packageNames) 
            {
                (bool status, _, _, _) = await GetPackageStatus(packageName);

                if (!status) {
                    missingPackages.Add(packageName);
                }
            }
            return missingPackages;
        } 

        private static Distro? TryAlternativeCommands() 
        {
            foreach (var altCmd in alternativeCommands) 
            {
                try 
                {
                    (var output, var error) = RunCommand("/bin/bash", $"-c '{altCmd}'");
                    if (output == null || error != null) {
                        continue;
                    }
                    
                    switch (output) {
                        case "FreeBSD" when altCmd is "uname -o":
                            return Distros.FreeBSD;
                    }
                }
                catch (Exception ex) 
                {
                    Warning.Write(ex.Message);
                }
            }
            return null;
        }
    
    
        
    }
}
