using Spectre.Console;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Input;
using static BrowserAutomationMaster.Messaging.Success;
using static MacPackager.CommandManager;

namespace MacPackager
{
    public class Menu
    {   
        public enum MenuOption
        {
            BuildPackage,
            EditConfig,
            GUI,
            Help,
            NewConfig,
            ShowConfig,
            Exit,
            Invalid
        }

        public static MenuOption NewMenu()
        {
            Dictionary<int, MenuOption> menuOptionsMapping = new()
            {
                { 1, MenuOption.BuildPackage },
                { 2, MenuOption.EditConfig },
                { 3, MenuOption.NewConfig },
                { 5, MenuOption.ShowConfig },
                { 6, MenuOption.GUI },
                { 7, MenuOption.Help },
                { 8, MenuOption.Exit },
            };

            var accentColor = GetAccentColor().ToMarkup();
            var (bgColor, fgColor) = GetHighlights();
            
            var style = new Style(
                foreground: fgColor,
                background: bgColor,
                decoration: Decoration.Bold
            );
            
            var selectionPrompt = new SelectionPrompt<string>()
                .HighlightStyle(new Style(
                    foreground: GetForeground(),
                    decoration: Decoration.Bold
                ))
                .Title($"[{accentColor}]Welcome to the BAMM for macOS Packager![/]\n\n" +
                        "Please select your desired action from the menu options below:")
                .AddChoices([.. menuOptionsMapping.Values.Select(x => x.ToString())])
                .HighlightStyle(style)
                .PageSize(10);

            var selectedDisplayOption = AnsiConsole.Prompt(selectionPrompt);
            var parsed = Enum.TryParse(typeof(MenuOption), selectedDisplayOption, out object? selectedMenuOption);

            if (parsed && selectedMenuOption is MenuOption castedOption) {
                return castedOption;
            }
            return MenuOption.Invalid;
        }

        public static void HandleHelpSelection()
        {
            while (true) 
            {
                string[] options = [.. CommandList.Select(cmd => cmd.Name), "Exit App"];
                string command = WriteListFromOptions(
                    options: options,
                    noun: "command",
                    pageSize: options.Length
                );
                ShowCommandDetails(command.Trim());

                string choice = AskForInput(
                    "\nWould you like to continue learning more about BAM Manager (BAMM)? [y/n]:"
                );
                
                if (!choice.Equals("y")) {
                    Environment.Exit(1);
                }
            }
        }
        public static void ShowCommandDetails(string command)
        {
            if (command.Trim() == "Exit App") { 
                Environment.Exit(0); 
            }

            else
            {
                // Ensures no invalid command will be passed to show
                while (string.IsNullOrEmpty(command) || !CommandExists(command))
                {
                    Write(
                        $"Invalid command '{command}' provided, for more information on valid commands, please type:\n\nbamm-macos-packager help --all"
                    );
                    command = AskForInput("Please provide a valid command for more information.\n");
                }
                var exArray = GetExamples(command);
                var examples = exArray.Length != 0 ? string.Join("\n", exArray) : "Not Found";
                WriteSuccessMessage(
                    $"\nCommand: {command}\n" +
                    $"\n\nDescription:\n{GetDescription(command)}" +
                    $"\n\nExamples:\n{Markup.Escape(examples)}\n"
                );
            }
        }
    }
}
