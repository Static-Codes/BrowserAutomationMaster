using Spectre.Console;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;

namespace BrowserAutomationMaster.Managers
{
    public class Command(string name, string description, string[] examples, CommandType type)
    {
        public string Name { get; private init; } = name;
        public string Description { get; private init; } = description;
        public string[] Examples { get; private init; } = examples;
        public CommandType Type { get; private init; } = type;
    }
    public enum CommandType
    {
        Action = 0,
        Argument = 1,
        Feature = 2,
    }

    public static class CommandManager
    {
        public static readonly List<Command> CommandList = new()
        {
            {
                new Command(
                    name: "--bs",
                    description: $"Instructs BAMM to use BrowserStack, for more information see: {BASE_REPO_LINK}",
                    examples: [ "bamm --bs" ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--editbsconf",
                    description: $"Edit the Browserstack Config found at {GetBrowserStackConfigPath()}",
                    examples: [ "bamm --editbsconf"],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--gui",
                    description: "Launches an HTTP Server and the user's default browser to the Graphical User Interface (GUI)",
                    examples: [
                        "bamm --gui",
                        "bamm --gui --port==42069"
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--nohwc",
                    description: 
                        "Instructs BAMM not to check your system's hardware for compatibility, " +
                        "this should not be done unless you've already verified BAMM can run on your machine.",
                    examples: [ "bamm --nohwc" ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--query-display",
                    description: "Displays whether or not your system has the $DISPLAY variable set, does not work on Windows!",
                    examples: ["bamm --query-display"],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--set-custom-useragent",
                    description:
                        "Sets a custom user agent for the current script.\n" +
                        "Use this instead set-custom-useragent if the script needs to start with a certain user agent.",
                    examples: [
                        "bamm --set-custom-useragent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0\"",
                        "bamm --set-custom-useragent \"custom-user-agent-string-for-your-site\"",
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "--set-timeout",
                    description: "Sets the timeout for all actions in all scripts to 10 seconds",
                    examples: [ "bamm --set-timeout 10" ],
                    type: CommandType.Argument
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
                    examples: [ "bamm --nohwc --platform-debug" ],
                    type: CommandType.Argument
                )
            },
            {
                new Command(
                    name: "add",
                    description:
                        string.Join(NLC, [
                            "Adds the specified file to the userScripts directory.",
                            "The script cannot already exist in the userScript directory.",
                        ]),
                    examples: [ "bamm add \"path/to/external/file.bamc\"", ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "add-header",
                    description: "Adds an HTTP Header for the current request.",
                    examples: [
                        "add-header \"DNT\" \"1\"",
                        "add-header \"Referer\" \"https://google.com/search\"",
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "add-headers",
                    description: "Adds multiple HTTP Headers for the current request via a JSON Object.",
                    examples: [
                        Markup.Escape("add-headers {{\"header-name1\": \"header-value1\", \"header-name2\": \"header-value2\"}}"),
                        Markup.Escape("add-headers {{\"DNT\": \"1\", \"Referer\": \"https://google.com/search\"}}"),
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "browser",
                    description: "Specifies the browser you wish to use, if not specified the default is firefox.",
                    examples: [
                        "browser \"chrome\"",
                        "browser \"firefox\""
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "clear",
                    description: "Deletes the specified application directory.",
                    examples: [
                        "bamm clear compiled",
                        "bamm clear config",
                        "bamm clear userScripts",
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "click",
                    description:
                        "Clicks the specified button element.  " +
                        "Supports ID, CLASS_NAME, NAME, TAG_NAME, and XPATH selectors. ",
                    examples: [
                        "click \"#id-selector\"",
                        "click \".class-name\"",
                        "click \"[name='actual_value']\"",
                        "click \"tag-name\"",
                        "click \"[data-value=\"some complex 'value' with quotes\"]\"",
                        "click \"//div[@class='ql-editor ql-blank textarea new-input-ui']//p\"",
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "click-at-position",
                    description: "Clicks at a specific point on screen, based on the provided coordinates \"X\", \"Y\"",
                    examples: [
                        "click-at-position \"600\" \"600\"",
                        "click-at-position \"1200\" \"200\""
                    ],
                    type: CommandType.Action
                )

            },

            {
                new Command(
                    name: "click-exp",
                    description:
                        "Alternative to click; use this if click is causing issues.  " +
                        "Only supports CSS Selectors.",
                    examples: [ "click-exp 'css-selector.item_element'" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "close-current-tab",
                    description: "Closes the currrent tab and will close the browser if there's only one open tab.",
                    examples: [ "close-current-tab" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "compile",
                    description:
                        "Compiles the specified .bamc file.\n\n" +
                        "The full file path is NOT required if the file exists in the `compiled` directory.",
                    examples: [
                        "bamm compile ebay.bamc",
                        "bamm compile path/to/external/newFile.bamc"
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "delete",
                    description: "Deletes the specified .bamc file.",
                    examples: [
                        "bamm delete filename.bamc",
                        "bamm delete scriptName.BAMC"
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "disable-pycache",
                    description:
                        "Instructs the compiler to disable the writing of the __pycache__ directory.\n" +
                        "This directory is written by Visual Studio Code and contains .pyc files.",
                    examples: [ "feature \"disable-pycache\"" ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name : "disable-ssl",
                    description: "Instructs the compiler to disable SSL certificate authentication for the given session.",
                    examples: [ "feature \"disable-pycache\"" ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name : "end-javascript",
                    description:
                        "Instructs the parser that the end of a JavaScript code block was reached. " +
                        "An error will be thrown if end-javascript is not found within the file " +
                        "(when a start-javascript is present). ",
                    examples: [ "end-javascript" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "fill-text",
                    description: "",
                    examples: [
                        "fill-text \"selector\" \"Value you want to include\"",
                        "fill-text \"#id-selector\" \"Text\""
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "fill-text-exp",
                    description: "More advanced version of fill-text.",
                    examples: [
                        "fill-text-exp \"selector\" \"Value you want to include\"",
                        "fill-text-exp \"#id-selector\" \"Text\""
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "get-text",
                    description: "Gets the text for a specified element. Supports ID, NAME, TAG NAME, and XPATH selectors.",
                    examples: [
                        "get-text \"#id\"",
                        "get-text \".class-name\"",
                        "get-text \"tag_name\"",
                        "get-text \"xpath\\is\\also\\supported\"",
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "help",
                    description: "Brings up the help menu.",
                    examples: [
                        "bamm help commandName",
                        "bamm help browser",
                        "bamm help take-screenshot",
                        "bamm help save-as-html",
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "open-new-tab",
                    description: "A new browser tab is opened, the system will then pause for the number of seconds specified, then visits the requested url.",
                    examples: [
                        "open-new-tab \"https://google.com\" \"3\"",
                        "open-new-tab \"https://github.com\" \"5\""
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "run",
                    description:
                        "Runs any python file however it is strongly recommended to ONLY use this command for scripts compiled using BAMM, " +
                        "specifically ones located in the compiled directory. There is no guarantee this will work with external python scripts",
                    examples: [
                        "bamm run localFile.bamc",
                        "bamm run path/to/external/file.bamc"
                    ],
                    type: CommandType.Argument
                )
            },

            {
                new Command(
                    name: "run-headless",
                    description: "Instructs the compiler to allow headless execution for the duration of the current script.",
                    examples: [ "feature \"run-headless\"" ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name : "save-as-html",
                    description: "Saves the current page's HTML to a file with the specified filename.",
                    examples: [ "save-as-html \"filename.html\"" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "save-as-html-exp",
                    description:
                        "Saves the current page's HTML to a file with the specified name but uses different logic.\n" +
                        "Use this if save-as-html doesn't fit your needs.",
                    examples: [ "save-as-html-exp \"filename.html\"" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "select-option",
                    description: "Selects an <option> from a <select> dropdown menu. Currently only supports <select><option></option></select>.",
                    examples: [
                        "select-option \"[name='dropdown']\" 2",
                        "select-option \"#dropdown\" 5",
                        "select-option \"select\" 5"
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "select-element",
                    description:
                        "Selects the element associated with the provided selector (if found).\n" +
                        "This currently works but, there's no logic to access the selected element.\n" +
                        "This should only be done if you're manually editing the compiled Python script. ",
                    examples: [ "select-element \"selector\"" ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name : "set-custom-useragent",
                    description: "Sets a custom user agent for the rest of the script.",
                    examples: [
                        "set-custom-useragent \"Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0\"",
                        "set-custom-useragent \"custom-user-agent-string-for-your-site\"",
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "start-javascript",
                    description:
                        "Instructs the parser to read all following lines as a .js code block, until end-javascript is found.\n" +
                        "Will throw an error if end-javascript is not found within the file.",
                    examples: [
                        "start-javascript",
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "take-screenshot",
                    description:
                        "Takes a screenshot of the browser after executing the previous line. " +
                        "It's recommended to add a \"wait-for-seconds\" command before executing this. ",
                    examples: [
                        "take-screenshot \"filename.png\"",
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "visit",
                    description: "Visits a specified URL.",
                    examples: [
                        "visit \"https://url-to-visit.com/page.html\"",
                        "visit \"https://google.com/\""
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "wait-for-seconds",
                    description: "Waits for the specified number of seconds before continuing, supports decimals",
                    examples: [
                        "wait-for-seconds 1",
                        "wait-for-seconds .5",
                        "wait-for-seconds 0.5"
                    ],
                    type: CommandType.Action
                )
            },

            {
                new Command(
                    name: "use-http-proxy",
                    description:
                        "Uses the entered http proxy for the session.\n" +
                        "Use feature \"use-http-proxy\" \"NULL:NULL@IP:PORT\" " +
                        "if no user:pass authentication is required.",
                    examples: [
                        "feature \"use-http-proxy\" \"USER:PASS@IP:PORT\"",
                        "feature \"use-http-proxy\" \"NULL:NULL@IP:PORT\""
                    ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name: "use-https-proxy",
                    description:
                        "Uses the entered https proxy for the session.\n" +
                        "Use feature \"use-https-proxy\" \"NULL:NULL@IP:PORT\" " +
                        "if no user:pass authentication is required.",
                    examples: [
                        "feature \"use-https-proxy\" \"USER:PASS@IP:PORT\"",
                        "feature \"use-https-proxy\" \"NULL:NULL@IP:PORT\""
                    ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name: "use-socks4-proxy",
                    description:
                        "Uses the entered SOCKS4 proxy for the session.\n" +
                        "Use feature \"use-socks4-proxy\" \"NULL:NULL@IP:PORT\" " +
                        "if no user:pass authentication is required.",
                    examples: [
                        "feature \"use-socks4-proxy\" \"USER:PASS@IP:PORT\"",
                        "feature \"use-socks4-proxy\" \"NULL:NULL@IP:PORT\""
                    ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name: "use-socks5-proxy",
                    description:
                        "Uses the entered SOCKS5 proxy for the session.\n" +
                        "Use feature \"use-socks5-proxy\" \"NULL:NULL@IP:PORT\" " +
                        "if no user:pass authentication is required.",
                    examples: [
                        "feature \"use-socks5-proxy\" \"USER:PASS@IP:PORT\"",
                        "feature \"use-socks5-proxy\" \"NULL:NULL@IP:PORT\""
                    ],
                    type: CommandType.Feature
                )
            },

            {
                new Command(
                    name: "uninstall",
                    description: "Uninstalls BAMM on windows, and gives instructions or Mac/Linux",
                    examples: [
                        "bamm uninstall",
                    ],
                    type: CommandType.Argument
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

        public static List<Command> GetCommands(CommandType type)
        {
            if (!Enum.TryParse(typeof(CommandType), type.ToString(), out object? result)) {
                throw new ArgumentException($"Invalid type provided to CommandManager.GetCommands({type}");
            }
            return [.. CommandList.Where(cmd => cmd.Type.Equals(type))];
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

        public static CommandType? GetType(string name)
        {
            IEnumerable<Command>? commands = CommandList.Where(cmd => cmd.Name.Equals(name));
            if (!commands.Any()) {
                return null;
            }
            return commands.First().Type;
        }
    }
}
