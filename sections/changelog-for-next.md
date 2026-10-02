## Changelog

`405d277` → `43e8ab7` (branch `canary`) · 200 commits · 172 files changed, 11,433 insertions(+), 31,152 deletions(-)

<details>
  <summary> Click here to view a summary of changes </summary>

## Overview

After many months of inactivity, BAMM v1.0.0A8 is ready for release!

---

## New Features


### Added a File Dialog for file input


<details>
  <summary> Click here for a detailed summary (For developers) </summary>

- Ported [NativeFileDialogSharp](https://github.com/milleniumbug/NativeFileDialogSharp) from NET6.0 to NET10.0, then embedded the native runtimes on all supported platforms — excluding ARM32 / ARMv7 (ARMel / ARMhf)

  - Windows
    - `x64` -> `nfd.dll`

  - macOS
    - `x64` (Intel Macs) -> `libnfd.dylib`
    - `arm64` (M-Series Macs) -> `libnfd.dylib` (This is experimental, expect bugs.)

  - Linux
    - `x64` -> `libnfd.so`
    - `arm64` -> `libnfd.so`

- Replaced `usingNFD` flag with `NFDIsCallable()`. Now unsupported architectures fail gracefully instead of throwing an exception.

- Added `Loader.cs` which extracts the embedded runtime for the appropriate platform then writes this a temp file on disk.

- Added new `NativeResolver` which points the P/Invoke at `"nfd"` so the bindings resolve it.

- Added a reusable class `LibraryUtility` and abstraction layer `FileDialogHelper` which is responsible for loading the loading logic.

</details>


### Commands

- Renamed `**platform-debug**` to **`--platform-info`** (new debug/diagnostics command, originally `--platform-debug`; renamed for clarity): prints platform, distribution, architecture, and Native File Dialog status.

- Added **`--allow-multiple-instances`**: bypasses the single instance guard, allowing BAMM to run alongside another instance. Intended for automated/headless invocation and unit test execution.

- Reworked backend for the **`use-mobile-user-agent`** feature command. `UserAgentManager` was split into `UserAgentHelper`,`UserAgentUtility`, `Types.UserAgent`.

### GUI Improvements (User and Developer)

  <summary> Click to see a summary of changes impacting users </summary>

  <details>
  
  - **11% memory reduction:** GUI startup memory dropped from **236MB → 210MB** (measured with a minimal Linux Firefox install).

  - **No browser required:** The GUI no longer requires a browser to be installed to start.
  
  </details>

  <br>

  <summary> Click to see a summary of changes impacting developers </summary>
  
  <details>
  
  - **Embedded GUI:** `gui.zip` is now downloaded at *build* time from the `gui` branch via a new `RetrieveAndEmbedResources` MSBuild target and embedded as a resource, replacing the runtime `DownloadGUI()` call.

  - **Versioned GUI:** the GUI now ships from its own repository ([BAMM-GUI](https://github.com/Static-Codes/BAMM-GUI)) on its own release cadence, so `RetrieveAndEmbedResources` pulls it from `releases/latest/download/gui.zip` instead of the `gui` branch. The `gui` branch is no longer a build input.
    - Added a **`/gui_version`** endpoint to the GUI's `HttpListener`, reporting the version of the GUI this build serves. The value is read out of the embedded archive at runtime rather than hardcoded, so it cannot drift from the GUI that was actually embedded. The GUI can compare it against its own `GUI_VERSION` const in `scripts/version.js`.

  - **Server split:** `LocalServerManager.cs` was broken up from 943 LoC into `Server.cs`, `BackendFunctions.cs`, and `Response.cs` under the new `Core.GUI` namespace; `LocalServerManager` was renamed `GUIServer`.

  - **Improved Endpoint Handling** Added `StopExecution()`, `EndpointFunctions.Terminate()`, a version endpoint, and associated error handling.

  - Added `ScreenHelper` to query display/screen information.
  
  - Added `gui.zip` to `.gitignore`
  
  - Added a target `RemoveGUIAfterEmbed` which handles removal of the embedded `gui.zip` artifact.
  
  </details>

### Improved Linux Distribution Support (User)

<summary> For more information on changes to Linux support, click here </summary>

<details>
- **Added experimental ALT Linux support:** 
  - There's currently no alt linux packaging, downloading the raw binary for your retrospective platform should work!

- **Added experimental PCLinuxOS support** 
  - Added `PCLinuxOSBuild` which generates the RPM `.spec` file. This processes will prompt you to install `rpm-build`, `rpm-tools`, and `pkgutils` on your machine, then create a tarbell in the PCLinuxOS specific layout.

- **Display Server detection:** 

  - Added `DisplayServer` enum
  
  - Added amore robust detection routine, which is also now exposed through `--query-display`.

  - Moved Linux related types to `Core/Types/Linux/`.

  - Added `PackageTypeExtension` to manage the `PackageType` enum.

- **Distribution detection now comes from [WhichDistroSharp](https://www.nuget.org/packages/WhichDistroSharp/) 1.0.1:**

  - Replaced the hand-rolled `/etc/os-release` parsing with the library's, and replaced the hand-maintained package manager table with the data the library ships.
  - `Distros.cs` now stores only what is unique to BAMM. A display name, dependency lists and installation type. Each entry names the upstream distros it covers.

  - **Raspberry Pi OS, openSUSE Leap and openSUSE Tumbleweed no longer prompt for their base distro.**
    - Their real ID values, `raspbian`, `opensuse-leap` and `opensuse-tumbleweed`, matched no entry before, so **those hosts were asked to pick a base on every run**.
    - `install.sh` has always treated Raspbian as Debian family.

  - **The prompt now names what was detected**, instead of dropping the user into an unexplained list. `Detected: <name> <version> (ID: <id>)` followed by the reason it is unsupported.

  - **Removed the `lsb_release` and `neofetch` subprocess spawns.**
    - `GetFullDistroName()` spawned `lsb_release -a`, and failing that ran `neofetch` into a temp file and stripped the ANSI back out, purely to print a distro name in a debug line.
    - The same data is now read from the one detection path. The trade off is that a host with no `os-release` at all reports "Generic Linux" instead of trying `neofetch`.

  - `PlatformInfo.CurrentPlatform` holds the detected os-release fields, so diagnostics read them instead of re-parsing the file.
  - `--platform-info` gained a `Detected Distro` line, and `--show-distro` gained the detected name, ID and version.

  - **The `1.0.0` release of WhichDistroSharp could not be used as published.**
    - Two of it's archive samples, Bodhi and Vanilla OS, declare `ID=ubuntu` because they are derivatives, and the generated map deduplicated keys first wins over entries sorted by name.
    - `ubuntu` therefore resolved to `Bodhi`, and **`IsUbuntu()` was false on Ubuntu**.
    - BAMM consumes `1.0.1`, where a sample is only mapped when it's ID matches it's own directory name.

- **Identity comparisons that compared display names:**
  - `IsKali()` matched `Name == "Kali Linux"`, and the PCLinuxOS packaging guard matched `Name != "PCLinuxOS"`.
  - Both now compare against the registered entry, which survives a rename.

</details>

### Test Suite (Developer)

<summary> For more information on changes to the test suite, click here </summary>

<details>

- **Script execution tests:** a new `ScriptExecution` suite compiles every discoverable `.bamc` script by running the real BAMM CLI as a child process with `compile <script>.bamc`, and judges each run by its exit code. Scripts come from the real `userScripts` directory, falling back to the repository's `examples/`. Each child gets a throwaway AppData directory, so nothing is written to the developer's real `userScripts/` or `compiled/` directories. Tagged `[Trait("Category", "E2E")]` and excluded from the default run; use `dotnet test --filter "Category=E2E"`.
  - `VisitUrlPreflight` probes every `visit` and `open-new-tab` target using the same reachability rules as the `Transpiler`, so an environmental network failure is reported as a skip rather than a failure. A clean exit passes, a failure alongside an unreachable URL skips and names the URL, anything else fails.
  - Overridable with the `BAMM_EXE` environment variable, and `BAMM_TEST_TIMEOUT_SECONDS` for the per-script budget.

- **Command registry tests:** `CommandRegistryTests` pins the registration of `--allow-multiple-instances` in `Commands.CommandList`, so a refactor cannot silently drop a command and leave `bamm help <argument>` and the documentation out of sync with the binary.

- **GUI version tests:** `GuiVersionTests` covers the `/gui_version` lookup, including that the archive is actually embedded and that the `GUI_VERSION` regex ignores commented-out assignments.

- **The Linux installer is documented:**
  - `sections/installation.md` now states the architectures and package families BAMM is published for, and links to the coverage record for what is not installable.
  - It also says plainly that the installer does not handle Arch, Gentoo or BSD packages, rather than leaving a user to find that out from a failure.
  - `install.sh` carries the reason it's ordered the way it is, since that ordering is load bearing and otherwise invisible.

- **Terminal probe tests:** `TerminalProbeTests` asserts that `GetTerminalBackgroundColor()` is skipped when stdin is redirected, guarding against the `/dev/tty` corruption described under Bug Fixes.

- **Compilation validation tests:** `CompilationValidationTests` covers the defects above that are reachable without spawning the CLI.
  - Checks that `Transpiler`'s accepted command set matches `Parser.HandleLineValidation()`, that every shipped example passes the parser, that multiple `feature` commands are accepted while a late one is still rejected, and that trailing comments no longer break a command line.
  - Reads `Transpiler.validCommands` through a new `InternalsVisibleTo`, rather than widening that field to public for the sake of a test.

- **Distro mapping tests:** `DistroMappingTests` covers the move of the package manager table into WhichDistroSharp.
  - `ForwardedPlatformValuesAreUnchanged` pins all 15 entries to the literals that were in `Distros.cs` before the move, so a refactor that changes what BAMM executes fails the suite instead of **failing on a user's machine**.
  - The rest covers the mapping itself, the invariants that keep it unambiguous, and that `PackageManagerInfo.Unknown` carries empty commands rather than the string `"unknown"`, which would be spawned as a process.

- **Package artifact format tests:** `PackageArtifactFormatTests` covers `PackageArtifactFormats`, which is not a translation of `PackageType`. Not what a distro's package manager consumes, but what BAMM publishes.
  - PCLinuxOS reports `PackageType.Rpm` with an `apt` manager, and Arch and Gentoo publish `pkg.tar.xz` and `tbz2`, so a mapping derived from either would be wrong.
  - `EveryPackageTypeHasAnEntry` guards an omitted entry, which otherwise reports a supported distribution as unsupported.

- **Distro package map tests:** `DistroPackageMapTests` covers the `package-formats.json` the Linux installer reads.
  - `EveryLibraryDistroAppearsInTheMap` and `EveryEntryIsPresentEvenWhenBammShipsNoPackage` guard a key going missing, which makes the installer report a supported distribution as unsupported.
  - A `null` has to be recorded rather than the key omitted, since absence is indistinguishable from an incomplete map.
  - The rest cover the 110 keys, the pinned schema, and that `null` values survive serialisation.

- **Upstream coverage tests:** `UpstreamCoverageTests` makes the distributions BAMM has no support entry for an explicit contract, rather than an accident of what `Distros.cs` declares.
  - WhichDistroSharp knows 110 distributions and the 15 support entries claim 17 of them. The other 93 resolve to `null`, so `DistroManager` asks the user to pick a base **on every run**.
  - The suite compares that gap against `sections/known-distribution-gaps.md` in both directions, so a new WhichDistroSharp release cannot add distributions that pass unnoticed, and adding support cannot leave the list stale.
  - `DistroMappingTests` covers the reverse direction and the uniqueness of the mapping, but nothing noticed a distribution being left unclaimed.

- **Linux installer tests:** `test-install.sh` covers `install.sh`, which is `curl | bash` and cannot be reached by the xunit suite.
  - It extracts the lookup functions from the script and exercises them against a committed snapshot of the generated map, with no network and no `sudo`.
  - "The map agrees with the built in list" is what makes it safe to drop the hardcoded lists in a later change, since it proves every distribution those lists name resolves identically from the map.
  - It also asserts that every helper is defined before it's first used, which is what broke the guards described under Bug Fixes.
  - The download command line is checked for the stale package failure above. Running the script needs a package manager and root, so that one is asserted against the source instead.

- **Test output is now contained:** see the `xunit.runner.json` and `ConsoleCapture` entries under Bug Fixes.

- **CI runs again:**
  - `dotnet.yml` triggered on `branches: ["main"]` for both push and pull request. `main` is not a branch in this repository, it's was renamed to `stable`.
  - GitHub resolves a missing ref to whatever it last pointed at instead of failing, so **the trigger stopped firing without ever reporting a problem**.
  - The last successful run was 2026-01-16, so nothing committed since, including the suites above, was built or tested automatically.
  - It now triggers on `canary` and `stable`.

</details>

<!-- - **`src/Tests/CommandList.txt`:** New canonical dump of all actions and feature commands, generated for the upcoming GUI refactor. -->

### Publisher / Build (Developer)

<summary> For more information on building/publishing related changes, click here </summary>

<details>
  - **.NET SDK bootstrap:** Added around 500 LoC to `DotnetHelper` which will download and install the .NET SDK if it's missing during the build.
  
  - Added field `PythonVar` to `Distro`, with `python` as a default for PCLinuxOS, and `python3` for all other distros.
  
  - **PCLinuxOS packaging:** 
    
    - Added `Build/BuildInfo.cs`, `BaseVersion`, `VersionIdentifier` split.
  
    - Added `Build/Processes/PCLinuxOSBuild.cs`
    
    - Moved `ArchBuild` to `Build/Processes/`

</details>


### Hardware Detection Changes (Developer)

<summary> For more information on hardware detection related changes, click here </summary>

<details>

- Added `Types.HardwareInformation` wrapping **Hardware.Info** v101.1.1.1 (by @Jinjinov), replacing the previous `CpuInfoSharp` binding to PyTorch's `cpuinfo` C library.

- `Win.GetPhysicalCoreCount()` moved to `ProcessorInfo.GetPhysicalCoreCountWindows()`.

</details>

### BrowserStack (Developer)

<summary> For more information on BrowserStack related changes, click here </summary>

<details>

- **Removed `browserstack.json` and `DeviceManager`**
  - This change removed 24,000+ LoC between the JSON file and the old `DeviceManager`. The data was converted to a typed object at `Core/Python/BrowserStack/Devices.cs`, and now fits within around 800 LoC!

- **Browser version updates:** Edge v143 / v144 / v145-beta (v142-beta removed); Chrome v143 stable, v144, v145-beta on Windows and macOS; Firefox v145 / v146 / v147 / v148-beta; Safari 26.0 → 26.2 on macOS Tahoe.

- **Android 16 (Baklava)** support was re-introduced.
- The BrowserStack SDK is now downloaded at runtime if it's not already installed in the active Virtual Environment, removing the file dependency entirely.

- Renamed `InstanceManager` to `Instance`.

</details>

### Settings (Developers)

<summary> For more information on BAMM settings related changes, click here </summary>

<details>

- Renamed `config.ini` to `settings.ini`.

- Refactored `Settings` into `AppSettingsUtility`, `Types.AppSettings`, `Types.SettingOverrideResult`, and `Helpers.StringHelper`.

- Added a `ToPascalCase` extension and a dedicated `ByteArrayExtension` in `Core.Extensions`.

- Refactored `PyPiPackageManager` to use a `PyPiPackage` class instead of `packages.json`, then removed `packages.json`.

</details>

---

## Technical Changes

### Reorganization

The vast majority of commits in this release (86/184) are purely related to renaming, and reference cleanups.

- **`Managers` → `Core`:** the root namespace was renamed and reorganized into layers: 
  ```
  Core
    Common
    Compilation
    Extensions
    GUI
    Helpers
    Messaging
    Parsing
    Python
    SystemInfo
    Types
    Utilities
  ```  

- **Resource relocation:** 
  - Moved `AppData/` to `Resources/`. 
  - Moved `userScripts/` to `Resources/`.

- **Constants:** 
  - Embedded resource paths were moved to `Core.Common.Constants`.
  - Renamed `ConstantManager` → `Constants`.

### API Changes

> All types are in the root namespace `BrowserAutomationMaster` unless stated. Every entry below was verified against the source at both `405d277` and `43e8ab7`.

- `ProcessManager.CheckForMultipleInstances` gained an optional `bool allowMultipleInstances = false` parameter. The default preserves existing behavior, so no call site breaks.

- `Transpiler.validCommands` was added as `internal` (visible to the test project only), as the list of commands the compilation pass accepts.

- `LineValidationHelpers` gained optional `bool stripped = false` parameters, to repair a merge conflict regression (#15):
  - `IsArgQuoted(string arg)` → `IsArgQuoted(string arg, bool stripped = false)`
  - `ValidateTwoArgCommand(..., bool[]? optionalChecks = null)` → `ValidateTwoArgCommand(..., bool[]? optionalChecks = null, bool stripped = false)`

  `stripped` is true when the arguments were split on `' "'`, which consumes each opening quote and leaves only the trailing one. Without it, `add-header` and other special-cased commands failed their own quote check.

- `IsResolvableLink` gained an optional `bool disableSSL = false` parameter, set by `feature "disable-ssl"`.

- `CompilationHandler.OpenNewTab` gained an optional `bool disableSSL = false` parameter, forwarded to `IsResolvableLink`.

- `RequestManager.NetworkClient.GetClientWithRedirectsAllowed` gained an optional `bool disableSSL = false` parameter, which installs a permissive certificate validation callback.

In `Core.Types.Linux.Distro`: `ParseXDGSessionType` and `ParseDesktopSession` gained a required string parameter passed from `GetActiveDisplayServer()`. Previously, both parsing functions ignored the data inside their respective conditional, and would each make an unnecessary call to `Environment.GetEnvironmentVariable()`.

#### The `Distro` type

`Distro` no longer declares the platform's package manager. It composes the `IDistroPackageInfo` that WhichDistroSharp reports and forwards the properties, so the data has one owner.

- **Added** `Distro.PackageInfo` (`IDistroPackageInfo`), `Distro.SupportedDistros` (`WhichDistro[]`, the upstream distributions this entry answers for), and `Distro.Is(Distro?)` for reference-identity comparison.

- **Removed** the constructor parameters `ID`, `BaseDistro`, `PackageManager`, `InstallCommand`, `UninstallCommand`, `QueryCommand`, `QueryArguments`, `PackageType` and `InstallationKeyword`; the corresponding properties remain and now forward to `PackageInfo`.

- **Removed** the `Distro.ID`, `Distro.ReleaseFilePath` and `Distro.ReleaseIdentifier` members. `ID` was read only by the old string lookup, and the other two were never read at all.

- `Distro.BaseDistro` is now `WhichDistroSharp.DistroFamily` rather than the deleted `DistroBase` enum. The `DistroBase` enum is **deleted**; `DistroFamily` replaces it, with `Fedora` renamed to `RHEL` and `ArchLinux` to `Arch`.

- `Distro.PackageType` is now `WhichDistroSharp.PackageType`. The local `Core/Types/Linux/PackageType` enum and the local `PackageTypeExtension` class are both **deleted**; `GetPackageFileType()` now comes from the library.

- **Deleted** `DirectoryManager.GetTemporaryNeofetchPath()`, the four dead `RegexManager` patterns `PrecompiledLSBRRegex`, `PrecompiledNFRegex`, `PrecompiledOSRNameRegex` and `PrecompiledOSRPrettyNameRegex`, and the private `Functions` parsers `ParseOSRelease`, `ParseLSBRelease`, `ParseNeofetch` and `StripANSI`.

#### Namespaces

| Before | After |
| --- | --- |
| `BrowserAutomationMaster.Managers` | `BrowserAutomationMaster.Core` |
| `Managers.Common` | `Core.Common` |
| `Managers.Utilities` | `Core.Utilities` |
| `Managers.Helpers` | `Core.Helpers` |
| `Managers.Types` | `Core.Types` |
| `Managers.GUI` | `Core.GUI` |
| `Managers.Compilation` | `Core.Compilation` |
| `Managers.Messaging` | `Core.Messaging` |
| `Managers.Parsing` | `Core.Parsing` |
| `Managers.Python` / `Managers.Python.BrowserStack` | `Core.Python` / `Core.Python.BrowserStack` |
| `Managers.OS` (also briefly `Core.OS`) | `Core.SystemInfo.OS` |
| `Managers.SystemInfo.CPU` | `Core.SystemInfo.CPU` |
| — *(new)* | `Core.Extensions`, `Core.SystemInfo.OS.Generic`, `Core.SystemInfo.OS.Unix`, `Core.SystemInfo.OS.Unix.Linux`, `Core.SystemInfo.RAM`, `Core.Types.Linux`, `BrowserAutomationMaster.Resources.NativeFileDialog` |

#### Classes, structs, and enums

| Data type | Before | After |
| --- | --- | --- |
| `static class` | `Managers.AnsiManager` | `Core.Common.ANSI` |
| `static class` | `Managers.CommandManager` | `Core.Common.Commands` |
| `class` (+ `partial class Config`, `partial class ConfigParser`) | `Managers.ConfigManager` | `Core.Utilities.AppSettingsUtility` (+ `Core.Types.AppSettings`, `Core.Types.SettingOverrideResult`) |
| `class` | `Managers.ArchiveManager` | `Core.Helpers.ArchiveHelper` |
| `class` | `Managers.EmbeddedResourceManager` | `Core.Helpers.EmbeddedResourceHelper` |
| `internal class` | `Managers.Python.BrowserStack.DeviceManager` | `Core.Python.BrowserStack.Devices` |
| `class` | `Managers.Python.BrowserStack.InstanceManager` | `Core.Python.BrowserStack.Instance` |
| `class` | `Managers.CPUInfoManager` + `Managers.CPUCoreManager` | `Core.SystemInfo.CPU.ProcessorInfo` |
| `enum` | `Managers.RequiredCPUInstruction` | `Core.SystemInfo.CPU.RequiredInstructions` |
| `struct` + `class` | `Managers.MemoryInfoManager` (held `struct MemoryInfo`) | `Core.SystemInfo.RAM.MemoryMonitor` + `Core.SystemInfo.RAM.MemoryInfo` (`readonly struct`) |
| `class` | `Managers.Python.RuntimeManager` | `Core.Python.Runtime` |
| `internal class` | `Managers.Python.VEnvManager` | `Core.Python.VirtualEnvironment` |
| `partial class` | `Managers.Python.PyPiPackageManager` | `Core.Python.PyPi` (+ `partial class PyPiPackage`, `static class PyPiPackageExtensions`) |
| `static class` | `Managers.Python.ScriptValidationManager` | `Core.Python.ScriptValidator` (+ `readonly struct ValidationResult`) |
| `class` | `Managers.UninstallationManager` | `Core.Utilities.AppRemovalUtility` |
| `class` | `Managers.UpdateManager` | `Core.Utilities.AppUpdateUtility` |
| `static class` | — *(new)* | `Core.Types.Linux.PackageArtifactFormats` |
| `static class` | — *(new)* | `Core.Types.Linux.DistroPackageMap` (+ `Core.Types.Linux.PackageFormatDocument`) |
| `class` | `Managers.EditorManager` | `Core.Utilities.EditorUtility` (+ `Core.Types.Editor` and its subclasses) |
| `class` | `Managers.ExtensionManager` (+ `ExtensionHelper`) | `Core.Utilities.ExtensionUtility` |
| `class` | `Managers.UserScriptManager` | `Core.Utilities.UserScriptUtility` |
| `class` | `Managers.UserAgentManager` | `Core.Utilities.UserAgentUtility` (+ `static class Core.Helpers.UserAgentHelper`, `Core.Types.UserAgent`) |
| `class` | `Managers.InternalPlatforms` | `Core.Types.PlatformInfo` (held by `Core.Types.UserInfo`) |
| — *(new)* | — | `Core.Types.HardwareInformation`, `Core.Types.UserInfo` |
| `class` (+ nested `class Theme`) | `Managers.ThemeManager` | `Core.Utilities.ThemeUtility` (+ `Core.Types.Theme`) |
| `partial class` | `Managers.UnixFilePermissionManager` | `Core.SystemInfo.OS.Unix.UnixFilePermissions` |
| `enum` | `Managers.ApplicationNames` | `Core.Types.AppNames` |
| `partial class` | `Managers.Installations` | `Core.Types.Installations` |
| `class` | `Managers.PlatformManager` | `Core.Common.PlatformManager` |
| `class` | `Managers.AppManager.AppInfo` | `Core.SystemInfo.OS.Generic.AppInfo` |
| `static class` | `Managers.AppManager.InstalledApps` | `Core.SystemInfo.OS.Generic.InstalledApps` |
| `class` | `Managers.AppManager.OS.Linux.DistroManager` | `Core.SystemInfo.OS.Unix.Linux.DistroManager` |
| `static class` | `Managers.AppManager.OS.Linux.Distro` | `Core.SystemInfo.OS.Unix.Linux.Distros` |
| `enum` | `Managers.PackageType` / `Managers.InstallationType` | `Core.Types.Linux.PackageType` / `Core.Types.Linux.InstallationType` |
| — *(new)* | — | `Core.Types.Linux.Distro` (`class`), `Core.Types.Linux.DistroBase` (`class`), `Core.Types.Linux.DisplayServer` (`enum`) |
| `class` | `Managers.ConstantManager` | `Core.Common.Constants` |
| `class` | `Managers.LocalServerManager` | `Core.GUI.Server` + `Core.GUI.BackendFunctions` + `Core.GUI.Response` |

#### Members

| Data type | Before | After |
| --- | --- | --- |
| `static method` | `LineValidationHelpers.IsArgQuoted(string)` | `LineValidationHelpers.IsArgQuoted(string, bool stripped = false)` |
| `static method` | `LineValidationHelpers.ValidateTwoArgCommand(..., bool[]? optionalChecks = null)` | `LineValidationHelpers.ValidateTwoArgCommand(..., bool[]? optionalChecks = null, bool stripped = false)` |
| `static property` | `AppSettingsUtility.GlobalConfig` | `AppSettingsUtility.GlobalSettings` |
| `static method` | `MemoryInfoManager.RunCheck` | `MemoryMonitor.GetMemoryInfoAsync` |
| `static method` | `Win.GetPhysicalCoreCount` | `ProcessorInfo.GetPhysicalCoreCountWindows` |
| `method` | `ArchBuild.Sha512SumsAndStream` | `ArchBuild.GetSha512SumsAndStream` |
| `static method` | — *(new)* | `Transpiler.GetGlobalPythonVersion` |
| `extension method` | — *(new)* | `StringExtensions.ToPascalCase` (`Core.Helpers`), `ByteArrayExtension` (`Core.Extensions`) |
| `static method` | — *(new)* | `Loader.NFDIsCallable`, `Loader.InitializeNativeFileDialog`, `LibraryUtility` load helpers |
| `static method` | — *(new)* | `Core.GUI.Server.StopExecution`, `Core.GUI.BackendFunctions.Terminate` |
| `readonly struct` | `ScriptValidationManager.PythonValidationResult` | `ScriptValidator.ValidationResult` |
| `static field` | `BuildInfo.AppName` (`"bamm"`, the published binary) | `BuildInfo.BinaryName` |
| `property` | `Distro.PackageManager` (`"apt-get"`, the command that drives the package manager) | `Distro.PackageManagerCommand` |

- `BuildInfo.AppName` was renamed rather than merely qualified because it collided with the unrelated `DirectoryManager.AppName` (`"BrowserAutomationMaster"`, the settings folder). Files importing both through `using static` could not tell them apart, and using the wrong one produces a package named after the settings folder, or a settings folder named after the binary. The value is unchanged.

- `Distro.PackageManager` was renamed to `Distro.PackageManagerCommand` for the same reason, and to match the library property it forwards. It holds the command a user types to drive the package manager, `"apt-get"`, while `IDistroPackageInfo.PackageManager` is the package manager as a concept, an enum member such as `Apt`. The two often share a value and diverge where the concept and its command have different names, so two members named `PackageManager` holding different types invited reading one as the other.

- `PackageArtifactFormats` answers a different question from `PackageType`: not what format a distro's package manager consumes, but what BAMM actually publishes. PCLinuxOS reports `PackageType.Rpm` with an `apt` package manager, and Arch and Gentoo are published as `pkg.tar.xz` and `tbz2`, so neither the package type nor the package manager alone yields the right answer. The table is an allowlist of the four formats the Publisher can build, not a translation of every `PackageType`; `GetArtifactExtension` throws on a member with no entry at all, so a library upgrade that adds one fails loudly rather than reporting a supported distribution as unsupported.

- `DistroPackageMap` emits every distribution the library knows, including the ones BAMM publishes no package for, which carry a null format. Omitting those keys would be indistinguishable from an incomplete map, and the installer reports the two cases differently. The Publisher writes the file next to the built artifacts on every release, and `install.sh` fetches it under the same release tag as the package it installs, so a script can never pair itself with a stale table.

### Cleanups

- Alphabetized functions, messages, error strings, and `NativeMethods.txt`.

- Removed unused imports across the codebase, plus dead code: `colors.json`, `BrowserVersionManager`, `useragents.json` logic, stray `libnfd.so`, `LibraryInfo`, and the CpuInfoSharp runtime folder (~800KB of binaries).

- Re-introduced `RunOnCompile`; furthered memory-leak prevention in `ProcessManager` and `VirtualEnvironment`.

- Replaced the `====`-delimited comment banners in `Core/Python/BrowserStack/Devices.cs` with `#region` directives, so its structure is navigable in the IDE.

- Extracted the HTML instruction string out of `BackendFunctions.Redirect()` into `GetRedirectInstructions()`, leaving `Redirect()` responsible only for writing the response.

- Corrected the GUI extraction message, which still described the archive as downloaded at runtime from the `gui` branch. It reports the version read out of the embedded archive.


---

## 🐛 Bug Fixes

- **Python 3.13 and 3.14 could not be selected:**
  - `HandlePythonVersionSelection` filled a fixed `new string[6]`, while it's version mapping lists eight entries.
  - Installing six or more interpreters dropped the last two, so 3.13 and 3.14 could never be chosen.
  - The list is now built from the mapping, which keeps the count from drifting again.
  - The routine also passed that raw array to the selection prompt, so a user with two versions installed saw **four blank options** alongside them. The prompt now receives only the versions that were found.

- **A test that only failed on a developer's machine:**
  - `TerminalProbeTests` asserted it's own precondition with `Assert.True(Console.IsInputRedirected, ...)`.
  - This holds under CI and in a non-interactive shell, and is false for anyone running `dotnet test` from an interactive terminal, where the tty is inherited. The suite was **green in automation and red on a workstation**.
  - It now uses `SkippableFact` with `Skip.IfNot`, which is how the rest of this suite already handles an unmet precondition.

- **The apt-cache refresh never ran:**
  - `RefreshDebianAptCache()` compared the `Distro` object against a `DistroBase` value, so it was always false and `apt-get update` never executed.
  - This includes the Arch and PCLinuxOS packaging paths, which call it specifically because a dependency is missing from the local cache.
  - `object.Equals` accepts anything and returns false when the runtime types differ, so this **compiled cleanly and read correctly**. Renaming the enum alone would not have fixed it.
  - It now reads `BaseDistro`, like every other family comparison in the codebase.

- **Unknown commands silently ignored:** fixed `bamm compile` accepting a command it does not recognise and emitting nothing for it, producing a script that quietly does less than it appears to. `Parser.HandleLineValidation()` has always rejected these, but the compilation pass re-implements parsing rather than calling the parser, so it never saw them. A command check now runs before the emit switch; `CompilationValidationTests` asserts the accepted set stays in step with the parser's. The switch itself could not do this: most of its cases are guarded by a `when` clause that matches only on failure, so a *successful* command falls straight through and is indistinguishable from an unrecognised one.

- **The Linux installer could install a package left over from a failed run:**
  - `wget` derives the file name from the URL, and saves to `<name>.1` when the target already exists rather than overwriting.
  - The temporary directory is only removed on success, so an interrupted or failed run leaves a package behind and the next run installed **that one instead of the new release**.
  - The download now uses `wget -O`, which forces the name and truncates.

- **The Linux installer's error guards did nothing:**
  - `show_error_and_exit` is called three times near the top of `install.sh` and defined near the bottom. Bash resolves a function when it is called and not when the file is read, so each call reported `command not found` and **the script carried on**.
  - A macOS user was told nothing and the script went on to install Linux packages, a host with no `os-release` was never reported as unsupported, and neither was a host with an unparseable `ID`.
  - The functions and constants are now defined before first use.
  - This predates the WhichDistroSharp work, and is verified by running the script with a stubbed `uname`.

- **`install.sh` did not recognise Oracle Linux:**
  - The script matched the ID `oracle`, but Oracle Linux's real ID is `ol`, so it fell through to "not currently supported" despite this changelog claiming Oracle support.
  - The ID extraction only worked by accident. `sed 's/ID=[ ]*//'` leaves the quotes in `ID="ubuntu"`, and it was unanchored, so `ID_LIKE` could match instead.
  - Both are fixed, the read now falls back to `/usr/lib/os-release`, and the distro lists cover the entries BAMM supports.

- **The Linux installer only supported twelve distributions:**
  - `install.sh` chose the package format by matching ID against two hardcoded lists of six names each.
  - Everything else was reported as unsupported, including Deepin, Devuan, Amazon Linux and the openSUSE releases, which BAMM ships a working `.deb` or `.rpm` for.
  - It now reads a package format map generated from WhichDistroSharp and published with each release, covering the 110 distributions the library knows, 79 of which resolve to a package BAMM publishes.
  - It falls back to the old lists if that download fails, so an offline or rate-limited fetch never blocks an install.

- **Unsupported-distribution messages pointed at the wrong problem:**
  - A user on a distribution BAMM ships no package for was told it was "not currently supported", which is not actionable.
  - A distribution the library recognizes but has no package manager for is now reported separately from one that is not recognized, and a distribution BAMM has no installable build for is named as such.
  - Arch now reports that BAMM does not yet publish an installable `pkg.tar.xz` package, rather than claiming Arch is unknown.

- **CI tested a different repository than the one under review:**
  - `dotnet.yml` checked out the triggering commit, then immediately ran a second `git clone` into `$HOME` and built that copy instead.
  - The clone is of the default branch, so a pull request was validated against unreviewed code and **it's own changes were never tested**.
  - The clone is removed and the steps now build the tree `actions/checkout` produced, addressed to the solution file.
  - It's trigger also named `main`, which is not a branch in this repository. See the Improved section.

- **`disable-ssl` ignored during compilation:** `feature "disable-ssl"` was applied to the generated script but not to the compile-time URL check, so a self-signed host failed to compile even though the script would have loaded it. The reachability probe now accepts any certificate when the feature is set, mirroring the script's own `CERT_NONE` verification mode. This also unblocked `examples/Firefox/no-ssl-example.bamc`, which had never compiled anywhere.

- **`--gui --port==X` ignored:** the port was parsed out of the argument and then discarded, so the listener always bound the default `8008` regardless of what was requested. The parsed port is now forwarded to `StartGUIThread()`, and a value outside 1–65535 is rejected with a clear message instead of being passed to `HttpListener`.

- **Unknown GUI routes returned no response:** requesting a path the `HttpListener` does not serve only logged a warning and wrote nothing, leaving the connection open. A requester saw a network error rather than a diagnosable `404`. An unrecognised route now returns `404` with the reason in the response body.

- **Multiple `feature` commands were impossible:** the first `feature` line closed the feature block, so a second one was reported as misplaced. A script could declare at most one feature. The ordering rule is unchanged; a `feature` line no longer closes its own block.

- **Trailing comments on `visit` lines:** `GetDesiredUrls()` split raw lines, so a `visit` command carrying a comment was not recognized and the script was reported as containing no `visit` commands at all. Comments are now stripped there too, matching `HandleCompilation()`. The command is also matched on its first argument instead of with `Contains("visit")`, which would have matched a feature named `use-visit-proxy`.

- **Bare trailing comments:** `DeleteCommentIfPresent()` only detected `" // "`, so a line ending in `//` with nothing after it was not treated as a comment. A comment is now also recognized when the slashes are preceded by whitespace, which keeps a URL's `https://` intact.

- **`examples/Firefox/no-ssl-example.bamc`:** corrected two errors in the example itself. It used `feature "no-ssl"`, which is not a real feature; and its `feature` lines sat after the `visit`, which the ordering rule forbids. It is now the only example that exercises the feature block at all, and it compiles.

- **Trailing comments:** Fixed `bamm compile` rejecting a script whose commands carried an inline `//` comment. `Transpiler.HandleCompilation()` now strips comments before a line is split into tokens, using `Parser.DeleteCommentIfPresent()` — the same rule `Parser.IsValidFile()` applies — so a script the parser accepts can always be compiled. Previously the comment pushed the line past the valid token counts and the compile failed with `Invalid command syntax.`; `examples/Chrome/marketplace.bamc` and `examples/Firefox/no-ssl-example.bamc` were both affected. JavaScript blocks are excluded, since a `//` inside one is JavaScript syntax.

- **ARM64 crash:** Fixed premature crash on ARM64 caused by ANSI not being able to determine the active platform.

- **Linux installation:** Fixed a critical oversight where a **symlinked** binary would fail to install.

- **PCLinuxOS packaging:**
  - Fixed specification file generation.
  - Fixed an `rpmbuild` single-quote bug.
  - Added a missing file state check in `BuildPCLinuxOSPackage()`
  - Fixed dependency errors that broke package installation.

- **.NET detection:** fixed `DotnetIsInstalled()` logic.

- **Settings:** corrected `Dictionary<string, List<KeyValuePair<string, string>>>` → `Dictionary<string, Dictionary<string, string>>`; added an overlooked null check in `GetExtensionPaths()`; denested `ValidateConfigContents`; fixed a `NameError` from incorrect embedded-string usage.

- **Paths:** fixed path breakages caused by the new directory structure and the macOS `free` binary path.

- **PyPI:** fixed package-installation failures on PCLinuxOS; refined decompression logic.

- **Test output:** fixed the test report becoming unreadable gibberish, twice over.
  - BAMM's validation routines write to the process-global `Console`, and xunit v2 runs one collection per test class in parallel, so concurrent suites interleaved their output character by character. Added `xunit.runner.json` with `parallelizeTestCollections: false`; the suites are fast enough that serializing them costs almost nothing.
  - The inverse tests deliberately feed malformed input to BAMM to prove it is rejected, and those diagnostics then landed in the report padded to the console width, making a passing run look like a wall of errors. `ConsoleCapture` now diverts them into a buffer, so a passing run reports only results.

- **Terminal input corruption:** `GetTerminalBackgroundColor()` no longer probes `/dev/tty` when stdin is not a terminal. The probe writes an OSC 11 query to `/dev/tty` and reads the reply back from `/dev/tty`; because `/dev/tty` is the controlling terminal shared with the parent, a BAMM run with redirected stdin (a pipe, an editor, CI, or a test host) left the reply in the shell's input queue, where the **next command typed at that terminal read it**. It also removes the spurious "An exception occured while determining default theme" error, which was `ForegroundMatch` failing to parse the concatenated replies.

- **GUI:** fixed GUI download logic; `RunOnCompile` restored.

- **Windows:** refined `IsSupportedWindowsVersion()`.


- **Misc:** trailing newline on `fileHeaderStr`, missing NFD library check, `packages.json` file existence check, redundant exception handling removed.

---

## 📦 Removed / Deleted

- `AppData/browserstack.json` — 24,061 lines
- `AppData/useragents.json` — 98 lines
- `AppData/colors.json` — 581 lines
- `AppData/packages.json` — 17 lines
- `Managers/ConfigManager.cs`, `Managers/EditorManager.cs`, `Managers/LocalServerManager.cs`, `Managers/PlatformManager.cs`, `Managers/UserAgentManager.cs`
- `Managers/Python/BrowserStack/BrowserVersionManager.cs`, `DeviceManager.cs`
- `Managers/Python/PyPiPackageManager.cs`
- `Resources/CpuInfoSharp/` (wrappers + all 7 native runtimes)
- `Helpers/InstallationCheck.cs` (215 lines)

---

## 📖 Documentation

- **README:** replaced the flat "Linux (ARM32, ARM64, x64)" line with a detailed per-architecture support matrix (x86_64 / ARM64 / ARM32) listing Alt Linux, Arch, Debian 12+, elementary OS, Fedora 42+, FreeBSD, Linux Mint, PCLinuxOS, Ubuntu 22.04/24.04/25.10, and Zorin OS.

- **CREDITS:** added @milleniumbug for the Native File Dialog bindings.

- `src/Tests/CommandList.txt` added as a living reference for all CLI commands.

</details>
