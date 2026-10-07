using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.SystemInfo.OS.Generic;
using BrowserAutomationMaster.Core.Types.Linux;
using Spectre.Console;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using static BrowserAutomationMaster.Core.Common.ANSI;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Compilation.Transpiler;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux.DistroManager;
using static BrowserAutomationMaster.Core.Types.Installations;
using static BrowserAutomationMaster.Core.Utilities.AppSettingsUtility;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using static System.Runtime.InteropServices.Architecture;
using static WhichDistroSharp.WhichDistroSharp;

namespace BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux
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
                            string.Join(Environment.NewLine, [
                                "BAM Manager (BAMM) was unable to detect any packages from the following package managers:",
                                Environment.NewLine,
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


                if (GlobalSettings.ShowAppCheck)
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

        public static string? GetAbsolutePathOfSymLink(string symLinkPath) 
        {    
            // ----------------------------------
            // Pass 1: File.ResolveLinkTarget
            // ----------------------------------
            var targetObj = File.ResolveLinkTarget(symLinkPath, true);
            if (targetObj != null) {
                return targetObj.FullName;
            }

            // ----------------------------------
            // Pass 2: using readlink from glibc
            // ----------------------------------

            // Docs: https://www.man7.org/linux/man-pages/man2/readlink.2.html
            // Original Syntax: 
            // #include <unistd.h>
            // ssize_t readlink(const char *restrict path, char buf[restrict bufsiz], size_t bufsiz);

            // Returns:  
            // On success, these calls return the number of bytes placed in buf.
            // (If the returned value equals bufsiz, then truncation may have occurred.)  
            // On error, -1 is returned and errno is set to indicate the error.


            [DllImport("libc", SetLastError = true)]
            static extern int readlink(string path, byte[] buf, int bufsiz);

            // https://linuxvox.com/blog/linux-max-path-length/
            const int MAX_PATH_SIZE = 4096;

            byte[] buffer = new byte[MAX_PATH_SIZE];

            int result = readlink(symLinkPath, buffer, buffer.Length);

            if (result == -1) {
                throw new Exception(
                    string.Join(Environment.NewLine, [
                        $"Unable to determine the absolute path for the symlink: {symLinkPath}",
                        $"readlink exited with a status code of {Marshal.GetLastPInvokeError()}"
                    ])
                );
            }

            return Encoding.UTF8.GetString(buffer, 0, result);
        }

        // Unlike DistroManager.DetermineDistro() this is only used for debugging purposes.
        public static string GetFullDistroName()
        {
            var platform = GlobalUserInfo.PlatformInfo.CurrentPlatform ?? DetectPlatform();

            if (!string.IsNullOrWhiteSpace(platform.PrettyName)) {
                return platform.PrettyName;
            }

            if (!string.IsNullOrWhiteSpace(platform.Name)) {
                return platform.Name;
            }

            return "Generic Linux";
        }

        public static string? GetTerminalBackgroundColor()
        {
            bool[] statesToReturnBlack = [
                GlobalUserInfo.PlatformInfo.IsMacOS,
                GlobalUserInfo.PlatformInfo.IsPiDevice,
                IsKali()
            ];

            try
            {
                string black = "0000/0000/0000";
                
                if (statesToReturnBlack.Any(stateToReturnBlack => stateToReturnBlack)) {
                    return black;
                }

                // This probe writes an OSC 11 query to /dev/tty and reads the terminal's reply back
                // from /dev/tty. Both share the controlling terminal's input queue with whatever
                // launched this process, so when stdin is not a terminal (pipes, editors, CI, test
                // hosts) the reply is never reliably consumed and its bytes leak into the parent's
                // input, where the next command typed at that terminal reads them.
                // Only probe when there is an interactive terminal to answer.
                if (Console.IsInputRedirected || !HasDisplayVariableSet())
                {
                    return null;
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

        public static bool HasDisplayVariableSet()
        {
            // This check doesnt need to non-unix systems.
            if (!GlobalUserInfo.PlatformInfo.IsUnixLike) { 
                return true; 
            }

            return !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("DISPLAY")
            );
        }

        // Installs the required packages and writes the required wheels to disks (if needed)
        public static async Task InstallRequiredLinuxPackages()
        {
            try
            {
                // This empty file will be written once the packages are installed, then checked in subsequent runtimes.
                // var linuxPackageFile = GetLinuxPackageFile();

                // if (File.Exists(linuxPackageFile)) {
                //     return;
                // }

                Warning.Write("Querying packages, please wait...");

                // Exits if GlobalUserInfo.PlatformInfo.CurrentDistribution is null.
                CheckLinuxDistro();


                // Adds the DEBIAN_FRONTEND=noninteractive prefix if the current distro in use is based off Debian.
                var installPrefix = GlobalUserInfo.PlatformInfo.CurrentDistribution!.BaseDistro.Equals(DistroFamily.Debian) switch 
                {
                    true => string.Join(' ', [
                        "DEBIAN_FRONTEND=noninteractive", 
                        GlobalUserInfo.PlatformInfo.CurrentDistribution!.PackageManagerCommand,
                        GlobalUserInfo.PlatformInfo.CurrentDistribution.InstallCommand
                    ]),

                    _ => string.Join(' ', [
                        GlobalUserInfo.PlatformInfo.CurrentDistribution!.PackageManagerCommand,
                        GlobalUserInfo.PlatformInfo.CurrentDistribution.InstallCommand
                    ])
                };

                var installCMD = $"-c \"sudo {installPrefix}";

                var pyVersion = GetMissingPythonVersion();
                while (pyVersion == null || !PyVersionRegex.IsMatch(pyVersion))
                {
                    Warning.Write("Unable to detect the installed version of Python.");
                    pyVersion = Input.AskForInput(pyVerInputMessage);
                }

                string[] requiredPackages = GlobalUserInfo.PlatformInfo.CurrentDistribution!.RequiredPackages;

                var missingPackages = await FindMissingPackages(requiredPackages);

                if (installPrefix == null) 
                {
                    WriteAndExit(
                        message: 
                            string.Join(Environment.NewLine, [
                                "Unable to install the following required Linux Packages:",
                                string.Join(Environment.NewLine, requiredPackages), 
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
                    commands[i] = $"{installCMD} {requiredPackages[i]}\"";

                    Warning.Write($"Installing package: {requiredPackages[i]}");
                    (var output, _) = RunCommand("/bin/bash", $"{commands[i]}");
                    WriteSuccessMessage(output);
                }
            }
            catch (Exception e) {
                WriteAndExit($"Unable to install the required Linux Packages.\n\nError Log:\n{e}", 1);
            }


        }
        
        // Due to the unique nature of how ANSI is handled on Kali Linux
        private static bool IsKali()  {
            return GlobalUserInfo.PlatformInfo.CurrentDistribution?.Is(Distros.KaliLinux) == true;
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
                // Reads BaseDistro, not the Distro object. 
                // Comparing a Distro against a DistroFamily returns false.
                // object.Equals accepts any value and quietly returns false if runtime types differ, so apt-get update never ran.
                if (GlobalUserInfo.PlatformInfo.CurrentDistribution!.BaseDistro.Equals(DistroFamily.Debian)) {
                    Warning.Write("One or more dependencies are requiring a refresh of the apt-cache, please wait.");
                    RunCommand("apt-get", "update");
                }
            }
            catch (Exception ex) {
                WriteAndExit(
                    message: 
                        string.Join(Environment.NewLine, [
                            "Failed to update apt-cache using apt-get update",
                            "Error Log:",
                            ex.Message
                        ]),
                    status: 1
                );
            }
        }
        
        public static void RaspberryPiCheck()
        {
            if (!OperatingSystem.IsLinux()) {
                return;
            }

            try
            {
                var cpuContents = File.ReadAllLines("/proc/cpuinfo");

                if (cpuContents == null) { return; }


                foreach (var line in cpuContents)
                {
                    if (string.IsNullOrEmpty(line)) { continue; }

                    var match = PrecompiledRPIRegex().Match(line);

                    if (match == null || match.Groups.Count == 0) { continue; }
                    
                    match.Groups.TryGetValue("model", out var modelNameMatch);

                    if (modelNameMatch == null || !modelNameMatch.Success) { continue; }

                    var modelName = $"Raspberry Pi {modelNameMatch.Value}";

                    // Checks if the partial model string is present in modelNameMatch.Value
                    var validatedMatches = RPIModels.Where(m => modelNameMatch.Value.Contains(m.Key));

                    if (validatedMatches == null) {
                        WriteAndExit($"The {modelName} is not supported", status: 1);
                    }

                    // The value of the pair is a boolean determining whether the specified model can run the GUI.
                    var validatedMatch = validatedMatches.First();

                    GlobalUserInfo.PlatformInfo.IsPiDevice = true;
                    GlobalUserInfo.PlatformInfo.IsUnixLike = true;
                    GlobalUserInfo.PlatformInfo.SetRaspiModel(modelName, validatedMatch.Value);
                    
                }
            }
            catch (Exception ex) {
                Warning.Write($"A non fatal error occured while attempting to read from /proc/cpuinfo\n\nError Log:\n{ex.Message}");
            }
        }

        public static (string output, string error) RunCommand(string cmd, string args)
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
                
                if (proc == null) { return (output, error); }

                output = proc.StandardOutput.ReadToEnd();
                error = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode == 0) { return (output, error); }
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
    }
}
