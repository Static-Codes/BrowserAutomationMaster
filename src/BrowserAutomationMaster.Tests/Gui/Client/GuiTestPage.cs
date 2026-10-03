using Microsoft.Playwright;
using BrowserAutomationMaster.Tests.Gui.Fixtures;

namespace BrowserAutomationMaster.Tests.Gui.Client
{
    /// <summary>
    /// A GUI page, reset to a known state before each test uses it. <br/>
    /// Source: index.html, scripts/create/create_script.js
    /// </summary>
    /// <remarks>
    /// Every test gets a fresh page rather than sharing one. That is what makes the reset discipline
    /// mechanical instead of a list of things to remember: a new context starts with no cookies, no
    /// storage and no routes, so there is nothing left over from the previous test to explain a failure.
    /// <para>
    /// The reset matters because of two traps in the GUI itself. <c>localStorage["commands"]</c> must be
    /// a JSON <em>object</em>: an array makes <c>clearCurrentScriptState</c> call
    /// <c>window.location.reload(true)</c>, and the test reloads into the same bad state. And
    /// <c>window.prompt</c> has to be stubbed, because the export handler loops on it asking for a
    /// filename until one is accepted.
    /// </para>
    /// </remarks>
    public sealed class GuiTestPage : IAsyncDisposable
    {
        /// <summary>
        /// Served in place of the Notyf CDN bundle.
        /// </summary>
        /// <remarks>
        /// <c>show()</c> appends the same markup the real library does
        /// (<c>notyf__toast</c> wrapping <c>notyf__message</c>), so a test can assert on a toast the way
        /// a screenshot or a DOM check would, rather than only on the record the shim keeps.
        /// </remarks>
        private const string NOTYF_SHIM = """
            (() => {
              const record = (type, message) => {
                window.__guiAlerts = window.__guiAlerts || [];
                window.__guiAlerts.push({ type: String(type), text: String(message) });
              };

              class Notyf {
                constructor(options) { this.options = options || {}; record('notyf', JSON.stringify(this.options)); }

                _append(type, message) {
                  record(type, message);

                  const host = document.getElementById('notyf') || (() => {
                    const created = document.createElement('div');
                    created.id = 'notyf';
                    document.body.appendChild(created);
                    return created;
                  })();

                  const toast = document.createElement('div');
                  toast.className = 'notyf__toast';
                  const body = document.createElement('div');
                  body.className = 'notyf__message';
                  body.textContent = String(message);
                  toast.appendChild(body);
                  host.appendChild(toast);

                  return toast;
                }

                success(message) { this._append('success', message); return this; }
                error(message) { this._append('error', message); return this; }
                warning(message) { this._append('warning', message); return this; }
                info(message) { this._append('info', message); return this; }
                dismiss() { return this; }
                show() { return this; }
              }

              window.Notyf = Notyf;
            })();
            """;


        private readonly IBrowser browser;
        private readonly List<(string Type, string Message)> stubbedAlerts = [];

        private GuiTestPage(IBrowser browser, IBrowserContext context, IPage page, GuiTestServer server)
        {
            this.browser = browser;
            Context = context;
            Page = page;
            Server = server;
        }

        /// <summary>The browser context, created fresh per test.</summary>
        public IBrowserContext Context { get; }

        /// <summary>The page under test.</summary>
        public IPage Page { get; }

        /// <summary>The static server serving the GUI.</summary>
        public GuiTestServer Server { get; }

        /// <summary>Alerts raised since the last reset, in order, as (type, message) pairs.</summary>
        public IReadOnlyList<(string Type, string Message)> Alerts => stubbedAlerts.ToArray();

        /// <summary>
        /// Opens a page against the served GUI, with every dependency stubbed in place.
        /// </summary>
        public static async Task<GuiTestPage> OpenAsync(BrowserFixture fixture, string file = "index.html")
        {
            IBrowserContext context = await fixture.Browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = fixture.Server.Origin
            });

            IPage page = await context.NewPageAsync();

            GuiTestPage testPage = new(fixture.Browser, context, page, fixture.Server);

            await testPage.RouteExternalDependencies();
            await testPage.RouteBamEndpoints();
            await testPage.InstallAlertShim();

            await page.GotoAsync(testPage.Server.Url(file), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
            await testPage.CaptureAlerts();

            return testPage;
        }

        /// <summary>
        /// Serves the Notyf library locally and blocks every other external origin.
        /// </summary>
        /// <remarks>
        /// index.html pulls Notyf from a jsdelivr CDN. Left alone, every client test opens a real
        /// network connection to fetch it: slow, non-deterministic, and failing outright on a machine
        /// without egress — which is the same shape as "the GUI is broken", except nothing is.
        /// <para>
        /// The shim has to be <em>served</em>, not just declared. <c>new Notyf(...)</c> runs at parse
        /// time (index.html:366), so the global has to exist before that line executes, and an
        /// init-script shim cannot be installed early enough to also stop the request. Serving the URL
        /// answers the request from the page's own origin, so the run is fully offline.
        /// </para>
        /// </summary>
        private async Task RouteExternalDependencies()
        {
            // Fulfilled rather than aborted: the script element is a classic <script src>, and an aborted
            // one raises an error the GUI's own error handling would surface.
            await Context.RouteAsync(
                "**/notyf.min.js*",
                routeCall => routeCall.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "text/javascript",
                    Body = NOTYF_SHIM
                })
            );

            // The library's stylesheet is routed as well, not just its script. A page with the script but
            // not the stylesheet still constructs and calls Notyf, so nothing would fail — the toasts would
            // simply be unstyled, and a screenshot or a DOM check could not tell a rendered notification
            // from an unstyled one.
            await Context.RouteAsync(
                "**/notyf*.css*",
                routeCall => routeCall.FulfillAsync(new RouteFulfillOptions
                {
                    Status = 200,
                    ContentType = "text/css",
                    Body = """
                        .notyf__toast { opacity: 1; transition: none; }
                        .notyf__message { display: block; padding: 0.5rem; }
                        """
                })
            );

            // Everything else on another origin is refused outright rather than answered empty: fonts, and
            // any third-party dependency a future GUI change adds. Blocked rather than fulfilled so it
            // shows up as a blocked request instead of silently succeeding and depending on the network
            // for its content.
            await Context.RouteAsync(
                "https://**/*",
                routeCall => routeCall.AbortAsync()
            );
        }

/// <summary>
        /// Fulfils every BAMM endpoint from the declared contract, so no test depends on a live server.
        /// </summary>
        /// <remarks>
        /// Routed rather than stubbed in the page so the GUI's own <c>fetch</c>, CORS and
        /// <c>response.ok</c> handling all run for real. Only the answers are synthetic. <br/>
        /// <c>/load</c> always answers 200 with an empty Items map: a non-200 there sends index.html
        /// down its error path, and the state under test is the script creator, not the dropdown's
        /// failure handling.
        /// </remarks>
        private async Task RouteBamEndpoints()
        {
            foreach (EndpointContract endpoint in CommandContract.Endpoints.Where(e => e.Kind != EndpointKind.IndexPage))
            {
                string route = endpoint.Path;

                await Context.RouteAsync($"**{route}*", async routeCall =>
                {
                    string body = routeCall.Request.Url.Contains("/load", StringComparison.Ordinal)
                        ? """{"JsonResponse":{"Success":true,"Error":null},"Items":{}}"""
                        : routeCall.Request.Url.Contains("/validate", StringComparison.Ordinal)
                            ? """{ "success": true }"""
                            : routeCall.Request.Url.Contains("/version", StringComparison.Ordinal)
                                ? $$"""{"version":"{{GuiSourceFixture.Instance.Version}}","is_latest":true}"""
                                : """{"success":true}""";

                    await routeCall.FulfillAsync(new RouteFulfillOptions
                    {
                        Status = 200,
                        ContentType = "application/json",
                        Body = body
                    });
                });
            }
        }

        /// <summary>
        /// Replaces Notyf and window.prompt so alert text is assertable and the export prompt cannot hang.
        /// </summary>
        /// <remarks>
        /// Done before any navigation, because index.html constructs <c>new Notyf(...)</c> at parse
        /// time. Without the shim that constructor throws a ReferenceError against the CDN script,
        /// and <c>createAlert</c> — the assertion channel for roughly twenty validation paths — is gone
        /// before a test can use it.
        /// </remarks>
        private async Task InstallAlertShim()
        {
            await Context.AddInitScriptAsync(
                """
                () => {
                  window.__guiAlerts = [];

                  class NotyfShim {
                    constructor(options) { window.__guiAlerts.push({ type: 'notyf', text: JSON.stringify(options ?? {}) }); }
                    success(message) { window.__guiAlerts.push({ type: 'success', text: String(message) }); return this; }
                    error(message) { window.__guiAlerts.push({ type: 'error', text: String(message) }); return this; }
                    warning(message) { window.__guiAlerts.push({ type: 'warning', text: String(message) }); return this; }
                    info(message) { window.__guiAlerts.push({ type: 'info', text: String(message) }); return this; }
                    dismiss() { return this; }
                  }

                  // Only the things index.html does not define for itself. createAlert is handled after
                  // load, by CaptureAlerts below: it is a global function declaration, and a global
                  // declaration is a DefineProperty on the window rather than a Set, so installing a
                  // non-writable accessor beforehand stops the declaration outright instead of
                  // shadowing it.
                  Object.defineProperty(window, 'Notyf', {
                    configurable: false,
                    get: () => NotyfShim,
                    set: () => {}
                  });

                  // Stubbed to a constant rather than to null: the export handler repeats its prompt
                  // until the answer ends in .bamc, so an unstubbed prompt spins forever.
                  window.prompt = function () { return 'gui-compliance.bamc'; };

                  window.confirm = function () { return true; };

                  window.alert = function () { return undefined; };
                }
                """
            );
        }

        /// <summary>
        /// Replaces <c>createAlert</c> with a recorder, once the page's own definition exists.
        /// </summary>
        /// <remarks>
        /// This is the only channel the GUI has for telling a user why something was refused, and it is
        /// declared as a global function, so it cannot be shadowed from an init script. A global
        /// function declaration leaves the property writable, so a plain assignment afterwards does
        /// replace it — which is the one point at which the replacement is possible. <br/>
        /// Called after every navigation, including the one in <see cref="ResetAsync"/>.
        /// </remarks>
        private async Task CaptureAlerts()        {
            await Page.EvaluateAsync(
                """
                () => {
                  window.__guiAlerts = [];

                  window.createAlert = function (type, message) {
                    window.__guiAlerts.push({ type: String(type), text: String(message) });
                  };
                }
                """
            );
        }

        /// <summary>Re-arms the alert recorder after a reload the test performed itself.</summary>
        public Task CaptureAlertsForTest() => CaptureAlerts();

        /// <summary>
        /// Puts the page back into a known state: no routes, no cookies, no storage, no script state.
        /// </summary>
        public async Task ResetAsync(string file = "index.html")
        {
            await Context.UnrouteAllAsync();
            await Page.GotoAsync("about:blank");
            await Context.ClearCookiesAsync();

            stubbedAlerts.Clear();

            await RouteExternalDependencies();
            await RouteBamEndpoints();
            await InstallAlertShim();

            await Page.GotoAsync(Server.Url(file), new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            await CaptureAlerts();
            await SeedEmptyScriptState();
        }

        /// <summary>
        /// Seeds <c>localStorage["commands"]</c> as an empty object.
        /// </summary>
        /// <remarks>
        /// An object, not an array. create_script.js reads <c>Object.keys(commands)</c> and the add
        /// command stores <c>commands[name] = ...</c>, so an array works by accident until it is
        /// stringified back into storage — and an array there sends <c>clearCurrentScriptState</c> into
        /// <c>window.location.reload(true)</c>, which reloads into the same array and spins. The reload
        /// loop is the failure mode, not the parse.
        /// </remarks>
        public async Task SeedEmptyScriptState()
        {
            await Page.EvaluateAsync(
                """
                () => {
                  localStorage.setItem('commands', JSON.stringify({}));
                  localStorage.setItem('dark-mode', 'false');
                }
                """
            );
        }

        /// <summary>
        /// Every alert raised, read out of the page.
        /// </summary>
        /// <remarks>
        /// Read after each action rather than only at the end: a test that asserts on the final state
        /// cannot tell which of three steps produced the message it is looking at.
        /// </remarks>
        public async Task<List<(string Type, string Message)>> ReadAlertsAsync()
        {
            List<string> raw = [.. await Page.EvaluateAsync<string[]>("() => (window.__guiAlerts ?? []).map(a => a.type + '|' + a.text)")];

            stubbedAlerts.Clear();

            return [.. raw.Select(entry =>
            {
                string[] parts = entry.Split('|', 2);

                return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
            })];
        }

        /// <summary>Runs a script in the page and returns its JSON result.</summary>
        public async Task<T> EvaluateAsync<T>(string script)
            => await Page.EvaluateAsync<T>(script);

        /// <summary>Runs a script in the page with one argument, and returns its result.</summary>
        public async Task<T> EvaluateAsync<T>(string script, object argument)
            => await Page.EvaluateAsync<T>(script, argument);

        /// <summary>
        /// Selects a command by setting the value and dispatching change, without Playwright's
        /// visibility check.
        /// </summary>
        /// <remarks>
        /// <c>#command-select</c> sits in a section index.html only reveals once its creator view is
        /// shown, so <c>SelectOptionAsync</c> waits for it to become visible and times out. The GUI
        /// binds its handler to the change event, which is all selecting a command actually does, so
        /// this drives the same thing without the actionability wait.
        /// </remarks>
        public async Task SelectCommandAsync(string command)
        {
            await Page.EvaluateAsync(
                """
                (command) => {
                  const select = document.getElementById('command-select');
                  select.value = command;
                  select.dispatchEvent(new Event('change', { bubbles: true }));
                }
                """,
                command
            );
        }

        public async ValueTask DisposeAsync()
        {
            await Context.CloseAsync();
        }
    }
}
