using System.Net;
using System.Text;
using System.Text.Json;
using static BrowserAutomationMaster.Core.Common.RegexManager;

namespace BrowserAutomationMaster.Core.GUI 
{
    public static class Response
    {
        private static readonly Dictionary<string, string> Items = [];

        public static readonly DictionaryJsonResponse validResponse = new(
            response: new BasicJsonResponse(Success: true),
            items: Items
        );

        public static DictionaryJsonResponse Success(Dictionary<string, string> data)
        {
            return new DictionaryJsonResponse(
                response: new BasicJsonResponse(Success: true),
                items: data
            );
        }

        public static DictionaryJsonResponse Error(string error)
        {
            return new DictionaryJsonResponse(
                response: new BasicJsonResponse(Success: false) { Error = error },
                items: Items
            );
        }

        public static string EscapeMultiLineBlock(string block)
        {
            return block
                // Escapes backslashes first, otherwise subsequent escapes will double them
                .Replace("\\", "\\\\") 
                // Escapes double quotes within the code
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        public static async Task HandleInvalidResponse(HttpListenerResponse response, string error)
        {
            var invalidResp = JsonSerializer.Serialize(Error(error));
            var respBytes = Encoding.UTF8.GetBytes(invalidResp);
            await Server.WriteResponse(response, respBytes);
        }

        public static async Task HandleValidResponse(HttpListenerResponse response, Dictionary<string, string> items)
        {
            try
            {
                var validRespObj = JsonSerializer.Serialize(Success(items));
                var validRespBytes = Encoding.UTF8.GetBytes(validRespObj);
                await Server.WriteResponse(response, validRespBytes);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.Message);
            }
        }
        
        public static bool IsB64(string b64string)
        {
            if (string.IsNullOrEmpty(b64string)) {
                return false;
            }

            return 
                PrecompiledBase64Regex().IsMatch(b64string) &&
                b64string.Length % 4 == 0;
        }
    }
}