using System.Net;
using System.Net.Http.Headers;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// The CORS preflight that <c>/export</c> depends on. <br/>
    /// Source: Core/GUI/Server.cs (AddOptionResponseHeaders), Core/GUI/BackendFunctions.cs (Export)
    /// </summary>
    /// <remarks>
    /// The GUI sends <c>Content-Type: application/json</c> to /export, which is not CORS-safelisted, so
    /// a browser issues a preflight first from the GUI's <c>file://</c> origin. If the preflight is not
    /// answered satisfactorily the export never leaves the page.
    /// <para>
    /// Three <c>AddHeader("Access-Control-Allow-Origin", "*")</c> calls reach this path — from
    /// <c>AddOptionResponseHeaders</c>, from the router, and from Export's own OPTIONS branch. They used
    /// to be read as three headers, which would fail a browser's "exactly one" rule. Probing
    /// <c>HttpListener</c> directly shows they are not: <c>AddHeader</c> goes through
    /// <c>WebHeaderCollection</c>, which folds same-named values, so exactly one reaches the wire. That
    /// is why there is no source change here — the duplication is confusing to read but harmless in
    /// practice, and these tests pin the behaviour so a refactor cannot quietly start emitting three.
    /// </para>
    /// </remarks>
    // Category=Server marks the tier that needs a listener but no browser, so the CI split can
    // hold it in the browser-free job. Tier A carries no category at all: it is in every job.
    [Trait("Category", "Server")]
    [Collection(GuiServerCollection.COLLECTION_NAME)]
    public class PreflightTests(GuiServerCollection collection, ITestOutputHelper output)
    {
        private async Task<HttpResponseMessage> SendPreflightAsync(string route)
        {
            using HttpRequestMessage preflight = new(HttpMethod.Options, route);

            // Access-Control-Request-Headers is what a browser adds for a non-safelisted request header.
            // Without it a server is free to answer a bare OPTIONS, so the test would pass without
            // exercising the part that decides the real preflight.
            preflight.Headers.Add("Origin", "null");
            preflight.Headers.Add("Access-Control-Request-Method", "GET");
            preflight.Headers.Add("Access-Control-Request-Headers", "content-type");

            return await collection.Client.SendAsync(preflight);
        }

        [Theory]
        [InlineData("/export")]
        [InlineData("/load")]
        [InlineData("/validate")]
        [InlineData("/version")]
        public async Task EveryPreflightedRoute_AnswersWithExactlyOneAllowedOrigin(string route)
        {
            using HttpResponseMessage response = await SendPreflightAsync(route);

            output.WriteLine($"OPTIONS {route} -> {(int)response.StatusCode}");

            IEnumerable<string> origins = response.Headers.GetValues(CommandContract.CORS_ORIGIN_HEADER);

            // The Fetch specification allows exactly one Access-Control-Allow-Origin. Three AddHeader
            // calls collapse into one inside WebHeaderCollection, and this is what pins that: should a
            // refactor ever stop them collapsing, a browser rejects the preflight and export breaks.
            string[] values = [.. origins];

            output.WriteLine(string.Join(", ", values.Select(value => $"[{value}]")));

            Assert.Single(values);
            Assert.Equal(CommandContract.CORS_ORIGIN_VALUE, values[0]);
        }

        [Fact]
        public async Task PreflightForExport_AllowsTheHeaderTheGuiActuallySends()
        {
            using HttpResponseMessage response = await SendPreflightAsync("/export");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"OPTIONS /export -> {(int)response.StatusCode} {body}");

            // Without this the browser rejects the preflight regardless of the origin header, and the
            // export silently does nothing.
            Assert.True(
                response.Headers.Contains(CommandContract.CORS_ALLOWED_HEADERS_HEADER),
                $"No {CommandContract.CORS_ALLOWED_HEADERS_HEADER} on the /export preflight."
            );

            IEnumerable<string> allowed = response.Headers.GetValues(CommandContract.CORS_ALLOWED_HEADERS_HEADER);

            output.WriteLine(string.Join(", ", allowed.Select(value => $"[{value}]")));

            Assert.Contains(
                allowed,
                value => value.Contains("Content-Type", StringComparison.OrdinalIgnoreCase)
            );
        }

        [Fact]
        public async Task PreflightForExport_AllowsTheMethodTheGuiActuallySends()
        {
            using HttpResponseMessage response = await SendPreflightAsync("/export");

            Assert.True(
                response.Headers.Contains("Access-Control-Allow-Methods"),
                "No Access-Control-Allow-Methods on the /export preflight."
            );

            IEnumerable<string> methods = response.Headers.GetValues("Access-Control-Allow-Methods");
            string joined = string.Join(", ", methods);

            output.WriteLine($"Access-Control-Allow-Methods: {joined}");

            // index.html sends GET with a JSON content type, which is unusual enough to be worth stating
            // outright: a fix that narrowed this to POST would look right and break the GUI.
            Assert.Contains("GET", methods.SelectMany(m => m.Split(',')).Select(m => m.Trim()), StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PreflightForExport_AnswersNoContentWithoutABody()
        {
            using HttpResponseMessage response = await SendPreflightAsync("/export");

            string body = await response.Content.ReadAsStringAsync();
            output.WriteLine($"OPTIONS /export -> {(int)response.StatusCode}, body length {body.Length}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(0, body.Length);
        }

        [Fact]
        public async Task TheExportPreflight_AgreesWithTheDeclaredContract()
        {
            // The contract declares which routes are preflighted. If the GUI started preflighting a
            // route the contract does not list, its answer would not be covered by any assertion here.
            foreach (string route in CommandContract.PreflightRoutes)
            {
                using HttpResponseMessage response = await SendPreflightAsync(route);

                output.WriteLine($"{route} is declared preflighted -> {(int)response.StatusCode}");

                Assert.True(
                    response.Headers.Contains(CommandContract.CORS_ORIGIN_HEADER),
                    $"{route} is declared as preflighted but answers no {CommandContract.CORS_ORIGIN_HEADER}."
                );
            }
        }

        [Fact]
        public async Task AnOrdinaryRequest_IsStillAnsweredWithTheAllowedOrigin()
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "/version");
            request.Headers.Add("Origin", "null");

            using HttpResponseMessage response = await collection.Client.SendAsync(request);

            output.WriteLine($"GET /version with Origin -> {(int)response.StatusCode}");

            // The non-preflight path gets its origin from the router's own AddHeader rather than from
            // AddOptionResponseHeaders, so it is a separate code path with the same requirement.
            Assert.Contains(CommandContract.CORS_ORIGIN_VALUE, response.Headers.GetValues(CommandContract.CORS_ORIGIN_HEADER));
        }
    }
}
