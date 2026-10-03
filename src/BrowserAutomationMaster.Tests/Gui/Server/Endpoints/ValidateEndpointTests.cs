using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// <c>/validate</c> on every path a request can take. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (Validate)
    /// </summary>
    /// <remarks>
    /// The whole point of this class is that all of these answer the same shape. The verdict path and
    /// the guard paths used to disagree — the verdict answered <c>{"success": false, "error": …}</c>
    /// and a guard answered a PascalCase DictionaryJsonResponse — and because index.html gates on
    /// <c>response.ok</c> and then reads <c>data.success</c>, the disagreement meant every rejection
    /// reached the user as "No details provided."
    /// <para>
    /// No request here mutates server state, so they share the collection's listener.
    /// </para>
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class ValidateEndpointTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        private static readonly JsonDocumentOptions READ = new() { AllowTrailingCommas = true };

        /// <summary>
        /// Asserts the response is a well-formed /validate answer: a JSON object carrying a real
        /// boolean <c>success</c>, plus an <c>error</c> whenever <c>success</c> is false.
        /// </summary>
        private async Task<JsonElement> AssertValidateShape(HttpResponseMessage response, string what)
        {
            string body = await response.Content.ReadAsStringAsync();

            output.WriteLine($"{what} -> {(int)response.StatusCode} {body}");

            using JsonDocument document = JsonDocument.Parse(body, READ);
            JsonElement root = document.RootElement;

            Assert.Equal(JsonValueKind.Object, root.ValueKind);

            Assert.True(
                root.TryGetProperty("success", out JsonElement success),
                $"{what}: no 'success' in {body}"
            );

            // Not "true"/"false" as strings, and not absent. index.html reads `data.success` as a
            // boolean, so a string here is truthy for a failure and the script would be reported valid.
            Assert.True(
                success.ValueKind is JsonValueKind.True or JsonValueKind.False,
                $"{what}: success was {success.ValueKind} ({success.GetRawText()}), not a boolean: {body}"
            );

            if (success.ValueKind is JsonValueKind.False)
            {
                Assert.True(
                    root.TryGetProperty("error", out JsonElement error) && error.ValueKind is JsonValueKind.String,
                    $"{what}: a failed validation must carry a string 'error': {body}"
                );

                Assert.False(
                    string.IsNullOrWhiteSpace(error.GetString()),
                    $"{what}: the error was empty, which is what the GUI shows as 'No details provided.'"
                );
            }

            // Cloned because a JsonElement is only valid while the JsonDocument that owns it lives,
            // and this one is disposed at the end of the using above.
            return root.Clone();
        }

        [Fact]
        public async Task MissingContents_IsRejectedWith400AndAReason()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/validate");

            // 400, not 200. The guard used to leave the status unset and WriteResponse defaulted it to
            // 200, so a request that could not be judged at all looked like a successful one.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            JsonElement root = await AssertValidateShape(response, "GET /validate");

            Assert.False(root.GetProperty("success").GetBoolean());
            Assert.Contains("contents", root.GetProperty("error").GetString()!, StringComparison.Ordinal);
        }

        [Fact]
        public async Task ContentsThatAreNotBase64_IsRejectedWith400AndAReason()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync(
                "/validate?contents=this%20is%20not%20base%2064%20at%20all");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            JsonElement root = await AssertValidateShape(response, "GET /validate?contents=<not base64>");

            Assert.False(root.GetProperty("success").GetBoolean());
            Assert.Contains("base64", root.GetProperty("error").GetString()!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ContentsThatAreEmpty_IsRejectedWith400AndAReason()
        {
            // Valid base64 — it is the base64 of an empty byte array — but there is nothing to split
            // into command lines, so this is the "no new line characters" guard rather than the
            // base64 guard. Worth pinning separately: the two are indistinguishable to a user who
            // pasted nothing.
            string encoded = Convert.ToBase64String([]);

            using HttpResponseMessage response = await collection.Client.GetAsync(
                $"/validate?contents={Uri.EscapeDataString(encoded)}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            JsonElement root = await AssertValidateShape(response, "GET /validate?contents=<empty>");

            Assert.False(root.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task AValidScript_IsAcceptedWith200()
        {
            string script = "browser \"chrome\"\nvisit \"https://example.com\"";

            using HttpResponseMessage response = await collection.Client.GetAsync(
                $"/validate?contents={Uri.EscapeDataString(System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(script)))}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            JsonElement root = await AssertValidateShape(response, "GET /validate (valid script)");

            Assert.True(root.GetProperty("success").GetBoolean());
        }

        [Fact]
        public async Task AScriptWithAnUnknownFeature_IsRejectedWith200AndAReason()
        {
            // A judgeable script that fails its check, which is a different path from a malformed
            // request: it answers 200, because /validate successfully determined the verdict.
            //
            // The feature guard is the one rejection that is safe to drive from a shared listener.
            // Parser.IsValidFileContents returns false through WriteErrorAndReturnBool, which does not
            // exit; every other validation failure accumulates into invalidLines and reaches
            // WriteAndExit, which would end the server process and every test after this one.
            string script = "browser \"chrome\"\nfeature \"not-a-real-feature\"";

            using HttpResponseMessage response = await collection.Client.GetAsync(
                $"/validate?contents={Uri.EscapeDataString(System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(script)))}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            JsonElement root = await AssertValidateShape(response, "GET /validate (unknown feature)");

            Assert.False(root.GetProperty("success").GetBoolean());

            // The verdict body carries a generic reason, not the parser's message. Asserted so a change
            // to it is noticed: the GUI can only show what is in this string.
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("error").GetString()));
        }

        [Fact]
        public async Task EveryRejectedPathStillReportsWhatWasWrong()
        {
            // The regression this class exists for, stated as one test: whatever the request, a user
            // has to be able to find out why. An empty or absent error is the old failure.
            (string label, string route)[] requests =
            [
                ("no contents", "/validate"),
                ("bad base64", "/validate?contents=not-valid-base64!"),
            ];

            foreach ((string label, string route) in requests)
            {
                using HttpResponseMessage response = await collection.Client.GetAsync(route);

                string body = await response.Content.ReadAsStringAsync();
                output.WriteLine($"{label}: {(int)response.StatusCode} {body}");

                // Index.html's own fallback, verbatim: this is what a user sees when the endpoint
                // answers 200 or omits the reason.
                Assert.DoesNotContain("No details provided", body, StringComparison.Ordinal);
            }
        }
    }
}
