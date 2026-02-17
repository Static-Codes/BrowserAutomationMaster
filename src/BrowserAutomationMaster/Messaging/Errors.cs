using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Managers.Common.ANSI;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Messaging.Debug;

namespace BrowserAutomationMaster.Messaging
{
    public class Errors
    {

        public static string GenerateErrorMessage(string fileName, string line, int lineNumber, string issueText)
        {
            return "BAM Manager (BAMM) was unable to continue due to an unexpected error.\n" +
                $"File: {fileName}\n" +
                $"Line Number: {lineNumber}\n" +
                $"Line: {line}\n" +
                $"Error Log: {issueText}";
        }

        public static string GetValidationErrorMessage(string fileName, string line, int lineNumber, string firstArg, string selectorString) {
            return string.Join(NLC, [
                $"BAM Manager (BAMM) ran into a BAMC validation error:",
                NLC,
                $"File: \"{fileName}\"",
                $"Invalid syntax on line {lineNumber}",
                $"Line: {line}",
                $"Valid Syntax: {firstArg} {selectorString}"
            ]);
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

        public static void Write(string message, bool noNewLines = false)
        {
            if (noNewLines)
            {
                WriteMessageNoNewLines(message, isError: true);
                return;
            }
            
            WriteMessage(message, isError: true);
        }

        [DoesNotReturn]
        public static void WriteAndExit(string message, int status, bool writePlatformDebugInfo = true)
        {
            var output = writePlatformDebugInfo switch 
            {
                true => $"{message}\n{GetPlatformInfoForErrorLog()}",
                false => message
            };

            WriteMessage(output, isError: true);
            ReadKey();
            Environment.Exit(status);
        }
        
        public static string? WriteErrorAndReturnNull(string message)
        {
            WriteMessage(message, isError: true);
            ReadKey();
            return null;
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
