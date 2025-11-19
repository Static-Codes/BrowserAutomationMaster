using Spectre.Console;
using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Messaging
{
    public class Menu
    {   
        public enum MenuOption
        {
            Add,
            Compile,
            Run,
            GUI,
            Help,
            Exit,
            Invalid
        }

        public static MenuOption New()
        {
            Dictionary<int, MenuOption> menuOptionsMapping = new()
            {
                { 1, MenuOption.Add },
                { 2, MenuOption.Compile },
                { 3, MenuOption.Run },
                { 4, MenuOption.GUI },
                { 5, MenuOption.Help },
                { 6, MenuOption.Exit },
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
                .Title($"[{accentColor}]Welcome to the BAM Manager (BAMM)![/]\n\n" +
                        "Please select your desired action from the menu options below:")
                .AddChoices([.. menuOptionsMapping.Values.Select(x => x.ToString())])
                .HighlightStyle(style)
                .PageSize(10);

            var selectedDisplayOption = AnsiConsole.Prompt(selectionPrompt);
            var parsed = Enum.TryParse(typeof(MenuOption), selectedDisplayOption, out object? selectedMenuOption);

            if (parsed && selectedMenuOption is MenuOption castedOption){
                return castedOption;
            }
            return MenuOption.Invalid;
        }
    }
}
