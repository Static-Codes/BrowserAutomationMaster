using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Diagnostics;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers.AppManager.OS
{
    public static partial class Linux
    {
        public static bool IsChromeOS { get; set; } = false;

        // Debian Package Manager
        readonly public static bool HasDPKG = CommandExists("dpkg");

        // Flatpak Package Manager
        readonly public static bool HasFlatpak = CommandExists("flatpak");

        // Red Hat Package Manager
        readonly public static bool HasRPM = CommandExists("rpm");

        readonly public static List<AppInfo> dpkgApps = HasDPKG ? ParseDpkgList() : [];

        readonly public static List<AppInfo> flatpakApps = HasFlatpak ? ParseFlatpakList() : [];

        readonly public static List<AppInfo> rpmApps = HasRPM ? ParseRpmList() : [];

        public static List<AppInfo> GetApps()
        {
            try
            {
                if (dpkgApps.Count == 0 && flatpakApps.Count == 0 && rpmApps.Count == 0)
                    Errors.WriteAndExit(
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

                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.

                foreach (var (Name, Apps) in appSources)
                {
                    if (Apps.Count == 0)
                        Warning.Write($"No apps found for: {Name}");

                    else if (Apps.Count == 1)
                        Success.WriteSuccessMessage($"Found 1 app from: {Name}");

                    else
                        Success.WriteSuccessMessage($"Found {Apps.Count} apps from: {Name}");
                }

                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                return [.. dpkgApps.Concat(flatpakApps).Concat(rpmApps).Distinct()];
            }

            catch (Exception ex)
            {
                Errors.WriteAndExit(
                    $"BAM Manager (BAMM) was unable to parse installed system applications, " +
                    $"please see the error below:\n\n{ex}",
                    status: 1
                );
                return [];
            }
        }

        public static void ChromeOSCheck()
        {

            if (!OperatingSystem.IsLinux())
            {
                IsChromeOS = false;
                return;
            }

            try
            {
                string cmdline = File.ReadAllText("/proc/cmdline");
                IsChromeOS = cmdline.Contains("cros_");
            }
            catch
            {
                IsChromeOS = false;
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

        public static string? GetTerminalBackgroundColor()
        {
            try
            {
                string black = "0000/0000/0000";
                
                if (IsChromeOS || IsOSX)
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

        public static void InstallRequiredLinuxPackages(List<AppInfo> appsInfo)
        {

            Warning.Write("Installing the Required Linux Packages, please wait up to 60 seconds");

            var inputMessage = @"Supported versions include:
- Python 3.9.X
- Python 3.10.X
- Python 3.11.X
- Python 3.12.X
- Python 3.13.X
- Python 3.14.X

Examples:
Python 3.9
Python 3.12.7
Python 3.9.8

Version: ";

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
                    Errors.WriteAndExit("An error occured while attempting to access your Distribution's Package Manager, please try again.", 1);
            }



            var installPrefix = (HasDPKG, HasRPM) switch
            {
                (true, _) => $"DEBIAN_FRONTEND=noninteractive {PKM_CMD} install -y",
                (_, true) => $"{PKM_CMD} install -y",
                (_, _) => null
            };

            var installCMD = $"-c \"sudo {installPrefix}";

            var potentialVersion = Installations.GetMissingPyVersion();
            while (potentialVersion == null || !PyVersionRegex.IsMatch(potentialVersion))
            {
                Warning.Write("Unable to detect the installed version of Python.");
                potentialVersion = Input.AskForInput(inputMessage);
            }


            string[] packages = [
                "xclip", // Used for auto_copy_path
                $"python{potentialVersion.Replace("Python ", "")}-venv"  // Used for majority of BAMM to create vEnv(s)
            ];

            if (installPrefix == null)
                Errors.WriteAndExit($"Unable to install the following required Linux Packages:\n{string.Join('\n', packages)}", 1);


            string[] commands = new string[packages.Length];

            var actionString = $"to install the following required Linux Packages:\n{string.Join('\n', packages)}";

            for (int i = 0; i < packages.Length; i++)
            {
                commands[i] = $"{installCMD} {packages[i]}\"";
                var appInfo = new AppInfo() { Name = packages[i] };

                // Skips pre-existing installations
                if (appsInfo.Contains(appInfo))
                    continue;

                WriteMessage($"Installing package: {packages[i]}");
                Console.WriteLine(RunCommand("/bin/bash", $"{commands[i]}", installingPackages: true));
            }
        }

        // Parses apps installed via DPKG (Debian Package Manager) (apt utilizes DPKG so most users will be using apt install.)
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
                        apps.Add(
                            new AppInfo { 
                                Name = parts[0], 
                                Version = parts[1] 
                            }
                        );
                }
                return apps;
            }
            catch { 
                Errors.Write("DPKG not found, checking another method."); 
                return []; 
            }
        }

        // Parses apps installed via RPM (Red Hat Package Manager) (only for CentOS, Fedora, Oracle Linux, etc.)
        private static List<AppInfo> ParseRpmList()
        {
            var apps = new List<AppInfo>();
            var output = RunCommand("rpm", "-qa");
            
            foreach (var line in output.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    apps.Add(new AppInfo { Name = line });
            }

            return apps;
        }

        // Parses apps installed via Flatpak
        private static List<AppInfo> ParseFlatpakList()
        {
            var apps = new List<AppInfo>();
            var output = RunCommand("flatpak", "list");

            foreach (var line in output.Split('\n'))
            {
                var parts = line.Split('\t');
                
                if (parts.Length >= 2)
                    apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
            }

            return apps;
        }
        public static string RunCommand(string cmd, string args, bool installingPackages = false)
        {
            try
            {

                //if (installingPackages)
                //    Console.WriteLine($"{cmd} {args}");

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
                Errors.WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to execute a necessary command, if this issue persists, " +
                        $"please make a bug report at {ISSUES_LINK}\nError log:\nUnable to execute\n" +
                        $"{cmd}\nException:\n{ex.Message}",
                    status: 1
                );
                return string.Empty;
            }
        }

    }
}