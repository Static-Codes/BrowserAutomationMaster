using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster.Messaging
{
    public class Input
    {
        public static string? WriteTextAndReturnRawInput(string inputMessage)
        {
            Console.WriteLine(inputMessage);
            return Console.ReadLine();
        }
        public static object? WriteTextAndReturnInputType(string inputMessage, string panicMessage, Type desiredType, bool repeatUntilValid = false)
        {
            string? rawInputString;
            if (desiredType == null)
            {
                Errors.WriteErrorAndExit($"Invalid type provided to WriteTextAndReturnInputType(.., .., {desiredType}).\nIf you are seeing this there is invalid code written and it should be addressed immediately ", 1);
            }

            while (true)
            {
                rawInputString = WriteTextAndReturnRawInput(inputMessage);

                if (rawInputString != null)
                {
                    if (desiredType == typeof(int))
                    {
                        try { return Convert.ToInt32(rawInputString); }
                        catch
                        {
                            if (!repeatUntilValid)
                            {
                                return null;
                            }
                        }
                    }
                    else if (desiredType == typeof(string))
                    {
                        try { return rawInputString; }
                        catch
                        {
                            if (!repeatUntilValid)
                            {
                                return null;
                            }
                        }
                    }

                }
                if (!repeatUntilValid)
                {
                    return null;
                }
                inputMessage = panicMessage; // Starts writing the panic message instead of the initial input message.
            }

        }

    }
}
