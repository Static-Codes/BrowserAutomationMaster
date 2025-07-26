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
            string menuText = "\nWelcome to the BAM Manager (BAMM)!\n\n" +
                "Please select the number correlating to your desired action from the menu options below:\n\n" +

                "1. Add local .BAMC File to userScripts Directory\n" +
                "2. Compile .BAMC File from userScripts Directory\n" +
                "3. Run .py script compiled by BAMM\n" +
                "4. Help\n" +
                "5. Exit\n\n";

            string invalidChoiceText =
                $"Invalid option please enter a number between 1 and {menuOptionsMapping.Count}.\n\n{menuText}";

            WriteMessage(menuText);
            while (true)
            {
                // ? Declares userChoice as a nullable value, as input cannot be verified without sanitization.
                bool validChoice = int.TryParse(ReadLine(), out int optionNumber);
                if (validChoice && menuOptionsMapping.TryGetValue(optionNumber, out MenuOption selection))
                {
                    Spectre.Console.AnsiConsole.Clear(); // Clears Terminal prior to proceeding.
                    return selection;
                }
                Errors.WriteErrorAndContinue(invalidChoiceText);
            }
        }
    }
}
