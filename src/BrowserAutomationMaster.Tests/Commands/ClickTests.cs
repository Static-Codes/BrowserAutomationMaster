using Xunit;
using static BrowserAutomationMaster.Core.Parsing.Parser;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>click</c> command.
    /// Docs: click "selector"  -- Supports ID, NAME, TAG NAME, and XPATH selectors.
    /// Source: Parsing/LineValidation.cs -> LineValidation.BasicCommands (firstArg == "click")
    /// </summary>
    /// <remarks>
    /// <c>click</c> arguments are split on a bare space, so a selector cannot contain one.
    /// A spaced selector such as <c>//button[contains(text(), 'Submit')]</c> is only expressible
    /// with <c>click-exp</c>, which re-splits on " '" and therefore preserves spaces.
    /// See ClickExpTests for that case.
    /// </remarks>
    public class ClickTests
    {
        [Theory]
        [InlineData("click \"login-button\"")]              // ID-style selector
        [InlineData("click \"submit\"")]                     // NAME-style selector
        [InlineData("click \"button\"")]                     // TAG NAME selector
        [InlineData("click \"#login-btn\"")]                 // CSS-style selector
        [InlineData("click \"//button[@id='submit']\"")]     // XPATH selector without spaces
        public void Click_DocumentedSyntax_IsValid(string line)
        {
            bool result = HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented click syntax.");
        }

        [Fact]
        public void Click_RejectsSelectorContainingSpaces()
        {
            // A space inside the quotes splits into extra arguments, so this is not valid click
            // syntax. Use click-exp for selectors containing spaces.
            bool result = HandleLineValidation("test.bamc", "click \"//button[contains(text(), 'Submit')]\"", 1);
            Assert.False(result, "Expected click with a spaced selector to be rejected.");
        }
    }
}
