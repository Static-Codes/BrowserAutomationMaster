using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>open-new-tab</c> command.
    /// Docs: open-new-tab "https://google.com" "3"
    /// Source: Parsing/LineValidation.cs -> LineValidation.OpenNewTab
    /// </summary>
    public class OpenNewTabTests
    {
        [Theory]
        [InlineData("open-new-tab \"https://google.com\" \"3\"")]
        [InlineData("open-new-tab \"https://example.com\" \"0\"")]
        [InlineData("open-new-tab \"file://path/to/local.html\" \"5\"")]
        public void OpenNewTab_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented open-new-tab syntax.");
        }
    }
}