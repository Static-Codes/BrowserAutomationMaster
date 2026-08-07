using BrowserAutomationMaster.Parsing;
using System.IO;
using Xunit;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Covers the <c>browser</c> command.
    /// Docs: https://static-codes.github.io/BAMM-Docs/advanced_docs.html#browser
    /// Syntax: browser "browser-type"   (browser-type: "chrome" | "firefox")
    /// Source: Parsing/LineValidation.cs -> LineValidation.Browser
    /// </summary>
    public class BrowserCommandTests
    {
        [Theory]
        [InlineData("browser \"chrome\"")]
        [InlineData("browser \"firefox\"")]
        public void Browser_DocumentedSyntax_IsValid(string line)
        {
            bool result = Parser.HandleLineValidation("test.bamc", line, 1);
            Assert.True(result, $"Expected '{line}' to be valid per documented browser syntax.");
        }

        [Fact]
        public void Browser_AsFirstLineOfFile_PassesFullFileValidation()
        {
            // Integration-level check: 'browser' must additionally be restricted to
            // "chrome" or "firefox" (enforced by Parser.BrowserRegex) and must be the
            // first line of a valid .bamc file.
            string[] lines = 
            {
                "browser \"chrome\"",
                "visit \"https://example.com\""
            };

            string tempFilePath = Path.GetTempFileName();
            
            try
            {
                File.WriteAllLines(tempFilePath, lines);
                
                bool result = Parser.IsValidFile(tempFilePath);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        [Fact]
        public void Browser_FirefoxAsFirstLineOfFile_PassesFullFileValidation()
        {
            string[] lines = 
            {
                "browser \"firefox\"",
                "visit \"https://example.com\""
            };

            string tempFilePath = Path.GetTempFileName();
            
            try
            {
                File.WriteAllLines(tempFilePath, lines);
                
                bool result = Parser.IsValidFile(tempFilePath);
                Assert.True(result);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
}