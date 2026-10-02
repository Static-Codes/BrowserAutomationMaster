using BrowserAutomationMaster.Core.Common;
using Spectre.Console;
using static BrowserAutomationMaster.Core.Common.Commands;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;

namespace BrowserAutomationMaster.Core.Messaging
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
                    string.Join(NLC, [
                        $"\nCommand: {command}\n" +
                        $"\nType: {Commands.GetType(command)}" + 
                        $"\n\nDescription:\n{GetDescription(command)}" +
                        $"\n\nExamples:\n{Markup.Escape(examples)}\n"
                    ])
                );
            }
        }

    }
}
