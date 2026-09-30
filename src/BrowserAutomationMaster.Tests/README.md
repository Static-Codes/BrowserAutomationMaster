# BrowserAutomationMaster Tests

This XUnit test suite validates all lexical parsing, regex execution, and command syntax logic used in BAMM.

These tests will simulate `.bamc` script parsing against the current source at build-time.

## Tests Structure

* **`Commands`**: Asserts true/false outcomes against expected conditions for all valid BAMM commands. There are also inverse tests, that are intended to fail; these tests will pass if the syntax is deemed invalid.  Includes but is not limited to `browser`, `click`, `add-header`, `fill-text`, `wait-for-seconds`, etc.

* **`Features`**: Validates feature commands used for script configurations in the header of the file. Including, but not limited to `feature "use-http-proxy"`, `feature "disable-ssl"`, `feature "add-extension"`, `feature "disable-pycache"`, etc.

* **`FormatValidation`**: Functionality tests for [`RegexManager`](../BrowserAutomationMaster/Core/Common/RegexManager.cs) targeting URLs, Proxies, User Agents, and Numbers. This is intended to prevent regression in this area of the codebase.

* **`Selectors`**: Verifies that the selector string that were provided correctly map to one of the classes in [`Selector.cs`](../BrowserAutomationMaster/Core/Parsing/Selectors.cs) (XPath, CSS, ID, Class, TagName, etc).

* **`Integration`**: Executes tests on the [`Parsing`](../BrowserAutomationMaster/Core/Parsing/) namespace; these tests mimick the execution of the [BAMC examples](../../examples/) provided in the repository.

* **`PlatformInitializer.cs`**: This module calls `PlatformManager.SetPlatform()` once prior to any test runs. BAMM detects the host platform in `ProgramFunctions.InitializeAsync()` through `PlatformManager.SetPlatform()`. Without initialization, the PlatformInfo flags retain their default value (false); This will cause calls to `GetAppDataDirectory()` within the test suite to throw a `PlatformNotSupportedException`.

* **`ConsoleCapture.cs`**: Keeps BAMM's console output out of the test report. Several suites feed deliberately malformed input to BAMM to prove that it gets rejected, and the validation routines print a full error every time. Without this, a passing run fills the report with those errors. The text is still available on `ConsoleCapture.Captured` for tests that want to assert on it, and per-test diagnostics should go to `ITestOutputHelper` instead.

* **`TerminalProbeTests.cs`**: Guards [`Functions.GetTerminalBackgroundColor()`](../BrowserAutomationMaster/Core/SystemInfo/OS/Unix/Linux/Functions.cs), which asks the terminal for its background color by writing an OSC 11 query to `/dev/tty` and reading the reply from `/dev/tty`. When stdin is redirected, as it is under a test host, a pipe, or an editor, nothing consumes that reply. It then sits in the terminal's input buffer and gets picked up by the next command the user types. The test asserts that the probe is skipped in that situation.

* **`xunit.runner.json`**: Runs test collections one at a time. By default xunit v2 gives each test class its own collection and runs them in parallel, and because every suite here writes to the process-global `Console`, their output interleaves into noise. Serializing costs almost nothing at this suite's size.

* **`ScriptExecution`**: Compiles every `.bamc` script it finds by running the real BAMM CLI as a child process with `compile <script>.bamc`, and judges each run by its exit code. Scripts come from the real `userScripts` directory when it has any, and from the build-time copy of [`examples/`](../../examples/) otherwise. Every child is given a throwaway AppData directory, and the script is staged inside that directory's `userScripts` folder, because [`UserScriptUtility`](../BrowserAutomationMaster/Core/Utilities/UserScriptUtility.cs) only compiles scripts sitting in one. That keeps your real `userScripts/` and `compiled/` directories untouched. Before launching, `VisitUrlPreflight` checks that every `visit` and `open-new-tab` target actually resolves, following the same rules as the `Transpiler`, so a flaky network is reported as a skip rather than a failure. A clean exit passes, a failure alongside an unreachable URL skips and names the URL, and anything else fails with whatever the child printed. Tagged `[Trait("Category", "E2E")]` so that `dotnet test --filter "Category=E2E"` runs just these. Set `BAMM_EXE` to test a different BAMM build, or `BAMM_TEST_TIMEOUT_SECONDS` to change how long each script gets.

## Running the Tests

Ensure you have the [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

```bash
# Navigate to the Unit Tests directory.
cd Path/To/BrowserAutomationMaster.Tests

# Run the test suite.
dotnet test