using System.Text.Json;

namespace BrowserAutomationMaster.Core.Extensions 
{
    public static class ByteArrayExtension
    {
        public static async Task<T?> Deserialize<T>(this byte[] data) where T : class
        {
            using var stream = new MemoryStream(data);
            return await JsonSerializer.DeserializeAsync(stream, typeof(T)) as T;
        }
    }
}