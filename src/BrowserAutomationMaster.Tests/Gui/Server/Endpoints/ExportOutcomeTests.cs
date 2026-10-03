using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Server.Endpoints
{
    /// <summary>
    /// Whether an export actually produces a usable script, and what a refusal leaves behind. <br/>
    /// Source: Core/GUI/BackendFunctions.cs (Export), scripts/create/create_script.js
    /// (addCommandToCommandList, validateScriptContents)
    /// </summary>
    [Trait("Category", "Server")]
    public class ExportOutcomeTests(ITestOutputHelper output)
    {
        private static string Encode(string contents)
            => Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(contents)));

        private static string Decode(string base64) => Encoding.UTF8.GetString(Convert.FromBase64String(base64));

        [Fact]
        public async Task ARefusedExport_LeavesAnEmptyFileThatLoadThenLists()
        {
            await using GuiServerProcess server = await GuiServerProcess.StartAsync(_ => { });
            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

            string fileName = $"gui-compliance-refused-{Guid.NewGuid():N}.bamc";

            using HttpResponseMessage refused = await client.GetAsync(
                $"/export?filename={Uri.EscapeDataString(fileName)}&contents={Encode("{ not json }")}");

            output.WriteLine($"GET /export -> {(int)refused.StatusCode}");

            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

            // File.Create runs before the line loop, so the file exists — empty — by the time the
            // rejection happens. Nothing removes it, and /load enumerates by extension rather than by
            // content, so the empty file is offered as a script.
            Assert.Contains(fileName, server.StagedScriptNames);

            using HttpResponseMessage loaded = await client.GetAsync("/load");

            using JsonDocument document = JsonDocument.Parse(await loaded.Content.ReadAsStringAsync());
            JsonElement items = document.RootElement.GetProperty("Items");

            string key = Assert.Single(
                items.EnumerateObject()
                    .Where(property => property.Name.EndsWith(fileName, StringComparison.Ordinal))
                    .Select(property => property.Name));

            string contents = Decode(items.GetProperty(key).GetString()!);

            output.WriteLine($"listed as: '{contents}' ({contents.Length} chars)");

            // Selecting it yields a script with no commands, which the creator reports as "No commands
            // present in the current script."
            Assert.Equal("", contents);
        }

        /// <summary>
        /// An export whose entries are JSON objects — the shape the GUI produces — writes the script.
        /// </summary>
        /// <remarks>
        /// The command list holds <c>buildCommandText</c>'s output, which is a JSON object, and
        /// <c>reindexCommands</c> rebuilds <c>commands</c> from those list items' text. So the payload
        /// <c>/export</c> receives is JSON and <c>Export</c>'s deserialisation is correct. This test
        /// exists to keep it that way from this side: if the GUI ever sent bare command lines, every
        /// export would be refused and this would fail with the parsing error.
        /// </remarks>
        [Fact]
        public async Task AnExportOfTheGuisPayload_WritesAUsableScript()
        {
            await using GuiServerProcess server = await GuiServerProcess.StartAsync(_ => { });
            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

            string[] payload =
            [
                """{"browser":"\"chrome\""}""",
                """{"visit":"\"https://example.com\""}""",
            ];

            using HttpResponseMessage exported = await client.GetAsync(
                $"/export?filename={Uri.EscapeDataString("gui-payload.bamc")}&contents={Encode(string.Join('\n', payload))}");

            string body = await exported.Content.ReadAsStringAsync();
            output.WriteLine($"GET /export -> {(int)exported.StatusCode} {body}");

            Assert.Equal(HttpStatusCode.OK, exported.StatusCode);

            using HttpResponseMessage loaded = await client.GetAsync("/load");

            using JsonDocument document = JsonDocument.Parse(await loaded.Content.ReadAsStringAsync());
            JsonElement items = document.RootElement.GetProperty("Items");

            string key = Assert.Single(
                items.EnumerateObject()
                    .Where(property => property.Name.EndsWith("gui-payload.bamc", StringComparison.Ordinal))
                    .Select(property => property.Name));

            string written = Decode(items.GetProperty(key).GetString()!);

            output.WriteLine($"written: {written.Replace('\n', '|')}");

            // The .bamc holds command lines, which is what the parser reads.
            Assert.Equal(["browser \"chrome\"", "visit \"https://example.com\""], written.Split('\n', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// An entry's second property is dropped, so a feature's argument never reaches the script.
        /// </summary>
        /// <remarks>
        /// The defect, pinned from the server's side. <c>buildFeatureCommandText</c> emits the feature
        /// name and its argument as two properties of one object —
        /// <c>{"feature": "\"use-http-proxy\"", "proxy-string": "\"USER:PASS@IP:PORT\""}</c> — and
        /// <c>Export</c> takes <c>contentDict.First()</c>, writing only that pair. The GUI's
        /// <c>validateScriptContents</c> reads <c>keys[0]</c> for the same reason.
        ///
        /// So a proxy feature is exported as <c>feature "use-http-proxy"</c> with no proxy string, which
        /// <c>Parser.IsValidProxyFormat</c> rejects. The user can add a proxy feature through every
        /// guard the creator applies and only find out at validation time that the script will not run.
        /// <para>
        /// Which side should change is a design decision across two repositories: /export could write
        /// every property, or <c>buildFeatureCommandText</c> could fold the argument into the feature's
        /// own value. Both are one-line changes on a different repository, so this states the behaviour
        /// rather than picking for them.
        /// </para>
        /// </remarks>
        [Fact]
        public async Task AFeatureEntryLosesItsArgumentOnTheWayToDisk()
        {
            await using GuiServerProcess server = await GuiServerProcess.StartAsync(_ => { });
            using HttpClient client = new() { BaseAddress = new Uri(server.BaseUrl), Timeout = TimeSpan.FromSeconds(15) };

            string[] payload =
            [
                """{"browser":"\"chrome\""}""",
                """{"feature":"\"use-http-proxy\"","proxy-string":"\"USER:PASS@IP:PORT\""}""",
            ];

            using HttpResponseMessage exported = await client.GetAsync(
                $"/export?filename={Uri.EscapeDataString("feature-argument.bamc")}&contents={Encode(string.Join('\n', payload))}");

            output.WriteLine($"GET /export -> {(int)exported.StatusCode}");

            // Accepted — the entry parses. The loss is silent, which is the worse half.
            Assert.Equal(HttpStatusCode.OK, exported.StatusCode);

            using HttpResponseMessage loaded = await client.GetAsync("/load");

            using JsonDocument document = JsonDocument.Parse(await loaded.Content.ReadAsStringAsync());
            JsonElement items = document.RootElement.GetProperty("Items");

            string key = Assert.Single(
                items.EnumerateObject()
                    .Where(property => property.Name.EndsWith("feature-argument.bamc", StringComparison.Ordinal))
                    .Select(property => property.Name));

            string written = Decode(items.GetProperty(key).GetString()!);

            output.WriteLine($"written: {written.Replace('\n', '|')}");

            string[] lines = written.Split(['\n'], StringSplitOptions.RemoveEmptyEntries);

            output.WriteLine(string.Join(Environment.NewLine, lines));

            Assert.Contains(lines, line => line == "feature \"use-http-proxy\"");
            Assert.DoesNotContain(lines, line => line.Contains("USER:PASS", StringComparison.Ordinal));
        }
    }
}