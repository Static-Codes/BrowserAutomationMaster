using Spectre.Console;
using static BrowserAutomationMaster.Managers.Common.ANSI;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Messaging
{
    public class Input
    {
        public static bool ConditionAccepted(string condition) { return condition.Trim().Equals("y", CCIC); }
        public static bool ConditionRejected(string condition) { return condition.Trim().Equals("n", CCIC); }
        public static string AskForInput(string inputMessage)
        {
            TextPrompt<string>? prompt;
            if (inputMessage.Contains("[y/n]:"))
            {
                inputMessage = inputMessage.Replace("[y/n]:", "");

                prompt = new TextPrompt<string>(
                    Markup.Escape(inputMessage)
                ).AddChoices(["y", "n"]);

                return AnsiConsole.Prompt(prompt);
            }
            prompt = new TextPrompt<string>(Markup.Escape(inputMessage));
            return AnsiConsole.Prompt(prompt);
        }

        public static string WriteListFromOptions(string[] options, string noun = "action", int pageSize = 3)
        {
            SetAnsiColors();
            var (bgColor, fgColor) = GetHighlights();
            var style = new Style(
                foreground: fgColor,
                background: bgColor,
                decoration: Decoration.Bold
            );
            var prompt = new SelectionPrompt<string>() {
                SearchEnabled = true,
            }
            .HighlightStyle(style)
            .Title($"Please select your desired {noun} from the menu options below:")
            .AddChoices(
                options.Select(
                    opt => opt.EscapeMarkup()
                )
            )
            .PageSize(Math.Max(pageSize, options.Length / 2));

            return AnsiConsole.Prompt(prompt);
        }

        public static object? WriteTextAndReturnInputType(string inputMessage, string panicMessage, Type desiredType, bool repeatUntilValid = false, bool isOptionNumber = false)
        {
            string rawInputString;
            if (desiredType == null)
            {
                WriteAndExit(
                    message:
                        $"Invalid type provided to WriteTextAndReturnInputType(.., .., {desiredType}).\n" +
                        $"If you are seeing this there is invalid code written and it should be addressed immediately ",
                    status: 1
                );
            }

            while (true)
            {
                rawInputString = WriteListFromOptions(inputMessage.Split('\n'));
                if (desiredType == typeof(int))
                {
                    if (isOptionNumber)
                    {
                        var chars = new List<char>();
                        foreach (var c in rawInputString)
                        {
                            if (char.IsNumber(c)) {
                                chars.Add(c);
                                continue;
                            }
                            break;
                        }
                        try { return string.Join("", chars); }
                        catch (Exception ex) {
                            WriteMessage(ex.Message, isError: true);
                            if (!repeatUntilValid) { return null; }
                        }

                    }
                    try { return Convert.ToInt32(rawInputString); }
                    catch {
                        if (!repeatUntilValid) { return null; }
                    }
                }
                else if (desiredType == typeof(string))
                {
                    try { return rawInputString; }
                    catch {
                        if (!repeatUntilValid) { return null; }
                    }
                }
                if (!repeatUntilValid) {
                    return null;
                }
                WriteMessage(panicMessage, isError: true);
            }

        }

    }
}
