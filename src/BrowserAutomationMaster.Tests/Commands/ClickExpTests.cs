using BrowserAutomationMaster.Core.Parsing;
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

        /// <summary>
        /// Documents a real parser limitation: an XPATH selector that itself contains quoted text
        /// cannot be expressed in either command.
        /// <c>click</c> splits on a bare space, so the selector's spaces break it apart;
        /// <c>click-exp</c> re-splits on " '", so the inner quotes break it apart instead.
        /// Neither command supports quoting escapes, so there is currently no way to write
        /// <c>//button[contains(text(), 'Submit')]</c>.
        /// </summary>
        [Fact]
        public void SelectorWithNestedQuotes_IsRejected_KnownLimitation()
        {
            bool result = Parser.HandleLineValidation("test.bamc", "click-exp '//button[contains(text(), 'Submit')]'", 1);
            Assert.False(result, "Known limitation: a selector with nested quotes is not supported.");
        }
    }
}