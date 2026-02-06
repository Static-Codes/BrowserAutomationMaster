using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.DistroManager;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.WheelManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux
{
    public static partial class Functions
    {

        // Debian Package Manager
        public static readonly bool HasDPKG = CommandExists("dpkg");

        // Flatpak Package Manager
        public static readonly bool HasFlatpak = CommandExists("flatpak");

        // Red Hat Package Manager
        public static readonly bool HasRPM = CommandExists("rpm");

        public static readonly bool HasPacman = CommandExists("pacman");

        public static readonly List<AppInfo> dpkgApps = HasDPKG ? ParseDpkgList() : [];

        public static readonly List<AppInfo> flatpakApps = HasFlatpak ? ParseFlatpakList() : [];

        public static readonly List<AppInfo> rpmApps = HasRPM ? ParseRpmList() : [];

        public static readonly List<AppInfo> pacmanApps = HasPacman ? ParsePacmanList() : [];

        public static readonly Dictionary<string, bool> RPIModels = new()
        {
            { "2 Model B", false },
            { "3 Model B", false },
            { "3 Model B+", false },
            { "4 Model B", true },
            { "400", true },
            { "5", true },
            { "Compute Module 3", false },
            { "Compute Module 3+", false },
            { "Compute Module 4", true },
            { "Compute Module 4S", true }
        };

        private static readonly string pyVerInputMessage = 
        @"Supported versions include:
            - Python 3.9.X
            - Python 3.10.X
            - Python 3.11.X
            - Python 3.12.X
            - Python 3.13.X
            - Python 3.14.X
            - Python 3.15.X (UNTESTED BUT HYPOTHETICALLY SUPPORTED)

        Examples:
            - Python 3.9
            - Python 3.12.7
            - Python 3.9.8

        Version: ".Replace("            ", "");

        public static List<AppInfo> GetApps()
        {
            try
            {
                var totalAppCount = dpkgApps.Count + 
                                    flatpakApps.Count + 
                                    rpmApps.Count + 
                                    pacmanApps.Count;

                if (totalAppCount == 0) {
                    WriteAndExit(
                        message:
                            string.Join(NLC, [
                                "BAM Manager (BAMM) was unable to detect any packages from the following package managers:",
                                NLC,
                                "- dpkg",
                                "- flatpak", 
                                "- rpm",
                                "- pacman"
                            ]),
                        status: 1
                    );
                }

                var appSources = new List<(string Name, List<AppInfo> Apps)>
                {
                    ("Debian Package Manager (dpkg)", dpkgApps),
                    ("Flatpak", flatpakApps),
                    ("RedHat Package Manager (rpm)", rpmApps),
                    ("Pacman", pacmanApps)
                };


                if (GlobalConfig.ShowAppCheck)
                {

                    AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                    
                    foreach (var (Name, Apps) in appSources)
                    {
                        if (Apps.Count == 0) {
                            Warning.Write($"No apps found for: {Name}");
                        }

                        else if (Apps.Count == 1) {
                            WriteSuccessMessage($"Found 1 app from: {Name}");
                        }

                        else {
                            WriteSuccessMessage($"Found {Apps.Count} apps from: {Name}");
                        }
                    }
                }

                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                return [.. dpkgApps
                            .Concat(flatpakApps)
                            .Concat(rpmApps)
                            .Concat(pacmanApps)
                            .Distinct()];
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    $"BAM Manager (BAMM) was unable to parse installed system applications, " +
                    $"please see the error below:\n\n{ex}",
                    status: 1
                );
                return [];
            }
        }

        public static void ARM32Check()
        {

            bool[] invalidStates = [
                Platforms.IsLinux,
                Platforms.IsWindows,
                Platforms.IsMacOS,
                !Platforms.IsChromeOS,
                Platforms.CurrentArchitecture != Arm,
            ];


            // If this is true, the OS does not require precompiled wheels.
            if (invalidStates.Any(invalidState => invalidState)) {
                return;
            }
           
            var psi = new ProcessStartInfo()
            {
                // Currently has a bug where --print-architecture writes to std out, this needs to be adjusted likely with >/dev/null 2>&1
                FileName = HasDPKG ? "dpkg" : (HasRPM ? "rpm" : "bin/bash"),
                Arguments = HasDPKG ? "--print-architecture" : (HasRPM ? "--queryformat \"%{ARCH}\\n\" -qf /bin/ls" : "lscpu"),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            
            using var proc = ProcessFactory.SpawnProcess(psi, "check if the current system is using the ARMHF Architecture", runSync: true, writeSTDInOut: false).Result;
            (int ExitCode, List<string> STDOut, List<string> STDErr) = ProcessFactory.GetProcessResponse(proc).Result;

            if (STDOut.Count == 0)
            {
                Warning.Write("Unable to determine if the current CPU ABI is ARMHF, you may experience runtime issues.");
                return;
            }

            if (STDOut.Any(a => a.Contains("armhf", OIC))) {
                Platforms.IsARMhf = true;
            }

            else if (STDOut.Any(a => a.Contains("armel", OIC))) {
                Platforms.IsARMel = true;
            }

        }

        // Due to the unique nature of how ANSI is handled on Kali Linux
        public static bool IsKali() 
        {
            return 
                Platforms.CurrentDistribution != null && 
                Platforms.CurrentDistribution.Name.Equals("Kali Linux");
        }

        public static void ChromeOSCheck()
        {

            if (!OperatingSystem.IsLinux()) {
                return;
            }

            try
            {
                string cmdline = File.ReadAllText("/proc/cmdline");
                Platforms.IsChromeOS = cmdline.Contains("cros_");
            }

            catch (Exception ex)
            {
                Warning.Write(
                    string.Join(
                        string.Empty, [
                            "Unable to complete ChromeOS Check, if you are using ChromeOS, ",
                            $"please make a bug report at {ISSUES_LINK}\n\n",
                            $"Error Log:\n{ex}"
                    ])
                );
            }
        }

        // Instead of parsing each distro by type finding the available commands is much more efficient.
        public static bool CommandExists(string cmd)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = cmd,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                })!;

                string result = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                return !string.IsNullOrWhiteSpace(result);
            }
            catch
            {
                return false;
            }
        }

        // Unlike DistroManager.DetermineDistro() this is only used for debugging purposes.
        public static string GetFullDistroName()
        {
            var lsbrPresent = CommandExists("lsb_release");
            var neofetchPresent = CommandExists("neofetch");
            string? distroName;

            if (lsbrPresent) { 
                distroName = RunLSBR(); 
            }

            else if (neofetchPresent) {
                distroName = RunNeofetch();
            }

            else {
                distroName = RunOSR();
            }

            return distroName ?? "Generic Linux";
        }

        public static string? GetTerminalBackgroundColor()
        {
            bool[] statesToReturnBlack = [
                Platforms.IsChromeOS,
                Platforms.IsMacOS,
                Platforms.IsRaspi,
                IsKali()
            ];

            try
            {
                string black = "0000/0000/0000";
                
                if (statesToReturnBlack.Any(stateToReturnBlack => stateToReturnBlack)) {
                    return black;
                }
                
                string tempFile = Path.GetTempFileName();

                string command = "bash";

                string args = string.Join(' ', [
                    "-c",
                    "\"printf '\\e]11;?\\e\\\\' >/dev/tty;",
                    "read -rs -t 3 -d $'\\\\' response </dev/tty;",
                    $"echo \\\"$response\\\" | xxd > {tempFile}\""
                ]);

                // string args = $"-c \"printf '\\e]11;?\\e\\\\' >/dev/tty; read -rs -t 3 -d $'\\\\' response </dev/tty; echo \\\"$response\\\" | xxd > {tempFile}\"";

                (var output, var error) = RunCommand(command, args);
                Thread.Sleep(300);

                if (File.Exists(tempFile))
                {
                    string hexDump = File.ReadAllText(tempFile);
                    File.Delete(tempFile);

                    if (!string.IsNullOrWhiteSpace(hexDump))
                    {
                        var match = ForegroundMatch.Match(hexDump);
                        var groups = match.Groups;
                        
                        // groups[0] is the whole match
                        if (groups.Count == 3) {
                            return groups[1].Value + groups[2].Value;
                        }

                        return hexDump;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                WriteMessage($"Error reading terminal color: {ex.Message}");
                return null;
            }
        }

        public static bool HasDisplayVarSet()
        {
            // This check doesnt need to non-unix systems.
            if (!Platforms.IsUnixLike) { 
                return true; 
            }

            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
        }

        // Installs the required packages and writes the required wheels to disks (if needed)
        public static async Task InstallRequiredLinuxPackages()
        {
            bool[] platformsThatRequireWheels = [
                Platforms.IsARMel, 
                Platforms.IsARMhf, 
                Platforms.IsChromeOS, 
                Platforms.IsRaspi
            ];

            // Ensuring the wheels are only downloaded on platforms that potentially require it.
            bool wheelsRequired = platformsThatRequireWheels.Any(platform => platform);

            try
            {
                // This empty file will be written once the packages are installed, then checked in subsequent runtimes.
                // var linuxPackageFile = GetLinuxPackageFile();

                // if (File.Exists(linuxPackageFile)) {
                //     return;
                // }

                Warning.Write("Querying packages, please wait...");

                


                // Exits if Platforms.CurrentDistribution is null.
                CheckLinuxDistro();


                // Adds the DEBIAN_FRONTEND=noninteractive prefix if the current distro in use is based off Debian.
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

                var installCMD = $"-c \"sudo {installPrefix}";

                var pyVersion = Installations.GetMissingPyVersion();
                while (pyVersion == null || !PyVersionRegex.IsMatch(pyVersion))
                {
                    Warning.Write("Unable to detect the installed version of Python.");
                    pyVersion = Input.AskForInput(pyVerInputMessage);
                }

                string[] requiredPackages = Platforms.CurrentDistribution!.RequiredPackages;
                string[] optionalPackages = GetBrowserStackStatus() ? Platforms.CurrentDistribution!.OptionalPackages : [];
                string[] packages = [.. requiredPackages, .. optionalPackages];

                var missingPackages = await FindMissingPackages(packages);

                if (installPrefix == null) 
                {
                    WriteAndExit(
                        message: 
                            string.Join(NLC, [
                                "Unable to install the following required Linux Packages:",
                                string.Join(NLC, packages), 
                            ]), 
                        status: 1
                    );
                }

                if (missingPackages.Count == 0) 
                {
                    WriteSuccessMessage("No additional package installations are required.");
                    return;
                }

                string[] commands = new string[missingPackages.Count];

                Warning.Write("Installing required packages:");
                foreach (var package in missingPackages) {
                    Console.WriteLine($"\t- {package}");
                }

                Write("You will be prompted for your super user password shortly.");
                Thread.Sleep(500);

                for (int i = 0; i < commands.Length; i++)
                {
                    commands[i] = $"{installCMD} {packages[i]}\"";

                    Warning.Write($"Installing package: {packages[i]}");
                    (var output, _) = RunCommand("/bin/bash", $"{commands[i]}");
                    WriteSuccessMessage(output);
                }

                await DownloadWheels();
            }
            catch (Exception e)
            {
                WriteAndExit($"Unable to install the required Linux Packages.\n\nError Log:\n{e}", 1);
            }


        }
        
        private static List<AppInfo> ParseDpkgList()
        {
            try
            {
                var apps = new List<AppInfo>();
                (var output, var error) = RunCommand("dpkg-query", "-W -f \"${Package}\t${Version}\n\"");
                foreach (var line in output.Split('\n'))
                {
                    var parts = line.Trim('\'').Split("\t");
                    
                    if (parts.Length >= 2)
                    {
                        apps.Add(
                            new AppInfo { 
                                Name = parts[0], 
                                Version = parts[1],
                                Path = "", // Path is required per the struct but isnt needed here, thus the empty string.
                            }
                        );
                    }
                }
                return apps;
            }
            catch { 
                Write("DPKG not found, checking another method."); 
                return []; 
            }
        }

        /// <summary> Parses apps installed via RPM (Red Hat Package Manager) (only for CentOS, Fedora, Oracle Linux, etc.) </summary>
        /// <returns>A List of AppInfo</returns>
        private static List<AppInfo> ParseRpmList()
        {
            var apps = new List<AppInfo>();
            (var output, _) = RunCommand("rpm", "-qa");
            
            foreach (var line in output.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    apps.Add
                    (
                        new AppInfo { 
                            Name = line,
                            Path = "" // Path is required per the struct but isnt needed here, thus the empty string. 
                        }
                    );
                }
            }

            return apps;
        }

        private static List<AppInfo> ParsePacmanList() 
        {
            try
            {
                var apps = new List<AppInfo>();
                var command = string.Join(' ', [
                    "-c",
                    "\"pacman -Ql |", 
                    "grep '/usr/bin/[^/]' |",
                    "awk '{print $1, $2}' |", 
                    "sort -u -k1,1\""
                ]);

                (var output, var error) = RunCommand("/bin/bash", command);
                
                foreach (var line in output.Split('\n'))
                {
                    var parts = line.Trim().Split(' ');
                    
                    if (parts.Length >= 2)
                    {
                        apps.Add(
                            new AppInfo { 
                                Name = parts[0], 
                                Version = "",
                                Path = parts[1],
                            }
                        );
                    }
                }
                return apps;
            }
            catch { 
                Write("Pacman not found, checking another method."); 
                return []; 
            }
        }


        /// <summary> Parses apps installed via Flatpak </summary>
        /// <returns>A List of AppInfo</returns>
        private static List<AppInfo> ParseFlatpakList()
        {
            var apps = new List<AppInfo>();
            (var output, _) = RunCommand("flatpak", "list");

            foreach (var line in output.Split('\n'))
            {
                var parts = line.Split('\t');
                
                if (parts.Length >= 2) 
                {
                    apps.Add
                    (
                        new AppInfo 
                        { 
                            Name = parts[0], 
                            Version = parts[1],
                            Path = ""  // Path is required per the struct but isnt needed here, thus the empty string. 
                        }
                    );
                }
            }

            return apps;
        }

        public static void RefreshDebianAptCache() 
        {
            try 
            {
                if (Platforms.CurrentDistribution!.Equals(DistroBase.Debian)) {
                    Warning.Write("One or more dependencies are requiring a refresh of the apt-cache, please wait.");
                    RunCommand("apt-get", "update");
                }
            }
            catch (Exception ex) {
                WriteAndExit(
                    message: 
                        string.Join(NLC, [
                            "Failed to update apt-cache using apt-get update",
                            "Error Log:",
                            ex.Message
                        ]),
                    status: 1
                );
            }
        }
        
        public static void RPICheck()
        {
            if (!OperatingSystem.IsLinux()) {
                return;
            }

            try
            {
                var cpuContents = File.ReadAllLines("/proc/cpuinfo");

                if (cpuContents == null) {
                    return;
                }


                foreach (var line in cpuContents)
                {
                    if (string.IsNullOrEmpty(line)) {
                        continue;
                    }

                    var match = PrecompiledRPIRegex().Match(line);

                    if (match == null || match.Groups.Count == 0) { 
                        continue;
                    }
                    
                    match.Groups.TryGetValue("model", out var modelNameMatch);

                    if (modelNameMatch == null || !modelNameMatch.Success) { 
                        continue;
                    }

                    var modelName = $"Raspberry Pi {modelNameMatch.Value}";

                    // Checks if the partial model string is present in modelNameMatch.Value
                    var validatedMatches = RPIModels.Where(m => modelNameMatch.Value.Contains(m.Key));

                    if (validatedMatches == null) {
                        WriteAndExit($"The {modelName} is not supported", status: 1);
                    }

                    // The value of the pair is a boolean determining whether the specified model can run the GUI.
                    var validatedMatch = validatedMatches.First();

                    Platforms.IsRaspi = true;
                    Platforms.IsUnixLike = true;
                    Platforms.SetRaspiModel(modelName, validatedMatch.Value);
                    
                }
            }
            catch (Exception ex) {
                Warning.Write($"A non fatal error occured while attempting to read from /proc/cpuinfo\n\nError Log:\n{ex.Message}");
            }
        }

        public static (string, string) RunCommand(string cmd, string args)
        {
            string output = string.Empty;
            string error = string.Empty;
            try
            {
                ProcessStartInfo procStartInfo = new()
                {
                    FileName = cmd,
                    Arguments = args, 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true

                };
                
                using var proc = Process.Start(procStartInfo);
                
                if (proc == null) {
                    return (output, error);
                }

                output = proc.StandardOutput.ReadToEnd();
                error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0) {
                    return (output, error);
                }
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to execute a necessary command, if this issue persists, " +
                        $"please make a bug report at {ISSUES_LINK}\nError log:\nUnable to execute\n" +
                        $"{cmd}\nException:\n{ex.Message}",
                    status: 1
                );
            }

            return (output, error);
        }

        // Executing: 'cat /etc/os-release'
        private static string? RunOSR()
        {
            var fileName = "/etc/os-release";
            try
            {
                if (!File.Exists(fileName)) {
                    return null;
                }

                var contentArray = File.ReadAllLines(fileName);

                var contentString = string.Join(NLC, contentArray);

                var osrMatch = PrecompiledOSRPrettyNameRegex().Match(contentString);

                if (osrMatch.Success) {
                    return osrMatch.Groups[1].Value;
                }

                osrMatch = PrecompiledOSRNameRegex().Match(contentString);

                if (osrMatch.Success) {
                    return osrMatch.Groups[1].Value;
                }
            }
            catch {
                Warning.Write("Unable to determine detailed OS info for debugging purposes.");
                Warning.Write("You may see the generic \"Linux\" identifier.");
            }
            return null;
        }
        
        // Executing: 'lsb_release -a' 
        private static string? RunLSBR()
        {
            try
            {
                (var lsbrResult, _) = RunCommand("/bin/bash", "-c \"lsb_release -a\"");

                var lsbrMatch = PrecompiledLSBRRegex().Match(lsbrResult);

                if (!lsbrMatch.Success) {
                    return null;
                }

                return lsbrMatch.Groups[1].Value;
            }
            catch {
                Warning.Write("Unable to determine detailed OS info for debugging purposes.");
                Warning.Write("You may see the generic \"Linux\" identifier.");
            }
            return null;
        }

        // Executing: 'neofetch'
        private static string? RunNeofetch()
        {
            try
            {
                var nfTmpFilePath = GetTemporaryNeofetchPath();
                RunCommand("/bin/bash", $"-c \"neofetch > {nfTmpFilePath}\"");

                if (!File.Exists(nfTmpFilePath)) {
                    return null;
                }

                var contentArray = File.ReadAllLines(nfTmpFilePath);

                File.Delete(nfTmpFilePath); // Cleanup since this tmp file isnt needed
                
                var rawContentString = string.Join(NLC, contentArray);

                var contentString = StripANSI(rawContentString);

                var nfMatch = PrecompiledNFRegex().Match(contentString);
                
                if (nfMatch.Success) {
                    return nfMatch.Groups[1].Value;
                }

                return null;
            }
            catch {
                Warning.Write("Unable to determine detailed OS info for debugging purposes.");
                Warning.Write("You may see the generic \"Linux\" identifier.");
            }
            // catch (Exception ex){}
            return null;
        }
        
        private static string StripANSI(string text) {
            string ANSIPattern = @"\x1b\[[0-?]*[ -/]*[@-~]";
            return Regex.Replace(text, ANSIPattern, string.Empty);
        }
    }
}