using BrowserAutomationMaster.Managers;
using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Managers.AnsiManager;

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
            WriteMessage(message, isError: true);
        }

        [DoesNotReturn]
        public static void WriteErrorAndExit(string message, int status)
        {
            WriteMessage(message, isError: true);
            ReadKey();
            Environment.Exit(status);
        }
        
        public static bool WriteErrorAndReturnBool(string message, bool returnBool)
        {
            WriteMessage(message, isError: true);
            return returnBool;
        }
        public static string WriteErrorAndReturnEmptyString(string message)
        {
            WriteMessage(message, isError: true);
            ReadKey();
            return string.Empty;
        }

        
    }
}
