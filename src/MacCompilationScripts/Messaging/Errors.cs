namespace MacCompilationScripts.Messaging
{
    public class Errors
    {
        public static string GenerateErrorMessage(string fileName, string line, int lineNumber, string issueText)
        {
            return $"BAM Manager (BAMM) was unable to compile the selected .BAMC script.\nFile: {fileName}\nLine Number: {lineNumber}\nLine: {line}\nIssue: {issueText}";
        }

        public static void WriteErrorAndContinue(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public static void WriteErrorAndExit(string message, int status)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadKey();
            Environment.Exit(status);
        }
        public static bool WriteErrorAndReturnBool(string message, bool returnBool)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
            return returnBool;
        }

        public static string WriteErrorAndReturnEmptyString(string message)
        {
            Console.WriteLine(message);
            Console.ReadKey();
            return string.Empty;
        }

        
    }
}
