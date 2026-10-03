using System.Text.RegularExpressions;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;
using GuiServer = BrowserAutomationMaster.Core.GUI.Server;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// The GUI's fetch surface against the server's router, in both directions. <br/>
    /// Source: index.html, scripts/create/create_script.js, Core/GUI/Server.cs (HandleEndpointRequests)
    /// </summary>
    /// <remarks>
    /// Both directions, because each catches a different mistake. GUI → server catches the GUI calling
    /// something that answers 404, which the user sees as a network error. Server → GUI catches a route
    /// nothing fetches, which is dead surface that still has to be kept working.
    /// <para>
    /// The server half is read from source rather than from a running listener on purpose: Tier A has
    /// no process and no browser, and reading the <c>case</c> labels is the cheapest thing that stays
    /// true when the router is edited. Tier B proves the labels actually answer.
    /// </para>
    /// </remarks>
    public class EndpointSurfaceParityTests(ITestOutputHelper output)
    {
        private readonly GuiSourceParser gui = GuiSourceParser.Instance;

        /// <summary>The absolute path of the router's source, found by walking up from the test assembly.</summary>
        private static readonly string SERVER_SOURCE = FindRouterSource();

        [Fact]
        public void EveryRouteTheGuiFetches_IsRoutedByTheServer()
        {
            IReadOnlyList<string> fetched = [.. gui.FetchedRoutes];
            IReadOnlyList<string> routed = [.. RouterRoutes()];

            output.WriteLine($"fetched: {string.Join(", ", fetched)}");
            output.WriteLine($"routed:  {string.Join(", ", routed)}");

            List<string> unrouted = [.. fetched.Where(route => !routed.Contains(route, StringComparer.Ordinal))];

            Assert.True(
                unrouted.Count == 0,
                $"The GUI fetches {string.Join(", ", unrouted)}, which HandleEndpointRequests has no case for. " +
                $"Routed: {string.Join(", ", routed)}."
            );
        }

        /// <summary>
        /// Routes the server answers but the GUI never calls.
        /// </summary>
        /// <remarks>
        /// Asserted as an exact set rather than as empty, because one route legitimately has no caller.
        /// <c>/gui_version</c> exists so BAMM can report the version of the GUI it actually embeds,
        /// read back out of the archive, rather than a version someone maintained alongside it — but
        /// the GUI does not use it. index.html reads its own <c>GUI_VERSION</c> constant instead
        /// (<c>index.html:609</c>), and never references the route at all. <para>
        /// That is a real gap rather than harmless surface: the constant is maintained by hand and
        /// drifts from what BAMM ships, which is the failure
        /// <see cref="EmbeddedGuiZipParityTests"/> exists to catch. Wiring the GUI to
        /// <c>/gui_version</c> would close it. <para>
        /// Pinning the exact set means the day someone wires it up this test fails and says so, rather
        /// than passing silently over a route that has stopped being orphaned. Any other route appearing
        /// here is dead surface and has to justify itself.
        /// </remarks>
        [Fact]
        public void TheOnlyRoutedButUnfetchedRoute_IsGuiVersion()
        {
            IReadOnlyList<string> fetched = [.. gui.FetchedRoutes];

            List<string> unfetched = [.. RouterRoutes()
                .Where(route => route != "/")
                .Where(route => !fetched.Contains(route, StringComparer.Ordinal))];

            output.WriteLine($"routed but never fetched: {string.Join(", ", unfetched)}");

            Assert.Equal(["/gui_version"], unfetched);
        }

        [Fact]
        public void TheUnimplementedCreateRoute_IsNotFetched()
        {
            // createScriptURL is declared and never called, and /create has no case in the router. The
            // plan calls this out specifically: if it ever appears in the fetched set, the GUI has
            // started calling something the server does not answer.
            output.WriteLine(string.Join(", ", gui.FetchedRoutes));

            Assert.DoesNotContain("/create", gui.FetchedRoutes);
        }

        [Fact]
        public void TheRouterHasNoCaseForTheUnimplementedCreateRoute()
        {
            // The other half of the same claim, on the server side, so the pair cannot both be wrong.
            output.WriteLine(string.Join(", ", RouterRoutes()));

            Assert.DoesNotContain("/create", RouterRoutes());
        }

        [Fact]
        public void EveryFetchedRoute_IsInTheDeclaredContract()
        {
            IReadOnlyList<string> fetched = [.. gui.FetchedRoutes];

            List<string> undeclared = [.. fetched.Where(route => !CommandContract.Paths.Contains(route, StringComparer.Ordinal))];

            output.WriteLine($"fetched: {string.Join(", ", fetched)}");
            output.WriteLine($"contract: {string.Join(", ", CommandContract.Paths)}");

            Assert.True(
                undeclared.Count == 0,
                $"The GUI fetches {string.Join(", ", undeclared)}, which CommandContract does not declare. " +
                "Tier B asserts the server against the contract and Tier C fulfils routes from it, so an " +
                "undeclared route would be covered by nothing."
            );
        }

        [Fact]
        public void TheGuiPort_MatchesTheServersDefaultPort()
        {
            output.WriteLine($"guiPort {gui.GuiPort}, Server.DEFAULT_PORT {GuiServer.DEFAULT_PORT}");

            // index.html hardcodes the port in baseServerURL and never calls setPort(), so it is always
            // this constant no matter what query string the GUI was opened with. Asserting it against the
            // server's own default is what stops the two from drifting.
            Assert.Equal(GuiServer.DEFAULT_PORT, gui.GuiPort);
            Assert.Equal($"http://127.0.0.1:{GuiServer.DEFAULT_PORT}", gui.BaseServerUrl);
        }

        [Fact]
        public void EveryServerUrlTheGuiBuilds_UsesThatPort()
        {
            foreach ((string name, string url) in gui.ResolvedUrls)
            {
                output.WriteLine($"{name} -> {url}");

                Assert.StartsWith($"http://127.0.0.1:{gui.GuiPort}", url);
            }
        }

        /// <summary>
        /// The routes <c>HandleEndpointRequests</c> dispatches on, read from its <c>case</c> labels.
        /// </summary>
        /// <remarks>
        /// Commented-out labels are excluded by matching the leading <c>case</c> rather than the quoted
        /// route, which is how <c>// case "/upload"</c> stays out of the set.
        /// </remarks>
        private static IReadOnlyList<string> RouterRoutes()
        {
            string source = File.ReadAllText(SERVER_SOURCE);

            return
            [
                .. Regex.Matches(source, @"(?m)^\s*case\s+""(?<route>/[^""]*)""")
                    .Select(match => match.Groups["route"].Value)
                    .Distinct()
                    .OrderBy(route => route, StringComparer.Ordinal)
            ];
        }

        /// <summary>
        /// Locates <c>Server.cs</c> by walking up from the test assembly.
        /// </summary>
        /// <remarks>
        /// The solution file sits under src/, so the search looks for the file itself rather than
        /// assuming a layout above the repository root.
        /// </remarks>
        private static string FindRouterSource()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "BrowserAutomationMaster", "Core", "GUI", "Server.cs");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                $"No Core/GUI/Server.cs above '{AppContext.BaseDirectory}'. The router's routes are read from " +
                "the source, so this test cannot run without it."
            );
        }
    }
}