using BrowserAutomationMaster.Parsing;
using Xunit;

namespace BrowserAutomationMaster.Tests.Selectors
{
    public class SelectorTests
    {
        [Theory]
        [InlineData("#submit-button", SelectorCategory.Id)]
        [InlineData(".btn-primary", SelectorCategory.ClassName)]
        [InlineData("//div[@id='test']", SelectorCategory.XPath)]
        [InlineData("button", SelectorCategory.TagName)]
        [InlineData("[name='actual_value']", SelectorCategory.NameAttribute)]
        public void Parse_ValidSelectors_ReturnsCorrectCategory(string input, SelectorCategory expectedCategory)
        {
            // Act
            ParsedSelector result = SelectorParser.Parse(input);

            // Assert
            Assert.Equal(expectedCategory, result.Category);
            Assert.Equal(input, result.rawInput);
        }
    }
}