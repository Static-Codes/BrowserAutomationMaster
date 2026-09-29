using static BrowserAutomationMaster.Core.Parsing.Parser;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    public class JavaScriptBlockTests
    {
        [Fact]
        public void JavaScriptBlocks_ValidatesCorrectly()
        {
            // start-javascript must be valid
            Assert.True(HandleLineValidation("test.bamc", "start-javascript", 1));
            
            // end-javascript must be valid
            Assert.True(HandleLineValidation("test.bamc", "end-javascript", 2));
        }
    }
}
