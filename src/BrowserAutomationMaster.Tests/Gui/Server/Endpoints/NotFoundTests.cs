using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// An unknown route, and the status it answers. <br/>
    /// Source: Core/GUI/Server.cs (HandleEndpointRequests, default case)
    /// </summary>
    /// <remarks>
    /// The 404 is load-bearing and easy to lose. <c>WriteResponse</c> has to set the status before the
    /// write, because HttpListener flushes headers on the first write; a status assigned by a caller
    /// beforehand is therefore overwritten by the handler's own default. Making that honest turned the
    /// default case's <c>StatusCode = NotFound</c> assignment into a dead store and the route answered
    /// 400 until the status was passed through instead. This is the test that caught it.
    /// <para>
    /// Each test uses a route name nothing can serve, so the case under test is reached every time.
    /// </para>
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class NotFoundTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        [Theory]
        [InlineData("/not-a-route")]
        [InlineData("/gui/")]
        [InlineData("/Validate")]
        [InlineData("/does/not/exist")]
        public async Task AnUnknownRoute_Answers404AndNamesItself(string route)
        {
            using HttpResponseMessage response = await collection.Client.GetAsync(route);

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"GET {route} -> {(int)response.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains(route, body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task AnUnknownRoute_StillAnswersInTheSharedEnvelope()
        {
            using HttpResponseMessage response = await collection.Client.GetAsync("/not-a-route");

            string body = await response.Content.ReadAsStringAsync();

            // PascalCase with a JsonResponse/Items pair, the same shape /load uses. A guard path that
            // answered a different envelope would be a second inconsistency of the kind /validate had.
            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement root = document.RootElement;

            output.WriteLine($"GET /not-a-route -> {(int)response.StatusCode} {body}");

            Assert.True(root.TryGetProperty("JsonResponse", out JsonElement envelope), body);
            Assert.True(root.TryGetProperty("Items", out _), body);
            Assert.False(envelope.GetProperty("Success").GetBoolean());
        }

        [Fact]
        public async Task TheListenerKeepsServingAfterAnUnknownRoute()
        {
            using HttpResponseMessage unknown = await collection.Client.GetAsync("/not-a-route");
            using HttpResponseMessage known = await collection.Client.GetAsync("/version");

            output.WriteLine($"unknown -> {(int)unknown.StatusCode}, then /version -> {(int)known.StatusCode}");

            // The default case used to only log, leaving the connection open with nothing sent, so the
            // requester saw a network error and the connection leaked. Both halves matter: a status, and
            // a listener still there afterwards.
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            Assert.Equal(HttpStatusCode.OK, known.StatusCode);
        }
    }
}
