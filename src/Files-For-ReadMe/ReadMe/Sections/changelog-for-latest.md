# BAMM v1.0.0-Alpha4

## **Notes**

- v1.0.0-Alpha4 is still in development.
- There are 8 releases below aswell as the raw source code (source.zip).

---

## Changelog

### General Changes:

- Added 4 new commands (3 action, 1 feature).
- Added `feature` line validation in `Parsing.Parser.cs`
- Added support for Fedora based Linux Distros (CentOS, Oracle Linux, Qubes, etc)
- Added `Managers.AnsiManager`
- Cleaned up `Parsing.Parser.cs`.
- Created `Managers.AnsiManager`, `Managers.ConfigManager`, and `Managers.ThemeManager`
- Created `BrowserAutomationMaster.Transpiler` namespace
- Increased BAMC JavaScript validation accuracy
- Increased BAMC URL validation accuracy
- Moved code from `Transpiler.cs` to `CompilationHandler.cs`
- Moved `Parsing.Parser.Menu` and `Parsing.Parser.MenuOptions` to `Messaging.Menu`
- Moved `Transpiler.cs` and `CompilationHandler.cs` to `BrowserAutomationMaster.Transpiler`
- Refactored Managers.AppManager.OS.Win.GetPhysicalCoreCount to use CSWin32 for increased stability.
- Refactored `Managers.Python.VEnvManager` and `Managers.UpdateManager` to be asynchronous
- Removed 300+ lines of bloat from Parsing.Transpiler
- Removed redundant OS checks and tightened Windows checks.
- Removed unused features `async` and `bypass-cloudflare`
- Renamed internal class Managers.AppManager.OS.Windows -> Managers.AppManager.OS.Win

### New Action Commands:

- `click-at-position "600" "600"` - Clicks at a specific point on screen
- `close-current-tab` - Closes the currrent tab and will close the browser if there's only one open tab.
- `open-new-tab "https://google.com" "3"` - A new browser tab is opened, the system will then pause for the number of seconds specified, then visits the requested url.

---

### New Feature Command:

- `feature "disable-ssl"` - Disables SSL certificate authentication for the given session.

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A3-x64-Setup.exe`: For modern **64-bit** Windows systems. This is the most common version.
- `BAMM-v1.0.0A3-ARM64-Setup.exe`: For Windows devices running on **ARM** processors (like newer Microsoft Surface Pro models).

---

### macOS 🍎

- `bamm`: For all **Intel mac** users on MacOS 11.0+
- `bamm-silicon`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A3.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS.
- `bamm.v1.0.0A3.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Macs.
- `bamm.v1.0.0A3.linux-x64.deb`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes.
- `bamm.v1.0.0A3.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs.
