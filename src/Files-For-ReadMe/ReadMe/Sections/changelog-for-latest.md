# BAMM v1.0.0-Alpha3

## **Notes**

- There are 6 releases below aswell as the raw source code.
- `source.zip` contains a 22.9MB archive of the repo, this is with all of the bloat visual studio creates, aswell as published builds, for the pure source code (under 1MB when extracted), please download `BAMM-v1.0.0A2-Source.zip`

---

## Changelog

### General Changes:

- Added CPU detection logic for all supported platforms.
- Added CPU instruction parsing for x64 CPUs.
- Added Detection logic allowing multiple versions of Python 3.X to be used by BAMM.
- Added support for Linux on ARM64 CPUs, now Mac users running Linux will be able to use BAMM!
- Added Runtime support for scripts compiled using BAMM!
- Added UpdateManager.cs to handle cross platform updating.
- Removed support for Windows 10 32 Bit, as it is not feasible to continue supporting deprecated hardware. Pure x86 CPUs have not been produced since 2023, and the 4GiB RAM limit imposed by Windows 10, combined with the inferior hardware, will lead to a perpetually degrading experience.

### New CLI Command:

- `bamm run "path/to/python/file.py"` - Runs any python file however it is strongly recommended to ONLY use this command for scripts compiled using BAMM, specifically ones located in the compiled directory. There is no guarantee this will work with external python scripts.

### New Feature Command:

- `feature "run-headless"` - Instructs the compiler to allow headless execution for the duration of the current script.

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
