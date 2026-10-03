using System.Text;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Core.Common;
using Xunit;
using Xunit.Abstractions;

// Aliased because this file's own namespace ends in .Commands, which shadows the class it reads.
using CommandRegistry = BrowserAutomationMaster.Core.Common.Commands;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// The CLI registry and the published documentation agree. <br/>
    /// Source: Core/Common/Commands.cs, BAMM-Docs/advanced_docs.html
    /// </summary>
    /// <remarks>
    /// <c>Commands.cs</c> is the authority: it is what <c>bamm help</c> prints, so it cannot be checked
    /// against the docs — it *is* the reference. What can drift is the documentation, which is a
    /// separate repository edited by hand and has no test of its own. A command added to the registry
    /// and never documented is invisible to everyone except the person who added it.
    /// <para>
    /// One direction is enforced and one is reported. Registry → docs fails the build, because that is
    /// the actionable gap and the one this suite exists to close. Docs → registry is reported rather
    /// than enforced: a documented command BAMM does not have is a documentation bug, but it is not a
    /// correctness problem in BAMM, and it should not be able to break the build for the next person who
    /// runs a test.
    /// </para>
    /// </remarks>
    public class DocumentationSyncTests(ITestOutputHelper output)
    {
        private const string DOCS_FILE_NAME = "advanced_docs.html";

        /// <summary>
        /// Where the documentation was found, or why it could not be found.
        /// </summary>
        private static (string? Path, string? Reason) ResolveDocs()
        {
            string? configured = Environment.GetEnvironmentVariable("BAMM_DOCS_PATH");

            if (!string.IsNullOrEmpty(configured) && File.Exists(Path.Combine(configured, DOCS_FILE_NAME)))
            {
                return (configured, null);
            }

            // Four levels up from this assembly: BaseDirectory -> net10.0 -> <config> -> bin ->
            // BrowserAutomationMaster.Tests -> src -> the repository's parent, which is where the
            // sibling repositories live. Same shape as the BAMM-GUI fixture resolution.
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "BAMM-Docs");

                if (File.Exists(Path.Combine(candidate, DOCS_FILE_NAME)))
                {
                    return (candidate, null);
                }

                directory = directory.Parent;
            }

            return (null,
                $"No {DOCS_FILE_NAME} found. Set BAMM_DOCS_PATH to a checkout of Static-Codes/BAMM-Docs, " +
                "or clone it as a sibling of this repository.");
        }

        /// <summary>
        /// The three command tables, indexed the way the document orders them.
        /// </summary>
        /// <remarks>
        /// 0 is the script commands, 1 the <c>feature</c> commands, 2 the CLI arguments. Index matters
        /// as much as presence: a command documented in the wrong table tells a reader the wrong thing
        /// about how to invoke it, and a registry entry whose type disagrees with its table is exactly
        /// the kind of drift this is here to catch.
        /// </remarks>
        private static IReadOnlyList<IReadOnlyList<string>> ParseTables(string html)
        {
            List<IReadOnlyList<string>> tables = [];

            foreach (Match table in Regex.Matches(html, @"<table[\s\S]*?</table>").Take(3))
            {
                List<string> commands = [];

                foreach (Match row in Regex.Matches(table.Value, @"<tr[\s\S]*?</tr>").Skip(1))
                {
                    Match cell = Regex.Match(row.Value, @"<td[^>]*>\s*<code>\s*(?<name>[^<]+?)\s*</code>");

                    if (cell.Success)
                    {
                        commands.Add(cell.Groups["name"].Value);
                    }
                }

                tables.Add(commands);
            }

            return tables;
        }

        /// <summary>Which documented table an entry of this type belongs in.</summary>
        private static int ExpectedTable(CommandType type) => type switch
        {
            CommandType.Action => 0,
            CommandType.Feature => 1,
            _ => 2
        };

        [SkippableFact]
        public void EveryRegistryCommand_IsDocumented()
        {
            (string? root, string? reason) = ResolveDocs();

            Skip.If(root is null, reason!);

            IReadOnlyList<IReadOnlyList<string>> tables =
                ParseTables(File.ReadAllText(Path.Combine(root!, DOCS_FILE_NAME)));

            HashSet<string> documented = [.. tables.SelectMany(table => table)];

            List<string> missing = [.. CommandRegistry.CommandList
                .Where(command => !documented.Contains(command.Name))
                .OrderBy(command => command.Name, StringComparer.Ordinal)
                .Select(command => command.Name)];

            foreach (string name in missing)
            {
                output.WriteLine($"undocumented: {name}");
            }

            output.WriteLine($"{CommandRegistry.CommandList.Count} registered, {documented.Count} documented");

            Assert.True(
                missing.Count == 0,
                $"{missing.Count} command(s) are in Commands.cs but not in {DOCS_FILE_NAME}: " +
                $"{string.Join(", ", missing)}. The registry is what `bamm help` prints, so these are " +
                "undocumented to everyone reading the published docs."
            );
        }

        [SkippableFact]
        public void EveryDocumentedCommand_IsInTheRightTable()
        {
            (string? root, string? reason) = ResolveDocs();

            Skip.If(root is null, reason!);

            IReadOnlyList<IReadOnlyList<string>> tables =
                ParseTables(File.ReadAllText(Path.Combine(root!, DOCS_FILE_NAME)));

            Dictionary<string, CommandType> registered =
                CommandRegistry.CommandList.ToDictionary(command => command.Name, command => command.Type, StringComparer.Ordinal);

            List<string> misplaced = [];

            for (int table = 0; table < tables.Count; table++)
            {
                foreach (string name in tables[table])
                {
                    if (!registered.TryGetValue(name, out CommandType type))
                    {
                        continue;
                    }

                    int expected = ExpectedTable(type);

                    if (expected != table)
                    {
                        misplaced.Add($"{name} is CommandType.{type} (table {expected}) but documented in table {table}");
                    }
                }
            }

            foreach (string entry in misplaced)
            {
                output.WriteLine(entry);
            }

            Assert.True(
                misplaced.Count == 0,
                string.Join("; ", misplaced) + ". A command in the wrong table reads as if it were " +
                "invoked a different way than it is."
            );
        }

        /// <summary>
        /// A documented command BAMM does not have is documentation drift in the other direction.
        /// </summary>
        /// <remarks>
        /// Reported, never failed. Two such rows exist today — <c>backup</c> and <c>--force-error</c> —
        /// and they predate this suite. They are documentation bugs for the docs repository to fix, and
        /// making the build fail on them would mean a BAMM contributor cannot test without a docs fix.
        /// </remarks>
        [SkippableFact]
        public void EveryDocumentedCommand_ExistsInTheRegistry()
        {
            (string? root, string? reason) = ResolveDocs();

            Skip.If(root is null, reason!);

            IReadOnlyList<IReadOnlyList<string>> tables =
                ParseTables(File.ReadAllText(Path.Combine(root!, DOCS_FILE_NAME)));

            HashSet<string> registered = [.. CommandRegistry.CommandList.Select(command => command.Name)];

            List<string> phantom = [.. tables
                .SelectMany(table => table)
                .Where(name => !registered.Contains(name))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)];

            foreach (string name in phantom)
            {
                output.WriteLine($"documented but not in Commands.cs: {name}");
            }

            // Asserted as a count of zero via a pass, not a failure: see the remarks.
            output.WriteLine(phantom.Count == 0
                ? "No phantom documentation rows."
                : $"{phantom.Count} row(s) document a command BAMM does not have. This is drift in the " +
                  "documentation repository, not a defect in BAMM, so it is reported rather than failed.");
        }

        [Fact]
        public void EveryRegisteredCommand_HasAUsableDescription()
        {
            // Not a documentation test: `bamm help` prints this text, so an empty description is a user
            // -facing defect in BAMM itself. One entry has one today.
            List<string> blank = [.. CommandRegistry.CommandList
                .Where(command => string.IsNullOrWhiteSpace(command.Description))
                .Select(command => command.Name)
                .OrderBy(name => name, StringComparer.Ordinal)];

            foreach (string name in blank)
            {
                output.WriteLine($"empty description: {name}");
            }

            Assert.True(
                blank.Count == 0,
                $"{string.Join(", ", blank)} have no description, so `bamm help <command>` prints nothing " +
                "useful for them."
            );
        }

        [Fact]
        public void EveryRegisteredCommand_HasAnExampleThatNamesIt()
        {
            // The example is the second thing `bamm help` prints, and the first thing a user copies. An
            // example that names a different command is worse than none: it is confidently wrong. One is
            // wrong today — the `-n` alias for `new` carries `-o`'s example.
            List<string> mismatched = [];

            foreach (Command command in CommandRegistry.CommandList)
            {
                // A feature is invoked through the `feature` command, so its example names that token
                // and not the feature. Checking it against the registry name would report every feature
                // in the product as wrong.
                if (command.Type == CommandType.Feature)
                {
                    continue;
                }

                foreach (string example in command.Examples)
                {
                    string invoked = FirstInvokedWord(example);

                    if (invoked.Length == 0)
                    {
                        continue;
                    }

                    // A CLI argument is its own token, so `bamm --gui` is correct for `--gui`.
                    if (invoked == command.Name || command.Type == CommandType.Argument)
                    {
                        continue;
                    }

                    // An alias is invoked by the command it stands for: `-n` is the alias for `new`, so
                    // its example correctly says `new`. What would be wrong is an example naming some
                    // third command, which is the case caught below.
                    if (command.Type == CommandType.Alias)
                    {
                        List<string> aliases = [.. AliasesFor(invoked, out _)];

                        if (aliases.Contains(command.Name, StringComparer.Ordinal))
                        {
                            continue;
                        }
                    }

                    mismatched.Add($"{command.Name} has the example '{example}', which invokes '{invoked}' instead");
                }
            }

            foreach (string entry in mismatched)
            {
                output.WriteLine(entry);
            }

            Assert.True(
                mismatched.Count == 0,
                string.Join("; ", mismatched) + ". An example that invokes a different command teaches " +
                "the wrong thing."
            );
        }

        /// <summary>
        /// The aliases registered for a command name, by matching the alias description back to the
        /// command it names.
        /// </summary>
        /// <remarks>
        /// The registry records an alias's target in prose — "Alias to the 'new' command." — rather
        /// than as a field, so the association has to be read from there. Returns an empty sequence for
        /// anything that is not an alias, and the caller only asks about names that appeared as the
        /// invoked word in an example.
        /// </remarks>
        private static IReadOnlyList<string> AliasesFor(string commandName, out bool isAlias)
        {
            isAlias = false;

            if (!CommandRegistry.CommandList.Any(candidate => candidate.Name == commandName))
            {
                return [];
            }

            List<string> aliases = [.. CommandRegistry.CommandList
                .Where(command => command.Type == CommandType.Alias
                    && command.Description.Contains($"'{commandName}' command", StringComparison.Ordinal))
                .Select(command => command.Name)];

            isAlias = aliases.Count > 0;

            return aliases;
        }

        /// <summary>
        /// The first bare word of a command line, with the leading "bamm" removed.
        /// </summary>
        private static string FirstInvokedWord(string example)
        {
            string[] words = example.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
            {
                return "";
            }

            int start = words[0].EndsWith("bamm", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

            return start < words.Length ? words[start].Trim('"', '\'') : "";
        }
    }
}