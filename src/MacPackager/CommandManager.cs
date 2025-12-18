using static BrowserAutomationMaster.Managers.ConstantManager;

namespace MacPackager
{
    public class Command(string name, string description, string[] examples)
    {
        public string Name { get; private init; } = name;
        public string Description { get; private init; } = description;
        public string[] Examples { get; private init; } = examples;
    }

    public static class CommandManager
    {
        public static readonly List<Command> CommandList = new()
        {

            {
                new Command(
                    name: "--edit-config",
                    description: "Edits the Build Config associated with The BAMM for macOS Packager.",
                    examples: [ "bamm-macos-packager --edit-config"]
                )
            },

            {
                new Command(
                    name: "--gui",
                    description: 
                        string.Join(NLC, [
                            "Please Note:",
                            "This functional is not currently available in the packager.",
                            "The code for the menu and command execution were copied and modified directly from BAMM.",
                            "Launches an HTTP Server and the user's default browser to the Graphical User Interface (GUI)"
                        ]),
                    examples: [
                        "bamm-macos-packager --gui",
                        "bamm-macos-packager --gui --port==42069"
                    ]
                )
            },

            {
                new Command(
                    name: "--new-config",
                    description:
                        string.Join(NLC, [
                            "Overwrites the contents of the current build config (if present) with the default build config values.",
                            "This can be useful for situations where you have a missing or malformed build config."
                        ]),
                    examples: [
                        "bamm --new-config",
                        "bamm --new-config"
                    ]
                )
            },

            {
                new Command(
                    name: "--platform-debug",
                    description:
                        string.Join(NLC, [
                            "Displays the platform information associated with the current session.",
                            "This should only be used for development, or if requested on github.",
                        ]),
                    examples: [ "bamm-macos-packager --platform-debug" ]
                )
            },

            {
                new Command(
                    name: "--query-display",
                    description: "Displays whether or not your system has the $DISPLAY variable set, does not work on Windows!",
                    examples: ["bamm-macos-packager --query-display"]
                )
            },
            

            {
                new Command(
                    name: "--version",
                    description: "Displays the current version of BAMM, and whether there's a new version available.",
                    examples: [ "bamm --version" ]
                )
            },

            {
                new Command(
                    name: "build",
                    description:
                        string.Join(NLC, [
                            "Starts packaging with the provided values, instead of using the build config.",
                        ]),
                    examples: [ 
                        "bamm build --binary=<path> --target=x64    # For Intel Macs", 
                        "bamm build --binary=<path> --target=ARM64  # For Silicon Macs", 
                    ]
                )
            },
        };

        public static bool CommandExists(string name)
        {
            return CommandList.Any(cmd => cmd.Name.Equals(name));
        }

        public static Command? GetCommand(string name)
        {
            IEnumerable<Command>? command = CommandList.Where(cmd => cmd.Name.Equals(name));
            if (!command.Any()) {
                return null;
            }
            return command.First();
        }

        public static string? GetDescription(string name)
        {
            IEnumerable<Command>? command = CommandList.Where(cmd => cmd.Name.Equals(name));
            if (!command.Any()) {
                return null;
            }
            return command.First().Description;
        }

        public static string[] GetExamples(string name)
        {
            IEnumerable<Command>? command = CommandList.Where(cmd => cmd.Name.Equals(name));
            if (!command.Any()) {
                return [];
            }
            return command.First().Examples;
        }

        public static string[] GetExamples(Command c)
        {
            IEnumerable<Command>? commands = CommandList.Where(cmd => cmd.Equals(c));
            if (!commands.Any()) {
                return [];
            }
            return commands.First().Examples;
        }
    }
}