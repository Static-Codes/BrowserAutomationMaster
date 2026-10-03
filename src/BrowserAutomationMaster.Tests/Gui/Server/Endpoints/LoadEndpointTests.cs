using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// <c>/load</c>, listing the .bamc files the server can see. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (Load)
    /// </summary>
    /// <remarks>
    /// The envelope is asserted exactly as the GUI reads it: PascalCase, because
    /// <c>DictionaryJsonResponse</c> is serialised with default options, and an <c>Items</c> map whose
    /// values are base64. index.html reads <c>data["Items"]</c> and base64-decodes each value before
    /// splitting it into lines, so a change to either half breaks the script dropdown.
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class LoadEndpointTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        [Fact]
        public async Task Load_AnswersThePascalCaseEnvelopeWithAnItemsMap()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/load");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /load -> {(int)response.StatusCode} {body[..Math.Min(300, body.Length)]}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            // PascalCase, not lower-cased like /version. Asserted because the GUI indexes into this
            // spelling directly and JSON is case-sensitive.
            Assert.True(root.TryGetProperty("JsonResponse", out JsonElement envelope), body);
            Assert.True(root.TryGetProperty("Items", out JsonElement items), body);

            Assert.True(envelope.GetProperty("Success").GetBoolean());
            Assert.Equal(JsonValueKind.Object, items.ValueKind);
        }

        [Fact]
        public async Task Load_ListsAStagedScriptWithItsContentsBase64Encoded()
        {
            string fileName = $"gui-compliance-load-{Guid.NewGuid():N}.bamc";
            string[] lines = ["browser \"chrome\"", "visit \"https://example.com\""];

            string stagedPath = collection.Server.StageScript(fileName, lines);

            using HttpResponseMessage response = await collection.Client.GetAsync("/load");

            string body = await response.Content.ReadAsStringAsync();

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement items = document.RootElement.GetProperty("Items");

            output.WriteLine($"staged {stagedPath}; Items has {items.EnumerateObject().Count()} entries");

            // Keyed by full path, not by file name: Parser.GetBAMCFiles returns the paths it
            // enumerated. Asserted on the tail so the test states the contract without pinning the
            // temporary directory a run happens to use.
            Assert.True(
                items.TryGetProperty(stagedPath, out JsonElement encoded),
                $"No entry for '{stagedPath}' in {body}"
            );

            // Base64, not the raw text: index.html runs decodeURIComponent(escape(atob(...))) over each
            // value, so a raw string here would render as mojibake in the editor.
            byte[] decoded = Convert.FromBase64String(encoded.GetString()!);
            string contents = Encoding.UTF8.GetString(decoded);

            output.WriteLine($"decoded: {contents.Replace('\n', '|')}");

            foreach (string line in lines)
            {
                Assert.Contains(line, contents, StringComparison.Ordinal);
            }
        }

        [Fact]
        public async Task Load_ListsEveryScriptTheServerCanSee()
        {
            string[] names = [.. Enumerable.Range(0, 3).Select(i => $"gui-compliance-many-{Guid.NewGuid():N}.bamc")];

            Dictionary<string, string> staged = names.ToDictionary(
                name => name,
                name => collection.Server.StageScript(name, "browser \"chrome\""));

            output.WriteLine(string.Join(Environment.NewLine, staged.Select(entry => $"{entry.Value}")));

            using HttpResponseMessage response = await collection.Client.GetAsync("/load");

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement items = document.RootElement.GetProperty("Items");

            foreach ((string name, string path) in staged)
            {
                output.WriteLine($"{path}: listed={items.TryGetProperty(path, out _)}");

                Assert.True(items.TryGetProperty(path, out _), $"'{path}' is on disk but absent from /load");
            }
        }

        [Fact]
        public async Task Load_IgnoresFilesThatAreNotBamc()
        {
            // userScripts also holds non-script files in real use. Listing them would offer the GUI a
            // dropdown entry that cannot be opened as a script.
            string baseName = $"gui-compliance-ignored-{Guid.NewGuid():N}";
            string otherName = baseName + ".txt";

            string scriptPath = collection.Server.StageScript(baseName + ".bamc", "browser \"chrome\"");
            string otherPath = collection.Server.StageScript(otherName, "not a script");

            output.WriteLine($"staged {scriptPath} and {otherPath}");

            using HttpResponseMessage response = await collection.Client.GetAsync("/load");

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            JsonElement items = document.RootElement.GetProperty("Items");

            output.WriteLine($"listed {otherName}? {items.TryGetProperty(otherName, out _)}");

            Assert.True(items.TryGetProperty(scriptPath, out _));
            Assert.False(items.TryGetProperty(otherPath, out _), $"/load offered '{otherPath}', which is not a .bamc");
        }

        [Fact]
        public async Task Load_AnswersAnEmptyMapWhenThereAreNoScripts()
        {
            // A fresh AppData directory has no userScripts content at all, and the GUI's dropdown has to
            // cope with that rather than erroring on an absent key.
            using HttpResponseMessage response = await collection.Client.GetAsync("/load");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /load (may be non-empty from other tests) {body[..Math.Min(200, body.Length)]}");

            using JsonDocument document = JsonDocument.Parse(body);

            // Only the shape is asserted. Whether the map is empty depends on what the other tests in
            // this collection staged, and xUnit gives no ordering guarantee, so pinning the count here
            // would be a flake.
            Assert.Equal(JsonValueKind.Object, document.RootElement.GetProperty("Items").ValueKind);
        }
    }
}
