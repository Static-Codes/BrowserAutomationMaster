using Spectre.Console;
using static BrowserAutomationMaster.Managers.CommandManager;

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
                    Errors.WriteErrorAndContinue(
                        $"Invalid command '{command}' provided, for more information on valid commands, please type:\n\nbamm help --all"
                    );
                    command = Input.WriteTextAndReturnRawInput("Please provide a valid command for more information.\n");
                }
                var exArray = GetExamples(command);
                var examples = exArray.Length != 0 ? string.Join("\n", exArray) : "Not Found";
                Success.WriteSuccessMessage(
                    $"\nCommand: {command}\n" +
                    $"\nType: {command}" + 
                    $"\n\nDescription:\n{GetDescription(command)}" +
                    $"\n\nExamples:\n{Markup.Escape(examples)}\n"
                );
            }
        }

    }
}
