using System.Diagnostics;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.DotnetHelper;
using static Publisher.PlatformSelection;

namespace Publisher 
{
    public class Packager(PlatformOption platformOption)
    {
        private readonly PlatformOption platformOption = platformOption;
        
        

        private async Task PrebuildActions() 
        {
            if (!await DotnetIsInstalled()) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to locate a dotnet SDK binary in your system path.",
                        "Please ensure the dotnet SDK is installed, and is added to your system path.",
                    ]),
                    status: 1
                );
            }

            if (!platformOption.IsValidOption()) 
            {
                WriteAndExit(
                    message: "The provided platform option is invalid.",
                    status: 1
                );
            }
        }
        
        private async Task<bool> BuildArchPackage(string workingDir) {
            await Task.Delay(1);
            return true;   
        }
        
        private async Task<bool> BuildDebianPackage(string workingDir) 
        {

            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                GetRollForwardCommand(),
                $"\"dotnet deb --runtime {platformOption.ArchitectureInfo.RID}",
                // "-v diagnostic",
                "--configuration Release -- -p:BuildDebPackage=true\"",
            ]);
            
            Warning.Write("Building Debian package, please wait...");
            return await BuildCommands(buildCommand, workingDir);
        }

        private async Task<bool> BuildFedoraPackage(string workingDir) {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                GetRollForwardCommand(),
                $"\"dotnet rpm --runtime {platformOption.ArchitectureInfo.RID}",
                // "-v diagnostic",
                "--configuration Release -- -p:BuildRpmPackage=true\"",
            ]);
            
            return await BuildCommands(buildCommand, workingDir);
        }

        private async Task<bool> BuildGentooPackage(string workingDir) {
            await Task.Delay(1);
            return true;   
        }

        private async Task<bool> BuildStandaloneBinary(string workingDir)
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                "\"dotnet publish -c Release -r",
                platformOption.ArchitectureInfo.RID,
                "--self-contained true\""
            ]);

            return await BuildCommands(buildCommand, workingDir);
        }

        private async Task<bool> BuildWindowsInstaller(string workingDir) 
        {
            await BuildStandaloneBinary(workingDir);
            // DO .ISS logic here
            return true;
        }
        private static ProcessStartInfo GetPSI(string buildCommand, string workingDir)
        {
            return new ProcessStartInfo() {
                FileName = GetShellPath(),
                Arguments = $"{GetShellArg()} {buildCommand}",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };
        }

        private static string GetRollForwardCommand()
        {
            // dotnet-deb and dotnet-rpm are still on .NET 9 as of 01/31/2026
            return Platforms.IsWindows switch {
                true => "set DOTNET_ROLL_FORWARD=Major &&",
                false => "export DOTNET_ROLL_FORWARD=Major &&", 
            };
        }

        private static async Task<bool> BuildCommands(string buildCommand, string workingDir)
        {
            var psi = GetPSI(buildCommand, workingDir);

            using var process = await ProcessFactory.SpawnProcess(psi, "attempting to package BAMM");
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);
            
            HandleInvalidExitCodeIfPresent(ExitCode, STDErr);

            Console.WriteLine(string.Join(NLC, STDOut));

            // Success.WriteSuccessMessage("Successfully built Debian package at: ");
            
            return true;

        }

        private static void HandleInvalidExitCodeIfPresent(int ExitCode, List<string> STDErr) 
        {
            if (ExitCode != 0) {

                var errorLog = (STDErr != null) switch 
                {
                    true => string.Join(NLC, STDErr),
                    false => $"the {GetWhichCommand()} returned a non zero status code: {ExitCode}"
                };

                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to locate a dotnet SDK binary in your system path.",
                        "Please ensure the dotnet SDK is installed, and is added to your system path.",
                        "Error Log:",
                        errorLog
                    ]),
                    status: 1
                );
            }
        }
        
        public async Task<bool> HandlePackaging(string desiredBuildProcess, string workingDir) 
        {
            return desiredBuildProcess switch
            {
                "Debian Package (.deb)" => await BuildDebianPackage(workingDir),
                "Fedora Package (.rpm)" => await BuildFedoraPackage(workingDir),
                "Arch Package (.pkg.tar.xz)" => await BuildArchPackage(workingDir),
                "Gentoo Package (.tbz2)" => await BuildGentooPackage(workingDir),
                "Standalone Binary" => await BuildStandaloneBinary(workingDir),
                "Windows Installer" => await BuildWindowsInstaller(workingDir),
                _ => WriteErrorAndReturnBool(
                        message: "Invalid option selected, please try again.",
                        returnBool: false
                    ),
            };
        }

        public static void SetSelectedOS(string desiredBuildProcess, out string selectedOS) 
        {
            selectedOS = string.Empty;

            switch (desiredBuildProcess) {
                case "Debian Package (.deb)":
                case "Fedora Package (.rpm)":
                case "Arch Package (.pkg.tar.xz)":
                case "Gentoo Package (.tbz2)":
                    selectedOS = "Linux";
                    break;
                
                case "Standalone Binary":
                    string[] options = [.. GetAvailableOSNames()];
                    selectedOS = Input.WriteListFromOptions(options, "operating system", pageSize: options.Length);
                    break;

                case "Windows Installer":
                    selectedOS = "Windows";
                    break;
                
                default:
                    WriteAndExit(
                        message: "Invalid option selected, please try again.", 
                        status: 1
                    );
                    break;
            }
        }

    }
}