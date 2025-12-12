## Installation ⬇️


- **Windows**
  - Open PowerShell or Windows Terminal as an Administrator
    - This is done to avoid permission issues with Window's Execution Policy.
    - The alternative would be relying on Microsoft's built in [`Set-ExecutionPolicy`](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.security/set-executionpolicy?view=powershell-7.5) function to work 100% of the time, which is not the case on older builds of [Windows 10](https://serverfault.com/questions/696689/cannot-set-powershell-executionpolicy-for-currentuser), which have been reached EOL, but are still supported by BAMM.
  - Copy and paste the command below:
    ```
    <# Downloads and executes the installer script, which does the following:
    # 1. Determines the version of Windows you are using, either ARM64 or x64
    # 2. Downloads the appropriate version of BAMM.
    # 3. Starts the installer, but does not explicitly install BAMM, you can manually cancel if you change your mind.
    #>
    irm https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/Installers/UnixLike/install.sh | iex 
    ```
   
- **Linux/MacOS**
  - Open a terminal and execute the following command:
  <br></br>
  ```
  # Downloads and executes the installer script, which does the following:
  # 1. Determines the Distribution of Linux you are using, either Debian-Based or Fedora-Based
  # 2. Determines your CPU Architecture
  # 3. Downloads the appropriate package for BAMM.
  # 4. Installs the downloaded package.
  
  curl -sL "https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/Installers/UnixLike/install.sh" | /bin/bash
  ```

## Opening BAMM 🚀
- **Linux/Windows:** `bamm`
- **MacOS:** `./bamm`

## Uninstallation ⬇️
- `bamm --uninstall`
