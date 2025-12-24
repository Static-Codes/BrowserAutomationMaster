# BAMM v1.0.0-Alpha6

## **Notes**

- There are 10 binaries below aswell as the raw source code (source.zip).

---

## Changelog

<details>
  <summary> Click to see direct source changes </summary>

### General Changes:

- Added members `New` and `Open` to enum `MenuOption` in class `Menu` in namespace `BrowserAutomationMaster.Messaging`
- Added new class `EditorManager` in namespace `BrowserAutomationMaster.Managers`
- Added new class `Editor` in namespace `BrowserAutomationMaster.Managers`
- Added new class `ExtensionManager` in namespace `BrowserAutomationMaster.Managers`
- Added `Path` variable to class `AppInfo` in namespace `BrowserAutomationMaster.Managers.AppManager`
- Added `GetExecutablePath` to class `Linux` in namespace `BrowserAutomationMaster.Managers.AppManager`
- Deleted unused class `AppDataHelper` in namespace `BrowserAutomationMaster.Helpers`
- Fixed a bug in class `LineValidation` in namespace `BrowserAutomationMaster.Parsing` causing false positive syntax errors.
- Fixed a bug in class `UserScriptManager` in namespace `BrowserAutomationMaster.Managers` causing `bamm compile '<filename>'` to fail.
- Fixed a bug in class `Transpiler` in namespace `BrowserAutomationMaster.Compilation` causing `bamm compile '<filename>'` to fail. 

- Moved function `New` from class `Parser` in namespace `BrowserAutomationMaster.Parsing` to class `Menu` in namespace `BrowserAutomationMaster.Messaging`
- Patched a problematic bug causing unnecessary output in `LineValidation` in namespace `BrowserAutomationMaster.Parsing`
- Refactored classes `Linux`, `Mac`, and `Win` in namespace `BrowserAutomationMaster.Managers.AppManager.OS` to properly include required Path attribute.

</details>

### New Action Commands:

### New Feature Command:

### New CLI Arguments:

Creates a new .BAMC file in the `userScripts` directory.
Prompts the user to select a custom text editor to open the specified file.
If a custom text editor is not found, the OS default is used.
`bamm new 'filename.bamc'` 
`bamm -n 'filename.bamc'`

Opens a .BAMC file that exists in the `userScripts` directory.
If this file does not exist, the file is automatically created.
The user is then prompted to select a custom text editor to open the specified file.
If a custom text editor is not found, the OS default is used.
`bamm open 'filename.bamc'` 
`bamm -o 'filename.bamc'` 




## Releases

---

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A6-x64-Setup.exe`: For modern **64-bit** Windows systems, this is the most common version.
- `BAMM-v1.0.0A6-ARM64-Setup.exe`: For Windows devices running on **ARM64 (ARMv8)** processors, such as newer Surface Laptops.

---

### macOS 🍎

- `bamm`: For all **Intel mac** users on MacOS 11.0+
- `bamm-silicon`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A6.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A6.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on `M-Series Macs`, newer `Surface Laptops`, and `Raspberry Pi`.
- `bamm.v1.0.0A6-linux-arm.deb` : For **32-bit Debian-based** Linux distributions, running on older chromebooks or other armv7 device, tested on crouton using Debian 12.

- `bamm.v1.0.0A6.linux-x64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A6.linux-arm64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs or newer Surface Laptops.
- `bamm.v1.0.0A6.linux-arm.rpm`: For **32-bit Fedora-based** Linux distributions, running on older chromebooks or other ARMv7 device, tested on crouton using Fedora Server 43.

