using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers.AppManager.OS
{
    public static partial class Linux
    {
        public static bool IsChromeOS { get; set; } = false;
        private static readonly Regex ForegroundMatch = ForegroundColorRegex();
        [GeneratedRegex("rgb:([0-9a-fA-F]+/[0-9a-fA-F]+).*?\n.{51}([0-9a-fA-F]+/[0-9a-fA-F]+)", RegexOptions.Compiled)]
        private static partial Regex ForegroundColorRegex();
        public static List<AppInfo> GetApps()
        {
            try
            {
                List<AppInfo> dpkgApps = [];
                List<AppInfo> flatpakApps = [];
                List<AppInfo> rpmApps = [];

                if (CommandExists("dpkg"))
                    dpkgApps.AddRange(ParseDpkgList());
                if (CommandExists("flatpak"))
                    flatpakApps.AddRange(ParseFlatpakList());
                if (CommandExists("rpm"))
                    rpmApps.AddRange(ParseRpmList());

                if (dpkgApps.Count == 0 && flatpakApps.Count == 0 && rpmApps.Count == 0)
                {
                    Errors.WriteErrorAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to detect any of the following commands:\n\n" +
                            "dpkg\nflatpak\nrpm\n",
                        status: 1
                    );
                }

                var appSources = new List<(string Name, List<AppInfo> Apps)>
                {
                    ("Debian Package Manager (dpkg)", dpkgApps),
                    ("Flatpak", flatpakApps),
                    ("RPM", rpmApps)
                };

                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                foreach (var (Name, Apps) in appSources)
                {
                    if (Apps.Count == 0) { Warning.Write($"Found 0 apps from: {Name}"); }
                    else if (Apps.Count == 1) { Success.WriteSuccessMessage($"Found 1 app from: {Name}"); }
                    else { Success.WriteSuccessMessage($"Found {Apps.Count} apps from: {Name}"); }
                }
                AnsiConsole.WriteLine(); // Adding a leading newline for readablity within terminal.
                return [.. dpkgApps.Concat(flatpakApps).Concat(rpmApps).Distinct()];
            }

            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
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
        public static string? GetTerminalBackgroundColor()
        {
            try
            {
                string black = "0000/0000/0000";
                if (IsChromeOS) 
                {
                    return black; 
                }
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
                        {
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
                    {
                        apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
                    }
                }
                return apps;
            }
            catch { Errors.WriteErrorAndContinue("DPKG not found, checking another method."); return []; }
        }

        // Parses apps installed via RPM (Red Hat Package Manager) (only for CentOS, Fedora, Oracle Linux, etc.)
        private static List<AppInfo> ParseRpmList()
        {
            var apps = new List<AppInfo>();
            var output = RunCommand("rpm", "-qa");
            foreach (var line in output.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    apps.Add(new AppInfo { Name = line });
                }
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
                {
                    apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
                }
            }
            return apps;
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
                {
                    return string.Empty;
                }
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    return output;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
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