namespace MacCompilationScripts.Messaging
{
    public class Warning
    {
        public static void Write(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
