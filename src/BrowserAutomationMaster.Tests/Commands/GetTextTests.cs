using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>get-text</c> command.
    /// Docs: get-text "selector" -- Supports ID, NAME, TAG NAME, and XPATH selectors.
    /// Source: Parsing/LineValidation.cs -> LineValidation.BasicCommands (arg1 == "get-text")
    /// </summary>
    public class GetTextTests
    {
        [Theory]
        [InlineData("get-text \"result-heading\"")]
        [InlineData("get-text \"h1\"")]
        [InlineData("get-text \"//div[@class='container']/p[2]\"")]
        public void GetText_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented get-text syntax.");
        }
    }
}