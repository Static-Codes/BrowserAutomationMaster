using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>click</c> command.
    /// Docs: click "selector"  -- Supports ID, NAME, TAG NAME, and XPATH selectors.
    /// Source: Parsing/LineValidation.cs -> LineValidation.BasicCommands (arg1 == "click")
    /// </summary>
    public class ClickTests
    {
        [Theory]
        [InlineData("click \"login-button\"")]              // ID-style selector
        [InlineData("click \"submit\"")]                     // NAME-style selector
        [InlineData("click \"button\"")]                     // TAG NAME selector
        [InlineData("click \"//button[contains(text(), 'Submit')]\"")] // XPATH selector
        public void Click_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented click syntax.");
        }
    }
}