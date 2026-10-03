using System.Reflection;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Core.GUI;
using BrowserAutomationMaster.Tests.Gui.Fixtures;
using BrowserAutomationMaster.Tests.Gui.Server;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// Core/GUI's dead members, and that they are still dead. <br/>
    /// Source: Core/GUI/Server.cs, Core/GUI/Response.cs, Core/GUI/BackendFunctions.cs
    /// </summary>
    /// <remarks>
    /// "Full coverage of Core/GUI" can only mean full coverage of reachable code. These members are the
    /// rest: named here so that "this line is never executed" has an explanation, and checked so that
    /// the explanation cannot go stale. Wire one of them up and this fails, which forces a decision —
    /// use it properly, or delete it.
    /// <para>
    /// The check is deliberately one-directional. Asserting a member is <em>not</em> called is not
    /// something a coverage run can prove without running every path; what it can prove is that nothing
    /// in the sources references it, which is what catches "someone wired this up last month".
    /// </para>
    /// </remarks>
    public class UnreachableMemberTests(ITestOutputHelper output)
    {
        /// <summary>
        /// Every C# source under src/, excluding build output.
        /// </summary>
        /// <remarks>
        /// All of src/, not just Core/GUI. The members being checked live in Core/GUI, but a caller could
        /// live anywhere — and the point of the test is to notice someone wiring one up, which is exactly
        /// the kind of change someone would make from their own code rather than from the GUI's.
        /// </remarks>
        private const string TEST_PROJECT_MARKER = "BrowserAutomationMaster.Tests";

        private static readonly string[] ALL_SOURCES = FindSources();

        [Fact]
        public void EveryNamedMember_ExistsWhereTheSuiteSaysItDoes()
        {
            List<string> problems = [];

            foreach (string member in UnreachableMembers.Members.Keys)
            {
                bool found = ALL_SOURCES.Any(source =>
                    File.ReadAllText(source).Contains(member, StringComparison.Ordinal));

                output.WriteLine($"{member}: found in src/={found}");

                if (!found)
                {
                    problems.Add(member);
                }
            }

            Assert.True(
                problems.Count == 0,
                $"{string.Join(", ", problems)} are named as unreachable but no longer appear in Core/GUI. " +
                "Either they were renamed, which breaks the mapping, or deleted, in which case they should " +
                "come out of UnreachableMembers."
            );
        }

        [Fact]
        public void EveryNamedMember_HasAReason()
        {
            List<string> unreasoned = [.. UnreachableMembers.Members
                .Where(entry => string.IsNullOrWhiteSpace(entry.Value))
                .Select(entry => entry.Key)];

            output.WriteLine(string.Join(Environment.NewLine, UnreachableMembers.Members.Select(e => $"{e.Key}: {e.Value}")));

            Assert.True(
                unreasoned.Count == 0,
                $"These are listed as unreachable with no explanation: {string.Join(", ", unreasoned)}. " +
                "An unexplained entry is indistinguishable from an uncovered gap."
            );
        }

        /// <summary>
        /// Every named member is declared exactly once and referenced nowhere else — except by another
        /// member that is itself dead.
        /// </summary>
        /// <remarks>
        /// Word boundaries matter: a plain substring count reports <c>validResponse</c> 27 times, because
        /// it is a substring of <c>HandleValidResponse</c> and <c>invalidResp</c>. <br/>
        /// The exception is <see cref="UnreachableMembers.STOP_EXECUTION"/>, whose only call site is
        /// inside <see cref="UnreachableMembers.TERMINATE"/>. Dead-by-another-hop is still dead, so the
        /// test checks that the extra references fall inside a declared dead member rather than
        /// pretending the count is one.
        /// </remarks>
        [Fact]
        public void EveryNamedMember_IsReferencedOnlyByDeadCode()
        {
            List<string> reachable = [];

            foreach ((string member, _) in UnreachableMembers.Members)
            {
                List<(string File, int Line)> callSites = CallSites(member);

                output.WriteLine($"{member}: {callSites.Count} call site(s)");

                List<string> outside = [.. callSites
                    .Where(site => !InsideADeadMember(site.File, site.Line))
                    .Select(site => $"{Path.GetFileName(site.File)}:{site.Line}")];

                if (outside.Count > 0)
                {
                    reachable.Add($"{member} (from {string.Join(", ", outside)})");
                }
            }

            Assert.True(
                reachable.Count == 0,
                $"{string.Join("; ", reachable)}. These are listed as unreachable but called from live code. " +
                "Either use them properly or remove the entry."
            );
        }

        /// <summary>
        /// The lines of code in Core/GUI that name a member, excluding its own declaration.
        /// </summary>
        private static List<(string File, int Line)> CallSites(string member)
        {
            List<(string, int)> sites = [];

            foreach (string source in ALL_SOURCES)
            {
                string[] lines = File.ReadAllLines(source);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (IsComment(lines[i]) || !ContainsName(lines[i], member))
                    {
                        continue;
                    }

                    // A declaration names the member behind a modifier, with or without an argument
                    // list: a field with an initializer has no parentheses at all, which is how
                    // Response.validResponse was being counted as a call site.
                    bool isDeclaration =
                        Regex.IsMatch(lines[i], @"^\s*(?:public|private|internal|protected)")
                        && !Regex.IsMatch(lines[i], @"\.(?:Stop|Get|Set)\w*\(");

                    if (!isDeclaration)
                    {
                        sites.Add((source, i + 1));
                    }
                }
            }

            return sites;
        }

        /// <summary>
        /// Whether a line falls inside the declaration of a member that is itself listed as dead.
        /// </summary>
        private static bool InsideADeadMember(string file, int line)
        {
            return DeclarationSpans(file)
                .Where(span => UnreachableMembers.Members.Keys.Contains(span.Member))
                .Any(span => line >= span.FirstLine && line <= span.LastLine);
        }

        /// <summary>
        /// Every member declaration in a file, with the line range its braces cover.
        /// </summary>
        /// <remarks>
        /// Found by brace counting from each <c>public|private|internal</c> signature rather than by
        /// parsing C#, because the point is only to know roughly which method a line sits in.
        /// </remarks>
        private static List<(string Member, int FirstLine, int LastLine)> DeclarationSpans(string source)
        {
            List<(string, int, int)> spans = [];
            string[] lines = File.ReadAllLines(source);

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsComment(lines[i]))
                {
                    continue;
                }

                Match signature = Regex.Match(
                    lines[i],
                    @"^\s*(?:public|private|internal|protected)[^=;]*?\b(?<member>[A-Za-z_][A-Za-z0-9_]*)\s*\(");

                if (!signature.Success)
                {
                    continue;
                }

                int depth = 0;
                int opened = -1;

                for (int j = i; j < lines.Length; j++)
                {
                    depth += lines[j].Count(c => c == '{') - lines[j].Count(c => c == '}');

                    if (lines[j].Contains('{'))
                    {
                        opened = j;
                    }

                    if (opened >= 0 && depth == 0)
                    {
                        spans.Add((signature.Groups["member"].Value, opened + 1, j + 1));
                        break;
                    }
                }
            }

            return spans;
        }

        /// <summary>Whether a line names a member as a whole word rather than as part of a longer name.</summary>
        private static bool ContainsName(string line, string member)
        {
            return Regex.IsMatch(
                line,
                $@"\b{Regex.Escape(member)}\b");
        }

        [Fact]
        public void ValidResponse_IsNotRead()
        {
            // Response.validResponse is a prebuilt success object that nothing reads. Asserted on the
            // compiled field rather than on the source, so a rename does not make it vacuous.
            FieldInfo? field = typeof(Response).GetField(nameof(Response.validResponse), BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(field);

            bool read = ALL_SOURCES.Any(source => File.ReadAllLines(source).Any(line =>
                !IsComment(line) && line.Contains("Response.validResponse", StringComparison.Ordinal)));

            output.WriteLine($"Response.validResponse is read by name in src/: {read}");

            Assert.False(read);
        }

        [Fact]
        public void Upload_IsBothObsoleteAndUnrouted()
        {
            MethodInfo? upload = typeof(BackendFunctions).GetMethod(nameof(BackendFunctions.Upload));

            Assert.NotNull(upload);
            Assert.True(
                upload!.GetCustomAttribute<ObsoleteAttribute>() is not null,
                "BackendFunctions.Upload is listed as unreachable because it is retired deliberately. " +
                "The [Obsolete] attribute is half of that claim; the commented-out router case is the other."
            );

            // The router case is commented out, so no non-comment line anywhere in src/ dispatches to it.
            // That is the other half of the claim: [Obsolete] says "retired", the missing case says
            // "unreachable".
            bool routed = ALL_SOURCES.Any(source => File.ReadAllLines(source).Any(line =>
                !IsComment(line) && line.Contains("case \"/upload\"", StringComparison.Ordinal)));

            output.WriteLine($"routed: {routed}");

            Assert.False(routed);
        }

        /// <summary>
        /// The child processes left a coverage report, so the listener's own coverage was recorded.
        /// </summary>
        /// <remarks>
        /// The counterpart to <see cref="EveryNamedMember_IsReferencedOnlyByDeadCode"/>. Without
        /// <c>dotnet-coverage</c> wrapping the child's launch, every line of Core/GUI the listener
        /// executes reports as untested while the tests asserting it pass — <c>coverlet.collector</c>
        /// instruments only the test host, and shadow-loads its rewritten assemblies rather than touching
        /// the files on disk, so the child never loads an instrumented copy. That made "is Core/GUI
        /// covered" unanswerable. <br/>
        /// This asserts the reports exist and name Core/GUI, which is the cheapest proof that the
        /// collector reached the child at all. It does not assert a coverage percentage: line-level
        /// attribution across two producers is unreliable, because cobertura lists an async body on both
        /// the outer class and its state machine, so the per-line figure depends on how the two are
        /// combined. CI merges the reports and publishes the number instead.
        /// </remarks>
        [Fact]
        public void TheChildsCoverageWasCollected()
        {
            if (!ChildCoverage.IsAvailable)
            {
                // Not a failure: dotnet-coverage is an optional tool, and CI is what requires it.
                Skip.If(true, $"dotnet-coverage is not installed. Install it with '{ChildCoverage.INSTALL_HINT}' to record the GUI server's own coverage.");
            }

            string directory = ChildCoverage.OutputDirectory;

            Assert.True(Directory.Exists(directory), $"No coverage directory at '{directory}'.");

            string[] reports = [.. Directory.GetFiles(directory, "*.cobertura.xml")];

            output.WriteLine($"{reports.Length} child report(s) in {directory}");

            Assert.NotEmpty(reports);

            int withCoreGui = 0;

            foreach (string report in reports)
            {
                if (File.ReadAllText(report).Contains("Core/GUI", StringComparison.Ordinal))
                {
                    withCoreGui++;
                }
            }

            output.WriteLine($"{withCoreGui}/{reports.Length} name Core/GUI");

            Assert.True(
                withCoreGui > 0,
                $"None of the {reports.Length} child reports mention Core/GUI, so the collector did not " +
                "instrument the listener and the coverage report describes the test host alone."
            );
        }

        /// <summary>
        /// Finds src/ by walking up from the test assembly.
        /// </summary>
        /// <remarks>
        /// Locating the directory itself rather than the repository root, because the solution file sits
        /// under src/, and assuming a layout above it would break the moment a directory moved. Reading
        /// the tree rather than a copy in the output directory is the whole point: the claim is about
        /// the source, and a stale copy would make the test pass forever. <br/>
        /// obj/ and bin/ are excluded because generated sources carry their own copies of these
        /// declarations, which would double every count.
        /// </remarks>
        private static string[] FindSources()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, "src");

                if (Directory.Exists(Path.Combine(candidate, "BrowserAutomationMaster", "Core", "GUI")))
                {
                    return
                    [
                        .. Directory.GetFiles(candidate, "*.cs", SearchOption.AllDirectories)
                            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))

                            // Test projects name every one of these members, in the declaration lists and
                            // in the assertions themselves. Scanning them would find this file and
                            // conclude all four members are called from live code.
                            .Where(path => !path.Contains(TEST_PROJECT_MARKER, StringComparison.Ordinal))
                    ];
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"No Core/GUI directory above '{AppContext.BaseDirectory}'. The sources have to be read from " +
                "the tree; a copy in the output directory would make these tests pass regardless of the code."
            );
        }

        /// <summary>
        /// Occurrences of a name in src/, ignoring comment lines.
        /// </summary>
        /// <remarks>
        /// Comments are stripped because every one of these members is named in a doc comment explaining
        /// why it is dead. Counting those would make every entry look reachable, which is the opposite
        /// of what this is checking for.
        /// </remarks>
        private static int CountCodeOccurrences(string member)
        {
            return ALL_SOURCES.Sum(source => File.ReadAllLines(source).Count(line =>
                !IsComment(line) && line.Contains(member, StringComparison.Ordinal)));
        }

        private static bool IsComment(string line)
        {
            string trimmed = line.TrimStart();

            return trimmed.StartsWith("//", StringComparison.Ordinal)
                || trimmed.StartsWith("///", StringComparison.Ordinal)
                || trimmed.StartsWith("*", StringComparison.Ordinal);
        }
    }
}
