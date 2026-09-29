using Xunit;
using static BrowserAutomationMaster.Core.Parsing.Parser;

namespace BrowserAutomationMaster.Tests.Features
{
    public class FeatureTests
    {
        // Features are declared with the `feature "<name>"` form, optionally followed by a
        // second quoted argument (e.g. an extension path or a USER:PASS@IP:PORT proxy value).
        // Source: Parsing/LineValidation.cs -> LineValidation.Feature
        [Theory]
        [InlineData("feature \"disable-pycache\"", true)]
        [InlineData("feature \"disable-ssl\"", true)]
        [InlineData("feature \"run-headless\"", true)]
        [InlineData("feature \"add-extension\" \"path/to/ext\"", true)]
        [InlineData("feature \"use-http-proxy\" \"user:pass@127.0.0.1:8080\"", true)]
        [InlineData("feature \"use-socks5-proxy\" \"user:pass@127.0.0.1:8080\"", true)]
        [InlineData("feature \"invalid-proxy\"", false)]
        [InlineData("feature \"use-bogus-proxy\" \"user:pass@127.0.0.1:8080\"", false)]
        [InlineData("feature \"use-http-proxy\" \"invalid-proxy\"", false)]
        [InlineData("--disable-ssl", false)] // v1 syntax is no longer supported
        // Known parser gap: a trailing space yields an empty third argument, and the second
        // argument of a non-proxy feature is never inspected, so this is currently accepted.
        [InlineData("feature \"add-extension\" ", true)]
        public void ScriptFeatures_Validation(string line, bool expected)
        {
            bool result = HandleLineValidation("test.bamc", line, 1);
            Assert.Equal(expected, result);
        }
    }
}
