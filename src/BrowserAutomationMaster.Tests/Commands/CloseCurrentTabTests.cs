using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>close-current-tab</c> command.
    /// Docs: close-current-tab  -- No arguments.
    /// Source: Parsing/Parser.cs -> HandleLineValidation switch: "close-current-tab" => true
    /// </summary>
    public class CloseCurrentTabTests
    {
        [Fact]
        public void CloseCurrentTab_DocumentedSyntax_IsValid()
        {
            bool result = Parser.HandleLineValidation("test.bamc", "close-current-tab", 1);
            Assert.True(result);
        }
    }
}