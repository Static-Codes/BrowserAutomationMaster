#!/bin/bash

# DONT FORGET TO BUNDLE USING HOMEBREW!!!!
# https://docs.brew.sh/Cask-Cookbook

# Get the current version of macOS
# Check if that version is >= 11
# Get the current cpu architecture
# Assign the correct download url associated with the current cpu architecture
# Download the binary
# If the user is on silicon, the download needs to be renamed from bamm-silicon to bamm

# Give the user instructions on
# 1. How to give the appropriate permissions to the binary ('chmod +x')
# 2. How to bypass gatekeeper if they wish



get_macos_version() {
    sw_vers -productVersion | cut -d '.' -f 1,2
}


check_cpu() 
{
    ARCH=$(uname -m)
    
    if [ "$ARCH" = "x86_64" ]; then
        APP_NAME="bamm"

    elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
        APP_NAME="bamm-silicon"
        MAC_TYPE="M-Series"
    
    else
        show_error_and_exit "The BAMM Installer for macOS was unable to determine the current machine's CPU architecture, if this issue persists, please make a bug report at $BUG_REPORT_LINK"
    fi
}

# Credits to Lukechilds (https://gist.github.com/lukechilds/a83e1d7127b78fef38c2914c4ececc3c)
get_latest_release() {
  curl -s "https://api.github.com/repos/static-codes/browserautomationmaster/releases/latest" |
    grep '"tag_name":' |
    sed -E 's/.*"([^"]+)".*/\1/'
}


download_binary() {
    curl -sL "${DOWNLOAD_URL}" -o "${DOWNLOAD_LOCATION}/${APP_NAME}" || show_error_and_exit "Unable to download the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
}


show_error() {
    echo "[ERROR]: $1"
}

show_error_and_exit() {
    echo "[ERROR]: $1"
    exit
}

show_warning() {
    echo "[WARNING]: $1"
}

show_success() {
    echo "[SUCCESS]: $1"
}

show_info() {
    echo "[INFO]: $1"
}


APP_NAME=""
ARCH=""
BASE_RELEASE_URL="https://github.com/Static-Codes/BrowserAutomationMaster/releases/download"
BUG_REPORT_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/issues"
DOWNLOAD_LOCATION="$HOME/Desktop"
FINAL_BINARY_PATH="${DOWNLOAD_LOCATION}/bamm"
GATEKEEPER_BYPASSED=false
MAC_TYPE="Intel" # Assuming the current mac is Intel Based since they're cheaper.
MACOS_VERSION=$(get_macos_version)

# Used Basic Calculator (bc) to evaluate whether the current machine's macOS version is atleast 11.0
if echo "$MACOS_VERSION < 11.0" | bc -l | grep -q 1; then
    show_error "The BAMM Installer for macOS was unable to download the latest release as the current machine is not supported."
    show_info "Please ensure macOS 11 or later is installed, or try using the latest Windows version through Bootcamp." 
fi

LATEST_RELEASE=$(get_latest_release)


if [ -z "$MACOS_VERSION" ] || [ -z "$LATEST_RELEASE" ]; then
    show_error_and_exit "The BAMM Installer for macOS was unable to determine the latest release URL. If this issue persists, please make a bug report at $BUG_REPORT_LINK"
fi


REQUIRES_RENAME=false

# The binary for Apple M Series machines is named bamm-silicon.
# To align with the rest of the guide in the main repo, it needs to be renamed.
if [ "$APP_NAME" = "bamm-silicon" ]; then
    REQUIRES_RENAME=true
fi


# Downloading the binary
DOWNLOAD_URL="${BASE_RELEASE_URL}/${LATEST_RELEASE}/${APP_NAME}"
echo "=============================================="
show_info "Installing BAMM ${LATEST_RELEASE} for ${MAC_TYPE} Macs (${ARCH})"
show_info "Downloading from: ${DOWNLOAD_URL}"
show_info "Downloading to ${DOWNLOAD_LOCATION}/${APP_NAME}"
show_success "Downloaded BAMM ${LATEST_RELEASE} for ${MAC_TYPE} Macs (${ARCH})"


# Renaming the binary (if needed)
if [ $REQUIRES_RENAME = "true" ]; then
    show_info "Since the current machine is using an Apple Silicon CPU, the binary needs to be renamed, please wait."
    mv "${DOWNLOAD_LOCATION}/${APP_NAME}" "${FINAL_BINARY_PATH}"
    show_success "Renamed binary, continuing."
fi

# Giving the binary executable permissions.
show_info "The binary requires executable permissions, please wait."
chmod +x "${FINAL_BINARY_PATH}"
show_success "The binary was given the required executable permissions, continuing."


show_warning "The binary is currently protected by Apple Gatekeeper."
show_info "You will be asked if you want to bypass this, please note, this is not a requirement to complete the install, but it is a requirement to run BAMM"

read -r -p "Would you like to bypass Apple Gatekeeper now? (Y/n): " confirm


# Uses pattern matching to confirm the user input. If the
if [[ ! $confirm  =~ [Yy] ]]; then
    show_warning "The installation was successful, but BAMM is still protected by Apple Gatekeeper, you will need to add an exception manually to open BAMM."
else 
    xattr -d com.apple.quarantine "${FINAL_BINARY_PATH}"
    # All other comparisons were done with boolean operators, but this one liner is much more concise. 
    # $? returns the exit code associated with the last executed command.
    GATEKEEPER_BYPASSED=$?
fi

if [ $GATEKEEPER_BYPASSED -eq 0 ]; then
    show_success "Removed the Apple Gatekeeper Quarantine on BAMM."
else
    show_warning "Failed to remove the Apple Gatekeeper Quarantine, please ensure this restriction is removed before attempting to open BAMM."
    show_info "To remove the Apple Gatekeeper Quarantine (if present) enter the following command:"
    echo xattr -d com.apple.quarantine "${FINAL_BINARY_PATH}"
fi

show_success "Installation complete, thank you for choosing BAMM. - Static" 
echo "=============================================="
