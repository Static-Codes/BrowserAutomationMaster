using System.Diagnostics;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
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
        
        private async Task<bool> BuildArchPackage() {
            await Task.Delay(1);
            return true;   
        }

        private async Task<bool> BuildDebianPackage() 
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                $"dotnet deb --runtime {platformOption.ArchitectureInfo.RID}",
                "--configuration Release -- -p:BuildDebPackage=true",
            ]);
            
            return await BuildCommands(buildCommand);
        }

        private async Task<bool> BuildFedoraPackage() {
            await Task.Delay(1);
            return true;   
        }

        private async Task<bool> BuildGentooPackage() {
            await Task.Delay(1);
            return true;   
        }

        private async Task<bool> BuildStandaloneBinary()
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                "\"dotnet publish -c Release -r",
                platformOption.ArchitectureInfo.RID,
                "--self-contained true\""
            ]);

            return await BuildCommands(buildCommand);
        }

        private static ProcessStartInfo GetPSI(string buildCommand)
        {
            return new ProcessStartInfo() {
                FileName = GetShellPath(),
                Arguments = $"{GetShellArg()} {buildCommand}",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
        }

        private static async Task<bool> BuildCommands(string buildCommand)
        {
            var psi = GetPSI(buildCommand);

            using var process = await ProcessFactory.SpawnProcess(psi, "attempting to build the standalone binary for BAMM");
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);
            
            HandleInvalidExitCodeIfPresent(ExitCode, STDErr);
            
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
        
        public async Task<bool> HandlePackaging(string desiredBuildProcess) 
        {
            return desiredBuildProcess switch
            {
                "Debian Package (.deb)" => await BuildDebianPackage(),
                "Fedora Package (.rpm)" => await BuildFedoraPackage(),
                "Arch Package (.pkg.tar.xz)" => await BuildArchPackage(),
                "Gentoo Package (.tbz2)" => await BuildGentooPackage(),
                "Standalone Binary" => await BuildStandaloneBinary(),
                "Windows Installer" => await BuildStandaloneBinary(),
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