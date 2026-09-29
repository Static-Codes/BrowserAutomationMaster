# BrowserAutomationMaster Tests

This XUnit test suite validates all lexical parsing, regex execution, and command syntax logic used in BAMM.

These tests will simulate `.bamc` script parsing against the current source at build-time.

## Tests Structure

* **`Commands`**: Asserts true/false outcomes against expected conditions for all valid BAMM commands. There are also inverse tests, that are intended to fail; these tests will pass if the syntax is deemed invalid.  Includes but is not limited to `browser`, `click`, `add-header`, `fill-text`, `wait-for-seconds`, etc.

* **`Features`**: Validates feature commands used for script configurations in the header of the file. Including, but not limited to `feature "use-http-proxy"`, `feature "disable-ssl"`, `feature "add-extension"`, `feature "disable-pycache"`, etc.

* **`FormatValidation`**: Functionality tests for [`RegexManager`](../BrowserAutomationMaster/Core/Common/RegexManager.cs) targeting URLs, Proxies, User Agents, and Numbers. This is intended to prevent regression in this area of the codebase.

* **`Selectors`**: Verifies that the selector string that were provided correctly map to one of the classes in [`Selector.cs`](../BrowserAutomationMaster/Core/Parsing/Selectors.cs) (XPath, CSS, ID, Class, TagName, etc).

* **`Integration`**: Executes tests on the [`Parsing`](../BrowserAutomationMaster/Core/Parsing/) namespace; these tests mimick the execution of the [BAMC examples](../../examples/) provided in the repository.

* **`PlatformInitializer.cs`**: This module was added for 5 tests to pass the module calls `PlatformManager.SetPlatform()` once prior to any test runs. BAMM detects the host platform in `ProgramFunctions.InitializeAsync()` via `PlatformManager.SetPlatform()`. Without initialization, the PlatformInfo flags retain their default value (false); This will cause calls to `GetAppDataDirectory()` within the test suite to throw a `PlatformNotSupportedException`.

## Running the Tests

Ensure you have the [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.

```bash
# Navigate to the Unit Tests directory.
cd Path/To/BrowserAutomationMaster.Tests

# Run the test suite.
dotnet test