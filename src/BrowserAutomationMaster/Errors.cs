using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    internal class Errors
    {
        public static void WriteErrorAndExit(string message, int status)
        {
            Console.WriteLine(message);
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
