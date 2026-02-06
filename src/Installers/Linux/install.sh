#!/bin/bash

###### Start of CPU Architecture vars ######
CPU_ARCH=$(uname -m) || "Not Found"
IS_X64=false
IS_ARMV7=false
IS_ARMV8=false
###### Start of CPU Architecture vars ######

###### Start of OS vars ######
DEBIAN_DISTROS=("debian" "ubuntu" "pop" "linuxmint" "kali" "raspbian")
FEDORA_DISTROS=("fedora" "centos" "rhel" "almalinux" "rocky" "oracle")
IS_OSX=false

if [ "$(uname -s)" = "Darwin" ]; then
    IS_OSX=true
fi

if [ $IS_OSX = "true" ]; then
    show_error_and_exit "Please use the macOS installer for BAMM, located at https://bamm-install.vercel.app/macos"
fi

OS_ID=$(cat /etc/os-release | grep "^ID=") || "Not Found"

if [ "$OS_ID" = "Not Found" ] || [ -z "$OS_ID" ]; then
    show_error_and_exit "Unable to determine the operating system currently in use, OS_ID is null."
    return 1
fi

DISTRO_NAME=$(echo "$OS_ID" | sed 's/ID=[ ]*//') || "Not Found"

if [ "$DISTRO_NAME" = "Not Found" ] || [ -z "$OS_ID" ]; then
    show_error_and_exit "Unable to determine the operating system currently in use, DISTRO_NAME is null."
fi

IS_DEBIAN=false
IS_FEDORA=false
###### End of OS vars ######


echo "Welcome to the BAMM installer for $DISTRO_NAME!"


###### Start of CPU + OS Checks ######
if [ "$CPU_ARCH" = "x86_64" ]; then
    IS_X64=true
fi

if [ "$CPU_ARCH" = "armv7l" ]; then
    IS_ARMV7=true
fi

if [ "$CPU_ARCH" = "aarch64" ]; then
    IS_ARMV8=true
fi



for i in "${DEBIAN_DISTROS[@]}"; do
    if [[ $i =~ $DISTRO_NAME ]]; then
        IS_DEBIAN=true
    fi
done

for i in "${FEDORA_DISTROS[@]}"; do
    if [[ $DISTRO_NAME =~ $i ]]; then
        IS_FEDORA=true
    fi
done
###### End of CPU + OS Checks ######

## USED FOR DEBUGGING - DO NOT REMOVE COMMENTS.
# echo "============================================"
# echo "X64: $IS_X64"
# echo "ARMV7: $IS_ARMV7"
# echo "ARMV8: $IS_ARMV8"
# echo "Debian Based: $IS_DEBIAN"
# echo "Fedora Based: $IS_FEDORA"
# echo "OSX: $IS_OSX"
# echo "============================================"

###### Start of Platform independent installation logic ######

if [ -z "$HOME" ]; then
    TEMP_INSTALL_PATH=~/bamm-installation
else    
    TEMP_INSTALL_PATH="$HOME/bamm-installation"
fi

mkdir -p "$TEMP_INSTALL_PATH" || show_error_and_exit "Unable to create temporary installation directory"

cd "$TEMP_INSTALL_PATH" || show_error_and_exit "Unable to navigate to temporary installation directory"

# Credits to Lukechilds (https://gist.github.com/lukechilds/a83e1d7127b78fef38c2914c4ececc3c)
get_latest_release() {
  curl -s "https://api.github.com/repos/static-codes/browserautomationmaster/releases/latest" |
    grep '"tag_name":' |
    sed -E 's/.*"([^"]+)".*/\1/'
}

download_release() {
    if [ -z "$1" ] || ! [ -z "$2" ]; then
        wget "$1" || show_error_and_exit "Unable to download the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
        show_info "Downloaded latest release of BAMM."
    fi
}

show_error() {
    echo "[ERROR]: $1"
    exit
}

show_error_and_exit() {
    echo "[ERROR]: $1"
    exit
}

show_warning() {
    echo "[WARNING]: $1"
}

show_info() {
    echo "[INFO]: $1"
}


BUG_REPORT_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/issues"
VERSION_TAG=$(get_latest_release) || "Not Found" # Example: v1.0.0A5
RELEASE_VERSION=$(echo "$VERSION_TAG" | sed -r 's/^v//; s/A([0-9]+)/-alpha\1/i') # Example: 1.0.0-alpha5

if [ -z "$VERSION_TAG" ] || [ "$VERSION_TAG" = "Not Found" ]; then
    show_error_and_exit "Unable to determine the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
fi

BASE_DOWNLOAD_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/$VERSION_TAG"
###### End of Platform independent installation logic ######


###### Start of installation logic ######

if [ $IS_DEBIAN = "true" ]; then
    if [ $IS_X64 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-x64.deb"
    elif [ $IS_ARMV7 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-arm.deb"
    elif [ $IS_ARMV8 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-arm64.deb"
    else
        echo "Unsupported Debian-based architecture: $CPU_ARCH"; exit 1
    fi
    FULL_DOWNLOAD_LINK="$BASE_DOWNLOAD_LINK/$FILENAME"
    wget "$FULL_DOWNLOAD_LINK" || show_error_and_exit "Unable to download release script, please make a bug report at $BUG_REPORT_LINK"
    sudo dpkg -i "$FILENAME" || show_error_and_exit "Unable to execute the latest installer, please make a bug report at $BUG_REPORT_LINK"
    
elif [ $IS_FEDORA ]; then
    if [ $IS_X64 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-x64.rpm"
    elif [ $IS_ARMV7 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-arm.rpm"
    elif [ $IS_ARMV8 = "true" ]; then
        FILENAME="bamm.$RELEASE_VERSION.linux-arm64.rpm"
    else
        show_error_and_exit "Unsupported Fedora-based architecture: $CPU_ARCH";
    fi
    FULL_DOWNLOAD_LINK="$BASE_DOWNLOAD_LINK/$FILENAME"
    wget "$FULL_DOWNLOAD_LINK" || show_error_and_exit "Unable to download release script, please make a bug report at $BUG_REPORT_LINK"
    sudo dnf install -y "$FILENAME" || show_error_and_exit "Unable to execute the latest installer, please make a bug report at $BUG_REPORT_LINK"

elif [ -n "$DISTRO_NAME" ]; then
    show_error_and_exit "Linux distribution $DISTRO_NAME is not currently supported."
fi

rm -rf "$TEMP_INSTALL_PATH" || show_error_and_exit "Unable to remove the installer from $TEMP_INSTALL_PATH/$FILENAME, please manually remove this file."
show_info "Successfully installed latest release of BAMM ($VERSION_TAG)"
show_info "Installation location: /usr/local/bin/bamm"

###### End of installation logic ######
