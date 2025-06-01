using BrowserAutomationMaster.Messaging;
using System.ComponentModel.Design;
using System.Diagnostics;

namespace BrowserAutomationMaster.AppManager.OS
{
    public static class Linux
    {
        public static List<AppInfo> GetApps()
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

            if (dpkgApps.Count == 0 && flatpakApps.Count == 0 && rpmApps.Count == 0) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to detect any of the following commands:\n\ndpkg\nflatpak\nrpm\n", 1);
                return []; // This wont actually be returned but its here to appease the compiler's static nature.
            }
            
            var appSources = new List<(string Name, List<AppInfo> Apps)>
            {
                ("Debian Package Manager (dpkg)", dpkgApps),
                ("Flatpak", flatpakApps),
                ("RPM", rpmApps)
            };

            Console.WriteLine(); // Adding a leading newline for readablity within terminal.
            foreach (var (Name, Apps) in appSources) {
                if (Apps.Count == 0) { Warning.Write($"Found 0 apps from: {Name}"); }
                else if (Apps.Count == 1) { Success.WriteSuccessMessage($"Found 1 app from: {Name}"); }
                else { Success.WriteSuccessMessage($"Found {Apps.Count} apps from: {Name}"); }
            }
            Console.WriteLine(); // Adding a leading newline for readablity within terminal.

            try
            {
                return [
                    .. dpkgApps.Select(x => x),
                    .. flatpakApps.Where(x => true)
                    .Concat(rpmApps.OrderBy(x => x)).Distinct()
                ];
            }

            catch (Exception ex)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to parse installed system applications, please see the error below:\n\n{ex}", 1);
                return [];
            }
        }

        // Instead of parsing each distro by type finding the available commands is much more efficient.
        static bool CommandExists(string cmd)
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

        // Parses apps installed via DPKG (Debian Package Manager) (apt utilizes DPKG so most users will be using apt install.)
        static List<AppInfo> ParseDpkgList()
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
            catch { Errors.WriteErrorAndContinue("Houston we have a problem"); return []; } // Silently catches the error, no output is currently required.
        }

        // Parses app installed via RPM (Red Hat Package Manager) (only for CentOS, Fedora, Oracle Linux, etc.)
        static List<AppInfo> ParseRpmList()
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

        // Parses app installed via Flatpak
        static List<AppInfo> ParseFlatpakList()
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

        static string RunCommand(string cmd, string args)
        {
            try
            {
                var procStartInfo = new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(procStartInfo);
                if (proc == null) { return string.Empty; }

                string output = proc.StandardOutput.ReadToEnd();

                proc.WaitForExit();
                if (proc.ExitCode == 0) { return output; }
                else { return string.Empty; }
            }
            catch (Exception ex){
                Console.WriteLine (ex);
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to query installed apps using cmd: {cmd}", 1);

                return string.Empty; 
            }
        }

    }
}