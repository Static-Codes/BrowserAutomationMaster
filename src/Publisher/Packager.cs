
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using BrowserAutomationMaster.Messaging;
using Publisher.Build.Processes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.DistroManager;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.Functions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Managers.UnixFilePermissionManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.Build.BuildInfo;
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
        

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Platforms.IsUnixLike handles checks.")]
        private async Task<(bool, string?)> BuildArchPackage(string workingDir, string appVersion) 
        {
            bool[] invalidStates = [
                !Platforms.CurrentDistribution!.BaseDistro.Equals(DistroBase.ArchLinux),
                !Platforms.CurrentDistribution!.BaseDistro.Equals(DistroBase.Debian)
            ];

            // If both cases are true, execution haults, and an exception is thrown.
            if (invalidStates.All(state => state)) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Packaging for Arch is only available on Arch and Debian Based distros, please pick another option."
                    ]),
                    status: 1 
                );
            }
            
            await PrebuildActions();

            (var compilationStatus, var compiledBinaryPath) = await BuildStandaloneBinary(workingDir);

            if (!compilationStatus || compiledBinaryPath == null) {
                WriteAndExit("Binary compilation failed, please try again.", 1);
            }

            var archBuild = new ArchBuild() {
                binaryPath = compiledBinaryPath
            };

            var sourceBuildsDir = GetSourceBuildsDirectory();
            

            var archBuildDir = Path.Combine(sourceBuildsDir, "arch");
            
            // Deleting any previous builds to prevent unexpected behavior
            if (Directory.Exists(archBuildDir)) {
                Directory.Delete(archBuildDir, true);
            }

            // Dont include this in the refactoring of EnsureDirectoryExists usage
            EnsureDirectoryExists(archBuildDir);

            var PKGBUILDPath = Path.Combine(archBuildDir, "PKGBUILD");

            var finalBinaryPath = Path.Combine(archBuildDir, "bamm");

            (var pkgBuildStatus, var binaryStream) = await archBuild.WritePKGBUILDFile(PKGBUILDPath, appVersion);

            if (!pkgBuildStatus || binaryStream == null) {
                WriteAndExit("Failed to write PKGBUILD to disk.", 1);
            }

            if (binaryStream.CanSeek) {
                binaryStream.Position = 0;
            }

            try 
            {   
                using var finalBinaryStream = new FileStream(
                    finalBinaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 4096,
                    useAsync: true
                );

                await binaryStream.CopyToAsync(finalBinaryStream);
                await finalBinaryStream.FlushAsync();

                var requiresPermissions = !HasExecutablePermissions(finalBinaryPath);

                // If the binary has executable permissions, this operation is skipped.
                // If Linux's glibc or Apple's libc fail to give the binary executable permissions
                // Another attempt is made using .NET's builtin UnixFileMode
                if (requiresPermissions && !SetExecutablePermissions(finalBinaryPath))
                {
                    // Equivalent to 0755 (-rwxr-xr-x)
                    var unixFileMode = (
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                        UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                    );

                    // This may throw an exception if the application was downloaded as root.
                    File.SetUnixFileMode(finalBinaryPath, unixFileMode);
                }
            }    

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, 
                    [
                        $"Error building Arch Package",
                        "Error Log:",
                        ex.Message
                    ]), 
                    status: 1
                );
            }

            finally 
            {
                // Since the stream is no longer needed it can be disposed of safely.
                await binaryStream.DisposeAsync();
            }

            // Fun Fact:
            // sudo apt install pacman
            // Does infact NOT install the package manager "pacman", it installs a pacman game.
            // I was doing a test for Arch Packaging on Debian, the game popped up leaving me very confused.
            string[] packages = 
                Platforms.CurrentDistribution.BaseDistro.Equals(DistroBase.ArchLinux) ? 
                ["pacman", "makepkg"] : // Arch
                ["pacman-package-manager", "makepkg", "libarchive-tools"]; // Debian
            
            var missingPackages = await FindMissingPackages(packages);

            bool needsAptCacheRefresh = 
                Platforms.CurrentDistribution.BaseDistro.Equals(DistroBase.Debian) &&
                missingPackages.Contains("libarchive-tools");
            
            if (needsAptCacheRefresh) {
                RefreshDebianAptCache();
            }

            InstallMissingPackages([.. missingPackages]);

            var psi = new ProcessStartInfo() {
                FileName = "makepkg",
                Arguments = "-s --nodeps",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = archBuildDir
            };

            using var process = await ProcessFactory.SpawnProcess(psi, "package bamm as an arch package");
            (var ExitCode, var STDOut, var STDErr) = await ProcessFactory.GetProcessResponse(process);

            if (ExitCode != 0) {
                Write($"Failed to package bamm due to a non zero status code: {ExitCode}");
                return (false, "Not Built.");
            }

            var pkgDir = Path.Combine(archBuildDir, "pkg");

            if (!Directory.Exists(pkgDir)) {
                Write($"Failed to locate package directory: {pkgDir}");
                return (false, "Not Built.");
            }

            var fileName = string.Concat("bamm-", appVersion, "-1-x86_64.pkg.tar.gz");
            var filePath = Path.Combine(archBuildDir, fileName);

            return (true, filePath);
        }
        
        private async Task<(bool, string?)> BuildAltLinuxOSPackage(string workingDir) {
            Write("BuildAltLinuxOSPackage is not implemented, dont forget to fix this before the next release!");
            await Task.Delay(1);
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

        private async Task<(bool, string?)> BuildFedoraPackage(string workingDir) {
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

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Platforms.IsUnixLike handles checks.")]
        private async Task<(bool, string?)> BuildPCLinuxOSPackage(string workingDir) 
        {
            bool[] invalidStates = [
                Platforms.CurrentDistribution!.Name != "PCLinuxOS",
                !Platforms.CurrentDistribution!.BaseDistro.Equals(DistroBase.Debian)
            ];

            // If both cases are true, execution haults, and an exception is thrown.
            if (invalidStates.All(state => state)) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Packaging for PCLinuxOS is only available on PCLinuxOS and Debian Based distros, please pick another option."
                    ]),
                    status: 1 
                );
            }
            
            await PrebuildActions();

            // Building the binary
            (var compilationStatus, var compiledBinaryPath) = await BuildStandaloneBinary(workingDir);

            // Ensuring compilation was success prior to continuing.
            if (!compilationStatus || compiledBinaryPath == null) {
                WriteAndExit("Binary compilation failed, please try again.", 1);
            }


            Console.WriteLine("Adding executable permissions to the newly compiled binary");
            EnsureBinaryIsExecutable(compiledBinaryPath);
            Success.WriteSuccessMessage("Operation successful.");


            Console.WriteLine("Creating the required directories for the build process.");
            (var rpmRootDir, var specFileName, var archiveName) = PCLinuxOSBuild.CreateRequiredDirectories();

            var specsDir = Path.Join(rpmRootDir, "SPECS");        // ~/rpmbuild/SPECS/
            var sourcesDir = Path.Combine(rpmRootDir, "SOURCES"); // ~/rpmbuild/SOURCES/
            var rpmDir = Path.Combine(rpmRootDir, "RPMS");        // ~/rpmbuild/RPMS/
            var compilationDir = Path.Combine(rpmDir, "x86_64");  // ~/rpmbuild/RPMS/x86_64

            var specFilePath = Path.Combine(specsDir, specFileName);            
            Success.WriteSuccessMessage("Operation successful.");


            Console.WriteLine("Creating the top level directory of the package inside ~/rpmbuild/SOURCES");
            var TLD = Path.Combine(sourcesDir, $"{AppName}-{BaseVersion}");
            EnsureDirectoryExists(TLD);
            Success.WriteSuccessMessage("Operation successful.");
            

            Console.WriteLine("Copying the compiled binary from the dotnet publish directory to newly created top level directory.");
            var sourceBinaryPath = Path.Combine(TLD, AppName);

            try {   
                File.Copy(compiledBinaryPath, sourceBinaryPath);
                Success.WriteSuccessMessage("Operation successful.");
            }
            catch (Exception ex) 
            {
                WriteAndExit(
                    string.Join(NLC, [
                        "Unable to copy the compiled binary to rpmbuild's sources directory, please try again.",
                        "Error Log:",
                        ex.Message
                    ]), 
                    status: 1
                );
            }


            Console.WriteLine("Compressing the top level directory into a tarball archive (.tar.gz) as per PCLinuxOS packaging guidelines.");
            (var success, var archivePath) = ArchiveManager.CreateTarballArchive(TLD, sourcesDir, archiveName);
            Success.WriteSuccessMessage("Operation successful.");


            var pcLinuxOSBuild = new PCLinuxOSBuild() { archivePath = archivePath };


            Console.WriteLine("Creating the build .spec file required by PCLinuxOS.");
            (var buildSpecStatus, var binaryStream) = await pcLinuxOSBuild.WriteBuildSpecFile(specFilePath);

            if (!buildSpecStatus || binaryStream == null) {
                WriteAndExit($"Failed to write {specFileName} to disk.", 1);
            }
            Success.WriteSuccessMessage("Operation successful.");


            string[] packages = 
                Platforms.CurrentDistribution.BaseDistro.Equals(DistroBase.ArchLinux) ? 
                [ "rpm-build", "rpm-tools", "pkgutils" ] :  // PCLinuxOS
                [ "rpm" ]; // Debian
            

            Console.WriteLine("Checking system packages, to ensure all packages required for the build process are installed.");
            var missingPackages = await FindMissingPackages(packages);

            if (missingPackages.Count != 0) 
            {
                Console.WriteLine($"Located {missingPackages.Count} missing package(s) required for the build process.");
                bool needsAptCacheRefresh = 
                    Platforms.CurrentDistribution.BaseDistro.Equals(DistroBase.Debian) &&
                    missingPackages.Contains("libarchive-tools");

                if (needsAptCacheRefresh) {
                    RefreshDebianAptCache();
                }

                // Potential Conflict: 
                // apt-get install requires root (sudo). rpmbuild (in ~/rpmbuild) must run as a non root user.
                // Monitor closely

                InstallMissingPackages([.. missingPackages]);
                Success.WriteSuccessMessage("Installed all missing packages required for this build process, continuing..");
            } 
            else {
              Success.WriteSuccessMessage("No additional packages are required for this build process, continuing..");  
            }

            // On some Debian systems, rpmbuild defaults to /usr/src/rpm instead of ~/rpmbuild unless ~/.rpmmacros is configured.
            var psi = new ProcessStartInfo() {
                FileName = "rpmbuild",
                Arguments = $"--define '_topdir {rpmRootDir}' -bb {specFileName}",
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = specsDir
            };

            using var process = await ProcessFactory.SpawnProcess(psi, "package bamm as an PCLinuxOS package");
            (var ExitCode, var STDOut, var STDErr) = await ProcessFactory.GetProcessResponse(process);

            if (ExitCode != 0) {
                Write($"Failed to package bamm due to a non zero status code: {ExitCode}");
                return (false, "Not Built.");
            }

            if (!Directory.Exists(compilationDir)) {
                Write($"Failed to locate package directory: {compilationDir}");
                return (false, "Not Built.");
            }

            var releaseTag = $"0.{VersionIdentifier}.1pclos{DateTime.Now.Year}";
            var fileName = $"{AppName}-{BaseVersion}-{releaseTag}.x86_64.rpm";
            var filePath = Path.Combine(compilationDir, fileName);

            return (true, filePath);
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
        
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "Platform is already validated at this point.")]
        public static void EnsureBinaryIsExecutable(string compiledBinaryPath)
        {
            var requiresPermissions = !HasExecutablePermissions(compiledBinaryPath);

            // If the binary has executable permissions, this operation is skipped.
            // If Linux's glibc or Apple's libc fail to give the binary executable permissions
            // Another attempt is made using .NET's builtin UnixFileMode
            if (requiresPermissions && !SetExecutablePermissions(compiledBinaryPath))
            {
                // Equivalent to 0755 (-rwxr-xr-x)
                var unixFileMode = (
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute
                );

                // This may throw an exception if the application was downloaded as root.
                File.SetUnixFileMode(compiledBinaryPath, unixFileMode);
            }
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
        
        /// <summary>
        /// Summary:<br/><br/>
        /// Handles the packaging of BAMM from the downloaded codebase. <br/>
        /// Params: <br/><br/>
        /// <param name="desiredBuildProcess">desiredBuildProcess: <br/>- The selected process to execute.</param> <br/>
        /// <param name="workingDir">workingDir: <br/>- The directory to execute the selected process.</param> <br/>
        /// <returns> <br/>
        /// Returns: <br/><br/>
        /// Item1: <br/>- A boolean representing the packaging status <br/>
        /// Item2: <br/>- The path to the package or build directory (This will be handled in Publisher.Program) <br/>
        /// </returns>
        /// </summary>
        public async Task<(bool, string?)> HandlePackaging(string desiredBuildProcess, string workingDir, string appVersion) 
        {
            return desiredBuildProcess switch
            {
                "Alt Linux Package (.rpm)" => await BuildAltLinuxOSPackage(workingDir),
                "Arch Package (.pkg.tar.xz)" => await BuildArchPackage(workingDir, appVersion),
                "Debian Package (.deb)" => await BuildDebianPackage(workingDir),
                "Fedora Package (.rpm)" => await BuildFedoraPackage(workingDir),
                "Gentoo Package (.tbz2)" => await BuildGentooPackage(workingDir),
                "PCLinuxOS Package (.rpm)" => await BuildPCLinuxOSPackage(workingDir),
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

        private static void InstallMissingPackage(string packageName) {
            try 
            {
                var isMissingMakePKG = !CommandExists(packageName);

                if (isMissingMakePKG) {
                    var installPrefix = Platforms.CurrentDistribution!.BaseDistro.Equals(DistroBase.Debian) switch 
                    {
                        true => string.Join(' ', [
                            "DEBIAN_FRONTEND=noninteractive", 
                            Platforms.CurrentDistribution!.PackageManager,
                            Platforms.CurrentDistribution.InstallCommand
                        ]),

                        _ => string.Join(' ', [
                            Platforms.CurrentDistribution!.PackageManager,
                            Platforms.CurrentDistribution.InstallCommand
                        ])
                    };

                    var installArgs = $"-c \"sudo {installPrefix} {packageName}\"";

                    Warning.Write($"Installing {packageName}");
                    (var output, _) = RunCommand("/bin/bash", installArgs);
                    Success.WriteSuccessMessage(output);
                }
            }

            catch {
                WriteAndExit($"Failed to install {packageName}, please ensure it is installed, then try again.", 1);
            }
        }

        private static void InstallMissingPackages(string[] packageNames) 
        {
            foreach (var packageName in packageNames) {
                InstallMissingPackage(packageName);
            }
        }

        public static void SetSelectedOS(string desiredBuildProcess, out string selectedOS) 
        {
            selectedOS = string.Empty;

            switch (desiredBuildProcess) {
                case "Alt Linux Package (.rpm)":
                case "Arch Package (.pkg.tar.xz)":
                case "Debian Package (.deb)":
                case "Fedora Package (.rpm)":
                case "Gentoo Package (.tbz2)":
                case "PCLinuxOS Package (.rpm)":
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
                    "Standalone Binary" => Path.Combine(path, AppName),
                    _ => path
                };

                return (File.Exists(finalPath), finalPath);
            }

            return (false, null);

        }

        
    }


    
}