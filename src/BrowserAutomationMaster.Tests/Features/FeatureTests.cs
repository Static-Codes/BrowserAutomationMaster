using BrowserAutomationMaster;
using BrowserAutomationMaster.Core.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Features
{
    public class FeatureTests
    {
        [Theory]
        [InlineData("--disable-pycache", true)]
        [InlineData("--disable-ssl", true)]
        [InlineData("--add-extension \"path/to/ext\"", true)]
        [InlineData("--add-extension ", false)]
        [InlineData("--proxy \"127.0.0.1:8080\"", true)]
        [InlineData("--proxy \"http://user:pass@127.0.0.1:8080\"", true)]
        [InlineData("--proxy \"invalid-proxy\"", false)]
        public void ScriptFeatures_Validation(string line, bool expected)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.Equal(expected, result);
        }
    }
}