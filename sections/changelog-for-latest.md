## **Notes**

- There are 10 binaries below aswell as the raw source code (source.zip).

---

## Changelog

<details>
  <summary> Click here to view a summary of changes </summary>

  
## 🚀 High-Level Summary
This update significantly expands **Linux support**, introducing a robust Distribution Management system that supports Debian, Fedora, Arch, and Standalone based systems. It also introduces **Embedded Resources** to reduce runtime dependencies (offline capability) and refactors package management to differentiate between Python (PyPi) and OS-level packages.

---

## ✨ New Features

### Linux Distribution & Packaging Support
- **Multi-Distro Support:** Added explicit detection and handling for a wide range of Linux distributions including **Debian, Ubuntu, Fedora, Arch Linux, Kali Linux, OpenSUSE, Parrot OS, Pop!_OS, Linux Mint,** and **FreeBSD**.
- **Package Generation:** The Publisher can now generate native packages:
    * `.deb` for Debian/Ubuntu based systems.
    * `.rpm` for Fedora/RHEL/OpenSUSE based systems.
    * `PKGBUILD` / `.pkg.tar.xz` for Arch Linux.
- **Smart Uninstallation:** `UninstallationManager` now detects the underlying package manager (`apt`, `dnf`, `pacman`) to execute the correct removal commands (`remove --purge`, `remove -u`, `pacman -Rns`).

### Embedded Resources & Offline Capability
- **Embedded Wheels:** Python wheels (dependencies) for ARMv7/ARMhf are now embedded directly into the application assembly rather than downloaded at runtime.
- **Embedded Binaries:** Included `free-for-macOS` as an embedded resource to accurately detect memory usage on macOS (Apple Silicon & Intel).
- **Resource Extraction:** New logic to extract these embedded resources to disk on demand.

### Source Control & Compilation
- **Standalone Compilation:** Added logic to compile standalone binaries within the Publisher.
- **Source Retrieval:** Added `ArchiveManager` to handle the automatic downloading and extraction of the codebase (`.tar.gz` and `.zip`) for self-compilation.

---

## 🛠 Technical Improvements

- **Python Support:** Added experimental/untested support for **Python 3.15** (beta).
- **Memory Management (macOS):** Completely rewrote memory detection for macOS to use a bundled helper binary (`free`) instead of parsing `vm_stat` via shell scripts.
- **ARM Architecture:** Enhanced detection for ARMv7 (`armel`) vs ARMv7+FPU (`armhf`) to serve the correct Python wheels.
- **Refactoring:**
    * Renamed `PackageManager` to `PyPiPackageManager` to distinguish it from OS package managers.
    * Renamed `FilePermissionManager` to `UnixFilePermissionManager`.
    * Moved `PlatformManager` logic to use the new `Distro` class for identification.

---

## 🐛 Bug Fixes

* **Arch Linux:**
    * Fixed `pkgVer` logic in `archBuild` to reflect the proper version source.
    * Fixed a bug causing the inclusion of 2 trailing bytes in `PKGBUILD`.
    * Fixed python detection logic on Arch-based systems.
* **Crash Fixes:** Fixed a critical bug in `AnsiManager` causing premature crashing on all platforms when `GlobalSettings` is not set.
* **Logic Errors:**
    * Fixed binary copying logic in `ArchBuild.WritePKGBuildFile`.
    * Fixed bug caused by handling raw binary hash bytes as UTF8 text.
    * Fixed infinite loop potential in `GetSupportedPackageVersion`.

---

## 📦 New Classes & Files

The following classes were introduced to support the new architecture:

### Managers
- **`DistroManager`**: Handles the detection of the current Linux distribution by parsing `/etc/os-release` and managing fallback strategies.
- **`Distro`**: A data class defining the properties of a specific Linux distribution (Package Manager, Install Commands, Supported Architectures).
- **`EmbeddedResourceManager`**: Manages the retrieval and writing of resources (Wheels, Binaries, Scripts) embedded in the .NET assembly.
- **`ArchiveManager`**: Handles extraction of `.tar.gz` and `.zip` archives for source code management.
- **`UnixFilePermissionManager`**: Uses P/Invoke (`libc`) to check and set executable permissions (`chmod`, `access`) on Unix-like systems.

### Helpers / Enums
- **`Distros`**: Contains static definitions for all supported Linux distributions (e.g., `Distros.Ubuntu`, `Distros.ArchLinux`).
- **`PlatformSelection`**: Logic for selecting compilation targets in the Publisher.
- **`PackageType` & `InstallationType`**: Enums defining how the application is installed (Binary vs Package) and the file format.


</details>

## Releases

---

### Windows 💻

There are two versions for Windows. You most likely need the **x64** version.

- `BAMM-v1.0.0A8-x64-Setup.exe`: For modern **64-bit** Windows systems, this is the most common version.
- `BAMM-v1.0.0A8-ARM64-Setup.exe`: For Windows devices running on **ARM64 (ARMv8)** processors, such as newer Surface Laptops.

---

### macOS 🍎

- `BAMM-v1.0.0A8-Mac-Intel.app.zip`: For all **Intel mac** users on MacOS 11.0+
- `BAMM-v1.0.0A8-Mac-Silicon.app.zip`: For **Apple Silicon** users on MacOS 11.0+

---

### Linux 🐧

- `bamm.v1.0.0A8.linux-x64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A8.linux-arm64.deb`: For **64-bit Debian-based** Linux distributions, such as Ubuntu, Linux Mint, or Pop!\_OS on `M-Series Macs`, newer `Surface Laptops`, and `Raspberry Pi`.
- `bamm.v1.0.0A8-linux-arm.deb` : For **32-bit Debian-based** Linux distributions, running on older chromebooks or other armv7 device, tested on crouton using Debian 12.

- `bamm.v1.0.0A8.linux-x64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Intel and AMD CPUs from the last 20 years.
- `bamm.v1.0.0A8.linux-arm64.rpm`: For **64-bit Fedora-based** Linux distributions, such as CentOS, Oracle Linux, or Qubes on Macs or newer Surface Laptops.
- `bamm.v1.0.0A8.linux-arm.rpm`: For **32-bit Fedora-based** Linux distributions, running on older chromebooks or other ARMv7 device, tested on crouton using Fedora Server 43.

- `bamm.v1.0.0A8-1.pkg.tar.gz`: For **64-bit Arch-based** Linux distributions,

