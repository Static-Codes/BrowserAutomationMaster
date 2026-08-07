# BrowserAutomationMaster Tests

This xUnit test suite validates the lexical parsing, regex evaluations, and command syntax logic of `BrowserAutomationMaster` (BAM). It simulates C# `.bamc` script evaluation directly against the production source.

## Structure

* **`/Commands`**: Asserts true/false outcomes for all valid and invalid BAM syntax (e.g., `browser`, `click`, `fill-text`, `wait-for-seconds`).
* **`/Features`**: Validates top-level script configurations (`--proxy`, `--disable-ssl`, `--add-extension`, `--disable-pycache`).
* **`/FormatValidators`**: Pure function tests for `RegexManager` targeting URLs, Proxies, User-Agents, and Numbers.
* **`/Selectors`**: Verifies the identification of XPath, CSS, ID, and Class string inputs via `Selectors.cs`.
* **`/Integration`**: End-to-end parser tests mimicking the execution of the actual `/examples` (Chrome/Firefox) provided in the repository.
* **`/KnownIssues`**: Contains defensive tests for documented engine bugs to prevent CI pipeline failures while preserving awareness (e.g., the `Console.WindowWidth` crash in headless CI).

## Running the Tests

Ensure you have the .NET 10.0 SDK installed.

```bash
# Navigate to the Tests directory
cd BrowserAutomationMaster.Tests

# Run the suite
dotnet test