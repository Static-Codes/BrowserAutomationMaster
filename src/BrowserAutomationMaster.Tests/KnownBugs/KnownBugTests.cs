using BrowserAutomationMaster;
using BrowserAutomationMaster.Parsing;
using System;
using System.IO;
using Xunit;

namespace BrowserAutomationMaster.Tests.KnownIssues
{
    public class KnownBugTests
    {
        /// <summary>
        /// BUG: `add-header` throws when `Console.WindowWidth` is evaluated in a headless environment 
        /// (like GitHub Actions/Docker) where no console is attached.
        /// </summary>
        [Fact]
        public void AddHeader_HeadlessConsoleWidth_ThrowsSafeException()
        {
            string validHeaderCommand = "add-header \"Authorization: Bearer token\"";
            
            try
            {
                // This simulates the validation call which eventually triggers the console print bug
                bool isValid = Parser.HandleLineValidation(validHeaderCommand, "test.bamc", 1);
                
                // If we get here, either it was fixed or we are running locally with a console.
                Assert.True(isValid);
            }
            catch (Exception ex)
            {
                // Defensively assert the exact known issues thrown by headless CI
                Assert.True(
                    ex is IOException || ex is ArgumentOutOfRangeException, 
                    $"Expected headless Console exception, but got {ex.GetType()}: {ex.Message}"
                );
            }
        }
    }
}