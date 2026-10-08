using System.Diagnostics;
using System.IO;

namespace BrowserAutomationMaster.Tests.ScriptExecution
{
    /// <summary>
    /// Spawns the real BAMM CLI as a child process and reports how it exited. <br/>
    /// Source: BrowserAutomationMaster/UserScriptUtility.cs, ProgramFunctions.cs
    /// </summary>
    /// <remarks>
    /// This deliberately does not use <c>ProcessFactory.SpawnProcess</c> or <c>ScriptValidator</c>:
    /// both call <c>WriteAndExit</c> on failure, which would terminate the test host.
    /// </remarks>
    public static class BammProcess
    {
        private const int DEFAULT_TIMEOUT_SECONDS = 90;
        private const string TIMEOUT_ENVIRONMENT_VARIABLE = "BAMM_TEST_TIMEOUT_SECONDS";
        private const string EXECUTABLE_ENVIRONMENT_VARIABLE = "BAMM_EXE";
        private const string APP_NAME = "BrowserAutomationMaster";

        /// <summary>Why <see cref="TryResolve"/> failed, reported in the suite's skip reason.</summary>
        public static string ResolutionFailureReason { get; private set; } = string.Empty;

        /// <summary>
        /// The maximum time a single child is allowed to run, overridable via
        /// <c>BAMM_TEST_TIMEOUT_SECONDS</c>.
        /// </summary>
        public static TimeSpan Timeout { get; } = ResolveTimeout();

        /// <summary>
        /// Resolves the process start info used to launch BAMM. <br/>
        /// Probes, in order: the <c>BAMM_EXE</c> environment variable, <c>bamm.dll</c> launched
        /// through <c>dotnet exec</c>, then the apphost next to the test assembly.
        /// </summary>
        /// <returns>False when no launcher could be resolved, so the suite skips rather than fails.</returns>
        public static bool TryResolve(out ProcessStartInfo seed)
        {
            seed = new ProcessStartInfo();
            ResolutionFailureReason = string.Empty;

            string testOutputDirectory = AppContext.BaseDirectory;

            string? configured = Environment.GetEnvironmentVariable(EXECUTABLE_ENVIRONMENT_VARIABLE);
            if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            {
                seed.FileName = configured;
                return true;
            }

            string managedAssembly = Path.Combine(testOutputDirectory, "bamm.dll");
            string runtimeConfig = Path.Combine(testOutputDirectory, "BrowserAutomationMaster.Tests.runtimeconfig.json");
            string depsFile = Path.Combine(testOutputDirectory, "BrowserAutomationMaster.Tests.deps.json");

            // The main project sets SelfContained=true. Two consequences force `dotnet exec` to be
            // preferred over the apphost:
            //   1. bamm.runtimeconfig.json next to the test assembly is rewritten by the SDK to the
            //      framework-dependent "includedFrameworks" form, so the self-contained apphost
            //      cannot start there (exit code 131, "libhostpolicy.so ... was not found").
            //   2. bamm's own bamm.runtimeconfig.json / bamm.deps.json describe a self-contained
            //      layout that `dotnet exec` cannot satisfy, so the test project's
            //      framework-dependent pair must be passed explicitly.
            if (File.Exists(managedAssembly) && File.Exists(runtimeConfig) && File.Exists(depsFile))
            {
                seed.FileName = "dotnet";
                seed.ArgumentList.Add("exec");
                seed.ArgumentList.Add("--runtimeconfig");
                seed.ArgumentList.Add(runtimeConfig);
                seed.ArgumentList.Add("--depsfile");
                seed.ArgumentList.Add(depsFile);
                seed.ArgumentList.Add(managedAssembly);
                return true;
            }

            // Fallback: the apphost, used verbatim. Its process name is "bamm", so the single
            // instance check would pass even without --allow-multiple-instances.
            string apphost = Path.Combine(testOutputDirectory, OperatingSystem.IsWindows() ? "bamm.exe" : "bamm");
            if (File.Exists(apphost))
            {
                seed.FileName = apphost;
                return true;
            }

            ResolutionFailureReason =
                $"No BAMM launcher was found. Neither the {EXECUTABLE_ENVIRONMENT_VARIABLE} environment " +
                $"variable, '{managedAssembly}' (with its test project runtimeconfig/deps files), nor " +
                $"'{apphost}' exist under '{testOutputDirectory}'.";

            return false;
        }

        /// <summary>
        /// Compiles <paramref name="scriptPath"/> with the real BAMM CLI and returns how it exited.
        /// </summary>
        /// <remarks>
        /// The child is given a throwaway AppData directory, and the script is staged inside that
        /// directory's <c>userScripts</c> folder. This is required because <c>UserScriptUtility</c>
        /// resolves the compile argument to <c>Path.Combine(userScriptsDirectory, fileName)</c> and
        /// exits 1 when that resolved file is missing, so a script from anywhere else cannot compile.
        /// Isolating AppData keeps the developer's real userScripts/ and compiled/ directories untouched.
        /// </remarks>
        public static async Task<ScriptExecutionResult> Run(string scriptPath, TimeSpan timeout)
        {
            if (!TryResolve(out ProcessStartInfo seed))
            {
                return new ScriptExecutionResult(ScriptExecutionOutcome.Failed, -1, string.Empty, string.Empty)
                {
                    CommandLine = ResolutionFailureReason
                };
            }

            string tempRoot = Path.Combine(Path.GetTempPath(), "bamm-script-execution", Path.GetRandomFileName());

            // The child's AppData is redirected so nothing is written to the real
            // ~/.config/BrowserAutomationMaster. See GetAppDataLinux/GetAppDataMacOS/GetAppDataWindows
            // in DirectoryManager for how each platform resolves this location.
            string childAppData = ResolveChildAppData(tempRoot);
            string stagedScriptsDirectory = Path.Combine(childAppData, "userScripts");
            string stagedScriptPath = Path.Combine(stagedScriptsDirectory, Path.GetFileName(scriptPath));

            Directory.CreateDirectory(stagedScriptsDirectory);
            File.Copy(scriptPath, stagedScriptPath, overwrite: true);

            ProcessStartInfo startInfo = new()
            {
                FileName = seed.FileName,
                WorkingDirectory = tempRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };

            foreach (string argument in seed.ArgumentList)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // "compile" and the script path must occupy pArgs[0] and pArgs[1]: HandleCLIArguments
            // reads only those two positions to dispatch to UserScriptUtility. The flags are
            // matched with pArgs.Any(...) further down, so their position does not matter.
            startInfo.ArgumentList.Add("compile");
            startInfo.ArgumentList.Add(stagedScriptPath);
            startInfo.ArgumentList.Add("--allow-multiple-instances");
            startInfo.ArgumentList.Add("--nohwc");

            if (OperatingSystem.IsWindows())
            {
                startInfo.Environment["APPDATA"] = tempRoot;
            }
            else if (OperatingSystem.IsMacOS())
            {
                // Environment.SpecialFolder.UserProfile resolves from $HOME on macOS.
                startInfo.Environment["HOME"] = tempRoot;
            }
            else
            {
                startInfo.Environment["XDG_CONFIG_HOME"] = tempRoot;
            }

            try
            {
                using Process process = new() { StartInfo = startInfo };

                if (!process.Start())
                {
                    return new ScriptExecutionResult(ScriptExecutionOutcome.Failed, -1, string.Empty,
                        $"Failed to start '{startInfo.FileName}'.")
                    {
                        CommandLine = Describe(startInfo)
                    };
                }

                // Every BAMM error path funnels through Errors.WriteAndExit -> ANSI.ReadKey(), which
                // blocks on console input. Closing stdin makes ReadKey throw instead of hang, so a
                // genuine failure dies quickly with a non-zero code rather than being misreported as
                // a timeout.
                process.StandardInput.Close();

                Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
                Task<string> stderrTask = process.StandardError.ReadToEndAsync();

                using CancellationTokenSource cts = new(timeout);

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                }
                catch (OperationCanceledException)
                {
                    KillQuietly(process);
                    return new ScriptExecutionResult(ScriptExecutionOutcome.TimedOut, -1, string.Empty,
                        $"The child did not exit within the {timeout.TotalSeconds:0.#}s budget.")
                    {
                        CommandLine = Describe(startInfo)
                    };
                }

                string stdout = await stdoutTask;
                string stderr = await stderrTask;

                ScriptExecutionOutcome outcome = process.ExitCode == 0
                    ? ScriptExecutionOutcome.Compiled
                    : ScriptExecutionOutcome.Failed;

                return new ScriptExecutionResult(outcome, process.ExitCode, stdout, stderr)
                {
                    CommandLine = Describe(startInfo),
                    CompiledFiles = CollectCompiledFiles(childAppData)
                };
            }
            finally
            {
                // Deleting the temp root keeps repeated runs from accumulating staged scripts and
                // compiled output.
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        /// <summary>
        /// Mirrors the per-platform AppData resolution in <c>DirectoryManager</c>, relative to the
        /// throwaway root that the child is given.
        /// </summary>
        private static string ResolveChildAppData(string tempRoot)
        {
            if (OperatingSystem.IsMacOS())
            {
                // ~/Library/Application Support/BrowserAutomationMaster
                return Path.Combine(tempRoot, "Library", "Application Support", APP_NAME);
            }

            // %APPDATA%\BrowserAutomationMaster on Windows, $XDG_CONFIG_HOME/BrowserAutomationMaster
            // on Linux and ChromeOS.
            return Path.Combine(tempRoot, APP_NAME);
        }

        private static string[] CollectCompiledFiles(string appData)
        {
            string compiledDirectory = Path.Combine(appData, "compiled");

            try
            {
                if (!Directory.Exists(compiledDirectory))
                {
                    return [];
                }

                return [.. Directory.EnumerateFiles(compiledDirectory, "*.py", SearchOption.AllDirectories)];
            }
            catch
            {
                return [];
            }
        }

        private static void KillQuietly(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception)
            {
                // The process may have exited between the check and the kill; nothing to report.
            }
        }

        private static string Describe(ProcessStartInfo startInfo)
        {
            return $"{startInfo.FileName} {string.Join(' ', startInfo.ArgumentList)}";
        }

        private static TimeSpan ResolveTimeout()
        {
            string? configured = Environment.GetEnvironmentVariable(TIMEOUT_ENVIRONMENT_VARIABLE);

            if (int.TryParse(configured, out int seconds) && seconds > 0)
            {
                return TimeSpan.FromSeconds(seconds);
            }

            return TimeSpan.FromSeconds(DEFAULT_TIMEOUT_SECONDS);
        }
    }
}
