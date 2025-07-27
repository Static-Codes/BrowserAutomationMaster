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
                { 4, MenuOption.Help },
                { 5, MenuOption.Exit },
            };
            var selectionPrompt = new SelectionPrompt<string>()
                .HighlightStyle(new Style(
                    foreground: ToSpectreColor(GetColors().newFG),
                    background: ToSpectreColor(GetColors().newBG),
                    decoration: Decoration.Bold
                ))
                .Title("[bold blue]Welcome to the BAM Manager (BAMM)![/]\n\n" +
                        "Please select your desired action from the menu options below:")
                .AddChoices([.. menuOptionsMapping.Values.Select(x => x.ToString())])
                .PageSize(10);



                string selectedDisplayOption = AnsiConsole.Prompt(selectionPrompt);
                bool parsed = Enum.TryParse(typeof(MenuOption), selectedDisplayOption, out object? selectedMenuOption);

                if (parsed && selectedMenuOption is MenuOption castedOption){
                    return castedOption;
                }
                return MenuOption.Invalid;
        }
    }
}
