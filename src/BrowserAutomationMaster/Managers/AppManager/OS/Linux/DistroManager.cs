using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux 
{
    public class DistroManager() 
    {
        public readonly static Distro[] distroObjects = [.. ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true)];

        public static Distro DetermineDistroFromID() 
        {
            var fileName = "/etc/os-release";
            try
            {

                if (!File.Exists(fileName)) 
                {
                    Warning.Write(
                        string.Join(NLC, [
                            "Unable to determine, the specific Linux distribution in use.",
                            "You will be prompted to select the base of your distro (Arch/Debian/Fedora/Etc)",
                            NLC,
                            "Warning: /etc/os-release was not found."
                        ])
                    );
                    return Distros.Unknown;
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
                            "Warning: ID field not found in os-release."
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
                        "You will be prompted to select the base of your distro (Arch/Debian/Fedora/Etc)",
                        NLC,
                        "Warning:",
                        ex.Message
                    ])
                );
                return Distros.Unknown;
            }
        }
    }
}