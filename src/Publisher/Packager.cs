using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.DotnetHelper;
using static Publisher.PlatformSelection;

namespace Publisher 
{
    public partial class Packager(PlatformOption platformOption)
    {
        public readonly static Dictionary<string, Regex> PackagePathRegexes = new() 
        {
            { "Debian Package", DebianPackageRegex() },
            { "Fedora Package", FedoraPackageRegex() },
            { "Standalone Binary", StandaloneBinaryRegex() },
        };

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
        
        private async Task<(bool, string?)>BuildArchPackage(string workingDir) {
            await PrebuildActions();

            await BuildStandaloneBinary(workingDir);
            // return await StartBuild(buildCommand, workingDir);
            return (true, null);
        }
        
        private async Task<(bool, string?)> BuildDebianPackage(string workingDir) 
        {

            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                GetRollForwardCommand(),
                $"dotnet deb --runtime {platformOption.ArchitectureInfo.RID}",
                // "-v diagnostic",
                "--configuration Release -- -p:BuildDebPackage=true",
            ]);
            
            Warning.Write("Building Debian package, please wait...");
            return await StartBuild(buildCommand, workingDir);
        }

        private async Task<(bool, string?)>BuildFedoraPackage(string workingDir) {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                GetRollForwardCommand(),
                $"dotnet rpm --runtime {platformOption.ArchitectureInfo.RID}",
                // "-v diagnostic",
                "--configuration Release -- -p:BuildRpmPackage=true",
            ]);
            
            return await StartBuild(buildCommand, workingDir);
        }

        private async Task<(bool, string?)> BuildGentooPackage(string workingDir) {
            await Task.Delay(1);
            return (true, null);   
        }

        private async Task<(bool, string?)> BuildStandaloneBinary(string workingDir)
        {
            await PrebuildActions();

            var buildCommand = string.Join(' ', [
                "dotnet publish -c Release -r",
                platformOption.ArchitectureInfo.RID,
                "--self-contained true"
            ]);

            return await StartBuild(buildCommand, workingDir);
        }

        private async Task<(bool, string?)> BuildWindowsInstaller(string workingDir) 
        {
            await BuildStandaloneBinary(workingDir);
            // DO .ISS logic here
            return (true, null);
        }
        private static ProcessStartInfo GetPSI(string buildCommand, string workingDir)
        {
            return new ProcessStartInfo() {
                FileName = GetShellPath(),
                Arguments = $"{GetShellArg()} \"{buildCommand}\"",
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
        
        public async Task<(bool, string?)> HandlePackaging(string desiredBuildProcess, string workingDir) 
        {
            return desiredBuildProcess switch
            {
                "Debian Package (.deb)" => await BuildDebianPackage(workingDir),
                "Fedora Package (.rpm)" => await BuildFedoraPackage(workingDir),
                "Arch Package (.pkg.tar.xz)" => await BuildArchPackage(workingDir),
                "Gentoo Package (.tbz2)" => await BuildGentooPackage(workingDir),
                "Standalone Binary" => await BuildStandaloneBinary(workingDir),
                "Windows Installer" => await BuildWindowsInstaller(workingDir),
                _ => (
                    WriteErrorAndReturnBool(
                        message: "Invalid option selected, please try again.",
                        returnBool: false
                    ), 
                    null
                )
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

        private static async Task<(bool, string?)> StartBuild(string buildCommand, string workingDir)
        {
            var psi = GetPSI(buildCommand, workingDir);
            
            using var process = await ProcessFactory.SpawnProcess(psi, "attempting to package BAMM");
            var (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);
            
            HandleInvalidExitCodeIfPresent(ExitCode, STDErr);

            // Handling STDOut ("declare x" is shown on linux systems for env vars due the use of the "which" command)
            // Console.WriteLine(string.Join(NLC, STDOut.Where(line => !line.StartsWith("declare x"))));
            
            var stdOutString = string.Join(NLC, STDOut);


            foreach ((var name, var regex) in PackagePathRegexes) 
            {
                var match = regex.Match(stdOutString);

                if (!match.Success) {
                    continue;
                }

                var capturedPaths = match.Groups.Cast<Group>()
                    .Skip(1) // Skipping the full match group (index 1)
                    .Where(g => g.Success && !string.IsNullOrWhiteSpace(g.Value))
                    .Select(g => g.Value.Trim())
                    .Distinct()
                    .ToArray();

                if (capturedPaths.Length == 0) {
                    continue;
                }

                string path;

                if (capturedPaths.Length > 1) {
                    Warning.Write("Multiple possible binary paths were detected in STDOut.");
                    path = Input.WriteListFromOptions(capturedPaths, "desired path", pageSize: capturedPaths.Length);
                } else {
                    path = capturedPaths[0];
                }

                // Handling case of standalone binary not including the binary name in the path.
                var finalPath = name switch {
                    "Standalone Binary" => Path.Combine(path, "bamm"),
                    _ => path
                };

                return (true, finalPath);
            }

            return (false, null);

        }

        
    }


    public class ArchBuild
    {
        public required string binaryPath;
        private readonly static byte[] pkgName = "pkgname=\"bamm\""u8.ToArray();
        private readonly static byte[] pkgVer = "pkgver=\"1.0.0A8\""u8.ToArray();
        private readonly static byte[] pkgDesc = "pkgdesc=\"BAM Manager (BAMM) is a Dynamic Scripting Language (DSL) that compiles into Python 3.9+ code.\""u8.ToArray();
        private readonly static byte[] arch = "arch=('x86_x64' 'aarch64', 'armv7h')"u8.ToArray();
        private readonly static byte[] license = "MIT"u8.ToArray();
        private readonly static byte[] depends = "depends=('icu' 'openssl' 'zlib' 'krb5' 'xclip')"u8.ToArray();
        private readonly static byte[] makeDepends = "makedepends=('dotnet-sdk') # Dotnet 10 is required for compilation"u8.ToArray();
        private async Task<(string, FileStream)> Sha512SumsAndStream() 
        {
            var stream = new FileStream(binaryPath, FileMode.Open);

            byte[] result = new byte[stream.Length];
            
            CancellationToken cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(30)
            ).Token;

            try 
            {
                using SHA512 sha512 = SHA512.Create();
                result = await sha512.ComputeHashAsync(stream, cts); 
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to calculate SHA512 sum of the provided binary.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }


            return (Encoding.UTF8.GetString(result), stream);
        }

        private readonly static byte[] package = """
            package() {
                
                install -Dm755 "${srcdir}/bamm" "${pkgdir}/usr/bin/bamm"
            }
            """u8.ToArray();

        /// <summary>
        /// <param name="outputPath">The path to the PKGBUILD file to be written to the system's disk.</param>
        /// <br />
        /// <returns>Returns a tuple: 
        /// <br />
        /// Item1: A boolean representing the status of the operation
        /// <br />
        /// Item2: The Stream object with the contents of the binaryPath passed to ArchBuild
        /// </returns>
        /// </summary>
        public async Task<(bool, Stream?)> WritePKGBUILDFile(string outputPath) 
        {
            var success = false;
            Stream? stream = null;
            try 
            {
                (var sha512Hash, var fileStream) = await Sha512SumsAndStream();
                var NLCBytes = Encoding.UTF8.GetBytes(NLC);
                
                var staticFields = ReflectionHelper.GetStaticFieldsOfType<byte[]>(typeof(ArchBuild), false);
                
                // Calculates sum of all field lengths + a newline for each field
                int totalLength = 0;
                foreach (var field in staticFields) {
                    totalLength += field.Length + NLCBytes.Length;
                }

                var fileContents = new byte[totalLength];
                int bytesWritten = 0;

                // Using a Span<byte> is more efficient than directly accessing the byte array.
                Span<byte> buffer = fileContents;

                foreach (var staticField in staticFields) 
                {
                    // Copies the field's content
                    staticField.CopyTo(buffer[bytesWritten..]);
                    bytesWritten += staticField.Length;

                    // Adds a new line char.
                    NLCBytes.CopyTo(buffer[bytesWritten..]);
                    bytesWritten += NLCBytes.Length;
                }

                stream = new FileStream(outputPath, System.IO.FileMode.CreateNew);
                await stream.WriteAsync(fileContents);
                success = true;
            }

            catch (Exception ex) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to write PKGBUILD file to disk.",
                        "Error Log:",
                        ex.Message ]
                    ),
                    status: 1
                );
            }

            return (success, stream);
        }

    }
}