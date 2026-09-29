using System.IO;
using Xunit;
using static BrowserAutomationMaster.Core.Parsing.Parser;

namespace BrowserAutomationMaster.Tests.Commands
{
    public class JavaScriptBlockTests
    {
        /// <summary>
        /// <c>start-javascript</c> / <c>end-javascript</c> are consumed by the file-level
        /// validator (Parser.cs, the JS Feature Check region) and are deliberately absent from the
        /// HandleLineValidation switch, so they must be exercised through Parser.IsValidFile.
        /// </summary>
        private static bool IsValidScript(string script)
        {
            string tempFilePath = Path.GetTempFileName();

            try
            {
                File.WriteAllText(tempFilePath, script);
                return IsValidFile(tempFilePath);
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
        public void JavaScriptBlocks_ValidatesCorrectly()
        {
            // A well-formed javascript block must validate.
            Assert.True(IsValidScript(
                "browser \"chrome\"\n" +
                "visit \"https://google.com\"\n" +
                "start-javascript\n" +
                "document.title = 'BAMM';\n" +
                "end-javascript\n" +
                "wait-for-seconds 1"
            ));
        }

        [Fact]
        public void InvalidCommand_IsRejected()
        {
            // Guards the negative path: IsValidFile must still reject genuinely invalid input,
            // so the passing case above is meaningful rather than a blanket "true".
            //
            // Note: an unterminated start-javascript block is currently accepted, and a closed
            // block with invalid JS calls WriteAndExit -> Environment.Exit, which would tear down
            // the test host. Neither is exercised here.
            Assert.False(IsValidScript(
                "browser \"chrome\"\n" +
                "visit \"https://google.com\"\n" +
                "this-is-not-a-command\n"
            ));
        }
    }
}
