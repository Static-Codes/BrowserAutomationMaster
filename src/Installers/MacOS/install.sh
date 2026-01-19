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
    
    if [ "$ARCH" = "x86_64" ]; then
        APP_NAME="bamm"

    elif [ "$ARCH" = "aarch64" ] || [ "$ARCH" = "arm64" ]; then
        APP_NAME="bamm-silicon"
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


handle_adhoc_signing() 
{
    # This enables Just In Time Compila
    # com.apple.security.cs.allow-jit -> Just In Time Compilation
    
    # This Required to write new memory
    # com.apple.security.cs.allow-unsigned-executable-memory
    
    # This is required to run BAMM without a paid developer certificate.
    # com.apple.security.cs.disable-library-validation
    
    # This is required for BAMM to make requests to the internet
    # com.apple.security.network.client
    
    # This is required for BAMM to make requests to the internet
    # com.apple.security.network.server

    # Creating a temporary entitlements file for ad-hoc signing.
    show_info "Creating a temporary .entitlements files for ad-hoc signing."
    
    ENTITLEMENTS_PATH="/tmp/bamm.entitlements"

    cat <<EOF > $ENTITLEMENTS_PATH
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
        <key>com.apple.security.network.client</key>
        <true/>
        <key>com.apple.security.network.server</key>
        <true/>
    </dict>
    </plist>
EOF

    show_success "Created bamm.entitlements at: $ENTITLEMENTS_PATH"
    
    # Applying the signature using the user's local 'codesign' utility
    CMD=$(codesign --force --options runtime --entitlements $ENTITLEMENTS_PATH --sign - "${FINAL_BINARY_PATH}")
    ERROR="Unable to apply the ad-hoc signature, please run the following command:\n${CMD}"

    show_info "Applying the ad-hoc signature for Apple Silicon using the codesign utility."
    $CMD || show_error_and_exit "$ERROR"

    rm "$ENTITLEMENTS_PATH"
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
FINAL_BINARY_PATH="${DOWNLOAD_LOCATION}/bamm"
GATEKEEPER_BYPASSED=1 # 1 = False | 0 = True
MAC_TYPE="Intel" # Assuming the current mac is Intel Based since they're cheaper.
MACOS_VERSION=$(get_macos_version)

# Used Basic Calculator (bc) to evaluate whether the current machine's macOS version is atleast 11.0
if echo "$MACOS_VERSION < 11.0" | bc -l | grep -q 1; then
    show_error "The BAMM Installer for macOS was unable to download the latest release as the current machine is not supported."
    show_info "Please ensure macOS 11 or later is installed, or try using the latest Windows version through Bootcamp." 
fi

check_cpu # Checks the CPU and assigns values to required variables

LATEST_RELEASE=$(get_latest_release) # Grabs the lastest release (v.X.X.XAX) (Example: v.1.0.0A7)
# LATEST_RELEASE="v1.0.0A7-silicon-alpha1"

# Null checks on macOS version and Latest Release tag
if [ -z "$MACOS_VERSION" ] || [ -z "$LATEST_RELEASE" ]; then
    show_error_and_exit "The BAMM Installer for macOS was unable to determine the latest release URL. If this issue persists, please make a bug report at $BUG_REPORT_LINK"
fi


# The binary for Apple M Series machines is named bamm-silicon.
# To align with the rest of the guide in the main repo, it needs to be renamed.
REQUIRES_RENAME=false
if [ "$APP_NAME" = "bamm-silicon" ]; then
    REQUIRES_RENAME=true
fi


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
if [ $REQUIRES_RENAME = "true" ]; then
    show_info "Since the current machine is using an Apple Silicon CPU, the binary needs to be renamed, please wait."
    mv "${DOWNLOAD_LOCATION}/${APP_NAME}" "${FINAL_BINARY_PATH}"
    show_success "Renamed binary, continuing."
fi

# Giving the binary executable permissions.
show_info "The binary requires executable permissions, please wait."
chmod +x "${FINAL_BINARY_PATH}"
show_success "The binary was given the required executable permissions, continuing."

# Gatekeeper Check and Confirmation (if present)

# Preemptively ensuring an empty input is handled, before it causes an error. (This would happen if the user presses enter without entering an option)
confirm=${confirm:-n}

if has_quarantine_attribute "$FINAL_BINARY_PATH"; then
    show_warning "The binary is currently protected by Apple Gatekeeper."
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


if [[ "$APP_NAME" == "bamm-silicon" ]]; then
    handle_adhoc_signing
fi

# Creating an alias for the binary
show_info "Checking value for \$SHELL, please wait."

if [[ -z $HOME ]]; then
    show_error_and_exit "Unable to determine the value of the current user's \$HOME variable."
fi

ALIAS_TEXT="alias bamm='$HOME/Desktop/bamm'"

if [[ "$SHELL" == "/bin/bash" ]]; then
    SHELL_ALIAS_PATH="$HOME/.bash_profile"
    printf "%s\n" "$ALIAS_TEXT" >> "$SHELL_ALIAS_PATH"
    source "$SHELL_ALIAS_PATH"
elif [[ "$SHELL" == "/bin/zsh" ]]; then
    SHELL_ALIAS_PATH="$HOME/.zshrc"
    printf "%s\n" "$ALIAS_TEXT" >> "$SHELL_ALIAS_PATH"
    source "$SHELL_ALIAS_PATH"
else
    show_error "Unable to determine the value of the current user's \$SHELL variable."
    show_info "Please use the following to run BAMM:" 
    show_info "$ALIAS_TEXT" 
fi


  
show_success "Installation complete, thank you for choosing BAMM. - Static" 
echo "=============================================="
