# BAMM v1.0.0-Alpha2

## BAMM Installation Guide

To install BAMM, please download the correct file for your computer's operating system from the list below.

### **Note**

- `source.zip` contains a 1.5GB archive of the repo, this is with all of the bloat visual studio creates, aswell as published builds, for the pure source code (only 64MB), please download `BAMM-v1.0.0A1-Source.zip`

---

### Windows 💻

There are three versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A1-x64-Setup.exe`: For modern **64-bit** Windows systems. This is the most common version.
- `BAMM-v1.0.0A1-ARM64-Setup.exe`: For Windows devices running on **ARM** processors (e.g., some Microsoft Surface Pro models).
- `BAMM-v1.0.0A1-x86-Setup.exe`: For older **32-bit** Windows systems.

---

### macOS 🍎

- `bamm`: This is the application for all **macOS** users.

---

### Linux 🐧

- `bamm.1.0.0A1.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS.

## Changelog

### General Changes:

- Added all examples in repo to the user's userScript directory.
- Added functionality to trim the size of the generated `.py` file, by ONLY including functions that are required by the `.bamc` file being compiled.
- Fixed bugs caused by adding comments to the script.
- Removed `src\MacCompilationScripts`
- Removed `src\Unused`

### New Commands:

- `bamm clear compiled` - Deletes the compiled directory and all it's contents
- `bamm clear userScripts` - Deletes the userScript directory and all it's contents (this will be rewritten)
- `bamm uninstall` - Uninstalls bamm and all associated data
- `bamm --set-custom-useragent "user-agent"` - This is an extension of the command below for CLI usage, see "CLI Examples" for more information.
- `set-custom-useragent "user-agent"` - Sets a custom user agent for the selenium session, see "BAMC Examples" below.
- `add-header "header-name" "header-value"` - Adds a header for the current request.

## BAMC Examples:

### Windows Firefox

###### `set-custom-useragent` "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"

### iOS Safari

###### - `set-custom-useragent` "Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Mobile/15E148 Safari/604.1"

## CLI Examples:

### Windows Firefox

###### `bamm --set-custom-useragent=="Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"`

### iOS Safari

###### `bamm --set-custom-useragent=="Mozilla/5.0 (iPhone; CPU iPhone OS 17_7_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.4 Mobile/15E148 Safari/604.1"`
