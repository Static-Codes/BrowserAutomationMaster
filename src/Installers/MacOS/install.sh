#!/bin/bash

bundle_exists() 
{
    show_info "Validating download bundle at: $BUNDLE_PATH"
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
    INVALID_RESP="Unable to determine latest macOS version, please ensure you are using the appropriate installer."
    sw_vers -productVersion | cut -d '.' -f 1,2 || show_error_and_exit "$INVALID_RESP"
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
BUNDLE_PATH="${DOWNLOAD_LOCATION}/BAMM.app"
EXECUTABLE_PATH="${BUNDLE_PATH}/Contents/MacOS/bamm"
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
sleep 1

show_info "Downloading from: ${DOWNLOAD_URL}"
show_info "Downloading to ${DOWNLOAD_LOCATION}/${APP_NAME}"
sleep 1

download_binary # Downloads the binary
sleep 1

bundle_exists "$BINARY_PATH" # Checks that the binary exists, exits with an error if not.
sleep 1

show_success "Downloaded BAMM ${LATEST_RELEASE} for ${MAC_TYPE} Macs (${ARCH})"

show_info "Extracting ${APP_NAME} bundle."
sleep 1
unzip -q "$BINARY_PATH" -d "$DOWNLOAD_LOCATION" || show_error_and_exit "The BAMM Installer for macOS was unable to extract the downloaded application binary, please manually extract this file at ${BINARY_PATH}"
show_success "Extracted ${APP_NAME} bundle."


show_info "Removing the compressed release archive."
sleep 1
rm "$BINARY_PATH" || show_error_and_exit "The BAMM Installer for macOS was unable to remove the compressed release archive."
show_success "Removed the compressed release archive."

# Giving the application bundle executable permissions.
show_info "The application bundle requires executable permissions, please wait."
sleep 1
chmod +x "${EXECUTABLE_PATH}"
show_success "The application bundle was given the required executable permissions, continuing."

# Gatekeeper Check and Confirmation (if present)
# Preemptively ensuring an empty input is handled, before it causes an error. (This would happen if the user presses enter without entering an option)
confirm=${confirm:-n}

if has_quarantine_attribute "$EXECUTABLE_PATH"; then
    show_warning "The application bundle is currently protected by Apple Gatekeeper."
    sleep 1
    show_info "You will be asked if you want to bypass this, please note, this is not a requirement to complete the install, but it is a requirement to run BAMM"

    # Attempting to redirect the current terminal's console input via /dev/tty
    read -r -p "Would you like to bypass Apple Gatekeeper now? (Y/n): " confirm < /dev/tty
    
    # Uses pattern matching to confirm the user input.
    if [[ $confirm =~ ^[yY]$ ]]; then
    
        # If the confirms their intent to bypass, the command is executed below
        xattr -d com.apple.quarantine "${BUNDLE_PATH}"
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
        echo "xattr -d com.apple.quarantine \"${BUNDLE_PATH}\""
    fi


# No restrictions are present, as such the installation completed with no issues. (This is the most likely outcome)
else
    show_info "The downloaded release is not quarantined by Apple Gatekeeper, skipping bypass confirmation."
fi

ALIAS_FILEPATH="$HOME/.bash_alias"
ALIAS_STRING="alias bamm='$EXECUTABLE_PATH'"

# Temporary overwrite protection since cat > will overwrite if passed mistakenly over >>
show_info "Adding temporary overwrite protection for file at: $ALIAS_FILEPATH"
sleep 1
set -o noclobber
show_success "Added temporary overwrite protection at: $ALIAS_FILEPATH"


sleep 2
show_info "Due to the complexities of newer macOS versions, the current solution is to create an alias for the BAMM executable."
sleep 1

show_info "Adding a zshell alias for the BAMM executable using the command: 'echo \"$ALIAS_STRING\" >> \"$ALIAS_FILEPATH\"'."
sleep 1
echo "$ALIAS_STRING" >> "$ALIAS_FILEPATH"
show_success "Added a zshell alias for the BAMM executable."


# show_info "Adding temporary overwrite protection for file at: $ALIAS_FILEPATH"
# sleep 1
# set -o noclobber
# show_success "Added temporary overwrite protection at: $ALIAS_FILEPATH"

# # NEED FIXING
show_info "Due to restrictions in newer versions of macOS, a signing identity is required to open BAMM from the app icon, please wait."
show_info "For more information, please visit: https://developer.apple.com/documentation/security/seccodesignatureflags/adhoc"
sleep 2

# Temporary .entitlements file logic
ENTITLEMENTS_FILE="/tmp/bamm.entitlements"
show_info "(1/3) Creating a temporary entitlements file to enable required permissions, please wait.."

cat <<EOF > "$ENTITLEMENTS_FILE"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>
    <key>com.apple.security.personal-information.keychain</key>
    <true/>
</dict>
</plist>
EOF

show_success "(1/3) Created the temporary entitlements file at: ${ENTITLEMENTS_FILE}"

# Ad-hoc signing logic.
show_info "(2/3) Adding Ad-hoc signing identity the temporary entitlements file."
codesign --force --entitlements "${ENTITLEMENTS_FILE}" --sign - "${BUNDLE_PATH}"
show_success "(2/3) Added required Ad-hoc signing identity."

# Entitlements removal logic.
show_info "(3/3) Removing temporary entitlements file."
rm "${ENTITLEMENTS_FILE}" || show_error "(3/3) Unable to remove the temporary entitlements file, please remove this using: rm '${ENTITLEMENTS_FILE}'"
show_success "(3/3) Removed temporary entitlements file." 

show_success "Installation complete, thank you for choosing BAMM! - Static" 
echo "=============================================="
