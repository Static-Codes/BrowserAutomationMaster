using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>save-as-html</c> command.
    /// Docs: save-as-html "filename.html"
    /// Source: Parsing/LineValidation.cs -> LineValidation.BasicCommands (arg1.Contains("save-as-html"))
    /// </summary>
    public class SaveAsHtmlTests
    {
        [Theory]
        [InlineData("save-as-html \"output.html\"")]
        [InlineData("save-as-html \"ebay-search.html\"")]
        [InlineData("save-as-html \"page-content.html\"")]
        public void SaveAsHtml_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented save-as-html syntax.");
        }
    }
}