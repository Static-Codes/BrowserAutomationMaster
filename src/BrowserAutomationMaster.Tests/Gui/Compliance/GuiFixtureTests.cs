using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// Proves the fixture can actually read the pinned tree. Everything else in Tier A is downstream
    /// of this: a parser that quietly returns nothing would turn every parity test into a pass.
    /// </summary>
    public class GuiFixtureTests(ITestOutputHelper output)
    {
        private readonly GuiSourceParser gui = GuiSourceParser.Instance;

        [Fact]
        public void Manifest_NamesTheTreeItResolved()
        {
            GuiSourceFixture fixture = GuiSourceFixture.Instance;

            output.WriteLine($"root      {fixture.Root}");
            output.WriteLine($"tag       {fixture.Manifest.Tag}");
            output.WriteLine($"sha       {fixture.Manifest.Sha}");
            output.WriteLine($"from      {fixture.Manifest.ResolvedFrom}");
            output.WriteLine($"version   {fixture.Manifest.Version}");

            Assert.NotEqual("", fixture.Manifest.Sha);
            Assert.NotEqual("", fixture.Manifest.Tag);
            Assert.NotEqual("", fixture.Manifest.ResolvedFrom);
            Assert.NotEqual("", fixture.Manifest.Version);
        }

        [Fact]
        public void EveryCommand_IsParsed()
        {
            Assert.NotEmpty(gui.Commands);

            output.WriteLine(string.Join(Environment.NewLine, gui.Commands.Select(c =>
                $"{c.Name}  args=[{string.Join(", ", c.ArgumentNames)}]  codeBlock={c.DeclaresIsCodeBlock}")));

            Assert.All(gui.Commands, command => Assert.False(string.IsNullOrWhiteSpace(command.Name)));
        }

        [Fact]
        public void EveryCommandArgumentList_IsAnObjectRatherThanNull()
        {
            // commandArgs: null makes renderArguments call Object.keys(null), so selecting the command
            // throws. Reading it as "no arguments" would hide that, hence the separate assertion.
            Assert.Empty(gui.Commands.Where(command => command.ArgumentListIsNull).Select(command => command.Name));
        }

        [Fact]
        public void Features_AreSeparatedFromActions()
        {
            output.WriteLine($"actions:  {string.Join(", ", gui.ActionCommands.Select(c => c.Name))}");
            output.WriteLine($"features: {string.Join(", ", gui.FeatureNames)}");

            Assert.NotEmpty(gui.ActionCommands);
            Assert.NotEmpty(gui.FeatureNames);
            Assert.DoesNotContain(gui.ActionCommands, command => command.IsFeature);
            Assert.All(gui.FeatureNames, feature => Assert.DoesNotContain("feature:", feature, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void BrowserCommand_OffersTheThreeBrowsersAsRadioValues()
        {
            GuiCommand browser = Assert.Single(gui.Commands, command => command.Name == "Browser");

            Assert.Equal(["chrome", "firefox", "safari"], browser.ArgumentOptions["browser"]);
        }

        [Fact]
        public void BaseServerUrl_UsesTheHardcodedPort()
        {
            output.WriteLine($"guiPort {gui.GuiPort}");
            output.WriteLine($"base   {gui.BaseServerUrl}");

            Assert.Equal("8008", gui.GuiPort);
            Assert.Equal($"http://127.0.0.1:{gui.GuiPort}", gui.BaseServerUrl);
        }

        [Fact]
        public void ResolvedUrls_CoverTheServersRoutesAndNothingElse()
        {
            output.WriteLine(string.Join(Environment.NewLine, gui.ResolvedUrls.Select(u => $"{u.Key} -> {u.Value}")));

            // Asserted as routes rather than as whole URLs: the query strings hold request-time values
            // (${b64Contents}, ${encodedFilename}) whose spellings are not the subject here. Every one
            // of these variables has to point at the declared contract. baseServerURL is the bare origin
            // rather than a route, so it is checked separately below and skipped here.
            Assert.Equal(
                ["/create", "/export", "/load", "/terminate", "/validate", "/version"],
                gui.ResolvedUrls
                    .Where(entry => entry.Key != "baseServerURL")
                    .Select(entry => RouteOf(entry.Value))
                    .Distinct()
                    .OrderBy(route => route, StringComparer.Ordinal)
            );
        }

        [Fact]
        public void EveryServerUrl_UsesTheHardcodedPort()
        {
            foreach ((string name, string url) in gui.ResolvedUrls)
            {
                output.WriteLine($"{name} -> {url}");

                Assert.StartsWith($"http://127.0.0.1:{gui.GuiPort}", url);
            }
        }

        private static string RouteOf(string url)
        {
            int pathStart = url.IndexOf('/', "http://".Length);
            int queryStart = url.IndexOf('?', pathStart);

            return queryStart < 0 ? url[pathStart..] : url[pathStart..queryStart];
        }

        [Fact]
        public void ProxyFeatures_MatchBammSProxyRegistry()
        {
            output.WriteLine(string.Join(", ", gui.ProxyFeatures));

            Assert.Equal(["use-http-proxy", "use-https-proxy", "use-socks4-proxy", "use-socks5-proxy"], gui.ProxyFeatures);
        }

        [Fact]
        public void FetchedRoutes_ExcludeTheUnimplementedCreateRoute()
        {
            // createScriptURL is declared and never fetched; /create has no case in the router. If it
            // ever appears here, the GUI started calling something the server does not answer.
            output.WriteLine(string.Join(", ", gui.FetchedRoutes));

            Assert.DoesNotContain("/create", gui.FetchedRoutes);
            Assert.Contains("/export", gui.FetchedRoutes);
            Assert.Contains("/load", gui.FetchedRoutes);
            Assert.Contains("/validate", gui.FetchedRoutes);
        }
    }
}
