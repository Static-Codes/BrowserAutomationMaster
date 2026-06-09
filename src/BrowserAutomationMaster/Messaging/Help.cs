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

﻿using System;
using BrowserAutomationMaster.Managers;
using Spectre.Console;
using static BrowserAutomationMaster.Managers.CommandManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Messaging
{
    public static class Help
    {
        //private static readonly List<Command> ActionCommands = GetCommands(CommandType.Action);
        //private static readonly List<Command> ArgumentCommands = GetCommands(CommandType.Argument);
        //private static readonly List<Command> FeatureCommands = GetCommands(CommandType.Feature);


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
                        $"Invalid command '{command}' provided, for more information on valid commands, please type:\n\nbamm help --all"
                    );
                    command = Input.AskForInput("Please provide a valid command for more information.\n");
                }
                var exArray = GetExamples(command);
                var examples = exArray.Length != 0 ? string.Join("\n", exArray) : "Not Found";
                WriteSuccessMessage(
                    $"\nCommand: {command}\n" +
                    $"\nType: {CommandManager.GetType(command)}" + 
                    $"\n\nDescription:\n{GetDescription(command)}" +
                    $"\n\nExamples:\n{Markup.Escape(examples)}\n"
                );
            }
        }

    }
}
