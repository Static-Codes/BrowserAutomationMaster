using BrowserAutomationMaster.Messaging;
using System.Diagnostics;

namespace BrowserAutomationMaster.AppManager.OS
{
    public static class Linux
    {
        public static List<AppInfo> GetApps()
        {
            List<AppInfo> apps = [];
            if (CommandExists("dpkg"))
                apps.AddRange(ParseDpkgList());
            if (CommandExists("rpm"))
                apps.AddRange(ParseRpmList());
            if (CommandExists("flatpak"))
                apps.AddRange(ParseFlatpakList());
            if (apps.Count == 0){
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to detect any of the following commands:\n\ndpkg\nflatpak\nrpm\n", 1);
                return []; // This wont actually be returned but its here to appease the compiler's static nature.
            }
            return apps;
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

        // Parses apps installed via DPKG (Debian Package Manager) (apt utilizes DPKG so most users will be using apt install.)
        private static List<AppInfo> ParseDpkgList()
        {
            try {
                var apps = new List<AppInfo>();
                var output = RunCommand("dpkg-query", "-W -f \"${Package}\t${Version}\n\"");
                foreach (var line in output.Split('\n'))
                {
                    var parts = line.Trim('\'').Split("\t");
                    if (parts.Length >= 2)
                    {
                        Success.WriteSuccessMessage("Dpkg App Found");
                        apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
                    }
                }
                return apps;
            }
            catch { Errors.WriteErrorAndContinue("Houston we have a problem");  return []; } // Silently catches the error, no output is currently required.
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
                    Success.WriteSuccessMessage("Rpm App Found");
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
                    Success.WriteSuccessMessage("Flatpak App Found");
                    apps.Add(new AppInfo { Name = parts[0], Version = parts[1] });
                }
            }
            return apps;
        }


    private static string RunCommand(string cmd, string args)
    {
        try
        {
            Console.WriteLine($"Running command: {cmd} {args}");
            var procStartInfo = new ProcessStartInfo
            {
                FileName = cmd,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var proc = Process.Start(procStartInfo))
            {
                if (proc == null)
                {
                    Console.WriteLine("DEBUG: Process.Start returned null.");
                    return string.Empty;
                }

                string output = proc.StandardOutput.ReadToEnd();
                string errorOutput = proc.StandardError.ReadToEnd();

                proc.WaitForExit();

                Console.WriteLine($"Exit Code: {proc.ExitCode}");
                if (!string.IsNullOrEmpty(output)) { Console.WriteLine($"Stdout:\n{output}"); }
                if (!string.IsNullOrEmpty(errorOutput)) { Errors.WriteErrorAndContinue($"Stderr:\n{errorOutput}"); }
                if (proc.ExitCode == 0) {  return output; }
                else { return string.Empty; }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception in RunCommand: {ex.ToString()}"); // Debug line
            return string.Empty; // Original behavior
        }
    }
}
}