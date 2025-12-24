using BrowserAutomationMaster.Managers;
using Spectre.Console;
using static BrowserAutomationMaster.Parsing.Parser;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using BrowserAutomationMaster.Managers.Python;

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

        private static MenuOption ShowMenu()
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
    
        public static KeyValuePair<MenuOption, string> New()
        {

            string selectedFile;
            bool userScriptDirExists = CreateUserScriptsDirectory();
            if (!userScriptDirExists) { 
                return KeyValuePair.Create(
                    MenuOption.Invalid, 
                    WriteErrorAndReturnEmptyString(noFilesFoundMessage)
                ); 
            }

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0) { 
                return KeyValuePair.Create(
                    MenuOption.Invalid, 
                    WriteErrorAndReturnEmptyString(noFilesFoundMessage)
                ); 
            }

            MenuOption selection = ShowMenu();
            int index;
            switch (selection)
            {
                case MenuOption.Add:
                    
                    string input = Input.WriteListFromOptions(["Select a File", "Exit"]);

                    if (input.Equals("Exit"))
                    {
                        WriteAndExit("Operation cancelled by user, BAM Manager (BAMM) will exit now.", 1); 
                    }

                    string path = Input.AskForInput("Path: ");
                    
                    if (!File.Exists(path))
                    {
                        WriteAndExit(
                            message:
                                "BAMM Manager (BAMM) was unable to find the provided file, " +
                                $"please ensure the file below exists:\n{path}",
                            status: 1
                        );
                    }

                    // This executes UserScriptManager.AddScript()
                    UserScriptManager _ = new(path, "add");
                    return KeyValuePair.Create(MenuOption.Add, path);

                case MenuOption.Compile:
                    HandleBAMCFileValidation(BAMCFiles);

                    index = HandleUserSelection(validFilesMapping);
                    selectedFile = BAMCFiles[index];
                    
                    return KeyValuePair.Create(
                        MenuOption.Compile, 
                        Path.Combine(
                            AppContext.BaseDirectory, 
                            "userScripts", 
                            selectedFile
                        )
                    );

                case MenuOption.Run:
                    selectedFile = RuntimeManager.HandleUserScriptChoice();
                    return KeyValuePair.Create(
                        MenuOption.Run, 
                        selectedFile
                    );
                
                case MenuOption.GUI:
                    return KeyValuePair.Create(MenuOption.GUI, string.Empty);


                // Add functionality to return back to the main menu after a completed action
                case MenuOption.Help:
                    HandleHelpSelection();
                    return KeyValuePair.Create(MenuOption.Help, string.Empty);

                case MenuOption.Exit:
                    Environment.Exit(0);
                    break; // Stupid requirement for c#'s static compiler
            }

            return KeyValuePair.Create(
                MenuOption.Help, 
                "If you're reading this a menu option was incorrectly handled.\n\n" +
                $"Please make a bug report {ISSUES_LINK}"
            );
        }
    }
}
