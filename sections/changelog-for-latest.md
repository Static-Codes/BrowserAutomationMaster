# BAMM v1.0.0-Alpha6

## **Notes**

- There are 10 releases below aswell as the raw source code (source.zip).

---

## Changelog

<details>
  <summary> Click to see direct source changes </summary>

### General Changes:

- Add changes here
</details>

### New Action Commands:

### New Feature Command:

### New CLI Arguments:

- `bamm --bs` - Instructs BAMM to use [BrowserStack](https://browserstack.com) to run the compiled scripts. This works on all platforms, but is the default for Raspberry Pi and ChromeOS, as they are too underpowered to run selenium.
- `bamm --editbsconf` - Edit and Overwrite Browserstack's YAML Config via an interactive process. (For advanced users)
- `bamm --force-error` - Forces a verbose error message, which is helpful for making a bug report.
- `bamm --gui` - Starts the Graphical User Interface for BAMM.
- `bamm --nohwc` - Instructs BAMM not to check your system's hardware for compatibility, this should not be done unless you've already verified BAMM can run on your machine.
- `bamm --platform-debug` - Displays information on the operating system and machine currently running BAMM.
- `bamm --query-display` - Displays the status of the $DISPLAY variable, use this to check if your system supports BAMM's GUI.
- `bamm --version` - Displays the current version of BAMM, and whether there's a new version available.
- `bamm backup` - Backs up all application files to `BAMM-Backup.zip`.
- `bamm restore` - Looks for a backup of BAMM's data, if the data is found, a restoration is attempted.



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

