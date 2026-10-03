using System.Net;
using System.Text.Json;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// The routes that need no request body: the GUI's own pages and the server's version answers. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (Redirect, Version, GetGuiVersion)
    /// </summary>
    /// <remarks>
    /// These are the safe half of Tier B: nothing here changes server state, so they can share one
    /// listener with the rest of the suite. See <see cref="ForbiddenRequests"/> for the requests that
    /// cannot share one.
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class RootAndVersionTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        private static readonly JsonDocumentOptions READ = new() { AllowTrailingCommas = true };

        [Fact]
        public async Task Root_ServesTheAccessInstructions()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/");

            string body = await response.Content.ReadAsStringAsync();

            output.WriteLine($"GET / -> {(int)response.StatusCode}, {body.Length} bytes");
            output.WriteLine(body[..Math.Min(400, body.Length)]);

            // Despite the name, Redirect does not redirect: it serves a 200 page telling the user to
            // open the extracted index.html themselves, because a browser will not load a file:// page
            // out of an http:// origin. Asserting the real behaviour rather than the method's name.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("Local GUI Access Required", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Root_NamesTheExtractedIndexPageTheUserHasToOpen()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/");

            string body = await response.Content.ReadAsStringAsync();

            // The page is the only thing telling the user where the GUI actually is, so it has to name
            // the file that was extracted — and specifically the file *this* server extracted, which is
            // under the redirected AppData root rather than the test host's.
            string expected = collection.Server.ExtractedMainGuiPage;

            output.WriteLine($"body names: {expected}");

            Assert.Contains(expected, body, StringComparison.Ordinal);

            // Both spellings, because the page offers both and they serve different purposes: the bare
            // path in the path-box is what the copy button hands over, and the file:// anchor is what
            // the browser can follow. Asserting only one would let the other regress into a path that
            // no longer resolves.
            Assert.Contains(
                $"href='file://{expected.Replace("\\", "/")}'",
                body,
                StringComparison.Ordinal
            );

            Assert.DoesNotContain("undefined", body, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Version_ReportsBammSVersionAndLateness()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/version");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /version -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            using JsonDocument document = JsonDocument.Parse(body, READ);
            JsonElement root = document.RootElement;

            // Lower-cased keys, unlike every other endpoint's PascalCase. Asserted because the GUI
            // reads version and is_latest exactly as spelled here.
            Assert.True(root.TryGetProperty("version", out JsonElement version), $"No 'version' in {body}");
            Assert.True(root.TryGetProperty("is_latest", out JsonElement isLatest), $"No 'is_latest' in {body}");

            Assert.Matches(@"^v\d+\.\d+\.\d+", version.GetString());

            // A JSON boolean, not the string "true". index.html reads this with
            // `isLatest ? "Yes" : "No (UPDATE REQUIRED)"`, and every non-empty string is truthy in
            // JavaScript, so a string here reports an out-of-date BAMM as up to date.
            Assert.True(
                isLatest.ValueKind is JsonValueKind.True or JsonValueKind.False,
                $"is_latest was {isLatest.ValueKind} ({isLatest.GetRawText()}), not a boolean: {body}"
            );
        }

        [Fact]
        public async Task GuiVersion_ReportsTheEmbeddedGuiversion()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/gui_version");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /gui_version -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            using JsonDocument document = JsonDocument.Parse(body, READ);
            JsonElement root = document.RootElement;

            Assert.True(root.TryGetProperty("gui_version", out JsonElement guiVersion), $"No 'gui_version' in {body}");

            // Not "unknown". An unresolvable archive used to answer that, which reads to the GUI as a
            // version and tells a user nothing.
            Assert.NotEqual("unknown", guiVersion.GetString());
            Assert.Matches(@"^\d+(\.\d+)+$", guiVersion.GetString());
        }

        [Fact]
        public async Task EveryDeclaredRoute_AnswersSomethingOtherThan404()
        {
            foreach (EndpointContract endpoint in CommandContract.Endpoints)
            {
                if (endpoint.Kind == EndpointKind.Terminate)
                {
                    // Would stop the listener for every test after it. Covered by ForbiddenRequests.
                    output.WriteLine($"skipping {endpoint.Path}: terminating the shared server");
                    continue;
                }

                using HttpResponseMessage response = await collection.Client.GetAsync(endpoint.Path);

                output.WriteLine($"GET {endpoint.Path} -> {(int)response.StatusCode}");

                Assert.True(
                    response.StatusCode != HttpStatusCode.NotFound,
                    $"{endpoint.Path} is in the declared contract ({endpoint.Summary}) but the router answered 404. " +
                    "Either the route was removed or the contract is stale."
                );
            }
        }
    }
}
