# BAMM v1.0.0-Alpha5

## **Notes**

- There are 8 releases below aswell as the raw source code (source.zip).

---

## Changelog

### General Changes:

- Added namespace `BrowserStack` in namespace `BrowserAutomationMaster.Managers.BrowserStack`
- Added `BAMConfig` class in namespace `BrowserAutomationMaster.Compilation`
- Added `BrowserFunctions` class in namespace `BrowserAutomationMaster.Compilation`
- Added `PlatformManager` class in namespace `BrowserAutomationMaster.Managers`
- Added `ProgramFunctions` class in namespace `BrowserAutomationMaster`
- Added `Script`, `ScriptBody`, `ScriptImports`, `ScriptRequirements` classes in namespace `BrowserAutomationMaster.Compilation`
- Added `BrowserVersionManager`, `DeviceManager` and `InstanceManager` inside namespace `BrowserAutomationMaster.Managers.BrowserStack`
- Added BrowserStack related functions to class `BrowserAutomationMaster.Managers.DirectoryManager`
- Refactored `BrowserAutomationMaster.Managers.PackageManager` to use externally hosted JSON, instead of embedded json.
- Refactored `BrowserAutomationMaster.Compilation.Transpiler` to use the newly created classes
  - `Script`
  - `ScriptBody`
  - `ScriptImports`
  - `ScriptRequirements`

### New Action Commands:

### New Feature Command:

### New CLI Arguments:

- `bamm backup` - Backs up all application files to `BAMM-Backup.zip`.

---

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A5-x64-Setup.exe`: For modern **64-bit** Windows systems. This is the most common version.
- `BAMM-v1.0.0A5-ARM64-Setup.exe`: For Windows devices running on **ARM** processors (like newer Microsoft Surface Pro models).

---

### macOS 🍎

- `bamm`: For all **Intel mac** users on MacOS 11.0+
- `bamm-silicon`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A5.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS.
- `bamm.v1.0.0A5.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Macs.
- `bamm.v1.0.0A5.linux-x64.deb`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes.
- `bamm.v1.0.0A5.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs.
