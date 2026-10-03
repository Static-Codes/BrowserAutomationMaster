using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// <c>/export</c>, writing a .bamc and refusing the requests it should. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (Export)
    /// </summary>
    /// <remarks>
    /// Writes only inside the collection's isolated AppData directory, and only ever with file names
    /// this class generates, so nothing here can reach a developer's real userScripts. <br/>
    /// This class shares the listener, so it avoids the one request that would tear it down: every
    /// rejection below is reached through the guard paths, none of which ends the process.
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class ExportEndpointTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        private static string Encode(string contents)
            => Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(contents)));

        /// <summary>
        /// The payload the GUI actually sends.
        /// </summary>
        /// <remarks>
        /// One JSON object per line, not a command line: Export JSON-deserialises each line into a
        /// single-key dictionary and writes <c>{key} {value}</c>. Only <c>start-javascript</c> and
        /// <c>end-javascript</c> are passed through as bare text, and <c>add-to-js</c> is base64 on the
        /// way in. A plain <c>browser "chrome"</c> line is a JSON parse error, not a command.
        /// </remarks>
        private static string ScriptPayload(params string[] commands)
            => string.Join('\n', commands.Select(command =>
            {
                if (command.StartsWith('{'))
                {
                    return command;
                }

                // The key is the command and the value is only its argument, which is what
                // buildStandardCommandText produces: {"browser": "\"chrome\""}. Putting the whole line
                // in the value would make Export write `browser browser "chrome"`.
                int space = command.IndexOf(' ', StringComparison.Ordinal);
                string key = space < 0 ? command : command[..space];
                string argument = space < 0 ? "" : command[(space + 1)..];

                return JsonSerializer.Serialize(new Dictionary<string, string> { [key] = argument });
            }));

        private async Task<(HttpStatusCode Status, string Body)> ExportAsync(string fileName, string? contents)
        {
            string route = contents is null
                ? $"/export?filename={Uri.EscapeDataString(fileName)}"
                : $"/export?filename={Uri.EscapeDataString(fileName)}&contents={Encode(contents)}";

            using HttpResponseMessage response = await collection.Client.GetAsync(route);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        private static string UniqueName() => $"gui-compliance-{Guid.NewGuid():N}.bamc";

        [Fact]
        public async Task AValidExport_IsAcceptedAndWritesTheFile()
        {
            string fileName = UniqueName();
            (HttpStatusCode status, string body) = await ExportAsync(fileName, ScriptPayload("browser \"chrome\"", "visit \"https://example.com\""));

            output.WriteLine($"GET /export?filename={fileName} -> {(int)status} {body}");

            Assert.Equal(HttpStatusCode.OK, status);

            // The point of the endpoint, asserted on the filesystem rather than on the response: the
            // response is the same on success and on refusal apart from its status.
            Assert.Contains(fileName, collection.Server.StagedScriptNames);
        }

        [Fact]
        public async Task AnExportWithNoContents_IsRefusedWith400()
        {
            (HttpStatusCode status, string body) = await ExportAsync(UniqueName(), contents: null);

            output.WriteLine($"GET /export (no contents) -> {(int)status} {body}");

            Assert.Equal(HttpStatusCode.BadRequest, status);
            Assert.Contains("contents", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnExportWhoseContentsAreNotBase64_IsRefusedWith400()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync(
                $"/export?filename={Uri.EscapeDataString(UniqueName())}&contents=not-valid-base64!");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /export (bad base64) -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("base64", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AFileNameThatDoesNotEndInBamc_IsRefusedWith400()
        {
            (HttpStatusCode status, string body) = await ExportAsync("gui-compliance.txt", ScriptPayload("browser \"chrome\""));

            output.WriteLine($"GET /export?filename=...txt -> {(int)status} {body}");

            Assert.Equal(HttpStatusCode.BadRequest, status);

            // The message says ".BAMC" — matched case-insensitively so a wording change to a different
            // case does not fail, but a message that stops naming the required extension does.
            Assert.Contains(".bamc", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ExportingOverAnExistingFile_IsRefusedWith400()
        {
            string fileName = UniqueName();

            (HttpStatusCode first, _) = await ExportAsync(fileName, ScriptPayload("browser \"chrome\""));
            (HttpStatusCode second, string body) = await ExportAsync(fileName, ScriptPayload("browser \"firefox\""));

            output.WriteLine($"first -> {(int)first}, second -> {(int)second} {body}");

            Assert.Equal(HttpStatusCode.OK, first);
            Assert.Equal(HttpStatusCode.BadRequest, second);

            // A refusal must not also have overwritten the file, or the name would be unusable.
            Assert.Contains(fileName, collection.Server.StagedScriptNames);
        }

        [Fact]
        public async Task AMalformedLine_IsRefusedAndTheServerKeepsListening()
        {
            // The regression this endpoint had. Export reported a bad line through HandleInvalidResponse,
            // which closes the response, then carried on and wrote to the closed response. The
            // ObjectDisposedException that followed reached StartServer, which treats it as fatal and
            // ends the process — so one malformed line in one export took the GUI server down for the
            // rest of the session. The test therefore checks the next request, not just this one: the
            // interesting half is everything that did not happen.
            string fileName = UniqueName();

            (HttpStatusCode refused, string body) = await ExportAsync(fileName, "this line is not json");

            output.WriteLine($"malformed line -> {(int)refused} {body}");

            Assert.Equal(HttpStatusCode.BadRequest, refused);

            using HttpResponseMessage after = await collection.Client.GetAsync("/version");

            output.WriteLine($"then GET /version -> {(int)after.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public async Task EveryRefusal_CarriesAReason()
        {
            // The same property as /validate: a user has to be able to find out what went wrong, and the
            // GUI shows nothing when the error is absent or empty.
            (string label, string route)[] requests =
            [
                ("no filename", "/export?contents=" + Encode(ScriptPayload("browser \"chrome\""))),
                ("wrong extension", $"/export?filename=not-a-script.txt&contents={Encode(ScriptPayload("browser \"chrome\""))}"),
                ("bad base64", $"/export?filename={UniqueName()}&contents=not-valid-base64!"),
            ];

            foreach ((string label, string route) in requests)
            {
                using HttpResponseMessage response = await collection.Client.GetAsync(route);

                string body = await response.Content.ReadAsStringAsync();
                output.WriteLine($"{label}: {(int)response.StatusCode} {body}");

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                using JsonDocument document = JsonDocument.Parse(body);
                string? error = document.RootElement.GetProperty("JsonResponse").GetProperty("Error").GetString();

                Assert.False(string.IsNullOrWhiteSpace(error), $"{label} was refused with no reason: {body}");
            }
        }
    }
}
