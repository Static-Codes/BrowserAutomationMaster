using System.Diagnostics;
using BrowserAutomationMaster.Managers;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.DotnetHelper;

namespace Publisher 
{
    public class Packager(PlatformOption platformOption)
    {
        private readonly PlatformOption platformOption = platformOption;
        
        public Dictionary<string, Task<bool>> GeneratePackagingOptions() 
        {
            return new() 
            {
                { "Debian Package (.deb)", BuildDebianPackage() },
                { "Fedora Package (.rpm)", BuildFedoraPackage() },
                { "Arch Package (.pkg.tar.xz)", BuildArchPackage() },
                { "Gentoo Package (.tbz2)", BuildGentooPackage() },
                { "Standalone Binary", BuildStandaloneBinary() },
            };
        }

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
        
        public async Task<bool> BuildArchPackage() {
            await Task.Delay(1);
            return true;   
        }

        public async Task<bool> BuildDebianPackage() 
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                $"dotnet deb --runtime {platformOption.ArchitectureInfo.RID}",
                "--configuration Release -- -p:BuildDebPackage=true",
            ]);
            
            return await BuildCommands(buildCommand);
        }

        public async Task<bool> BuildFedoraPackage() {
            await Task.Delay(1);
            return true;   
        }

        public async Task<bool> BuildGentooPackage() {
            await Task.Delay(1);
            return true;   
        }

        public async Task<bool> BuildStandaloneBinary()
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                "\"dotnet publish -c Release -r",
                platformOption.ArchitectureInfo.RID,
                "--self-contained true\""
            ]);

            return await BuildCommands(buildCommand);
        }

        private ProcessStartInfo GetPSI(string buildCommand)
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

        private async Task<bool> BuildCommands(string buildCommand)
        {
            var psi = GetPSI(buildCommand);

            using var process = await ProcessFactory.SpawnProcess(psi, "attempting to build the standalone binary for BAMM");
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);
            
            HandleInvalidExitCodeIfPresent(ExitCode, STDErr);
            
            return true;

        }
        private void HandleInvalidExitCodeIfPresent(int ExitCode, List<string> STDErr) 
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


    }
}