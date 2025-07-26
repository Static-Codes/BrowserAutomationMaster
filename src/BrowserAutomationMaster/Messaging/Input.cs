using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Messaging
{
    public class Input
    {
        public static string? WriteTextAndReturnRawInput(string inputMessage)
        {
            WriteMessage(inputMessage);
            //return Console.ReadLine();
            return ReadLine();
        }
        public static object? WriteTextAndReturnInputType(string inputMessage, string panicMessage, Type desiredType, bool repeatUntilValid = false)
        {
            string? rawInputString;
            if (desiredType == null)
            {
                Errors.WriteErrorAndExit(
                    message:
                        $"Invalid type provided to WriteTextAndReturnInputType(.., .., {desiredType}).\n" +
                        $"If you are seeing this there is invalid code written and it should be addressed immediately ",
                    status: 1
                );
            }

            while (true)
            {
                rawInputString = WriteTextAndReturnRawInput(inputMessage);

                if (rawInputString != null)
                {
                    if (desiredType == typeof(int))
                    {
                        try { return Convert.ToInt32(rawInputString); }
                        catch {
                            if (!repeatUntilValid) { return null; }
                        }
                    }
                    else if (desiredType == typeof(string))
                    {
                        try { return rawInputString; }
                        catch {
                            if (!repeatUntilValid) { return null; }
                        }
                    }

                }
                if (!repeatUntilValid) {
                    return null;
                }
                inputMessage = panicMessage; // Starts writing the panic message instead of the initial input message.
            }

        }

    }
}
