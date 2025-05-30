namespace MacCompilationScripts.Messaging
{
    public class Success
    {
        public static void WriteSuccessMessage(string message) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteSuccessMessageAndExit(string message, int exitCode) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            Environment.Exit(exitCode);
        }
    }
}
