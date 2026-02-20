using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;

namespace Tests
{
    
    public class Runner 
    {
        public static Dictionary<int, Func<bool>> CreateTests(Dictionary<int, (object, object)> data)
        {
            var tests = new Dictionary<int, Func<bool>>();

            foreach (var element in data)
            {
                var (valA, valB) = element.Value;

                tests.Add(element.Key, () => 
                { 
                    return (valA.Equals(valB)); 
                });
            }

            return tests;
        }

        public static bool RunTest(int testNumber, Func<bool> func) 
        {
            var passed = false;
            try 
            {
                passed = func.Invoke();
            }

            catch (Exception ex) 
            {
                Write
                (

                    string.Join(NLC, [
                        $"[ERROR]: Test #{testNumber} failed.",
                        $"[ERROR LOG]: {ex.Message}",
                        $"[STACKTRACE]: {ex.StackTrace}"
                    ])
                );
            }

            if (passed) {
                WriteSuccessMessage($"[SUCCESS]: Test #{testNumber} passed.");
            } else {
                Write($"[ERROR]: Test #{testNumber} failed.");
            }
            return passed;
        }
    }
}