## **Notes**

- There are 10 binaries below aswell as the raw source code (source.zip).

---

## Changelog

<details>
  <summary> Click to see direct source changes </summary>

### General Changes:
- Added members `CRXExtensionIDRegexPattern` and `XPIExtensionPathRegexPattern` in class `RegexManager` in namespace `BrowserAutomationMaster.Managers`

- Added member `Extensions` to class `BAMConfig` in namespace `BrowserAutomationMaster.Compilation`

- Added parameter `Extensions` to function `Visit` in class `CompilationHandler` in namespace `BrowserAutomationMaster.Compilation`

- Added temporary helper methods to class `ExtensionManager` in namespace `BrowserAutomationMaster.Managers` while BAMM is ported to .NET 10.

- Added new class `ExtensionManager` in namespace `BrowserAutomationMaster.Managers` responsible for parsing, download, validating, and adding the extension's contents to the current Selenium instance.

- Added new function `GetExtensionsDirectory` in class `DirectoryManager` in namespace `BrowserAutomationMaster.Managers`

- Added new constants for ExtensionManager in class `ConstantManagers` in namespace `BrowserAutomationMaster.Managers`.

- Fixed bug in class `Transpiler` in namespace `BrowserAutomationMaster.Compilation`, now `BAMConfig.CheckConfigLines()` is called in `Transpiler.SetBAMConfig()`.

- Removed member `hasComment` in class `Transpiler` in namespace `BrowserAutomationMaster.Compilation`, now comments are ignored during compilation.

</details>

### New Action Commands:

### New Feature Command:
`feature "add-extension" "url or path to extension"` - Downloads the extension at the specified path, and adds it to the running Selenium instance.

### New CLI Arguments:
`bamm --exit-on-ext-fail` - This flag will cause BAMM to exit if the built in extension validation fails, this can be useful for debugging.

## Releases

---

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A7-x64-Setup.exe`: For modern **64-bit** Windows systems, this is the most common version.
- `BAMM-v1.0.0A7-ARM64-Setup.exe`: For Windows devices running on **ARM64 (ARMv8)** processors, such as newer Surface Laptops.

---

### macOS 🍎

- `BAMM-v1.0.0A7-Mac-Intel.app.zip`: For all **Intel mac** users on MacOS 11.0+
- `BAMM-v1.0.0A7-Mac-Silicon.app.zip`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A7.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A7.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on `M-Series Macs`, newer `Surface Laptops`, and `Raspberry Pi`.
- `bamm.v1.0.0A7-linux-arm.deb` : For **32-bit Debian-based** Linux distributions, running on older chromebooks or other armv7 device, tested on crouton using Debian 12.

- `bamm.v1.0.0A7.linux-x64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A7.linux-arm64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs or newer Surface Laptops.
- `bamm.v1.0.0A7.linux-arm.rpm`: For **32-bit Fedora-based** Linux distributions, running on older chromebooks or other ARMv7 device, tested on crouton using Fedora Server 43.

