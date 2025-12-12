$arch = $Env:PROCESSOR_ARCHITECTURE
$releases_url = "https://api.github.com/repos/Static-Codes/BrowserAutomationMaster/releases"
$download_url = [string]::Empty
$desktop_path = [Environment]::GetFolderPath("Desktop")

If ([string]::IsNullOrEmpty($desktop_path)){
    echo "The BAMM for Windows Installer is unable to determine the path of the current machine's desktop, please try again."
    break
}

try {
    $response = Invoke-WebRequest -Uri $releases_url -UseBasicParsing
    $releases = $response.Content | ConvertFrom-Json
    $latest_release_tag = $releases[0].Tag_Name
    
    If ([string]::IsNullOrEmpty($latest_release_tag)) {
        echo "The BAMM for Windows Installer was unable to parse the latest release for BAMM, please try again."
        break
    }

    $message = [string]::Format("Determined the latest release is BAMM {0}", $latest_release_tag)
    echo $message
    Start-Sleep -Seconds 2
}
catch {
    echo "The BAMM for Windows Installer was unable to determine the latest release for BAMM, please try again."
    break
}

$download_path = -join($desktop_path, "\", "BAMM-Setup.exe")

If ($arch -eq "ARM") {
    echo "Due to system constraints, BAMM is not supported on Windows for ARM32, however, it is supported on Linux."
}


ElseIf ($arch -like "x86") {
    echo "Due to the increasing demanding requirements for modern browser automation, BAMM no longer supports 32bit Intel CPUs, sorry for any inconvenience."
}


ElseIf ($arch -eq "ARM64") {
    echo "Determined the current CPU is ARM64 (aarch64)"
    Start-Sleep -Seconds 2
    $download_url = [string]::Format("https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{0}/BAMM-{0}-ARM64-Setup.exe", $latest_release_tag)
}

ElseIf ($arch -in "AMD64", "x86_64") {
    echo "Determined the current CPU is x64 (AMD64 // x86_64)"
    Start-Sleep -Seconds 2
    $download_url = [string]::Format("https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/{0}/BAMM-{0}-x64-Setup.exe", $latest_release_tag)
}

ElseIf ([string]::IsNullOrEmpty($arch)) {
    echo "The BAMM for Windows Installer was unable to determine your CPU Architecture, please try again."
}

Else {
    echo "BAMM for Windows is not supported your current CPU Architecture, sorry for any inconvenience."
}

If ([string]::IsNullOrEmpty($download_url)) { 
    echo "The BAMM for Windows Installer was unable to download the latest release for BAMM, please try again."
    break
}


try {
    if ([System.IO.File]::Exists($download_path)) {
        echo "A copy of the installer exists on the current user's desktop, please wait while it's deleted."
        Start-Sleep -Seconds 3
        [System.IO.File]::Delete($download_path);
        echo "Successfully deleted the old installer."
        Start-Sleep -Seconds 2
    }

    echo "Downloading newest release of BAMM to the current user's Desktop, please wait."
    $web_client = New-Object System.Net.WebClient
    $web_client.DownloadFile($download_url, $download_path)

    if (![System.IO.File]::Exists($download_path)){
        echo "Failed to download the latest release of BAMM for Windows, please try again."
        break
    }
    
    Start-Sleep -Seconds 3
    $message = [string]::Format("Successfully downloaded the latest version of the installer to: {0}", $download_path)
    echo $message;

    echo "Installing BAMM, please accept the UAC prompt, when requested; this will allow the installer the required permissions to complete the installation."
    Start-Sleep -Seconds 3
    Start-Process -FilePath $download_path
    echo "Successfully started installation process, once the installation process is complete, feel free to delete the installer from your desktop."
    Start-Sleep -Seconds 3
}
catch {
    echo "The BAMM for Windows Installer was unable to determine the latest release for BAMM, please try again."
    break
}