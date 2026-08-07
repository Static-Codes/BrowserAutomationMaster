using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>fill-text-exp</c> command (advanced alternative to <c>fill-text</c>).
    /// Docs: fill-text-exp "selector" "Value you want to include"
    /// Source: Parsing/LineValidation.cs -> LineValidation.FillTextExp
    /// </summary>
    public class FillTextExpTests
    {
        [Theory]
        [InlineData("fill-text-exp \"selector\" \"Value you want to include\"")]
        [InlineData("fill-text-exp \"#pad_content\" \"If you are reading this then this script has worked for you\"")]
        public void FillTextExp_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented fill-text-exp syntax.");
        }
    }
}