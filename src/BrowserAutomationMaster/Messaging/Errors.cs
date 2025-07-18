using System.Diagnostics.CodeAnalysis;

namespace BrowserAutomationMaster.Messaging
{
    public class Errors
    {
        public static string GenerateErrorMessage(string fileName, string line, int lineNumber, string issueText)
        {
            return "BAM Manager (BAMM) was unable to compile the selected .BAMC script.\n" +
                $"File: {fileName}\n" +
                $"Line Number: {lineNumber}\n" +
                $"Line: {line}\n" +
                $"Issue: {issueText}";
        }

        [DoesNotReturn]
        public static void ThrowUnsupportedPlatformException() {
            throw new PlatformNotSupportedException(
                "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                "Windows 10/11\n" +
                "Linux\n" +
                "MacOS 11+\n"
            );
        }

        public static void WriteErrorAndContinue(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.White;
        }

        [DoesNotReturn]
        public static void WriteErrorAndExit(string message, int status)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                message + $"\n\n{Debug.GetPlatformInfoForErrorLog()}"
            );
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
