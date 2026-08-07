using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>fill-text</c> command.
    /// Docs: fill-text "selector" "Value you want to include"
    /// Source: Parsing/LineValidation.cs -> LineValidation.FillText
    /// </summary>
    public class FillTextTests
    {
        [Theory]
        [InlineData("fill-text \"username\" \"myemail@example.com\"")]
        [InlineData("fill-text \"#pad_code\" \"Thisisapasswordexamplethatisnotverysecure\"")]
        [InlineData("fill-text \"#searchboxinput\" \"Topeka, KS\"")]
        public void FillText_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented fill-text syntax.");
        }
    }
}