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

- Added `BrowserVersionManager`, `DeviceManager` and `InstanceManager` inside namespace `BrowserAutomationMaster.Managers.BrowserStack`

- Added BrowserStack related functions to class `BrowserAutomationMaster.Managers.DirectoryManager`

- Refactored `PackageManager` class in namespace `BrowserAutomationMaster.Managers` to use externally hosted JSON, instead of embedded json.

- Refactored `BrowserAutomationMaster.Compilation.Transpiler` to use the newly created classes
  - `Script`
  - `ScriptBody`
  - `ScriptImports`
  - `ScriptRequirements`
- Refactored `Transpiler` class in namespace `BrowserAutomationMaster.Managers` to use private modifiers on the majority of functions that were previously declared as public.

- Refactored `UninstallationManager` class in namespace `BrowserAutomationMaster.Managers` to properly execute the uninstallation process on Linux (MacOS still has to manually uninstall)

### New Action Commands:

### New Feature Command:

### New CLI Arguments:

- `bamm --bs` - Instructs BAMM to use [BrowserStack](https://browserstack.com) to run the compiled scripts, works on all platforms except ChromeOS, as chromebooks are too underpowered to run selenium.
- `bamm --editbsconf` - Edit and Overwrite Browserstack's YAML Config via an interactive process. (For advanced users)
- `bamm --nohwc` - Instructs BAMM not to check your system's hardware for compatibility, this should not be done unless you've already verified BAMM can run on your machine.
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

- `bamm.v1.0.0A5.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Intel and AMD CPUs from the last 15 years.

- `bamm.v1.0.0A5.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Macs or newer Surface Laptops.

- `bamm.v1.0.0A5.linux-x64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Intel and AMD CPUs from the last 15 years.

- `bamm.v1.0.0A5.linux-arm64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs or newer Surface Laptops.
