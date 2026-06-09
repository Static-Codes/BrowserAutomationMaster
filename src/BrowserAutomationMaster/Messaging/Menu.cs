// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

﻿using static BrowserAutomationMaster.Parsing.Parser;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Menu;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Python;
using Spectre.Console;

namespace BrowserAutomationMaster.Messaging
{
    public class Menu
    {   
        public enum MenuOption
        {
            Add,
            Compile,
            Exit,
            GUI,
            Help,
            Invalid,
            New,
            Open,
            Run,

        }

        private static MenuOption ShowMenu()
        {
            Dictionary<int, MenuOption> menuOptionsMapping = new()
            {
                { 1, MenuOption.Add },
                { 2, MenuOption.Compile },
                { 3, MenuOption.New },
                { 4, MenuOption.Open },
                { 5, MenuOption.Run },
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
                .Title($"[{accentColor}]Welcome to the BAM Manager (BAMM)![/]\n\n" +
                        "Please select your desired action from the menu options below:")
                .AddChoices([.. menuOptionsMapping.Values.Select(x => x.ToString())])
                .HighlightStyle(style)
                .PageSize(Math.Max(3, menuOptionsMapping.Count)); // A minimum of 3 and a max of menuOptionsMapping.Count

            var selectedDisplayOption = AnsiConsole.Prompt(selectionPrompt);
            var parsed = Enum.TryParse(typeof(MenuOption), selectedDisplayOption, out object? selectedMenuOption);

            if (parsed && selectedMenuOption is MenuOption castedOption) {
                return castedOption;
            }

            return MenuOption.Invalid;
        }
    
        public static async Task<KeyValuePair<MenuOption, string>> New()
        {
            if (!await CreateUserScriptsDirectory()) 
            {
                return MenuFunctions.Invalid("BAM Manager (BAMM) was unable to create the userScripts Directory.");
            }

            MenuOption selection = ShowMenu();
            return selection switch 
            {
                MenuOption.Add => MenuFunctions.Add(),
                MenuOption.Compile => MenuFunctions.Compile(),
                MenuOption.Exit => MenuFunctions.Exit(),
                MenuOption.GUI => MenuFunctions.GUI(),
                MenuOption.Help => MenuFunctions.Help(),
                MenuOption.Invalid => MenuFunctions.Invalid(),
                MenuOption.New => MenuFunctions.New(),
                MenuOption.Run => MenuFunctions.Run(),
                _ => MenuFunctions.Invalid(WriteErrorAndReturnEmptyString(noFilesFoundMessage)),
            };
        }
    }

    internal class MenuFunctions() 
    {
        public static KeyValuePair<MenuOption, string> Add() 
        {
            string input = Input.WriteListFromOptions(["Select a File", "Exit"]);

            if (input.Equals("Exit")) {
                WriteAndExit("Operation cancelled by user, BAM Manager (BAMM) will exit now.", 1); 
            }

            string path = Input.AskForInput("Path: ");
                    
            if (!File.Exists(path)) {
                WriteAndExit(
                    message:
                        "BAMM Manager (BAMM) was unable to find the provided file, " +
                        $"please ensure the file below exists:\n{path}",
                    status: 1
                );
            }

            // This executes UserScriptManager.AddScript()
            UserScriptManager _ = new(path, "add");
            
            return KeyValuePair.Create(
                key: MenuOption.Add, 
                value: path
            );
        }    

        public static KeyValuePair<MenuOption, string> Compile() 
        {

            string[] BAMCFiles = GetBAMCFiles();
            if (BAMCFiles.Length == 0) 
            { 
                return KeyValuePair.Create(
                    key: MenuOption.Invalid, 
                    value: WriteErrorAndReturnEmptyString(noFilesFoundMessage)
                ); 
            }

            
            HandleBAMCFileValidation(BAMCFiles);
            var index = HandleUserSelection(validFilesMapping);
                    
            return KeyValuePair.Create
            (
                key: MenuOption.Compile, 
                value: Path.Combine
                (
                    AppContext.BaseDirectory, 
                    "userScripts", 
                    BAMCFiles[index]
                )
            );
        }
        public static KeyValuePair<MenuOption, string> Exit() 
        {
            return KeyValuePair.Create
            (
                key: MenuOption.Exit, 
                value: string.Empty
            );
        }
        public static KeyValuePair<MenuOption, string> GUI() 
        {
            return KeyValuePair.Create
            (
                key: MenuOption.GUI, 
                value: string.Empty
            );
        }
        public static KeyValuePair<MenuOption, string> Help() 
        {
            HandleHelpSelection();
            return KeyValuePair.Create(
                key: MenuOption.Help, 
                value: string.Empty
            );
        }

        public static KeyValuePair<MenuOption, string> Invalid(string? message = null) 
        {
            return KeyValuePair.Create
            (
                key: MenuOption.Invalid, 
                value: message ?? string.Join(NLC, [
                    "If you're reading this a menu option was incorrectly handled.",
                    $"Please make a bug report at {ISSUES_LINK}"
                ])
            );
        }

        public static KeyValuePair<MenuOption, string> New() 
        {
            return KeyValuePair.Create
            (
                key: MenuOption.New,
                value: string.Empty
            );
        }

        public static KeyValuePair<MenuOption, string> Open() 
        {
            return KeyValuePair.Create
            (
                key: MenuOption.Open,
                value: string.Empty
            );
        }

        
        public static KeyValuePair<MenuOption, string> Run() 
        {
            var selectedFile = RuntimeManager.HandleUserScriptChoice();
            return KeyValuePair.Create(
                key: MenuOption.Run, 
                value: selectedFile
            );
        }
    }
}
