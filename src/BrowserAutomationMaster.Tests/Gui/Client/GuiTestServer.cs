using System.Net;
using System.Text;
using BrowserAutomationMaster.Tests.Gui.Fixtures;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// Serves the pinned GUI over HTTP, in-process, for the client tests. <br/>
    /// Source: index.html, scripts/*.js, styles/*.css
    /// </summary>
    /// <remarks>
    /// In-process rather than <c>python3 -m http.server</c> or <c>npx serve</c>: either would add a
    /// runtime dependency to a test run and take a process teardown, neither of which a suite can do
    /// reliably inside a container. The fixture tree is already memoised, so serving from it is a
    /// dictionary read. <br/>
    /// Over HTTP and never <c>file://</c>, because under <c>file://</c> the GUI's origin is opaque —
    /// <c>localStorage</c> throws, every fetch to BAMM fails CORS, and index.html's redirect guard
    /// fires. None of that is what the tests are about.
    /// </remarks>
    public sealed class GuiTestServer : IAsyncDisposable
    {
        private static readonly Dictionary<string, (string ContentType, byte[] Body)> CONTENT_TYPES = new(StringComparer.Ordinal)
        {
            [".html"] = ("text/html", []),
            [".js"] = ("text/javascript", []),
            [".css"] = ("text/css", []),
            [".json"] = ("application/json", []),
            [".ico"] = ("image/x-icon", []),
            [".svg"] = ("image/svg+xml", []),
        };

        private readonly HttpListener listener;
        private readonly CancellationTokenSource stopping = new();
        private readonly Task pump;

        private GuiTestServer(HttpListener listener, int port)
        {
            this.listener = listener;
            Port = port;
            pump = Task.Run(() => Pump(stopping.Token));
        }

        /// <summary>The port bound on loopback.</summary>
        public int Port { get; }

        /// <summary>The origin the GUI is served from.</summary>
        public string Origin => $"http://127.0.0.1:{Port}";

        /// <summary>Binds a free port and starts serving the pinned tree.</summary>
        public static async Task<GuiTestServer> StartAsync()
        {
            int port = ReserveFreePort();

            HttpListener listener = new();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            GuiTestServer server = new(listener, port);

            // One poll rather than a full serve-and-check: the listener is already started by the time
            // this returns, so a request cannot arrive before there is something to answer it.
            await Task.CompletedTask;

            return server;
        }

        /// <summary>
        /// The URL for a route in the GUI, e.g. <c>create_script.html</c>.
        /// </summary>
        public string Url(string file) => $"{Origin}/{file.TrimStart('/')}";

        public async ValueTask DisposeAsync()
        {
            await stopping.CancelAsync();

            listener.Close();

            try
            {
                await pump;
            }
            catch
            {
                // Closing the listener is what makes the pending GetContextAsync throw, and that is the
                // intended way to stop.
            }

            stopping.Dispose();
        }

        private async Task Pump(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context;

                try
                {
                    context = await listener.GetContextAsync();
                }
                catch
                {
                    return;
                }

                try
                {
                    Serve(context);
                }
                catch
                {
                    // A single failed request must not stop the server for the rest of the suite.
                }
            }
        }

        private static void Serve(HttpListenerContext context)
        {
            string route = context.Request.Url?.AbsolutePath.TrimStart('/') ?? "";

            if (route.Length == 0)
            {
                route = "index.html";
            }

            // Directory requests resolve to their index.html, matching how the real GUI is opened.
            if (GuiSourceFixture.Instance.DirectoryExists(route))
            {
                route = $"{route.TrimEnd('/')}/index.html";
            }

            byte[] body;

            try
            {
                body = GuiSourceFixture.Instance.ReadBytes(route);
            }
            catch (FileNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            string extension = Path.GetExtension(route).ToLowerInvariant();
            (string contentType, _) = CONTENT_TYPES.GetValueOrDefault(extension, ("application/octet-stream", []));

            context.Response.ContentType = $"{contentType}; charset=utf-8";
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentLength64 = body.Length;

            context.Response.OutputStream.Write(body, 0, body.Length);
            context.Response.Close();
        }

        private static int ReserveFreePort()
        {
            using System.Net.Sockets.TcpListener probe = new(IPAddress.Loopback, 0);

            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            return port;
        }
    }
}
