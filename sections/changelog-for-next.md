## Changelog

`405d277` → `0b9fbb4` (branch `canary`) · 184 commits · 145 files changed, 10,322 insertions(+), 30,999 deletions(-)

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

- Reworked backend for the **`use-mobile-user-agent`** feature command. `UserAgentManager` was split into `UserAgentHelper`,`UserAgentUtility`, `Types.UserAgent`.

### GUI Improvements (User and Developer)

  <summary> Click to see a summary of changes impacting users </summary>

  <details>
    - **11% memory reduction:** GUI startup memory dropped from **236MB → 210MB** (measured with a minimal Linux Firefox install).

    - **No browser required:** The GUI no longer requires a browser to be installed to start.
  </details>

  <summary> Click to see a summary of changes impacting developers </summary>
  
<details>
  
  - **Embedded GUI:** `gui.zip` is now downloaded at *build* time from the `gui` branch via a new `RetrieveAndEmbedResources` MSBuild target and embedded as a resource, replacing the runtime `DownloadGUI()` call.

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

> All types are in the root namespace `BrowserAutomationMaster` unless stated. Every entry below was verified against the source at both `405d277` and `0b9fbb4`.

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
| `static property` | `AppSettingsUtility.GlobalConfig` | `AppSettingsUtility.GlobalSettings` |
| `static method` | `MemoryInfoManager.RunCheck` | `MemoryMonitor.GetMemoryInfoAsync` |
| `static method` | `Win.GetPhysicalCoreCount` | `ProcessorInfo.GetPhysicalCoreCountWindows` |
| `method` | `ArchBuild.Sha512SumsAndStream` | `ArchBuild.GetSha512SumsAndStream` |
| `static method` | — *(new)* | `Transpiler.GetGlobalPythonVersion` |
| `extension method` | — *(new)* | `StringExtensions.ToPascalCase` (`Core.Helpers`), `ByteArrayExtension` (`Core.Extensions`) |
| `static method` | — *(new)* | `Loader.NFDIsCallable`, `Loader.InitializeNativeFileDialog`, `LibraryUtility` load helpers |
| `static method` | — *(new)* | `Core.GUI.Server.StopExecution`, `Core.GUI.BackendFunctions.Terminate` |
| `readonly struct` | `ScriptValidationManager.PythonValidationResult` | `ScriptValidator.ValidationResult` |

### Cleanups

- Alphabetized functions, messages, error strings, and `NativeMethods.txt`.

- Removed unused imports across the codebase, plus dead code: `colors.json`, `BrowserVersionManager`, `useragents.json` logic, stray `libnfd.so`, `LibraryInfo`, and the CpuInfoSharp runtime folder (~800KB of binaries).

- Re-introduced `RunOnCompile`; furthered memory-leak prevention in `ProcessManager` and `VirtualEnvironment`.

---

## 🐛 Bug Fixes

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
