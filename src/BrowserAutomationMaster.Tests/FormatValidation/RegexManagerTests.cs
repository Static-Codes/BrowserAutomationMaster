using BrowserAutomationMaster;
using BrowserAutomationMaster.Parsing;
using BrowserAutomationMaster.Managers;
using Xunit;

namespace BrowserAutomationMaster.Tests.FormatValidation
{
    public class RegexManagerTests
    {
        [Theory]
        [InlineData("https://google.com", true)]
        [InlineData("http://localhost:3000", true)]
        [InlineData("ftp://fileserver", false)] // Only HTTP/HTTPS
        [InlineData("notalurl", false)]
        public void IsValidLink_Tests(string input, bool expected)
        {
            Assert.Equal(expected, Parser.IsValidLinkFormat(input));
        }

        [Theory]
        [InlineData("10", true)]
        [InlineData("3.14", true)]
        [InlineData("0.5", true)]
        [InlineData("-5", false)] // Time/Waits shouldn't be negative in this context
        [InlineData("abc", false)]
        public void IsValidNumber_Tests(string input, bool expected)
        {
            Assert.Equal(expected, Parser.IsValidNumberFormat(input));
        }

        [Theory]
        [InlineData("Mozilla/5.0 (Windows NT 10.0)", true)]
        [InlineData("", false)]
        public void IsValidUserAgent_Tests(string input, bool expected)
        {
            Assert.Equal(expected, RegexManager.PrecompiledUserAgentRegex().IsMatch(input));
        }
    }
}