using System.Text.Json;
using System.Text.Json.Serialization;

namespace BrowserAutomationMaster.Messaging
{
    internal class Help
    {
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

            catch (JsonException ex)
            {
                Console.WriteLine($"JSON Deserializing Error: {ex.Message}");
                return null;
            }
        }

        public static void Test()
        {
            string jsonInput = @"
{
    ""Actions"": {
        ""click"": ""Clicks the specified button element"",
        ""click-exp"": ""Alternative to click, use this if click is causing issues."",
        ""get-text"": ""Gets the text for a specified element."",
        ""fill-text"": ""Assigns the specified value to the selected element."",
        ""save-as-html"": ""Saves the current pages HTML to a file with the specified name."",
        ""save-as-html-exp"": ""Saves the current pages HTML to a file with the specified name but uses different logic, use this if 'save-as-html' doesn't fit your needs.'"",
        ""select-option"": ""Selects an <option> from a <select> dropdown menu, currently only supports <select><option></option></select>"",
        ""select-element"": ""Selects the element associated with the provided selector (if found)."",
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
        ""end-javascript"": ""Instructs the parser that the end of a javascript code block was reached.  An error will be thrown if end-javascript is not found within the file."",
        ""start-javascript"": ""Instructs the parser to read all following lines as a .js code block, until end-javascript is found; Will throw an error if end-javascript is not found within the file."",
        ""use-http-proxy"": ""Uses the entered http proxy for the session."",
        ""use-https-proxy"": ""Uses the entered https proxy for the session."",
        ""use-socks4-proxy"": ""Uses the entered socks4 proxy for the session."",
        ""use-socks5-proxy"": ""Uses the entered socks5 proxy for the session.""
    }
}";

            HelpType? config = Parse(jsonInput); // Can potentially be null, thus the null check below.

            if (config != null)
            {
                Console.WriteLine("--- Actions ---");
                if (config.Actions != null) // Good practice to check for null before iterating
                {
                    foreach (var action in config.Actions)
                    {
                        Console.WriteLine($"{action.Key}: {action.Value}");
                    }
                }


                Console.WriteLine("\n--- Arguments (CLI) ---");
                if (config.Arguments != null)
                {
                    foreach (var arg in config.Arguments)
                    {
                        Console.WriteLine($"{arg.Key}: {arg.Value}");
                    }
                }


                Console.WriteLine("\n--- Features ---");
                if (config.Features != null)
                {
                    foreach (var feature in config.Features)
                    {
                        Console.WriteLine($"{feature.Key}: {feature.Value}");
                    }
                }


                // Example: Accessing a specific action's description
                if (config.Actions == null) { }
                else { 
                    if (config.Actions.TryGetValue("click", out string? clickDescription)) {
                        Console.WriteLine($"\nDescription of 'click': {clickDescription}");
                    }
                }
            }
            else
            {
                Console.WriteLine("Configuration could not be parsed.");
            }
        }
    }
}