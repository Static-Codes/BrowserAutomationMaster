# BAMM v1.0.0-Alpha5

## **Notes**

- There are 10 releases below aswell as the raw source code (source.zip).

---

## Changelog

- Added support for:
  <br>
  - ChromeOS <img style="height: 14px; width: 14px;" src="https://www.google.com/chrome/static/images/chrome-logo.svg">
  - Raspberry Pi <img style="height: 14px; width: 14px;" src="https://upload.wikimedia.org/wikipedia/en/c/cb/Raspberry_Pi_Logo.svg">
<details>
  <summary> Click to see direct source changes </summary>

### General Changes:

- Added namespace `BrowserAutomationMaster.Managers.BrowserStack`

- Added namespace `BrowserAutomationMaster.Managers.LocalServer`

- Added `BAMConfig` class in namespace `BrowserAutomationMaster.Compilation`

- Added `BrowserFunctions` class in namespace `BrowserAutomationMaster.Compilation`

- Added `BrowserVersionManager`, `DeviceManager` and `InstanceManager` inside namespace `BrowserAutomationMaster.Managers.BrowserStack`

- Added class `RegexManager` in namespace `BrowserAutomationMaster.Managers` and moved all Regex functions to this new class.

- Added check in class `ProgramFunctions` to prevent the GUI server from starting if $DISPLAY is not set

- Added functions to class `DirectoryManager` in namespace `BrowserAutomationMaster`

  - `GetProjectRequirementsPath`
  - `GetProjectVEnvPath`
  - `GetProjectVEnvPythonPath`
  - `GetProjectVEnvPipPath`

- Added `MemoryInfo` struct in class `MemoryInfoManager` within namespace `BrowserAutomationMaster.Managers` + Refactored `MemoryInfoManager`

- Added `PlatformManager` class in namespace `BrowserAutomationMaster.Managers`

- Added `ProcessFactory` class in namespace `BrowserAutomationMaster.Managers`

- Added `ProcessFactory.ProcessResponse` struct in namespace class `BrowserAutomationMaster.Managers`

- Added `ProgramFunctions` class in namespace `BrowserAutomationMaster`

- Added `RPICheck` function in `Linux` class in namespace `BrowserAutomationMaster.Managers.AppManager.OS`

- Added BrowserStack related functions to class `BrowserAutomationMaster.Managers.DirectoryManager`


- Fixed a Windows-Specific bug causing black text to be outputted on a black terminal backgrounds, when multiple instances of are opened.  
  - Expected behavior is now red text on a black background.

- Improved Browser command parsing within `Parser` class in namespace `BrowserAutomationMaster.Managers.Parsing`

- Improved Physical CPU Core count logic in class `CPUCoreManager` in namespace `BrowserAutomationMaster.Managers`

- Improved Python version detection in class `InstallationCheck` in namespace `BrowserAutomationMaster.Helpers`

- Refactored all `== null` conditions to `is null` to utilize pattern matching, a feature that has been available since C# 7.

- Refactored `PackageManager` class in namespace `BrowserAutomationMaster.Managers` to use externally hosted JSON, instead of embedded json.

- Refactored `Parser.HandleLineValidation` function in namespace `BrowserAutomationMaster.Compilation` by splitting it into `Parser.LineValidation`

- Refactored `Transpiler` class in namespace `BrowserAutomationMaster.Compilation` to use the newly created classes

  - `Script`
  - `ScriptBody`
  - `ScriptImports`
  - `ScriptRequirements`

- Refactored `Transpiler` class in namespace `BrowserAutomationMaster.Managers` to use private modifiers on the majority of functions that were previously declared as public.

- Refactored `UninstallationManager` class in namespace `BrowserAutomationMaster.Managers` to properly execute the uninstallation process on Linux (MacOS still has to manually uninstall)

</details>

### New Action Commands:

### New Feature Command:

### New CLI Arguments:

- `bamm --bs` - Instructs BAMM to use [BrowserStack](https://browserstack.com) to run the compiled scripts, works on all platforms except ChromeOS, as chromebooks are too underpowered to run selenium.
- `bamm --editbsconf` - Edit and Overwrite Browserstack's YAML Config via an interactive process. (For advanced users)
- `bamm --force-error` - Forces a verbose error message.
- `bamm --gui` - Starts the Graphical User Interface for BAMM.
- `bamm --nohwc` - Instructs BAMM not to check your system's hardware for compatibility, this should not be done unless you've already verified BAMM can run on your machine.
- `bamm --platform-debug` - Displays information on the operating system and machine currently running BAMM.
- `bamm --query-display` - Displays the status of the $DISPLAY variable, use this to check if your system supports BAMM's GUI.
- `bamm backup` - Backs up all application files to `BAMM-Backup.zip`.



## Releases

---

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A5-x64-Setup.exe`: For modern **64-bit** Windows systems, this is the most common version.
- `BAMM-v1.0.0A5-ARM64-Setup.exe`: For Windows devices running on **ARM64 (ARMv8)** processors, such as newer Surface Laptops.

---

### macOS 🍎

- `bamm`: For all **Intel mac** users on MacOS 11.0+
- `bamm-silicon`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A5.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A5.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on `M-Series Macs` or newer `Surface Laptops`.
- `bamm.v1.0.0A5-linux-arm.deb` : For **32-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on older chromebooks.

- `bamm.v1.0.0A5.linux-x64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A5.linux-arm64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs or newer Surface Laptops.
- `bamm.v1.0.0A5.linux-arm.rpm`: For **32-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on older chromebooks.

