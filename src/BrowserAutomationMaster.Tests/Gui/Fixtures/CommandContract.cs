using System.Collections.ObjectModel;

namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// The HTTP surface BAMM's GUI server is expected to expose, declared here rather than observed.
    /// </summary>
    /// <remarks>
    /// This is a checked-in declaration, deliberately not derived from Tier B's runtime output. xUnit
    /// gives no ordering guarantee between collections, so a Tier C test that read its routes out of
    /// a live server would be depending on Tier B having already run. Declaring the contract in one
    /// place means Tier B can check the server against it and Tier C can fulfil routes from it without
    /// either depending on the other.
    /// </remarks>
    public static class CommandContract
    {
/// <summary>
        /// Every route the server answers, and what it is expected to return.
        /// </summary>
        /// <remarks>
        /// Methods are plain strings rather than <see cref="System.Net.Http.HttpMethod"/> because this
        /// is a declaration read by both a live client and Playwright's route fulfiller, which deal in
        /// the wire spelling rather than in HttpMethod instances.
        /// </remarks>
        public static readonly IReadOnlyList<EndpointContract> Endpoints = new ReadOnlyCollection<EndpointContract>(
        [
            new("/",            "GET",  EndpointKind.IndexPage,    "Serves the GUI's index.html from the extracted gui directory."),
            new("/export",      "POST", EndpointKind.Export,         "Writes a .bamc. Answered with 400 on every guard path."),
            new("/load",        "GET",  EndpointKind.Load,           "Lists userScripts, base64'd, under Items."),
            new("/terminate",   "GET",  EndpointKind.Terminate,      "Stops the listener. Answers { \"terminated\": true }."),
            new("/validate",    "GET",  EndpointKind.Validate,       "Checks script contents. Always answers a success boolean."),
            new("/version",     "GET",  EndpointKind.Version,        "BAMM's own version and whether it is the latest."),
            new("/gui_version", "GET",  EndpointKind.GuiVersion,     "The GUI's own version, read back out of the embedded archive."),
        ]);

        /// <summary>Routes a client may send an OPTIONS preflight for.</summary>
        public static readonly IReadOnlyList<string> PreflightRoutes = new ReadOnlyCollection<string>(["/export"]);

        /// <summary>
        /// <c>/export</c> sends <c>Content-Type: application/json</c>, which is not CORS-safelisted, so
        /// the GUI's <c>file://</c> origin (null) makes the browser send a preflight first. The Fetch
        /// specification allows exactly one Access-Control-Allow-Origin; the server used to send three,
        /// one from Server.cs's own header, one from the router, and one from Export's OPTIONS branch.
        /// </summary>
        public const string CORS_ORIGIN_HEADER = "Access-Control-Allow-Origin";

        /// <summary>The one header value a preflight answer must carry for the GUI's export to work.</summary>
        public const string CORS_ORIGIN_VALUE = "*";

        /// <summary>
        /// The preflight also has to allow the request header the GUI actually sends. Without this the
        /// browser rejects the preflight and /export fails outright.
        /// </summary>
        public const string CORS_ALLOWED_HEADERS_HEADER = "Access-Control-Allow-Headers";

        public const string CORS_ALLOWED_HEADERS_VALUE = "Content-Type";

        /// <summary>Every route the contract covers, as bare paths.</summary>
        public static IReadOnlyList<string> Paths => [.. Endpoints.Select(endpoint => endpoint.Path)];
    }

    /// <summary>What a route does, which is what the response shape follows from.</summary>
    public enum EndpointKind
    {
        /// <summary>Serves the GUI's index.html.</summary>
        IndexPage,

        /// <summary>Writes a .bamc to the userScripts directory.</summary>
        Export,

        /// <summary>Lists the .bamc files the user has, base64'd.</summary>
        Load,

        /// <summary>Stops the listener.</summary>
        Terminate,

        /// <summary>Validates script contents without compiling them.</summary>
        Validate,

        /// <summary>BAMM's own version.</summary>
        Version,

        /// <summary>The embedded GUI's version.</summary>
        GuiVersion,
    }

    /// <summary>One route of the declared contract.</summary>
    /// <param name="Path">The route, always starting with a slash.</param>
    /// <param name="Method">The method the GUI sends.</param>
    /// <param name="Kind">What the route does.</param>
    /// <param name="Summary">What a reader needs to know about the contract; also used in failure messages.</param>
    public sealed record EndpointContract(string Path, string Method, EndpointKind Kind, string Summary);
}
