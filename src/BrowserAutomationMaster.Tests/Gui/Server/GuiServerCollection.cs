using Xunit;

namespace BrowserAutomationMaster.Tests.Gui.Server
{
    /// <summary>
    /// One GUI HTTP server for every Tier B test, started once and torn down once. <br/>
    /// Source: Core/GUI/Server.cs (StartServer)
    /// </summary>
    /// <remarks>
    /// Shared rather than per-test because starting one is expensive: the first start extracts the
    /// embedded gui.zip, and every later start repeats that against a fresh AppData directory.
    /// <para>
    /// The cost is that the suite is order-dependent, which is why this is a collection fixture — the
    /// collection boundary is what serialises it against other collections, and
    /// <c>xunit.runner.json</c> already sets <c>parallelizeTestCollections: false</c>. A test that
    /// cannot tolerate a shared server does not belong here; the suite has no such test, and the
    /// destructive requests that would need one are documented as absent on
    /// <see cref="ForbiddenRequests"/> rather than left to be discovered by a hung run.
    /// </para>
    /// <para>
    /// Requests deliberately not in this suite: <c>POST</c> and the other rejected methods, and
    /// <c>/terminate</c> outside of disposal. HttpListener answers a body-less POST itself with 411
    /// and disposes the listener; the pending <c>GetContextAsync</c> then throws
    /// <see cref="ObjectDisposedException"/>, which <c>StartServer</c> treats as fatal and turns into
    /// <c>WriteAndExit</c>. One such request would end the process and take every remaining test with
    /// it, for a method-rejection guard that is not what this suite is for.
    /// </para>
    /// </remarks>
    public sealed class GuiServerCollection : IAsyncLifetime
    {
        /// <summary>The shared name. Also the collection's disable-parallelisation key.</summary>
        public const string COLLECTION_NAME = "GuiServer";

        /// <summary>The one live server.</summary>
        public GuiServerProcess Server { get; private set; } = null!;

        /// <summary>A client pointed at <see cref="GuiServerProcess.BaseUrl"/>, reused by every test.</summary>
        public HttpClient Client { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            Server = await GuiServerProcess.StartAsync(message => Console.WriteLine(message));

            Client = new HttpClient
            {
                BaseAddress = new Uri(Server.BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        public async Task DisposeAsync()
        {
            if (Client is not null)
            {
                Client.Dispose();
            }

            if (Server is not null)
            {
                await Server.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Collections that need the GUI server, so a test class opts in by name.
    /// </summary>
    [CollectionDefinition(GuiServerCollection.COLLECTION_NAME)]
    public sealed class GuiServerCollectionDefinition : ICollectionFixture<GuiServerCollection>;

    /// <summary>
    /// The requests this suite must not make, and why. <br/>
    /// Source: Core/GUI/Server.cs (HandleEndpointRequests, StartServer)
    /// </summary>
    /// <remarks>
    /// Recorded here rather than left as folklore, because each one looks harmless in isolation and
    /// only its consequence — every later test in the collection failing at once — gives it away.
    /// </remarks>
    public static class ForbiddenRequests
    {
        /// <summary>
        /// Any of CONNECT, DELETE, HEAD, PATCH, POST, PUT, TRACE.
        /// </summary>
        /// <remarks>
        /// The router is supposed to answer these with 400 and keep listening. In practice a body-less
        /// one never reaches the router: HttpListener rejects it with 411 and disposes the listener, and
        /// the pending GetContextAsync throws ObjectDisposedException, which StartServer catches as
        /// fatal. Sending one with a Content-Length does reach the router, and that is the form the
        /// tests use.
        /// </remarks>
        public static readonly IReadOnlyList<string> REJECTED_METHODS = ["CONNECT", "DELETE", "HEAD", "PATCH", "POST", "PUT", "TRACE"];

        /// <summary>
        /// The rejected methods <see cref="System.Net.Http.HttpClient"/> can actually send.
        /// </summary>
        /// <remarks>
        /// <c>CONNECT</c> is missing because HttpClient refuses to build one without a Host header,
        /// which is a client constraint rather than anything about the server. The server's contract is
        /// <see cref="REJECTED_METHODS"/>; this is the subset the suite can exercise, and the two are
        /// asserted to agree so the omission stays visible instead of drifting.
        /// </remarks>
        public static readonly IReadOnlyList<string> SENDABLE_REJECTED_METHODS =
        [
            .. REJECTED_METHODS.Where(method => method != "CONNECT")
        ];

        /// <summary>
        /// <c>/terminate</c>, other than from <see cref="GuiServerProcess.DisposeAsync"/>.
        /// </summary>
        /// <remarks>
        /// It works exactly as advertised: the listener stops, isRunning goes false, and the loop
        /// exits. That is the problem — every test after it would fail to connect.
        /// </remarks>
        public const string TERMINATE = "/terminate";
    }
}
