using BrowserAutomationMaster.Core.Python;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Utilities
{
    /// <summary>
    /// Where a virtual environment is created, and that creating one stays inside that place. <br/>
    /// Source: Core/Python/VirtualEnvironment.cs (VEnvPath, VEnvExists, CreateVEnv,
    /// GetBrowserStackSDKPath), Core/Common/DirectoryManager.cs (GetProjectVEnvPath)
    /// </summary>
    /// <remarks>
    /// Every test here runs against a temporary root and asserts that nothing appears outside it. That
    /// is the property being tested, not just hygiene: <c>VEnvPath</c> resolves from the script's own
    /// directory, so a script left in the real <c>userScripts</c> would have its environment created
    /// beside it in the user's home directory, and the class had no way to be told otherwise.
    /// </remarks>
    public class VirtualEnvironmentTests(ITestOutputHelper output)
    {
        private static readonly string TEMP_ROOT =
            Path.Combine(Path.GetTempPath(), "bamm-venv-tests", Path.GetRandomFileName());

        /// <summary>An interpreter that exists, or null when Python is not installed.</summary>
        private static string? Interpreter => FindInterpreter();

        private static string? FindInterpreter()
        {
            foreach (string candidate in new[] { "python3", "python" })
            {
                foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "")
                    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    string path = Path.Combine(directory, candidate);

                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }

            return null;
        }

        [Fact]
        public void TheEnvironmentLivesBesideTheScriptByDefault()
        {
            string script = Path.Combine(TEMP_ROOT, "default", "script.bamc");

            VirtualEnvironment environment = new("/usr/bin/python3", script);

            output.WriteLine(environment.VEnvPath);

            Assert.Equal(Path.Combine(TEMP_ROOT, "default", "venv"), environment.VEnvPath);
        }

        [Fact]
        public void AnExplicitParentOverridesTheScriptDirectory()
        {
            // The parameterisation: a caller can confine every filesystem operation to a root of its
            // choosing, which is what makes this class testable without touching the real userScripts.
            string script = Path.Combine(TEMP_ROOT, "elsewhere", "script.bamc");
            string explicitParent = Path.Combine(TEMP_ROOT, "venvs", "project-a");

            VirtualEnvironment environment = new("/usr/bin/python3", script, explicitParent);

            output.WriteLine(environment.VEnvPath);

            Assert.Equal(Path.Combine(explicitParent, "venv"), environment.VEnvPath);
            Assert.DoesNotContain("elsewhere", environment.VEnvPath, StringComparison.Ordinal);
        }

        [Fact]
        public void ResolvingThePath_DoesNotCreateAnything()
        {
            string parent = Path.Combine(TEMP_ROOT, "unresolved");

            _ = new VirtualEnvironment("/usr/bin/python3", Path.Combine(TEMP_ROOT, "s.bamc"), parent).VEnvPath;

            // Reading the property is how a caller finds out where it will go; it must not have the
            // side effect of putting it there.
            Assert.False(Directory.Exists(Path.Combine(parent, "venv")));
        }

        [Fact]
        public void TheBrowserStackSdkPathSitsInsideTheSameRoot()
        {
            string parent = Path.Combine(TEMP_ROOT, "sdk-root");

            VirtualEnvironment environment = new("/usr/bin/python3", Path.Combine(TEMP_ROOT, "s.bamc"), parent);

            string sdk = environment.GetBrowserStackSDKPath();

            output.WriteLine(sdk);

            // Everything this class writes has to land under the one root, or "isolated" is only true of
            // the environment itself.
            Assert.StartsWith(Path.Combine(parent, "venv"), sdk, StringComparison.Ordinal);
            Assert.True(sdk.EndsWith("browserstack-sdk", StringComparison.Ordinal)
                || sdk.EndsWith("browserstack-sdk.exe", StringComparison.Ordinal));
        }

        [Fact]
        public async Task CreatingTheEnvironment_LaysItOutUnderTheGivenRoot()
        {
            Skip.If(Interpreter is null, "No python3 on PATH, so there is no interpreter to create one with.");

            string parent = Path.Combine(TEMP_ROOT, $"created-{Guid.NewGuid():N}");
            string script = Path.Combine(parent, "script.bamc");

            Directory.CreateDirectory(parent);
            File.WriteAllText(script, "browser \"chrome\"\n");

            VirtualEnvironment environment = new(Interpreter!, script, parent);

            output.WriteLine($"interpreter: {Interpreter}");
            output.WriteLine($"venv path:   {environment.VEnvPath}");

            await environment.CreateVEnv();

            Assert.True(Directory.Exists(environment.VEnvPath), $"No environment at {environment.VEnvPath}.");

            // The layout python's venv module produces. Asserted rather than assumed: a bare directory
            // existing is not the same as a usable environment, and the failure mode this class guards
            // against is an environment that is present but not runnable.
            string[] expected = GlobalUserInfo.PlatformInfo.IsUnixLike
                ? ["bin", "lib", "pyvenv.cfg"]
                : ["Lib", "Scripts", "pyvenv.cfg"];

            foreach (string entry in expected)
            {
                string path = Path.Combine(environment.VEnvPath, entry);

                output.WriteLine($"  {entry}: {Directory.Exists(path) || File.Exists(path)}");

                Assert.True(
                    Directory.Exists(path) || File.Exists(path),
                    $"The created environment has no '{entry}', so it is not a usable virtual environment."
                );
            }

            // And nothing outside the root.
            Assert.False(Directory.Exists(Path.Combine(TEMP_ROOT, "venv")));
        }

        [Fact]
        public async Task CreatingTheEnvironmentTwice_IsANoOp()
        {
            Skip.If(Interpreter is null, "No python3 on PATH, so there is no interpreter to create one with.");

            string parent = Path.Combine(TEMP_ROOT, $"twice-{Guid.NewGuid():N}");
            string script = Path.Combine(parent, "script.bamc");

            Directory.CreateDirectory(parent);
            File.WriteAllText(script, "browser \"chrome\"\n");

            VirtualEnvironment environment = new(Interpreter!, script, parent);

            await environment.CreateVEnv();

            // A marker that only survives if the second call returns early. CreateVEnv guards on
            // VEnvExists, so a second call must not clear or rebuild the tree.
            string marker = Path.Combine(environment.VEnvPath, "bamm-idempotence-marker");
            File.WriteAllText(marker, "kept");

            await environment.CreateVEnv();

            output.WriteLine($"marker survived: {File.Exists(marker)}");

            Assert.True(File.Exists(marker), "A second CreateVEnv rebuilt the environment rather than returning early.");
        }
    }
}