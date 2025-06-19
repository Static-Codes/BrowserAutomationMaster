# Browser Automation Master

A Domain Specific Language (DSL) that compiles into valid python code!

---

### Supported Browsers

- **Chrome**
- **Firefox**

### Supported Python Versions

- **3.9.x**
- **3.10.x**
- **3.11.x**
- **3.12.x**
- **3.13.x**
- **3.14.x**

### Supported Operating Systems

- Linux **(x64)**
- MacOS 11.0+ **(x64)**
- Windows 10 **(ARM64, x64, x86)**
- Windows 11 **(ARM64, x64, x86)**

### Supported Platforms

- **x86**
- **x64**
- **ARM64**

### System Requirements (Minimum Tested)

- 2+ Core CPU
- 4GB DDR3 RAM (The application itself uses under 200MB of RAM)
- Any Supported Browser
- Any Supported Python Version

### Tested On

- AMD Athlon Gold 3150U
- AMD Ryzen 7 2700X
- AMD EPYC 7282
- AMD Ryzen 9 5950X
- Intel I5 4260U (Mac Mini 2014)
- Intel I3 6100
- Intel I5 6500
- Intel I7 7700
- Intel I5 8500

---

## Installation

- **Linux `.deb` package**
  You can install the package by double clicking the .deb file, or by using:
  ```bash
  sudo dpkg -i <package_name>.deb
  # If there are dependency issues, fix them with:
  sudo apt-get install -f
  ```
- **MacOS**
  The compiled application will be in `bin/Release/netX.Y/osx-arm64/publish/`.
  Place it on your Desktop for easy access!
  use sudo chmod +x Desktop/bamm to make it an executable!
- **Windows**
  Simply double click the installer in `BrowserAutomationMaster\src\Published Builds\{Your architecture}\`

## Opening BAMM

- **MacOS**
  Desktop/bamm

- **Linux and Windows**
  bamm
