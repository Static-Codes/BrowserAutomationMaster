# Compile BrowserAutomationMaster (BAM) from Source

---
This guide provides instructions on how to compile BrowserAutomationMaster Manager (BAMM) from its source code for various operating systems.
- To return to the previous page, [click here](..)
---

## Prerequisites

Before you begin, ensure you have the following installed on your system:

1.  **.NET 8.x SDK:** The most recent 8.x SDK as of writing this, is 8.0.17. [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2.  **Git (Optional but Recommended):** For cloning the repo, you can also use the source code provided with each release.

## General Compilation Steps

1.  **Download and Extract the Source Code:**

    - Go to the latest release page: `https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest/`
    - Download the `BAMM-{Version}-Source.zip` file.
    - Extract/Unzip the source.

2.  **Navigate to the Source Directory:**

    ```bash
    cd BrowserAutomationMaster
    ```

3.  **Restore .NET Dependencies:**
    This step is usually handled automatically by the build/publish commands, but it's good practice to run it manually if you encounter issues:
    ```bash
    dotnet restore
    ```

## Platform-Specific Compilation Instructions

Choose the instructions relevant to your target operating system. All commands should be run from within the `BrowserAutomationMaster` source directory.

## Linux

### .deb Package Compilation (for Debian/Ubuntu-based systems)

This will create a `.deb` package for easy installation.

1.  **Install `dotnet-deb` tool (if not already installed):**

    ```bash
    dotnet tool install --global dotnet-deb
    # Ensure the .dotnet/tools directory is in your PATH
    # export PATH="$PATH:$HOME/.dotnet/tools" (add to your .bashrc or .zshrc for permanency)
    ```

2.  **Compile and Package:**
    ```bash
    dotnet deb --runtime linux-x64 --configuration Release
    ```
    The resulting `.deb` package will typically be found in the project's root directory or a sub-directory like `bin/Release/`. Check the command output for the exact location.

#### Generic Linux Publish (Self-Contained Application)

If you don't want a `.deb` package or are on a non-Debian-based Linux distribution, you can create a self-contained application.

1.  **Publish for Linux x64:**
    ```bash
    dotnet publish -c Release -r linux-x64 --self-contained true
    ```
    The compiled application will be in `bin/Release/netX.Y/linux-x64/publish/` (where `X.Y` is the .NET version, e.g., `net8.0`).

## macOS

This will create a self-contained application for macOS 11.0+ (Both Intel and Apple Silicon via Rosetta 2).

1.  **Publish for macOS x64:**

    ```bash
    dotnet publish -c Release -r osx-x64 --self-contained true
    ```

    The compiled application will be in `bin/Release/netX.Y/osx-x64/publish/`.

2.  **(Optional) Publish for macOS ARM64 (Apple Silicon):**
    This is for Apple Silicon users **ONLY**:
    ```bash
    dotnet publish -c Release -r osx-arm64 --self-contained true
    ```
    The compiled application will be in `bin/Release/netX.Y/osx-arm64/publish/`.

## Windows

This will create self-contained applications for different Windows architectures.

1.  **Publish for Windows x64 (64-bit):**

    ```bash
    dotnet publish -c Release -r win-x64 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-x64\publish\`.

2.  **Publish for Windows ARM64:**

    ```bash
    dotnet publish -c Release -r win-arm64 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-arm64\publish\`.

3.  **Publish for Windows x86 (32-bit):**

    ```bash
    dotnet publish -c Release -r win-x86 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-x86\publish\`.

4.  **Download [Inno Setup v6.4.3](https://jrsoftware.org/download.php/is.exe?site=1)**
    Ensure "Associate Inno Setup with the .iss file extension" is checked during setup.

5.  **Create an installer**
    Navigate to `BrowserAutomationMaster\src\Installer Files\Windows`
    Right click on the .iss file corresponding to your published build (ARM64, x64, x86)
    Click compile

    The installer will be in `BrowserAutomationMaster\src\Published Builds\{Your architecture}\`

## Troubleshooting Compilation Issues.

- **.NET SDK Not Found:** Ensure the .NET SDK is installed correctly and that the `dotnet` command is available in your system's PATH.
- **Incorrect Runtime Identifier:** Ensure you're using a valid runtime identifier (RID) for your target platform. You can find a list of RIDs [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).
