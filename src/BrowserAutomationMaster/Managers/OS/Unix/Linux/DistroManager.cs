using System.Diagnostics;
using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers.Common;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.OS.Unix.Linux
{
    public class DistroManager() 
    {
        public readonly static Distro[] distroObjects = [.. ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true)];
        private readonly static IEnumerable<string> altCmds = distroObjects.Select(d => $"{d.BackupReleaseCmd} {d.BackupReleaseCmdArgs}");

        public readonly static string invalidDistroMessage = string.Join(NLC, [
            "Currently unable to determine the current Distribution in use.",
            "As such, BAMM does not know how to execute it's uninstallation.",
            $"Please make a bug report at: {ISSUES_LINK}"
        ]);

        private static string[] GetSupportedDistroNames() {
            return [..distroObjects.Select(a => a.Name)];
        }

        public static Distro GetDistroByName(string name) 
        {
            var distro = distroObjects.Where(distro => distro.Name.Equals(name)).FirstOrDefault();
            return distro ?? Distros.Unknown;
        }

        public static Distro DetermineDistro() 
        {
            var fileName = "/etc/os-release";
            try
            {
                var releaseFileFound = File.Exists(fileName);

                if (!releaseFileFound) 
                {
                    Warning.Write($"Warning: {fileName} was not found.");
                    return TryAltCmds() ?? Distros.Unknown;
                }

                // Optimization: Find the line starting with ID=, split by '=', and trim quotes in one pass
                var idLine = File.ReadLines(fileName)
                    .FirstOrDefault(line => line.StartsWith("ID=", OIC));

                if (string.IsNullOrEmpty(idLine)) 
                {
                    Warning.Write(
                        string.Join(NLC, [
                            "Unable to determine, the specific Linux distribution in use.",
                            "You will be prompted to select the base of your distro (Arch/Debian/Fedora/Etc)",
                            NLC,
                            $"Warning: ID field not found in: {fileName}"
                        ])
                    );
                    return Distros.Unknown;
                }

                // Sanitizing captured value (For example: ID="ubuntu" -> ubuntu)
                var sanitizedID = idLine.Split('=')[1].Trim('"').Trim('\'');

                var distroObj = distroObjects.FirstOrDefault(distro => distro.ID == sanitizedID);

                if (distroObj != null) {
                    return distroObj;
                } 
                    
                return GetUserDistroChoice();

            }

            catch (Exception ex) 
            {
                Warning.Write(
                    string.Join(NLC, [
                        "Unable to determine, the specific Linux distribution in use.",
                        "You will be prompted to select the base of your distro.",
                        NLC,
                        "Warning:",
                        ex.Message
                    ])
                );
                return Distros.Unknown;
            }
        }

        public static void CheckLinuxDistro() 
        {
            if (Platforms.CurrentDistribution != null) {
                return;
            }
            
            
            var distroChoices = EnumHelper.GetStringReprs(typeof(Distros));
            
            var distroChoice = Input.WriteListFromOptions(distroChoices, noun: "distro");
            

            var memberObject = EnumHelper.GetEnumMemberFromStringRepr(typeof(Distros), distroChoice);

            if (memberObject == null) 
            {
                WriteAndExit(
                    string.Join(' ', [
                        "Unable to determine the current Linux Distribution in use,",
                        $"please make a bug report at: {ISSUES_LINK}", 
                    ]),
                    status: 1
                );
            }

            Platforms.CurrentDistribution = (Distro)memberObject;
        }

        public static Distro GetUserDistroChoice() 
        {
            var distroNames = GetSupportedDistroNames();
                    
            // Instead of adding another Distro object to Distros
            // creating a temporary instance of Distros.Unknown
            // then replacing .Name with "Not Listed" is more efficient.
            var unsupportedDistroObj = Distros.Unknown;
            unsupportedDistroObj.Name = "Not Listed";

            var userDistroChoice = Input.WriteListFromOptions(
                distroNames, 
                "distro", 
                pageSize: distroNames.Length
            );

            if (userDistroChoice.Equals("Not Listed")) {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Currently, BAMM only supports the listed distros.",
                        $"If your distro is not currently listed, please make a bug report at {ISSUES_LINK}",
                        "Your OS will be considered in a future update."
                    ]),
                    status: 1
                );   
            }

            return GetDistroByName(userDistroChoice);
        }

        private static Distro? TryAltCmds() 
        {
            foreach (var altCmd in altCmds) 
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
                    FileName = Platforms.CurrentDistribution!.QueryCommand,
                    Arguments = $"{Platforms.CurrentDistribution.QueryArguments} {packageName}",
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
                if (status && !string.IsNullOrWhiteSpace(Platforms.CurrentDistribution!.InstallationKeyword)) {
                    status = STDOut.Any(line => line.Contains(Platforms.CurrentDistribution!.InstallationKeyword));
                }

                return (status, ExitCode, STDOut, STDErr);
                
            }

            catch (Exception ex) 
            {
                Warning.Write(
                    string.Join(NLC, [
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
    }
}