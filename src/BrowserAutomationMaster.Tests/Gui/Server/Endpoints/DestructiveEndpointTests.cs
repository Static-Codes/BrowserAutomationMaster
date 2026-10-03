using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// The requests that change the listener's own state, each on a server of its own. <br/>
    /// Source: Core/GUI/Server.cs (HandleEndpointRequests, StartServer), Core/GUI/BackendFunctions.cs (Export)
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="GuiServerCollection"/> precisely because these cannot share a listener:
    /// <c>/terminate</c> stops it, and a rejected method is the one request that can end the process.
    /// Starting a server per test costs about a second each, which is the right price for not having
    /// one test's damage be every later test's failure. <para>
    /// Every server here writes only into its own redirected AppData directory.
    /// </para>
    /// </remarks>
    // Category=Server, like the rest of Tier B: this needs a listener but no browser. It is outside the
    // shared collection, so it also carries no [Collection] — the trait is what the CI filter reads.
    [Trait("Category", "Server")]
    public class DestructiveEndpointTests(ITestOutputHelper output)
    {
        // Not a primary-constructor use: StartAsync is called from instance methods, but a static local
        // cannot capture it, so the field is what the helper reads.
        private readonly Action<string> log = output.WriteLine;

        private Task<GuiServerProcess> StartAsync()
            => GuiServerProcess.StartAsync(log);

        [Fact]
        public async Task Terminate_AnswersAndStopsTheListener()
        {
            await using GuiServerProcess server = await StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

            using HttpResponseMessage response = await client.GetAsync("/terminate");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /terminate -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("terminated", body, StringComparison.OrdinalIgnoreCase);

            // The contract's own note: StopExecution sets isRunning false, the loop notices, and the
            // listener exits. So the assertion is that it is gone, which is the only observable proof.
            await WaitUntilRefusingAsync(client, "/gui_version");
        }

        [Fact]
        public async Task ARejectedMethod_IsRefusedAndTheListenerKeepsAnswering()
        {
            await using GuiServerProcess server = await StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

            // Content-Length: 0 rather than no body at all. HttpListener answers a body-less POST itself
            // with 411 and then disposes the listener, which makes the pending GetContextAsync throw
            // ObjectDisposedException; StartServer treats that as fatal and the process exits. That path
            // never reaches the router, so it cannot be what this test exercises — and sending it would
            // end the server, taking every later assertion with it.
            using HttpRequestMessage rejected = new(HttpMethod.Post, "/version")
            {
                Content = new ByteArrayContent([])
            };

            using HttpResponseMessage response = await client.SendAsync(rejected);

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"POST /version -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("Invalid HTTP Method", body, StringComparison.Ordinal);

            // The point of Phase 2.2. These two paths used to `return` out of the listener loop, so a
            // single bad request left the GUI unable to reach its own server for the rest of the session.
            using HttpResponseMessage after = await client.GetAsync("/version");

            output.WriteLine($"then GET /version -> {(int)after.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public async Task EveryRejectedMethod_IsRefusedAndTheListenerKeepsAnswering()
        {
            await using GuiServerProcess server = await StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

            foreach (string method in ForbiddenRequests.SENDABLE_REJECTED_METHODS)
            {
                using HttpRequestMessage rejected = new(new HttpMethod(method), "/version")
                {
                    Content = new ByteArrayContent([])
                };

                using HttpResponseMessage response = await client.SendAsync(rejected);

                string body = await response.Content.ReadAsStringAsync();
                output.WriteLine($"{method} /version -> {(int)response.StatusCode} {body[..Math.Min(120, body.Length)]}");

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            }

            using HttpResponseMessage after = await client.GetAsync("/version");

            output.WriteLine($"then GET /version -> {(int)after.StatusCode}");

            Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        }

        [Fact]
        public void EveryRejectedMethodTheServerLists_IsOneThisSuiteCanSend()
        {
            // CONNECT is the only one HttpClient cannot build, so the loop above covers six of the
            // server's seven. Asserted rather than left implicit: if the server's list grows, or if
            // HttpClient gains CONNECT support, this says so instead of the coverage quietly shrinking.
            output.WriteLine($"server: {string.Join(", ", ForbiddenRequests.REJECTED_METHODS)}");
            output.WriteLine($"sent:   {string.Join(", ", ForbiddenRequests.SENDABLE_REJECTED_METHODS)}");

            Assert.Equal(
                ForbiddenRequests.REJECTED_METHODS.Where(method => method != "CONNECT"),
                ForbiddenRequests.SENDABLE_REJECTED_METHODS
            );
        }

        [Fact]
        public async Task AnExportedScript_IsListedByLoadWithItsContentsIntact()
        {
            await using GuiServerProcess server = await StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

            string fileName = $"gui-compliance-roundtrip-{Guid.NewGuid():N}.bamc";
            string[] expected = ["browser \"chrome\"", "visit \"https://example.com\""];

            // The GUI's own wire format: one JSON object per line, key = command, value = its argument.
            // Plain command lines are a JSON parse error to Export, not a script.
            string payload = string.Join('\n',
            [
                """{"browser":"\"chrome\""}""",
                """{"visit":"\"https://example.com\""}""",
            ]);

            using HttpResponseMessage exported = await client.GetAsync(
                $"/export?filename={Uri.EscapeDataString(fileName)}" +
                $"&contents={Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)))}");

            string exportBody = await exported.Content.ReadAsStringAsync();
            output.WriteLine($"GET /export -> {(int)exported.StatusCode} {exportBody}");

            Assert.Equal(HttpStatusCode.OK, exported.StatusCode);

            using HttpResponseMessage loaded = await client.GetAsync("/load");

            string loadBody = await loaded.Content.ReadAsStringAsync();

            using JsonDocument document = JsonDocument.Parse(loadBody);
            JsonElement items = document.RootElement.GetProperty("Items");

            string[] keys = [.. items.EnumerateObject().Select(property => property.Name)];

            output.WriteLine(string.Join(Environment.NewLine, keys));

            // Keyed by full path: Parser.GetBAMCFiles returns the paths it enumerated.
            string match = Assert.Single(keys.Where(key => key.EndsWith(fileName, StringComparison.Ordinal)));

            byte[] decoded = Convert.FromBase64String(items.GetProperty(match).GetString()!);
            string contents = Encoding.UTF8.GetString(decoded);

            output.WriteLine($"round-tripped contents: {contents.Replace('\n', '|')}");

            // The whole point of the round trip: what Export wrote and what Load hands back are the same
            // command lines, because Load's consumer is the editor and it splits on newlines.
            Assert.Equal(expected, contents.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        [Fact]
        public async Task LoadAnswersTheErrorEnvelopeWhenItCannotListAnything()
        {
            await using GuiServerProcess server = await StartAsync();

            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

            // /load's success shape has Error: null. The failure shape is what HandleInvalidResponse
            // writes, and the point of asserting it is that it must still be the PascalCase envelope with
            // an Items member: /load is the one route whose GUI consumer reads data["Items"], so a shape
            // change there breaks the script dropdown in a way the other routes cannot.
            using HttpResponseMessage response = await client.GetAsync("/load");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET /load -> {(int)response.StatusCode} {body[..Math.Min(200, body.Length)]}");

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("JsonResponse", out JsonElement envelope), body);
            Assert.True(root.TryGetProperty("Items", out _), body);
            Assert.True(envelope.TryGetProperty("Success", out _), body);
            Assert.True(envelope.TryGetProperty("Error", out _), body);
        }

        /// <summary>
        /// Waits until the listener stops answering, which is how a stopped server is observed.
        /// </summary>
        /// <remarks>
        /// Polled rather than asserted immediately because the loop notices <c>isRunning</c> going false
        /// and exits between requests; a GET sent the instant after /terminate returns can still be
        /// answered by a connection already accepted.
        /// </remarks>
        private static async Task WaitUntilRefusingAsync(HttpClient client, string route, int timeoutSeconds = 10)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    using HttpResponseMessage answer = await client.GetAsync(route);
                }
                catch (HttpRequestException)
                {
                    return;
                }
                catch (TaskCanceledException)
                {
                    // A read timeout against a listener that has stopped but not yet released the socket.
                    continue;
                }

                await Task.Delay(100);
            }

            Assert.Fail($"The listener was still answering {route} after {timeoutSeconds}s. /terminate did not stop it.");
        }
    }
}