using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Tests.Gui.Fixtures
{
    /// <summary>
    /// The facts the compliance tests read out of the pinned GUI tree. <br/>
    /// Source: scripts/create/commands.js, index.html, scripts/create/create_script.js
    /// </summary>
    /// <remarks>
    /// Text scraping, not evaluation. Running the GUI's JavaScript here would mean shipping a JS
    /// engine into the test project and would quietly turn a compliance check into a re-implementation;
    /// the point of these tests is to read what the GUI actually ships. The patterns are anchored and
    /// each carries a fail-loud check, so a GUI that restructures and breaks a pattern produces a
    /// clear "no longer matches" message rather than a silently empty result.
    /// </remarks>
    public sealed class GuiSourceParser
    {
        private const string COMMANDS_JS = "scripts/create/commands.js";
        private const string CREATE_SCRIPT_JS = "scripts/create/create_script.js";
        private const string INDEX_HTML = "index.html";

        private static readonly Lazy<GuiSourceParser> shared = new(() => new GuiSourceParser());

        private readonly Lazy<IReadOnlyList<GuiCommand>> commands;
        private readonly Lazy<string> createScriptSource;
        private readonly Lazy<string> indexHtmlSource;

        private GuiSourceParser()
        {
            GuiSourceFixture fixture = GuiSourceFixture.Instance;

            createScriptSource = new Lazy<string>(() => fixture.ReadText(CREATE_SCRIPT_JS));
            indexHtmlSource = new Lazy<string>(() => fixture.ReadText(INDEX_HTML));
            commands = new Lazy<IReadOnlyList<GuiCommand>>(() => ParseCommands(fixture.ReadText(COMMANDS_JS)));
        }

        public static GuiSourceParser Instance => shared.Value;

        /// <summary>All entries of the GUI's <c>commandCollection</c>, in declaration order.</summary>
        public IReadOnlyList<GuiCommand> Commands => commands.Value;

        /// <summary>Every <c>commandName</c>, in declaration order.</summary>
        public IReadOnlyList<string> CommandNames => [.. Commands.Select(command => command.Name)];

        /// <summary>Every entry that is not a feature, in declaration order.</summary>
        public IReadOnlyList<GuiCommand> ActionCommands
            => [.. Commands.Where(command => !command.IsFeature)];

        /// <summary>
        /// The suffix of every <c>Feature:</c> entry — <c>"Feature: use-http-proxy"</c> yields
        /// <c>use-http-proxy</c>, which is the form BAMM's <c>Parser.featureArgs</c> holds.
        /// </summary>
        public IReadOnlyList<string> FeatureNames
            => [.. Commands.Where(command => command.IsFeature).Select(command => command.FeatureName)];

        /// <summary>The raw contents of scripts/create/create_script.js.</summary>
        public string CreateScriptSource => createScriptSource.Value;

        /// <summary>The raw contents of index.html.</summary>
        public string IndexHtmlSource => indexHtmlSource.Value;

        /// <summary>
        /// The port index.html falls back to. It hardcodes this and never calls setPort(), so the port is
        /// the same no matter what query string the GUI was opened with.
        /// </summary>
        public string GuiPort => ReadRequired(indexHtmlSource.Value, @"var\s+guiPort\s*=\s*""(\d+)"";", "guiPort");

        /// <summary>
        /// The origin every fetch in the GUI is built from, with the port already substituted in.
        /// </summary>
        /// <remarks>
        /// index.html declares it as a template literal, <c>`http://127.0.0.1:${guiPort}`</c>, so reading
        /// the template alone would report the placeholder rather than the origin a request goes to.
        /// </remarks>
        public string BaseServerUrl
        {
            get
            {
                string template = ReadRequired(indexHtmlSource.Value, @"var\s+baseServerURL\s*=\s*`([^`]+)`", "baseServerURL");

                return template.Replace("${guiPort}", GuiPort, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Every URL-valued variable, mapped to the template it is assigned. Read from the assignment
        /// rather than from the fetch call sites, because one variable is fetched from more than one
        /// place and one fetch builds its URL on the spot from another variable.
        /// </summary>
        public IReadOnlyDictionary<string, string> UrlVariables => ParseUrlVariables();

        /// <summary>
        /// Each variable's fully resolved URL, with variable-to-variable references followed through.
        /// </summary>
        /// <remarks>
        /// Needed because the GUI is not consistent: most routes have a <c>…URL</c> variable built
        /// straight from <c>baseServerURL</c>, but create_script.js builds <c>finalUrl</c> from
        /// <c>validateScriptURL</c>. Reading only the direct form would miss that second fetch's route.
        /// </remarks>
        public IReadOnlyDictionary<string, string> ResolvedUrls => ResolveUrls();

        /// <summary>
        /// The <c>proxyFeatures</c> list create_script.js checks a second proxy feature against.
        /// </summary>
        public IReadOnlyList<string> ProxyFeatures => ParseProxyFeatures();

        /// <summary>
        /// Every route the GUI fetches, de-duplicated. Includes anything built inline, not just the
        /// named variables, so a fetch added without a matching variable is still caught.
        /// </summary>
        public IReadOnlyList<string> FetchedRoutes => ParseFetchedRoutes();

        /// <summary>Whether the tree declares the named helper as a top-level function.</summary>
        public bool DeclaresFunction(string name)
            => Regex.IsMatch(CreateScriptSource, $@"(?m)^\s*function\s+{Regex.Escape(name)}\s*\(");

        private static string ReadRequired(string source, string pattern, string what)
        {
            Match match = Regex.Match(source, pattern);

            if (!match.Success)
            {
                throw new InvalidOperationException(
                    $"Could not read {what} from the pinned GUI. The pattern {pattern} no longer matches, so the " +
                    "GUI has been restructured and whichever compliance test depended on this needs rewriting."
                );
            }

            return match.Groups[1].Value;
        }

        private static IReadOnlyList<GuiCommand> ParseCommands(string source)
        {
            List<GuiCommand> parsed = [];

            // One match per `commandName:` literal. The command block's other properties are read
            // separately, relative to the match's index, so a reordering inside a block cannot
            // misattribute an argument list to the wrong command.
            MatchCollection names = Regex.Matches(source, @"commandName:\s*""([^""]+)""");

            if (names.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No commandName entries found in the pinned tree's {COMMANDS_JS}. The GUI's command " +
                    "registry could not be read, so no parity test can run."
                );
            }

            for (int i = 0; i < names.Count; i++)
            {
                Match name = names[i];

                // The block runs to the next commandName, or to the end of the array for the last entry.
                int blockStart = name.Index;
                int blockEnd = i + 1 < names.Count ? names[i + 1].Index : source.Length;
                string block = source[blockStart..blockEnd];

                parsed.Add(new GuiCommand(
                    Name: name.Groups[1].Value,
                    ArgumentNames: ParseArgumentNames(block),
                    DeclaresIsCodeBlock: Regex.IsMatch(block, @"\bisCodeBlock\s*:\s*true\b"),
                    ArgumentOptions: ParseArgumentOptions(block),
                    SourceText: block
                ));
            }

            return parsed;
        }

        private static IReadOnlyList<string> ParseArgumentNames(string block)
        {
            Match? commandArgs = Regex.Match(block, @"commandArgs\s*:\s*(\{[^}]*\}|null)");

            if (!commandArgs.Success)
            {
                return [];
            }

            // A bare null means the GUI will throw when the command is selected; the caller asserts on
            // that separately, so an empty list here is the honest answer rather than a guess.
            //
            // Matched as {[^}]*} rather than lazily to a closing brace on its own line: an argument
            // list never nests, and the lazy form runs past `{}` and picks up the keys of every
            // property after it, which reported Start-Javascript as taking commandDescription,
            // disabledOnLoad and placeholder.
            if (commandArgs.Groups[1].Value == "null")
            {
                return [];
            }

            MatchCollection keys = Regex.Matches(commandArgs.Groups[1].Value, @"(?:""([^""]+)""|([A-Za-z_][A-Za-z0-9_-]*))\s*:");

            return [.. keys.Select(key => key.Groups[1].Success ? key.Groups[1].Value : key.Groups[2].Value)];
        }

        /// <summary>
        /// For each argument, the option values it offers, or an empty list for a free-text argument.
        /// A quoted string in the source becomes its unquoted value, matching what the GUI writes into
        /// a command.
        /// </summary>
        private static IReadOnlyDictionary<string, IReadOnlyList<string>> ParseArgumentOptions(string block)
        {
            Dictionary<string, IReadOnlyList<string>> options = new(StringComparer.Ordinal);

            Match? commandArgs = Regex.Match(block, @"commandArgs\s*:\s*(\{[^}]*\}|null)");

            if (!commandArgs.Success || commandArgs.Groups[1].Value == "null")
            {
                return options;
            }

            string body = commandArgs.Groups[1].Value;

            foreach (Match pair in Regex.Matches(body, @"(?:""([^""]+)""|([A-Za-z_][A-Za-z0-9_-]*))\s*:\s*(\[[^\]]*\]|null)"))
            {
                string name = pair.Groups[1].Success ? pair.Groups[1].Value : pair.Groups[2].Value;
                string value = pair.Groups[3].Value;

                List<string> values = [];

                if (value != "null")
                {
                    // An element has to start at the bracket or the comma, or the ", " between two
                    // elements reads as a value of its own. The elements are single-quoted strings that
                    // contain double quotes, because the GUI quotes the value it will write out.
                    foreach (Match option in Regex.Matches(value, @"[,\[]\s*'([^']*)'"))
                    {
                        values.Add(option.Groups[1].Value.Trim('"'));
                    }
                }

                options[name] = values;
            }

            return options;
        }

        /// <summary>
        /// Every template-literal variable in the tree that interpolates at least one other variable,
        /// mapped to its template.
        /// </summary>
        /// <remarks>
        /// No name filter. The GUI is not consistent about naming — <c>exportScriptURL</c> is one, but
        /// the export handler assigns its own URL to a variable called plain <c>url</c> — and a filter
        /// loose enough to catch that would also catch <c>message</c>, the alert text template. Which
        /// ones are actually server URLs is decided by <see cref="ResolveUrls"/> instead.
        /// </remarks>
        private IReadOnlyDictionary<string, string> ParseUrlVariables()
        {
            Dictionary<string, string> urls = new(StringComparer.Ordinal);

            string source = IndexHtmlSource + "\n" + CreateScriptSource;

            // The template's contents only, without the delimiters, so substituting a referenced
            // template in cannot leave a stray backtick in the middle of a URL.
            foreach (Match match in Regex.Matches(source, @"(?:var|let|const)\s+(\w+)\s*=\s*`([^`]*\$\{\w+\}[^`]*)`"))
            {
                urls[match.Groups[1].Value] = match.Groups[2].Value;
            }

            if (!urls.ContainsKey("baseServerURL"))
            {
                throw new InvalidOperationException(
                    "No template-literal URL variables found in the pinned tree, so the GUI's endpoint " +
                    "surface cannot be read."
                );
            }

            return urls;
        }

        /// <summary>
        /// Each server URL the GUI builds, with variable-to-variable references followed through and
        /// non-URL templates dropped.
        /// </summary>
        /// <remarks>
        /// A reference to a variable that is not itself a template — <c>${b64Contents}</c> in
        /// <c>finalUrl</c>, <c>${encodedContents}</c> in the export handler — is a value filled in at
        /// request time, not something to follow, so it is left in place as a placeholder. The route
        /// is the part before the first <c>?</c>, which is unaffected.
        /// </remarks>
        private IReadOnlyDictionary<string, string> ResolveUrls()
        {
            Dictionary<string, string> templates = new(ParseUrlVariables(), StringComparer.Ordinal);

            Dictionary<string, string> resolved = new(StringComparer.Ordinal);

            foreach ((string name, string template) in templates)
            {
                string current = template;

                for (int depth = 0; depth < 10 && current.Contains("${", StringComparison.Ordinal); depth++)
                {
                    string before = current;

                    current = Regex.Replace(current, @"\$\{(\w+)\}", match =>
                        templates.TryGetValue(match.Groups[1].Value, out string? referenced)
                            ? referenced
                            : match.Value);

                    if (current == before)
                    {
                        break;
                    }
                }

                current = current.Replace("${guiPort}", GuiPort, StringComparison.Ordinal);

                // Only the ones that address the GUI's own server. `message`, the alert text template,
                // interpolates too but is not a route.
                if (current.StartsWith(BaseServerUrl, StringComparison.Ordinal))
                {
                    resolved[name] = current;
                }
            }

            return resolved;
        }

        private IReadOnlyList<string> ParseProxyFeatures()
        {
            Match block = Regex.Match(CreateScriptSource, @"const\s+proxyFeatures\s*=\s*\[(?<body>[\s\S]*?)\]");

            if (!block.Success)
            {
                throw new InvalidOperationException(
                    "Could not find the proxyFeatures list in create_script.js, so the GUI's proxy rule " +
                    "cannot be read."
                );
            }

            List<string> features = [.. Regex.Matches(block.Groups["body"].Value, @"""([^""]+)""").Select(m => m.Groups[1].Value)];

            return features;
        }

        /// <summary>
        /// The routes a <c>fetch()</c> call is actually pointed at, de-duplicated in the order first seen.
        /// </summary>
        /// <remarks>
        /// Read from the call sites rather than from the declarations. A declared URL variable that
        /// nothing fetches is not part of the GUI's endpoint surface: createScriptURL is assigned but
        /// never fetched, and <c>/create</c> has no case in the server's router, so counting it would
        /// assert a route that does not exist.
        /// </remarks>
        private IReadOnlyList<string> ParseFetchedRoutes()
        {
            IReadOnlyDictionary<string, string> urls = ResolveUrls();

            List<string> routes = [];

            string source = IndexHtmlSource + "\n" + CreateScriptSource;

            // The argument is the first token after `fetch(`, which is a bare variable name in every
            // case in the tree. Stopping at the comma or the closing paren keeps the options object out.
            foreach (Match call in Regex.Matches(source, @"fetch\(\s*(\w+)\s*[,)]"))
            {
                string argument = call.Groups[1].Value;

                if (!urls.TryGetValue(argument, out string? url))
                {
                    throw new InvalidOperationException(
                        $"fetch({argument}, ...) at the pinned tree, but no template literal is assigned to " +
                        $"{argument}. Either the GUI renamed it or built it a way this parser cannot follow, " +
                        "and in both cases the endpoint surface below is incomplete."
                    );
                }

                // Take the path and stop at the query string: /validate?contents=... is still /validate.
                Match route = Regex.Match(url, @"^[a-z]+://[^/]+(/[^?]*)");

                if (!route.Success)
                {
                    continue;
                }

                string path = route.Groups[1].Value;

                if (!routes.Contains(path))
                {
                    routes.Add(path);
                }
            }

            return routes;
        }
    }

    /// <summary>
    /// One entry of the GUI's <c>commandCollection</c>, as far as the parity tests need to see it.
    /// </summary>
    /// <param name="Name">The <c>commandName</c> as declared, e.g. <c>"Feature: use-http-proxy"</c>.</param>
    /// <param name="ArgumentNames">The argument keys declared for the command.</param>
    /// <param name="DeclaresIsCodeBlock">Whether the entry sets <c>isCodeBlock: true</c>.</param>
    /// <param name="ArgumentOptions">The option values offered per argument, if any.</param>
    /// <param name="SourceText">The entry's source text, for assertions more specific than the parsed fields.</param>
    public sealed record GuiCommand(
        string Name,
        IReadOnlyList<string> ArgumentNames,
        bool DeclaresIsCodeBlock,
        IReadOnlyDictionary<string, IReadOnlyList<string>> ArgumentOptions,
        string SourceText
    )
    {
        /// <summary>Whether this is one of the GUI's <c>Feature:</c> entries.</summary>
        public bool IsFeature => Name.StartsWith("Feature:", StringComparison.Ordinal);

        /// <summary>The feature suffix, in the form BAMM's Parser holds it. Empty for a non-feature.</summary>
        public string FeatureName => IsFeature ? Name["Feature: ".Length..].ToLowerInvariant() : "";

        /// <summary>
        /// The name in BAMM's script-command vocabulary: lowercased, and with the colon turned into a
        /// hyphen the way <c>buildStandardCommandText</c> does it. <c>Feature: use-http-proxy</c> and
        /// <c>Add-JS-Code</c> both come out as a hyphenated command name, which is why the feature axis
        /// compares <see cref="FeatureName"/> and this axis does not overlap with it.
        /// </summary>
        public string ScriptCommandName => Name.ToLowerInvariant().Replace("feature: ", "-").Replace(": ", "-");

        /// <summary>
        /// Whether the entry leaves its argument list null, which makes selecting it throw.
        /// The source text is checked for this because the parser reports an absent list as empty,
        /// which is also what <c>{}</c> legitimately produces.
        /// </summary>
        public bool ArgumentListIsNull => Regex.IsMatch(SourceText, @"commandArgs\s*:\s*null\b");
    }
}
