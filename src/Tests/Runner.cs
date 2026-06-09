// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

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