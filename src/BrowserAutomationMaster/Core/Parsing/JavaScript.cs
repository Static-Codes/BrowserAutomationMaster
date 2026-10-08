using Esprima;

namespace BrowserAutomationMaster.Core.Parsing 
{
    internal class JavaScript
    {
        public static bool IsValidSyntax(string jsCode, out string error)
        {
            try
            {
                ParserOptions options = new() { Tolerant = false };
                JavaScriptParser parser = new(options);
                parser.ParseScript(jsCode);
                error = string.Empty;
                return true;
            }
            catch (ParserException ex)
            {
                error = ex.Message; // Modify this maybe?
                return false;
            }
        }
    }
}