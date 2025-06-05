using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using System.Text.Json.Serialization;
using Esprima.Ast;

namespace BrowserAutomationMaster.Messaging
{
    public class Help
    {
        public static Dictionary<string, string> AllCommands { get; set; } = [];
        public class HelpType
        {
            [JsonPropertyName("Actions")]
            public Dictionary<string, string> Actions { get; set; } = [];

            [JsonPropertyName("Arguments (CLI)")]
            public Dictionary<string, string> Arguments { get; set; } = [];

            [JsonPropertyName("Features")]
            public Dictionary<string, string> Features { get; set; } = [];
        }

        public static HelpType? Parse(string jsonString) // Can return null, handle accordingly.
        {
            try { return JsonSerializer.Deserialize<HelpType>(jsonString)!; }

            catch {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) could not parse embedded help string, this is a huge bug and needs to be fixed.", 1);
                return null;
            }
        }

        public static void DisplayAvailableCommands()
        {
            string jsonInput = @"
{
    ""Actions"": {
        ""click"": ""Clicks the specified button element"",
        ""click-exp"": ""Alternative to click, use this if click is causing issues."",
        ""get-text"": ""Gets the text for a specified element."",
        ""end-javascript"": ""Instructs the parser that the end of a javascript code block was reached.  An error will be thrown if end-javascript is not found within the file."",
        ""fill-text"": ""Assigns the specified value to the selected element."",
        ""save-as-html"": ""Saves the current pages HTML to a file with the specified name."",
        ""save-as-html-exp"": ""Saves the current pages HTML to a file with the specified name but uses different logic, use this if 'save-as-html' doesn't fit your needs.'"",
        ""select-option"": ""Selects an <option> from a <select> dropdown menu, currently only supports <select><option></option></select>"",
        ""select-element"": ""Selects the element associated with the provided selector (if found)."",
        ""start-javascript"": ""Instructs the parser to read all following lines as a .js code block, until end-javascript is found; Will throw an error if end-javascript is not found within the file."",
        ""take-screenshot"": ""Takes a screenshot of the browser after executing the previous line."",
        ""visit"": ""Visits a specified url."",
        ""wait-for-seconds"": ""Waits for the specified number of seconds before continuing.""
    },
    ""Arguments (CLI)"": {
        ""add"":  ""Adds a local .bamc file to the userScripts directory.\nSyntax:bamm.exe add 'filename'"",
        ""--set-timeout"": ""Sets the default timeout for all selenium based browser actions.""
    },
    ""Features"": {
        ""async"": ""This indicates the compiler you want to create an asynchronous script, this should not be done unless you have experience using async functions in python."",
        ""browser"": ""This MUST be the first valid line of the file; If not supplied, defaults to firefox instance or user agent (depending on the other defined features)"",
        ""bypass-cloudflare"": ""Instructs the browser to use a more advanced approach to bypass cloudflare"",
        ""disable-pycache"": ""Instructs the compiler to disable the writing of the __pycache__ directory.  This directory is written by Visual Studio Code and contains .pyc files"",
        
        ""use-http-proxy"": ""Uses the entered http proxy for the session."",
        ""use-https-proxy"": ""Uses the entered https proxy for the session."",
        ""use-socks4-proxy"": ""Uses the entered socks4 proxy for the session."",
        ""use-socks5-proxy"": ""Uses the entered socks5 proxy for the session.""
    }
}";

            HelpType? config = Parse(jsonInput); // Can potentially be null, thus the null check below.


            if (config == null) {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) could not parse embedded help string, this is a huge bug and needs to be fixed.", 1);
            }

            Console.WriteLine("\n--- Action Commands ---");
            // Null check is above so if this line is reached config will not be null.
            if (config!.Actions != null) {
                foreach (var args in config.Actions) {
                    Console.WriteLine($"     {args.Key}");
                    if (!AllCommands.TryGetValue(args.Key, out string? _)) {
                        AllCommands.Add(args.Key, args.Value);
                    }
                }
            }


            Console.WriteLine("\n--- Command Line Arguments ---");
            if (config.Arguments != null) {
                foreach (var args in config.Arguments) {
                    Console.WriteLine($"     {args.Key}");
                    if (!AllCommands.TryGetValue(args.Key, out string? _)) {
                        AllCommands.Add(args.Key, args.Value);
                    }
                }
            }


            Console.WriteLine("\n--- Feature Commands ---");
            if (config.Features != null) {
                foreach (var args in config.Features) {
                    Console.WriteLine($"     {args.Key}");
                    if (!AllCommands.TryGetValue(args.Key, out string? _)) {
                        AllCommands.Add(args.Key, args.Value);
                    }
                }
            }
            Console.WriteLine("\n");
        }
        
        public static string GetDescriptionOfCommand(string command)
        {
            // This function assumes the paramater command is within AllCommands, and should only be run from ShowCommandDetails()
            if (!AllCommands.TryGetValue(command, out string? description)) { description = "Not Found"; }
            if (description.Equals("Not Found"))
            {
                if (command != "bamm help --all")
                {
                    Errors.WriteErrorAndContinue("Invalid command provided, for more information on valid commands, please type:\n\nbamm help --all");
                }
                else
                {
                    DisplayAvailableCommands();
                }
            }
            return description;
        }

        public static string GetExampleOfCommand(string command)
        {
            Dictionary<string, string> CommandExamples = new() {
                { "click", "click \"#id-selector\"\r\nclick \".class_name_selector\"\r\nclick \"//xpath//supported//aswell\"\r\nclick \"tag-name\"\n" },
                { "click-exp", "click-exp 'css-selectors-supported.with-click-exp'\n" },
                { "get-text", "get-text \"#id-selector\"\r\nget-text \".class_name_selector\"\r\nget-text \"//xpath//supported//aswell\"\r\nget-text \"tag-name\"\n" },
                { "end-javascript", "" },
                { "fill-text", "fill-text \"textbox-selector\" \"value\"\n" },
                { "save-as-html", "save-as-html \"filename.html\"\n" },
                { "save-as-html-exp", "save-as-html-exp \"filename2.html\"\n" },
                { "select-option", "select-option \"dropdown-selector\" 2\n\nThe number at the end of this command (in this case 2), correlates to the option number.\nFor example:\nYou are given 4 options\nA.\nB.\nC.\nD.\n\nIf you enter 2 as the option number, it will select B.\n" },
                { "select-element", "select-element \"dropdown-element-selector\"\r\n" },
                { "start-javascript", "start-javascript\n...Your javascript code goes here\nend-javascript\n" },
                { "take-screenshot", "take-screenshot \"filename.png\"" },
                { "visit", "visit \"https://url-to-visit.com\"" },
                { "wait-for-seconds", "wait-for-seconds 5\n\nThis command also accepts decimals, so if you want to wait for 1/5 of a second (200ms), just type .2" },
                { "add", "bamm add \"path\to\file.bamc\"\n" },
                { "--set-timeout", "bamm --set-timeout 5\n\nThis sets the timeout for all actions to 5 seconds.\n" },
                { "async", "feature \"async\"\n" },
                { "browser", "browser \"brave\"\nbrowser \"chrome\"\nbrowser \"firefox\"\n" },
                { "bypass-cloudflare", "feature \"bypass-cloudflare\"\n" },
                { "disable-pycache", "feature \"disable-pycache\"\n" },
                { "use-http-proxy", "feature \"use-http-proxy\"\n" },
                { "use-https-proxy", "feature \"use-https-proxy\"\n" },
                { "use-socks4-proxy", "feature \"use-socks4-proxy\"\n" },
                { "use-socks5-proxy", "feature \"use-socks5-proxy\"\n" },
            };
            if (!CommandExamples.TryGetValue(command, out string? example)) { example = "Not Found"; }
            if (example.Equals("Not Found")) {
                Errors.WriteErrorAndContinue("Invalid command provided, for more information on valid commands, please type:\n\nbamm help --all");
            }
            return example;
        }
        public static void ShowCommandDetails(string command)
        {
            if (command.Trim() == "bamm help --all") { DisplayAvailableCommands(); }
            else {
                // Ensures no invalid command will be passed to show
                while (string.IsNullOrEmpty(command) || !AllCommands.TryGetValue(command, out string? _)) {
                    Errors.WriteErrorAndContinue("Invalid command provided, for more information on valid commands, please type:\n\nbamm help --all");
                    command = Input.WriteTextAndReturnRawInput("Please provide a valid command for more information.\n") ?? "";
                }
                Success.WriteSuccessMessage($"\nCommand:\n{command}\n\nDescription:\n{GetDescriptionOfCommand(command)}");
            }
        }
        public static void ShowCommandExamples(string command)
        {
            // Ensures no invalid command will be passed to show
            while (string.IsNullOrEmpty(command) || !AllCommands.TryGetValue(command, out string? _))
            {
                Errors.WriteErrorAndContinue("Invalid command provided, for more information on valid commands, please type:\n\nbamm help --all");
                command = Input.WriteTextAndReturnRawInput("Please provide a valid command for more information.\n") ?? "";
            }
            Success.WriteSuccessMessage($"\nCommand: {command}\n\nExample(s):\n{GetExampleOfCommand(command)}");
        }

    }
}
