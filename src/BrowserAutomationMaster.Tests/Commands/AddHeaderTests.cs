using Xunit;
using static BrowserAutomationMaster.Core.Parsing.Parser;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>add-header</c> command.
    /// Docs: add-header "header-name" "header-value"
    /// Source: Parsing/LineValidation.cs -> LineValidation.AddHeader
    /// </summary>
    /// <remarks>
    /// <c>add-header</c> is in <c>lineArgSpecialCases</c>, so its arguments are split on
    /// ' "', which consumes the opening quote of each argument and leaves only the trailing one.
    /// AddHeader passes <c>stripped: true</c> so the quote check tolerates that. Without the
    /// flag, IsArgQuoted requires a quote at both ends and add-header rejects its own
    /// documented syntax.
    /// </remarks>
    public class AddHeaderTests
    {
        [Theory]
        [InlineData("add-header \"Authorization\" \"Bearer token\"")]
        [InlineData("add-header \"Accept-Language\" \"en-US,en;q=0.9\"")]
        [InlineData("add-header \"Cookie\" \"session=abc123\"")]
        public void AddHeader_AcceptsQuotedNameAndValue(string line)
        {
            bool result = HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented add-header syntax.");
        }

        [Theory]
        [InlineData("add-header \"Authorization\"")]                   // missing value
        [InlineData("add-header \"Authorization\" \"token\" \"extra\"")] // too many args
        [InlineData("add-header Authorization token")]                 // unquoted
        [InlineData("add-header \"Authorization\" \"token\" extra")]    // trailing content after the value
        public void AddHeader_RejectsMalformedInput(string line)
        {
            // The last case reaches the quote check itself rather than being rejected by the
            // argument count, so it guards against the validation being weakened.
            bool result = HandleLineValidation("test.bamc", line, 1);
            Assert.False(result, $"Expected '{line}' to be rejected.");
        }
    }
}
