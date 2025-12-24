#!/bin/bash

binary_exists() 
{
    show_info "Validating download binary at: $FINAL_BINARY_PATH"
    if [ ! -f "$1" ]; then
        show_error_and_exit "The BAMM installer for macOS was unable to download the latest release, if this issue persists, please make a bug report at $BUG_REPORT_LINK"
    fi
}

check_cpu() 
{
    ARCH=$(uname -m)

    if [ -z $LATEST_RELEASE ]; then
        show_error_and_exit "The BAMM installer for macOS was unable to determine the latest release version."
    fi
    
    if [ "$ARCH" = "x86_64" ]; then
        APP_NAME="BAMM-$LATEST_RELEASE-Mac-Intel.app.zip"

    elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
        APP_NAME="BAMM-$LATEST_RELEASE-Mac-Silicon.app.zip"
        MAC_TYPE="M-Series"
    
    else
        show_error_and_exit "The BAMM Installer for macOS was unable to determine the current machine's CPU architecture, if this issue persists, please make a bug report at $BUG_REPORT_LINK"
    fi
}

download_binary() {
    curl -sL "${DOWNLOAD_URL}" -o "${DOWNLOAD_LOCATION}/${APP_NAME}" || show_error_and_exit "Unable to download the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
}

# Credits to Lukechilds (https://gist.github.com/lukechilds/a83e1d7127b78fef38c2914c4ececc3c)
get_latest_release() {
  curl -s "https://api.github.com/repos/static-codes/browserautomationmaster/releases/latest" |
    grep '"tag_name":' |
    sed -E 's/.*"([^"]+)".*/\1/'
}

get_macos_version() {
    sw_vers -productVersion | cut -d '.' -f 1,2
}

has_quarantine_attribute() {
    # If xattr -p returns a non-zero exit code, the restriction is not currently present.
    # Using /dev/null suppresses the "No such xattr: com.apple.quarantine" error message.
    xattr -p com.apple.quarantine "$1" 2>/dev/null
    return $?
}

show_error() {
    echo "[ERROR]: $1"
}

show_error_and_exit() {
    echo "[ERROR]: $1"
    exit
}

show_info() {
    echo "[INFO]: $1"
}

show_warning() {
    echo "[WARNING]: $1"
}

show_success() {
    echo "[SUCCESS]: $1"
}

# ---- VARIABLES ----
APP_NAME=""
ARCH=""
BASE_RELEASE_URL="https://github.com/Static-Codes/BrowserAutomationMaster/releases/download"
BUG_REPORT_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/issues"
DOWNLOAD_LOCATION="$HOME/Desktop"
FINAL_BINARY_PATH="${DOWNLOAD_LOCATION}/BAMM.app"
GATEKEEPER_BYPASSED=1 # 1 = False | 0 = True
MAC_TYPE="Intel" # Assuming the current mac is Intel Based since they're cheaper.
MACOS_VERSION=$(get_macos_version)

# Used Basic Calculator (bc) to evaluate whether the current machine's macOS version is atleast 11.0
if echo "$MACOS_VERSION < 11.0" | bc -l | grep -q 1; then
    show_error "The BAMM Installer for macOS was unable to download the latest release as the current machine is not supported."
    show_info "Please ensure macOS 11 or later is installed, or try using the latest Windows version through Bootcamp." 
fi


LATEST_RELEASE=$(get_latest_release) # Grabs the lastest release (v.X.X.XAX) (Example: v.1.0.0A6)

check_cpu # Checks the CPU and assigns values to required variables

# Null checks on macOS version and Latest Release tag
if [ -z "$MACOS_VERSION" ] || [ -z "$LATEST_RELEASE" ]; then
    show_error_and_exit "The BAMM Installer for macOS was unable to determine the latest release URL. If this issue persists, please make a bug report at $BUG_REPORT_LINK"
fi


# The binary for Apple M Series machines is named bamm-silicon.
# To align with the rest of the guide in the main repo, it needs to be renamed.
# REQUIRES_RENAME=false
# if [ "$APP_NAME" = "bamm-silicon" ]; then
#     REQUIRES_RENAME=true
# fi


# Downloading the binary
DOWNLOAD_URL="${BASE_RELEASE_URL}/${LATEST_RELEASE}/${APP_NAME}"
BINARY_PATH="${DOWNLOAD_LOCATION}/${APP_NAME}"
echo "=============================================="
show_info "Installing BAMM ${LATEST_RELEASE} for ${MAC_TYPE} Macs (${ARCH})"
show_info "Downloading from: ${DOWNLOAD_URL}"
show_info "Downloading to ${DOWNLOAD_LOCATION}/${APP_NAME}"
download_binary # Downloads the binary
binary_exists "$BINARY_PATH" # Checks that the binary exists, exits with an error if not.
show_success "Downloaded BAMM ${LATEST_RELEASE} for ${MAC_TYPE} Macs (${ARCH})"


# Renaming the binary (if needed)
# if [ $REQUIRES_RENAME = "true" ]; then
#     show_info "Since the current machine is using an Apple Silicon CPU, the binary needs to be renamed, please wait."
#     mv "${DOWNLOAD_LOCATION}/${APP_NAME}" "${FINAL_BINARY_PATH}"
#     show_success "Renamed binary, continuing."
# fi

show_info "Extracting ${APP_NAME} bundle."
unzip "$BINARY_PATH" -d "$FINAL_BINARY_PATH" || show_error_and_exit "The BAMM Installer for macOS was unable to extract the downloaded application binary, please manually extract this file at ${BINARY_PATH}"

show_success "Extracted ${APP_NAME} bundle."


# Giving the application bundle executable permissions.
show_info "The application bundle requires executable permissions, please wait."
chmod +x "${FINAL_BINARY_PATH}"
show_success "The application bundle was given the required executable permissions, continuing."

# Gatekeeper Check and Confirmation (if present)
# Preemptively ensuring an empty input is handled, before it causes an error. (This would happen if the user presses enter without entering an option)
confirm=${confirm:-n}

if has_quarantine_attribute "$FINAL_BINARY_PATH"; then
    show_warning "The application bundle is currently protected by Apple Gatekeeper."
    show_info "You will be asked if you want to bypass this, please note, this is not a requirement to complete the install, but it is a requirement to run BAMM"

    # Attempting to redirect the current terminal's console input via /dev/tty
    read -r -p "Would you like to bypass Apple Gatekeeper now? (Y/n): " confirm < /dev/tty
    
    # Uses pattern matching to confirm the user input.
    if [[ $confirm =~ ^[yY]$ ]]; then
    
        # If the confirms their intent to bypass, the command is executed below
        xattr -d com.apple.quarantine "${FINAL_BINARY_PATH}"
        GATEKEEPER_BYPASSED=$?

        # Displays a success or warning message, associated with the bypass attempt.
        if [ "$GATEKEEPER_BYPASSED" = 0 ]; then
            show_success "Removed Apple Gatekeeper Quarantine."
        else
            show_warning "Failed to remove the Apple Gatekeeper Quarantine."
        fi

    else
        show_warning "The installation was successful, but BAMM is still protected by Apple Gatekeeper, you will need to add an exception to open BAMM."
        show_info "Please run the command below to lift the restrictions imposed by Apple Gatekeeper:"
        echo "xattr -d com.apple.quarantine \"${FINAL_BINARY_PATH}\""
    fi


# No restrictions are present, as such the installation completed with no issues. (This is the most likely outcome)
else
    show_info "The downloaded release is not quarantined by Apple Gatekeeper, skipping bypass confirmation."
fi

show_success "Installation complete, thank you for choosing BAMM. - Static" 
echo "=============================================="
