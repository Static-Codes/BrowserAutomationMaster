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
            string menuText = """
            
            Welcome to the BAM Manager (BAMM)!

            Please select the number correlating to your desired action from the menu options below:

            1. Add local .BAMC File to userScripts Directory
            2. Compile .BAMC File from userScripts Directory
            3. Run .py script compiled by BAMM
            4. Help
            5. Exit


            """;
            string invalidChoiceText =
                $"Invalid option please enter a number between 1 and {menuOptionsMapping.Count}.\n\n{menuText}";

            Console.WriteLine(menuText);
            while (true)
            {
                // ? Declares userChoice as a nullable value, as input cannot be verified without sanitization.
                bool validChoice = int.TryParse(Console.ReadLine(), out int optionNumber);
                if (validChoice && menuOptionsMapping.TryGetValue(optionNumber, out MenuOption selection))
                {
                    Console.Clear(); // Clears Terminal prior to proceeding.
                    return selection;
                }
                Console.WriteLine(invalidChoiceText);
            }
        }
    }
}
