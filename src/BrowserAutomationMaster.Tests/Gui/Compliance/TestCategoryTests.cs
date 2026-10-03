using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Gui.Compliance
{
    /// <summary>
    /// The CI filter split stays coherent. <br/>
    /// Source: .github/workflows/dotnet.yml, BrowserAutomationMaster.Tests/xunit.runner.json
    /// </summary>
    /// <remarks>
    /// CI runs two jobs: <c>--filter "Category!=Browser"</c> needs no browser, and
    /// <c>--filter "Category=Browser"</c> needs one. That split is only safe while every test is in
    /// exactly one of them. A new test class that forgets its trait lands in the first job, where a
    /// Playwright test skips rather than runs and reports green; a trait spelled wrong does the same.
    /// <para>
    /// This is checked by reflection over the assembly rather than by parsing the workflow, so it fails
    /// when a class is mis-tagged rather than when the workflow drifts. The workflow's own filters are
    /// asserted separately.
    /// </para>
    /// </remarks>
    public class TestCategoryTests(ITestOutputHelper output)
    {
        private const string BROWSER = "Browser";
        private const string SERVER = "Server";

        private static Assembly TestAssembly => typeof(TestCategoryTests).Assembly;

        /// <summary>
        /// Only this suite's test classes.
        /// </summary>
        /// <remarks>
        /// Scoped to the Gui namespaces on purpose. The pre-existing tests carry their own categories —
        /// <c>Category=E2E</c> for the ones that spawn a browser, and none at all for the rest — and
        /// none of them are subject to this split. A rule applied to the whole assembly would report
        /// dozens of unrelated classes as untagged.
        /// </remarks>
        private static IEnumerable<Type> TestClasses()
            => [.. TestAssembly.GetTypes()
                .Where(type => type.IsClass
                    && !type.IsAbstract
                    && type.Namespace is not null
                    && type.Namespace.StartsWith("BrowserAutomationMaster.Tests.Gui.", StringComparison.Ordinal))];

        /// <summary>
        /// The traits on a test class, read from the attribute metadata.
        /// </summary>
        /// <remarks>
        /// Via <c>GetCustomAttributesData</c> rather than <c>GetCustomAttributes&lt;TraitAttribute&gt;</c>
        /// because xUnit 2.9's <c>TraitAttribute</c> exposes only <c>TypeId</c> — the name and value
        /// are constructor arguments with no public properties behind them.
        /// </remarks>
        private static IReadOnlyList<string> TraitsOn(Type type)
        {
            return
            [
                .. type.GetCustomAttributesData()
                    .Where(attribute => attribute.AttributeType == typeof(TraitAttribute))
                    .Select(attribute =>
                        $"{attribute.ConstructorArguments[0].Value}={attribute.ConstructorArguments[1].Value}")
                    .OrderBy(value => value, StringComparer.Ordinal)
            ];
        }

        /// <summary>
        /// Every test method belongs to exactly one of the two CI jobs, or to neither.
        /// </summary>
        [Fact]
        public void EveryTestClass_IsEitherTierAUntaggedTierBOrTierC()
        {
            List<string> problems = [];

            foreach (Type type in TestClasses())
            {
                bool hasTests = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Any(method => method.GetCustomAttributes<FactAttribute>().Any()
                        || method.GetCustomAttributes<TheoryAttribute>().Any());

                if (!hasTests)
                {
                    continue;
                }

                IReadOnlyList<string> traits = TraitsOn(type);

                output.WriteLine($"{type.Name}: {(traits.Count == 0 ? "(untagged)" : string.Join(", ", traits))}");

                // StartsWith, not EndsWith: the endpoint tests live in .Gui.Server.Endpoints and the
                // behaviour tests in .Gui.Client, so neither tier's own name ends the namespace.
                bool tierA = IsUnder(type, ".Gui.Compliance");
                bool tierB = IsUnder(type, ".Gui.Server");
                bool tierC = IsUnder(type, ".Gui.Client");

                // Tier A is untagged on purpose. It has no requirements, so it belongs in both jobs'
                // default sweep, and a trait on it would be one more thing to keep in step.
                if (tierA && traits.Count != 0)
                {
                    problems.Add($"{type.Name}: Tier A should carry no category trait, has {string.Join(", ", traits)}");
                }

                if (!tierA && traits.Count == 0)
                {
                    problems.Add($"{type.Name}: {type.Namespace} needs a category trait or it is excluded from one CI job");
                }

                if (traits.Contains($"Category={SERVER}") && !tierB)
                {
                    problems.Add($"{type.Name}: tagged Server but is not under .Gui.Server");
                }

                if (traits.Contains($"Category={BROWSER}") && !tierC)
                {
                    problems.Add($"{type.Name}: tagged Browser but is not under .Gui.Client");
                }
            }

            Assert.True(problems.Count == 0, string.Join("; ", problems));
        }

        private static bool IsUnder(Type type, string tier)
            => type.Namespace?.Contains(tier, StringComparison.Ordinal) == true;

        /// <summary>
        /// The two CI jobs together run every test, with none in both.
        /// </summary>
        /// <remarks>
        /// The logic the workflow relies on: <c>Category!=Browser</c> and <c>Category=Browser</c> are
        /// complementary over untagged-plus-tagged. If a class ended up tagged <c>Browser</c> and
        /// <c>Server</c> at once it would satisfy neither filter's intent — the server job would drop it
        /// and the browser job would run it without a browser having been installed for a reason.
        /// </remarks>
        [Fact]
        public void NoTestClass_CarriesBothCategories()
        {
            List<string> both = [.. TestClasses()
                .Where(type => TraitsOn(type).Count > 1)
                .Select(type => $"{type.Name}: {string.Join(", ", TraitsOn(type))}")];

            output.WriteLine(string.Join(Environment.NewLine, both));

            Assert.True(both.Count == 0, $"These carry more than one category trait: {string.Join("; ", both)}.");
        }

        /// <summary>
        /// The workflow's filters match the categories this suite declares.
        /// </summary>
        /// <remarks>
        /// Read from the workflow rather than assumed. A renamed trait or a reworded filter would
        /// silently turn one job into a no-op — the build would still be green while running nothing.
        /// </remarks>
        [Fact]
        public void TheWorkflowFilters_MatchTheDeclaredCategories()
        {
            string workflow = File.ReadAllText(FindWorkflow());

            output.WriteLine("dotnet test steps: " + string.Join(" | ",
                Regex.Matches(workflow, @"--filter\s+""(?<filter>[^""]+)""").Select(m => m.Groups["filter"].Value)));

            Assert.Contains("Category!=Browser", workflow, StringComparison.Ordinal);
            Assert.Contains("Category=Browser", workflow, StringComparison.Ordinal);

            // Category=Server is deliberately absent from the workflow: it exists only so the tier can be
            // counted and asserted on, and it lands in the browser-free job by being simply not Browser.
            Assert.DoesNotContain("--filter \"Category=Server\"", workflow, StringComparison.Ordinal);

            // The categories the filters name have to be the ones classes actually carry, or the filters
            // match nothing and both jobs pass having run almost nothing.
            foreach (Type type in TestClasses().Where(type => TraitsOn(type).Count > 0))
            {
                foreach (string trait in TraitsOn(type))
                {
                    Assert.StartsWith("Category=", trait, StringComparison.Ordinal);

                    // Only the two this suite introduces are expected in the workflow's filters. Other
                    // categories in the assembly (Category=E2E among them) are the pre-existing suite's
                    // business and are swept up by Category!=Browser rather than filtered for.
                    Assert.True(
                        trait is $"Category={BROWSER}" or $"Category={SERVER}",
                        $"{type.Name} carries {trait}, which the workflow does not filter on"
                    );
                }
            }
        }

        /// <summary>
        /// Finds the CI workflow by walking up from the test assembly.
        /// </summary>
        private static string FindWorkflow()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory is not null)
            {
                string candidate = Path.Combine(directory.FullName, ".github", "workflows", "dotnet.yml");

                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                $"No .github/workflows/dotnet.yml above '{AppContext.BaseDirectory}'. This test exists to " +
                "keep the workflow's filters in step with the categories declared here."
            );
        }
    }
}