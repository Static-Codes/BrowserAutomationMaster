# Browser Automation Master 🤖
<img src="https://img.shields.io/github/v/release/static-codes/BrowserAutomationMaster.svg"> [![.NET](https://github.com/Static-Codes/BrowserAutomationMaster/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Static-Codes/BrowserAutomationMaster/actions/workflows/dotnet.yml)

A custom scripting language that compiles into Python 3.9+ code.

BAM Manager (BAMM) simplifies Selenium by allowing you to write automation scripts in a more concise, readable, and English-like language.  

This language is known as **BAMC** **(BAM Config)**. 

Your **.BAMC** file is then passed to the compiler, which creates a Python file containing your desired workflow, effectively reducing the amount of boilerplate you need to manage.

## Demo (v1.0.0A4)
https://github.com/user-attachments/assets/d49b53d6-8203-4d6b-948b-7133b335b653

## Note:
- BAMM will be rewritten and released as **BAMM** *Version* **2** *Alpha Build* **1** (`BAMM v2.0.0A1`)
- A new branch will be created [here](https://github.com/Static-Codes/BrowserAutomationMaster/tree/rewrite) once this development has started.

## Quick Start Guide 🚀
#### Using a GUI (**Recommended**)
  - Run **BAMM** with the following command to access the Script Builder's Graphical User Interface:

    ##### Windows
    ```powershell
    bamm --gui
    ```
    ##### macOS and Linux
    ```bash
    ./bamm --gui
    ```
#### Using the LSP (Advanced Users):
  - Download the VSCode/VSCodium Extension <a target="_new" href="https://github.com/Static-Codes/BAMM-LSP/releases/latest">here</a>
  - Visit the documentation <a href="https://static-codes.github.io/BAMM-Docs/" target="_new">here</a>

## Canary Versions 🐤
- To access features early or to contribute to BAMM, click [here](https://github.com/Static-Codes/BrowserAutomationMaster/tree/canary)

## Table of Contents 📖

### [Why Choose BAMM?](sections/why.md)

### [Installation/Uninstallation](sections/installation.md)

### [Examples](examples/)

### [Compile BAMM from Source](sections/compile.md)

### [Roadmap](sections/roadmap.md)

---

### Supported Browsers 🌐

- **Chrome**
- **Firefox**

### Supported Python Versions 🐍

- **3.9.x**
- **3.10.x**
- **3.11.x**
- **3.12.x**
- **3.13.x**
- **3.14.x**

### Supported Operating Systems 💻

- Linux **(ARM32, ARM64, x64)**
- MacOS 11.0+ **(ARM64, x64)**
- Windows 10/11 **(ARM64, x64)**

### Hardware Requirements ✨
  
- **Minimum Recommended**
  - 4 Core CPU @ 2 GHz
  - 4GB DDR4 RAM
  - An SSD with atleast 1GB of Free Space.

- **Lowest Validated**
    - Raspberry Pi 3 Model B
      - 4 Core ARM CPU @ 1.4GHz 
      - 1GB SDRAM
      - An SD Card with 1GB of Free Space.
