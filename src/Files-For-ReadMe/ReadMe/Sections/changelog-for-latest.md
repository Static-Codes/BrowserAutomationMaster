# BAMM v1.0.0-Alpha4

## **Notes**

- There are 6 releases below aswell as the raw source code.
- `source.zip` contains a 22.9MB archive of the repo, this is with all of the bloat visual studio creates, aswell as published builds, for the pure source code (under 1MB when extracted), please download `BAMM-v1.0.0A2-Source.zip`

---

## Changelog

### General Changes:

- Added 3 new commands
- Cleaned up the Parser class
- Increased BAMC JavaScript validation accuracy
- Increased BAMC URL validation accuracy
- Refactored Managers.AppManager.OS.Win.GetPhysicalCoreCount to use CSWin32 for increased stability.
- Renamed internal class Managers.AppManager.OS.Windows -> Managers.AppManager.OS.Win

### New Commands:

`close-current-tab` - Closes the currrent tab and will close the browser if there's only one open tab.
`open-new-tab "https://google.com" "3"` - A new browser tab is opened, the system will then pause for the number of seconds specified, then visits the requested url.
`click-at-position "600" "600"` - Clicks at a specific point on screen

---

### Windows 💻

There are three versions for Windows. You most likely need the **x64** version.

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
