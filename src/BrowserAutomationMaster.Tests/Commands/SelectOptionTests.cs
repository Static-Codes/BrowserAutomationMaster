using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>select-option</c> command.
    /// Docs: select-option "selector" 2  -- selects an &lt;option&gt; from a &lt;select&gt;.
    /// Source: Parsing/LineValidation.cs -> LineValidation.SelectOption
    /// Note: unlike most commands, the index argument (arg 2) is NOT quoted.
    /// </summary>
    public class SelectOptionTests
    {
        [Theory]
        [InlineData("select-option \"selector\" 2")]
        [InlineData("select-option \"#country-dropdown\" 0")]
        [InlineData("select-option \"#country-dropdown\" 10")]
        public void SelectOption_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented select-option syntax.");
        }
    }
}