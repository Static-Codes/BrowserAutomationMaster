using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>click-exp</c> command (CSS-selector alternative to <c>click</c>).
    /// Docs: click-exp 'css-selector.item_element'
    /// Source: Parsing/LineValidation.cs -> LineValidation.ClickExp
    ///
    /// Note: click-exp re-splits the raw line on " '" (space + single-quote) independently
    /// of the outer HandleLineValidation split, so it specifically requires single-quoted
    /// syntax, unlike most other commands which use double quotes.
    /// </summary>
    public class ClickExpTests
    {
        [Theory]
        [InlineData("click-exp 'css-selector.item_element'")]
        [InlineData("click-exp '#main-content'")]
        [InlineData("click-exp 'div.product-item > h3.title'")]
        public void ClickExp_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented click-exp syntax.");
        }
    }
}