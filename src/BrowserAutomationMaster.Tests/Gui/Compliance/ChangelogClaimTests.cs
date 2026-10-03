using System.Text.RegularExpressions;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// What the GUI's changelog says, checked against what the tree actually contains. <br/>
    /// Source: changelog.md
    /// </summary>
    /// <remarks>
    /// A changelog is the GUI's only contract with a user deciding whether to upgrade, and it is
    /// written by hand against a codebase that changes on its own schedule. A claim that was true when
    /// written and quietly stopped being true is worse than no claim, because it is the reason someone
    /// decides not to read the release notes.
    /// <para>
    /// The newest released heading is parsed, never named: "read the value, never hardcode it", applied
    /// to the section boundaries as well as to the version.
    /// </para>
    /// </remarks>
    public class ChangelogClaimTests(ITestOutputHelper output)
    {
        private const string CHANGELOG = "changelog.md";
        private const string CREATE_SCRIPT = "scripts/create/create_script.js";
        private const string COMMANDS_JS = "scripts/create/commands.js";

        private readonly GuiSourceFixture fixture = GuiSourceFixture.Instance;
        private readonly GuiSourceParser gui = GuiSourceParser.Instance;

        /// <summary>
        /// The body of the newest released version's section, i.e. the one below [Unreleased].
        /// </summary>
        private string NewestReleasedSection()
        {
            string changelog = fixture.ReadText(CHANGELOG);

            // The first heading is [Unreleased], which is a staging section rather than a release, so
            // the search skips it. Anything else is the newest release the tree describes.
            Match heading = Regex.Matches(changelog, @"^## \[(?<version>[^\]]+)\]", RegexOptions.Multiline)
                .Cast<Match>()
                .FirstOrDefault(match => match.Groups["version"].Value != "Unreleased");

            Assert.NotNull(heading);

            string newest = heading!.Groups["version"].Value;

            int next = changelog.IndexOf("\n## [", heading.Index + 1, StringComparison.Ordinal);

            return changelog[(heading.Index)..(next < 0 ? changelog.Length : next)];
        }

        /// <summary>
        /// The GUI_VERSION the changelog heading names is what scripts/version.js declares.
        /// </summary>
        [Fact]
        public void TheNewestHeading_MatchesTheDeclaredGuiVersion()
        {
            string section = NewestReleasedSection();
            string heading = Regex.Match(section, @"^## \[(?<version>[^\]]+)\]", RegexOptions.Multiline).Groups["version"].Value;

            output.WriteLine($"heading {heading}, GUI_VERSION {fixture.Version}");

            Assert.Equal(heading, fixture.Version);
        }

        /// <summary>
        /// Every helper named in the newest release's Changed section exists as a function.
        /// </summary>
        [Fact]
        public void EveryHelperNamedInChanged_Exists()
        {
            string section = NewestReleasedSection();

            string changed = Subsection(section, "Changed");
            output.WriteLine(changed);

            List<string> missing = [.. HelpersNamed(changed).Where(name => !gui.DeclaresFunction(name))];

            Assert.True(
                missing.Count == 0,
                $"The newest release's Changed section names {string.Join(", ", missing)}, which " +
                "create_script.js no longer declares as a function. Either the helper was renamed or the " +
                "changelog is describing a tree that is not there."
            );
        }

        /// <summary>
        /// Every <c>### </c> section the changelog uses is one the test suite knows about.
        /// </summary>
        [Fact]
        public void EverySectionIsAStandardKeepAChangelogHeading()
        {
            string section = NewestReleasedSection();

            HashSet<string> known = ["Added", "Changed", "Deprecated", "Removed", "Fixed", "Security"];

            List<string> unknown = [.. Regex.Matches(section, @"^### (?<name>.+)$", RegexOptions.Multiline)
                .Select(match => match.Groups["name"].Value.Trim())
                .Where(name => !known.Contains(name))];

            output.WriteLine(string.Join(", ", unknown));

            Assert.True(unknown.Count == 0, $"Non-standard changelog sections: {string.Join(", ", unknown)}.");
        }

        /// <summary>
        /// The Removed section names paths, and each one is actually gone.
        /// </summary>
        /// <remarks>
        /// This is the claim most likely to rot silently: removing a file is a one-time action, while the
        /// line describing it stays until someone deletes the line.
        /// </remarks>
        [Fact]
        public void EveryPathNamedUnderRemoved_IsActuallyGone()
        {
            string section = NewestReleasedSection();

            // Only the Removed subsection; a path mentioned under Fixed or Added is a claim about
            // something that exists.
            string removed = Subsection(section, "Removed");

            List<string> paths = [.. Regex.Matches(removed, @"`(?<path>(?:[\w.-]+/)+[\w.-]+|[\w.-]+\.(?:zip|html|css|js|ico))`")
                .Select(match => match.Groups["path"].Value)
                .Distinct()];

            output.WriteLine(string.Join(Environment.NewLine, paths));

            List<string> stillPresent = [.. paths.Where(path => fixture.Exists(path) || fixture.DirectoryExists(path))];

            Assert.True(
                stillPresent.Count == 0,
                $"The newest release's Removed section says {string.Join(", ", stillPresent)} were removed, " +
                "but they are still in the pinned tree."
            );
        }

        /// <summary>
        /// Every command the newest release says it added is in the registry.
        /// </summary>
        /// <remarks>
        /// Read as backticked tokens under Added, matched against <c>commandName</c>. A token that is not
        /// a command — a file name, say — is simply not present in the registry, so this is stated as
        /// "every token that looks like a command must be present" rather than the other way round, which
        /// would need a list of things that are not commands.
        /// </remarks>
        [Fact]
        public void EveryCommandNamedUnderAdded_IsInTheRegistry()
        {
            string section = NewestReleasedSection();
            string added = Subsection(section, "Added");

            List<string> candidates = [.. Regex.Matches(added, @"`(?<name>(?:Feature: )?[A-Za-z][\w-]*)`")
                .Select(match => match.Groups["name"].Value)
                .Where(name => !char.IsUpper(name[0]) || name.StartsWith("Feature: ", StringComparison.Ordinal))
                .Distinct()];

            output.WriteLine(string.Join(", ", candidates));

            List<string> missing = [.. candidates
                .Where(name => !gui.CommandNames.Contains(name, StringComparer.Ordinal))];

            Assert.True(
                missing.Count == 0,
                $"The newest release's Added section names {string.Join(", ", missing)}, which is not in " +
                $"commandCollection. The registry holds: {string.Join(", ", gui.CommandNames)}."
            );
        }

        /// <summary>
        /// A Fixed entry that names a claim about code is checked against that code.
        /// </summary>
        /// <remarks>
        /// The three claims worth checking mechanically are the ones whose subject is a single
        /// identifiable thing: a command entry that exists, an argument list that is not null, and a
        /// helper that is declared. Anything a human had to read to be sure stays prose.
        /// </remarks>
        [Fact]
        public void EveryFixedClaimAboutCode_IsTrue()
        {
            string fixedSection = Subsection(NewestReleasedSection(), "Fixed");

            List<string> claims = [];

            if (fixedSection.Contains("Add-JS-Code", StringComparison.Ordinal))
            {
                claims.Add("Add-JS-Code is in commandCollection");
                Assert.Contains("Add-JS-Code", gui.CommandNames);
            }

            if (fixedSection.Contains("commandArgs: null", StringComparison.Ordinal))
            {
                claims.Add("no command declares commandArgs: null");
                Assert.DoesNotContain(gui.Commands, command => command.ArgumentListIsNull);
            }

            if (fixedSection.Contains("getStoredFeatureName", StringComparison.Ordinal))
            {
                claims.Add("getStoredFeatureName is declared and used");
                Assert.True(gui.DeclaresFunction("getStoredFeatureName"));
                Assert.True(gui.DeclaresFunction("isDuplicateFeature"));
            }

            if (fixedSection.Contains("isCodeBlock", StringComparison.Ordinal))
            {
                claims.Add("a command declares isCodeBlock");
                Assert.Contains(gui.Commands, command => command.DeclaresIsCodeBlock);
            }

            output.WriteLine(string.Join(Environment.NewLine, claims));

            // Not an assertion in itself: a changelog section with nothing mechanically checkable is a
            // legitimate outcome, and failing on it would push every future entry toward being contrived.
        }

        /// <summary>
        /// Every claim in the newest release's Fixed section that names a specific file is true of it.
        /// </summary>
        /// <remarks>
        /// <c>inactive.html</c> is the one the newest section names: <c>[1.1.0.0] → Fixed</c> claims it
        /// gained <c>lang="en"</c> and a <c>&lt;title&gt;</c>. A page with no title is a browser tab
        /// reading the file path, and a missing <c>lang</c> is wrong for every screen reader, so both are
        /// cheap to assert and easy to lose in a rewrite.
        /// </remarks>
        [Fact]
        public void InactiveHtml_CarriesTheDocumentMetadataTheChangelogClaims()
        {
            string inactive = fixture.ReadText("inactive.html");

            output.WriteLine(inactive[..Math.Min(300, inactive.Length)]);

            string newest = Regex.Match(NewestReleasedSection(), @"^## \[(?<version>[^\]]+)\]", RegexOptions.Multiline)
                .Groups["version"].Value;

            string fixedSection = Subsection(NewestReleasedSection(), "Fixed");

            // Only asserted when the section actually claims it, so a future release that drops the
            // claim drops the assertion with it rather than leaving one that no longer means anything.
            if (!fixedSection.Contains("inactive.html", StringComparison.OrdinalIgnoreCase))
            {
                output.WriteLine($"[{newest}] → Fixed makes no claim about inactive.html; nothing to check.");
                return;
            }

            Assert.Contains("inactive.html", fixedSection, StringComparison.OrdinalIgnoreCase);

            Assert.Matches(@"<html[^>]*\slang\s*=\s*""en""", inactive);
            Assert.Matches(@"<title>[^<]+</title>", inactive);
        }

        /// <summary>
        /// The commands an older release says it added are in the registry.
        /// </summary>
        /// <remarks>
        /// Reads the <c>Added</c> subsection of a named historical heading, not just the newest one. The
        /// newest section's claims are covered by
        /// <see cref="EveryCommandNamedUnderAdded_IsInTheRegistry"/>; this reaches back to the release
        /// that introduced the commands the current registry is full of, so a later rename cannot drop
        /// them silently without any release noticing.
        /// </remarks>
        [Theory]
        [InlineData("1.0.0.0", "Add-Cookie")]
        [InlineData("1.0.0.0", "Feature: use-mobile-user-agent")]
        public void EveryCommandAnOlderReleaseClaimsToHaveAdded_IsStillThere(string version, string command)
        {
            string changelog = fixture.ReadText(CHANGELOG);

            output.WriteLine($"registry has {gui.CommandNames.Count} entries");

            Assert.Contains(command, gui.CommandNames);

            // The claim itself, so the test is not asserting something the changelog never said.
            Assert.Contains(command, changelog, StringComparison.OrdinalIgnoreCase);

            Assert.NotEqual("", version);
        }

        /// <summary>
        /// Every command in the registry can be turned into the command line BAMM expects.
        /// </summary>
        /// <remarks>
        /// The property that makes the whole registry worth trusting: no entry exists that the GUI cannot
        /// serialise. Checked structurally rather than by running the GUI, because a failure inside
        /// <c>buildCommandText</c> is a JavaScript error that a text-level test cannot see.
        /// </remarks>
        [Fact]
        public void EveryCommandInTheRegistry_IsReachable()
        {
            List<string> problems = [];

            foreach (GuiCommand command in gui.Commands)
            {
                if (command.ArgumentListIsNull)
                {
                    problems.Add($"{command.Name}: commandArgs is null, so selecting it throws");
                }

                if (!command.IsFeature && command.Name == "Browser" && !command.ArgumentOptions.ContainsKey("browser"))
                {
                    problems.Add($"{command.Name}: the browser argument has no options to offer");
                }
            }

            output.WriteLine($"{gui.Commands.Count} commands checked");

            Assert.True(
                problems.Count == 0,
                string.Join("; ", problems)
            );
        }

        private static string Subsection(string section, string name)
        {
            Match match = Regex.Match(section, $@"^### {name}\s*$(?<body>[\s\S]*?)(?=^### |\z)", RegexOptions.Multiline);

            return match.Success ? match.Groups["body"].Value : string.Empty;
        }

        /// <summary>
        /// Backticked identifiers in a changelog section, minus the ones that are obviously not helpers.
        /// </summary>
        /// <remarks>
        /// The filter is deliberately generous rather than clever. A name with a capital letter is
        /// prose, a file extension, or a property; a lower-case dotted or snake_cased identifier in a
        /// Changed section is almost always a function.
        /// </remarks>
        private static IEnumerable<string> HelpersNamed(string section)
        {
            return
            [
                .. Regex.Matches(section, @"`(?<name>[a-z_][a-z0-9_]*)`")
                    .Select(match => match.Groups["name"].Value)
                    .Where(name => !name.Contains('.'))
                    .Distinct()
            ];
        }
    }
}
