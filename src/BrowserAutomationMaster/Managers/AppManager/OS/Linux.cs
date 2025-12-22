using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.WheelManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers.AppManager.OS
{
    public static partial class Linux
    {

        // Debian Package Manager
        readonly public static bool HasDPKG = CommandExists("dpkg");

        // Flatpak Package Manager
        readonly public static bool HasFlatpak = CommandExists("flatpak");

        // Red Hat Package Manager
        readonly public static bool HasRPM = CommandExists("rpm");

        readonly public static List<AppInfo> dpkgApps = HasDPKG ? ParseDpkgList() : [];

        readonly public static List<AppInfo> flatpakApps = HasFlatpak ? ParseFlatpakList() : [];

        readonly public static List<AppInfo> rpmApps = HasRPM ? ParseRpmList() : [];

        public readonly static Dictionary<string, bool> RPIModels = new()
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

        public static List<AppInfo> GetApps()
        {
            try
            {
                if (dpkgApps.Count == 0 && flatpakApps.Count == 0 && rpmApps.Count == 0)
                    WriteAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to detect any of the following commands:\n\n" +
                            "dpkg\nflatpak\nrpm\n",
                        status: 1
                    );

                var appSources = new List<(string Name, List<AppInfo> Apps)>
                {
                    ("Debian Package Manager (dpkg)", dpkgApps),
                    ("Flatpak", flatpakApps),
                    ("RedHat Package Manager (rpm)", rpmApps)
                };


                if (GlobalConfig.ShowAppCheck)
                {

                    AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                    
                    foreach (var (Name, Apps) in appSources)
                    {
                        if (Apps.Count == 0)
                            Warning.Write($"No apps found for: {Name}");

                        else if (Apps.Count == 1)
                            WriteSuccessMessage($"Found 1 app from: {Name}");

                        else
                            WriteSuccessMessage($"Found {Apps.Count} apps from: {Name}");
                    }
                }

                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                return [.. dpkgApps.Concat(flatpakApps).Concat(rpmApps).Distinct()];
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
            // If this is true, the OS does not require precompiled wheels.
            if (Platforms.IsLinux || Platforms.IsWindows || Platforms.IsOSX || !Platforms.IsChromeOS || Platforms.CurrentArchitecture != Arm)
                return;
           
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

            if (STDOut.Any(a => a.Contains("armhf", OIC)))
                Platforms.IsARMhf = true;

            else if (STDOut.Any(a => a.Contains("armel", OIC)))
                Platforms.IsARMel = true;

        }

        public static void ChromeOSCheck()
        {

            if (!OperatingSystem.IsLinux())
                return;

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

        public static string? GetDistroNameString()
        {
            var lsbrPresent = CommandExists("lsb_release");
            var neofetchPresent = CommandExists("neofetch");
            string? distroName;

            if (lsbrPresent)
            { 
                distroName = RunLSBR(); 
            }

            else if (neofetchPresent)
            {
                distroName = RunNeofetch();
            }

            else 
            {
                distroName = RunOSR();
            }

            return distroName;
        }


        public static string? GetTerminalBackgroundColor()
        {
            try
            {
                string black = "0000/0000/0000";
                
                if (Platforms.IsChromeOS || Platforms.IsOSX || Platforms.IsRaspi)
                    return black; 
                
                string tempFile = Path.GetTempFileName();

                string command = "bash";
                string args = $"-c \"printf '\\e]11;?\\e\\\\' >/dev/tty; read -rs -t 3 -d $'\\\\' response </dev/tty; echo \\\"$response\\\" | xxd > {tempFile}\"";
                
                string response = RunCommand(command, args);
                Thread.Sleep(300);

                if (File.Exists(tempFile))
                {
                    string hexDump = File.ReadAllText(tempFile);
                    File.Delete(tempFile);

                    if (!string.IsNullOrWhiteSpace(hexDump))
                    {
                        var match = ForegroundMatch.Match(hexDump);
                        var groups = match.Groups;
                        
                        if (groups.Count == 3) // groups[0] is the whole match
                            return groups[1].Value + groups[2].Value;
                        
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
            if (!Platforms.IsUnixLike) { return true; } // This check doesnt need to include windows.
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));
        }

        public static async Task InstallRequiredLinuxPackages(List<AppInfo> appsInfo)
        {
            try
            {
                // This empty file will be written once the packages are installed, then checked in subsequent runtimes.
                var linuxPackageFile = GetLinuxPackageFile();

                if (File.Exists(linuxPackageFile)){
                    await DownloadWheels(); // Do not remove this ensure the wheels will always be downloaded.
                    return;
                }

                Warning.Write("Installing the Required Linux Packages (if not already installed.), please wait up to 60 seconds");

                var inputMessage = @"Supported versions include:
                    - Python 3.9.X
                    - Python 3.10.X
                    - Python 3.11.X
                    - Python 3.12.X
                    - Python 3.13.X
                    - Python 3.14.X

                    Examples:
                    - Python 3.9
                    - Python 3.12.7
                    - Python 3.9.8

                    Version: ".Replace("                    ", "");

                var PKM_CMD = (HasDPKG, HasRPM) switch
                {
                    (true, _) => "apt-get", // Debian-Based
                    (_, true) => "dnf",     // Fedora-Based
                    (_, _) => null
                };

                if (PKM_CMD == null)
                {
                    Warning.Write("An error occured while attempting to retrieve the Package Manager associated with your Distribution.");
                    var response = Input.WriteListFromOptions(["Debian-Based", "Fedora-Based"], "operating system");
                    PKM_CMD = response switch
                    {
                        "Debian-Based" => "apt-get", // Debian-Based
                        "Fedora-Based" => "dnf",     // Fedora Based
                        _ => "UNSELECTED DISTO"
                    };

                    if (PKM_CMD == "UNSELECTED DISTRO")
                        WriteAndExit("An error occured while attempting to access your Distribution's Package Manager, please try again.", 1);
                }



                var installPrefix = (HasDPKG, HasRPM) switch
                {
                    (true, _) => $"DEBIAN_FRONTEND=noninteractive {PKM_CMD} install -y",
                    (_, true) => $"{PKM_CMD} install -y",
                    (_, _) => null
                };

                var installCMD = $"-c \"sudo {installPrefix}";

                var pyVersion = Installations.GetMissingPyVersion();
                while (pyVersion == null || !PyVersionRegex.IsMatch(pyVersion))
                {
                    Warning.Write("Unable to detect the installed version of Python.");
                    pyVersion = Input.AskForInput(inputMessage);
                }

                // Python3.X-dev is used for brotli and zstandard for compression and decompression
                var optionalPackages = GetBrowserStackStatus() ?
                    $"libffi-dev build-essential python{pyVersion.Replace("Python ", "")}-dev" :
                    string.Empty;

                string[] packages = [
                    "xclip", // Used for auto_copy_path
                    $"python{pyVersion.Replace("Python ", "")}-venv",  // Used for majority of BAMM to create vEnv(s)
                    optionalPackages
                ];

                if (installPrefix == null)
                    WriteAndExit($"Unable to install the following required Linux Packages:\n{string.Join('\n', packages)}", 1);


                string[] commands = new string[packages.Length];

                for (int i = 0; i < packages.Length; i++)
                {
                    // Skips installation of additional packages if browserstack isn't used.
                    if (string.IsNullOrEmpty(packages[i]))
                        continue;

                    commands[i] = $"{installCMD} {packages[i]}\"";

                    var appInfo = new AppInfo() { 
                        Name = packages[i],
                        Path = "", // Path is required per the struct but isnt needed here, thus the empty string.
                    };

                    // Skips pre-existing installations
                    if (appsInfo.Contains(appInfo))
                    {
                        continue;
                    }

                    Warning.Write($"Installing package: {packages[i]}");
                    WriteSuccessMessage(RunCommand("/bin/bash", $"{commands[i]}"));
                }

                await DownloadWheels();
                
                File.Create(linuxPackageFile);
            }
            catch (Exception e)
            {
                WriteAndExit($"Unable to install the required Linux Packages.\n\nError Log:\n{e}", 1);
            }


        }

        /// <summary> Parses apps installed via DPKG (Debian Package Manager) (apt utilizes DPKG so most users will be using apt install.) </summary>
        /// <returns>A List of AppInfo</returns>
        private static List<AppInfo> ParseDpkgList()
        {
            try
            {
                var apps = new List<AppInfo>();
                var output = RunCommand("dpkg-query", "-W -f \"${Package}\t${Version}\n\"");
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
            var output = RunCommand("rpm", "-qa");
            
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


        /// <summary> Parses apps installed via Flatpak </summary>
        /// <returns>A List of AppInfo</returns>
        private static List<AppInfo> ParseFlatpakList()
        {
            var apps = new List<AppInfo>();
            var output = RunCommand("flatpak", "list");

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

        public static void RPICheck()
        {
            if (!OperatingSystem.IsLinux())
                return;

            try
            {
                var cpuContents = File.ReadAllLines("/proc/cpuinfo");

                if (cpuContents == null)
                    return;


                foreach (var line in cpuContents)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    var match = PrecompiledRPIRegex().Match(line);

                    if (match == null) continue;
                    if (match.Groups.Count == 0) continue;

                    match.Groups.TryGetValue("model", out var modelNameMatch);

                    if (modelNameMatch == null) continue;
                    if (!modelNameMatch.Success) continue;

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
            catch (Exception ex){
                Warning.Write($"A non fatal error occured while attempting to read from /proc/cpuinfo\n\nError Log:\n{ex.Message}");
            }
        }

        public static string RunCommand(string cmd, string args)
        {
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
                if (proc == null)
                    return string.Empty;
                
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0)
                    return output;

                return string.Empty;
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
                return string.Empty;
            }
        }

        // /etc/os-release
        private static string? RunOSR()
        {
            try
            {
                if (!File.Exists("/etc/os-release"))
                {
                    return null;
                }

                var contentArray = File.ReadAllLines("/etc/os-release");

                var contentString = string.Join(NLC, contentArray);

                var osrMatch = PrecompiledOSR1Regex().Match(contentString);

                if (osrMatch.Success){
                    return osrMatch.Groups[1].Value;
                }

                osrMatch = PrecompiledOSR2Regex().Match(contentString);

                if (osrMatch.Success){
                    return osrMatch.Groups[1].Value;
                }
            }
            catch {
                Warning.Write("Unable to determine detailed OS info for debugging purposes.");
                Warning.Write("You may see the generic \"Linux\" identifier.");
            }
            return null;
        }
        
        // lsb_release -a 
        private static string? RunLSBR()
        {
            try
            {
                var lsbrResult = RunCommand("/bin/bash", "-c \"lsb_release -a\"");

                var lsbrMatch = PrecompiledLSBRRegex().Match(lsbrResult);

                if (!lsbrMatch.Success)
                {
                    return null;
                }

                return lsbrMatch.Groups[1].Value;
            }
            catch {
                Warning.Write("Unable to determine detailed OS info for debugging purposes.");
                Warning.Write("You may see the generic \"Linux\" identifier.");
            }
            // catch (Exception ex){}
            return null;
        }

        // neofetch
        private static string? RunNeofetch()
        {
            try
            {
                var nfTmpFilePath = GetTemporaryNeofetchPath();
                RunCommand("/bin/bash", $"-c \"neofetch > {nfTmpFilePath}\"");

                if (!File.Exists(nfTmpFilePath))
                {
                    return null;
                }

                var contentArray = File.ReadAllLines(nfTmpFilePath);

                File.Delete(nfTmpFilePath); // Cleanup since this tmp file isnt needed
                
                var rawContentString = string.Join(NLC, contentArray);

                var contentString = StripANSI(rawContentString);

                var nfMatch = PrecompiledNFRegex().Match(contentString);
                if (nfMatch.Success)
                {
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
        private static string StripANSI(string text){
            string ANSIPattern = @"\x1b\[[0-?]*[ -/]*[@-~]";
            return Regex.Replace(text, ANSIPattern, string.Empty);
        }
    }
}