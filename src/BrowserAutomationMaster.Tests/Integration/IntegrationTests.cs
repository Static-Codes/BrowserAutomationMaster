using System.IO;
using Xunit;
using BrowserAutomationMaster.Compilation;

namespace BrowserAutomationMaster.Tests.Integration
{
    public class IntegrationTests
    {
        [Fact]
        public void Validate_GoogleGemini_Example()
        {
            // Note: The leading newline was removed so Lines[0] correctly targets the browser command
            string script = 
@"browser ""chrome""
visit ""https://gemini.google.com/""
wait-for-seconds 2
take-screenshot ""gemini.png""";

            string tempFilePath = Path.GetTempFileName();
            
            try
            {
                File.WriteAllText(tempFilePath, script);
                
                BAMConfig config = new BAMConfig(tempFilePath);
                config.CheckConfigLines();

                Assert.True(config.browserPresent);
                Assert.Equal("chrome", config.selectedBrowser);
                Assert.True(config.otherPresent);
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
        public void Validate_Firefox_CodedPad_Example()
        {
            // Note: '--disable-ssl' was updated to 'feature "disable-ssl"' to match BAMConfig's requirement 
            // where featureLines filters for lines starting with "feature".
            string script = 
@"browser ""firefox""
feature ""disable-ssl""
visit ""https://codedpad.com""
click-element ""#run-btn""
wait-for-seconds 1";

            string tempFilePath = Path.GetTempFileName();
            
            try
            {
                File.WriteAllText(tempFilePath, script);
                
                BAMConfig config = new BAMConfig(tempFilePath);
                config.CheckConfigLines();

                Assert.True(config.browserPresent);
                Assert.Equal("firefox", config.selectedBrowser);
                Assert.True(config.featurePresent);
                Assert.True(config.disableSSL);
                Assert.True(config.otherPresent);
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