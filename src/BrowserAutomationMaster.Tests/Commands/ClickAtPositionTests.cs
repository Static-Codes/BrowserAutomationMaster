using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>click-at-position</c> command.
    /// Docs: click-at-position "600" "600"
    /// Source: Parsing/LineValidation.cs -> LineValidation.ClickAtPosition
    /// </summary>
    public class ClickAtPositionTests
    {
        [Theory]
        [InlineData("click-at-position \"600\" \"600\"")]
        [InlineData("click-at-position \"0\" \"0\"")]
        [InlineData("click-at-position \"1920\" \"1080\"")]
        public void ClickAtPosition_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented click-at-position syntax.");
        }
    }
}