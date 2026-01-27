using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.Functions;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux 
{
    public class DistroManager() 
    {
        public readonly static Distro[] distroObjects = [.. ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true)];
        public readonly static IEnumerable<string> altCmds = distroObjects.Select(d => $"{d.BackupReleaseCmd} {d.BackupReleaseCmdArgs}");

        public static Distro DetermineDistro() 
        {
            var fileName = "/etc/os-release";
            try
            {
                var releaseFileFound = File.Exists(fileName);

                if (!releaseFileFound) 
                {
                    Warning.Write($"Warning: {fileName} was not found.");
                    return TryAltCmds() ?? Distros.Unknown;
                }

                // Optimization: Find the line starting with ID=, split by '=', and trim quotes in one pass
                var idLine = File.ReadLines(fileName)
                    .FirstOrDefault(line => line.StartsWith("ID=", StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(idLine)) 
                {
                    Warning.Write(
                        string.Join(NLC, [
                            "Unable to determine, the specific Linux distribution in use.",
                            "You will be prompted to select the base of your distro (Arch/Debian/Fedora/Etc)",
                            NLC,
                            $"Warning: ID field not found in: {fileName}"
                        ])
                    );
                    return Distros.Unknown;
                }

                // Sanitizing captured value (For example: ID="ubuntu" -> ubuntu)
                var sanitizedID = idLine.Split('=')[1].Trim('"').Trim('\'');

                Console.WriteLine(sanitizedID);

                return distroObjects.FirstOrDefault(distro => distro.ID == sanitizedID) ?? Distros.Unknown;

            }

            catch (Exception ex) 
            {
                Warning.Write(
                    string.Join(NLC, [
                        "Unable to determine, the specific Linux distribution in use.",
                        "You will be prompted to select the base of your distro.",
                        NLC,
                        "Warning:",
                        ex.Message
                    ])
                );
                return Distros.Unknown;
            }
        }

        private static Distro? TryAltCmds() 
        {
            foreach (var altCmd in altCmds) 
            {
                try 
                {
                    (var output, var error) = RunCommand("/bin/bash", $"-c '{altCmd}'");
                    if (output == null || error != null) {
                        continue;
                    }
                    
                    switch (output) {
                        case "FreeBSD" when altCmd is "uname -o":
                            return Distros.FreeBSD;
                    }
                }
                catch (Exception ex) 
                {
                    Warning.Write(ex.Message);
                }
            }
            return null;
        }
    }
}