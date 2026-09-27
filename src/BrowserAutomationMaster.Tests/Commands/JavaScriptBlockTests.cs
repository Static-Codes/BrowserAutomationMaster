using static BrowserAutomationMaster.Parsing.Parser;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    public class JavaScriptBlockTests
    {
        [Fact]
        public void JavaScriptBlocks_ValidatesCorrectly()
        {
            // start-javascript must be valid
            Assert.True(HandleLineValidation("start-javascript", "test.bamc", 1));
            
            // end-javascript must be valid
            Assert.True(HandleLineValidation("end-javascript", "test.bamc", 2));
        }
    }
}
