using BrowserAutomationMaster.Messaging;
using System.Diagnostics;

namespace BrowserAutomationMaster.AppManager.OS
{
    public static class Linux
    {
        public static List<AppInfo> GetApps()
        {
            if (CommandExists("dpkg"))
                return ParseDpkgList();
            if (CommandExists("rpm"))
                return ParseRpmList();
            if (CommandExists("flatpak"))
                return ParseFlatpakList();
            Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to detect any of the following commands:\n\ndpkg\nflatpak\nrpm\n", 1);
            return []; // This wont actually be returned but its here to appease the compiler's static nature.
        }

        // Instead of parsing each distro by type finding the available commands is much more efficient.
        private static bool CommandExists(string cmd)
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

        // Parses apps installed via DPKG (Debian Package Manager) (apt utilizes DPKG so most users will be using apt installed.)
        private static List<AppInfo> ParseDpkgList()
        {
            var apps = new List<AppInfo>();
            var output = RunCommand("dpkg-query", "-W -f='${Package} ${Version}\n'");
            foreach (var line in output.Split('\n'))
            {
                var parts = line.Trim('\'').Split(' ');
                if (parts.Length >= 2)
                {
                    apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
                }
            }
            return apps;
        }

        // Parses app installed via RPM (Red Hat Package Manager) (only for CentOS, Fedora, Oracle Linux, etc.)
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

        // Parses app installed via Flatpak
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

        private static string RunCommand(string cmd, string args)
        {
            try
            {
                var proc = Process.Start(new ProcessStartInfo
                {
                    FileName = cmd,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                });
                if (proc == null) { return string.Empty; }
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                return output;
            }
            catch { return string.Empty; }
        }
    }
}