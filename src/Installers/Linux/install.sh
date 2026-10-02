#!/bin/bash
# Copyright (C) 2026 Static Codes
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program. If not, see <https://www.gnu.org/licenses/>.

show_error_and_exit() {
    echo "[ERROR]: $1" >&2
    exit 1
}

show_warning() {
    echo "[WARNING]: $1" >&2
}

show_info() {
    echo "[INFO]: $1"
}

BUG_REPORT_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/issues"

# The ID to package-format map, published alongside each release. 
# Generated from WhichDistroSharp by DistroPackageMap in the BAMM source.
PACKAGE_FORMATS_FILE="package-formats.json"

# Credits to Lukechilds (https://gist.github.com/lukechilds/a83e1d7127b78fef38c2914c4ececc3c)
get_latest_release() {
    curl -s "https://api.github.com/repos/static-codes/browserautomationmaster/releases/latest" |
        grep '"tag_name":' |
        sed -E 's/.*"([^"]+)".*/\1/'
}

download_release() {
    wget -O "$2" "$1" || show_error_and_exit "Unable to download the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
    show_info "Downloaded latest release of BAMM."
}

PACKAGE_FORMATS_PATH=""

###### Start of CPU Architecture vars ######
CPU_ARCH=$(uname -m) || "Not Found"
IS_X64=false
IS_ARMV7=false
IS_ARMV8=false
###### End of CPU Architecture vars ######

###### Start of OS vars ######
# Fallback only.
# The ID values are the real os-release ID, not a display name. Oracle Linux reports "ol".
DEBIAN_DISTROS=("debian" "ubuntu" "pop" "linuxmint" "kali" "raspbian" "elementary" "zorin" "parrot")
FEDORA_DISTROS=("fedora" "centos" "rhel" "almalinux" "rocky" "ol")
IS_OSX=false

if [ "$(uname -s)" = "Darwin" ]; then
    IS_OSX=true
fi

if [ $IS_OSX = "true" ]; then
    show_error_and_exit "Please use the macOS installer for BAMM, located at https://bamm-install.vercel.app/macos"
fi

# /etc/os-release is a symlink on most systems and absent on others.
# /usr/lib/os-release is the file it points at.
OS_RELEASE_FILE=""
for candidate in /etc/os-release /usr/lib/os-release; do
    if [ -f "$candidate" ]; then
        OS_RELEASE_FILE="$candidate"
        break
    fi
done

if [ -z "$OS_RELEASE_FILE" ]; then
    show_error_and_exit "Unable to determine the operating system currently in use, no os-release file was found."
fi

# Anchored on ^ID= so ID_LIKE cannot match.
# The quotes are stripped rather than carried through into the comparison below.
DISTRO_NAME=$(sed -n -E 's/^ID="?([^"]*)"?[[:space:]]*$/\1/p' "$OS_RELEASE_FILE" | head -n 1)
DISTRO_PRETTY_NAME=$(sed -n -E 's/^PRETTY_NAME="?([^"]*)"?[[:space:]]*$/\1/p' "$OS_RELEASE_FILE" | head -n 1)
[ -z "$DISTRO_PRETTY_NAME" ] && DISTRO_PRETTY_NAME="$DISTRO_NAME"

if [ -z "$DISTRO_NAME" ]; then
    show_error_and_exit "Unable to determine the operating system currently in use, DISTRO_NAME is null."
fi
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

# The release suffix BAMM publishes for this CPU. The asset names are .linux-{x64,arm,arm64}.
ARCH_SUFFIX=""
if [ "$IS_X64" = "true" ]; then
    ARCH_SUFFIX="x64"
elif [ "$IS_ARMV7" = "true" ]; then
    ARCH_SUFFIX="arm"
elif [ "$IS_ARMV8" = "true" ]; then
    ARCH_SUFFIX="arm64"
fi

# Resolves the package format for the current distribution.
resolve_package_format() {
    local map_entry=""

    if [ -n "$PACKAGE_FORMATS_PATH" ] && [ -f "$PACKAGE_FORMATS_PATH" ]; then
        map_entry=$(lookup_distro_in_map "$DISTRO_NAME")

        if [ -n "$map_entry" ]; then
            if [ "$map_entry" = "null" ]; then
                echo "NOT_SUPPORTED"
            else
                echo "$map_entry"
            fi
            return
        fi

        # The key was absent, which means the map does not cover this distro. Falling through to
        # the hardcoded lists keeps such a machine installable.
        show_warning "The release package map has no entry for '$DISTRO_NAME', using the built-in list."
    fi

    for i in "${DEBIAN_DISTROS[@]}"; do
        if [ "$DISTRO_NAME" = "$i" ]; then
            echo "deb"
            return
        fi
    done

    for i in "${FEDORA_DISTROS[@]}"; do
        if [ "$DISTRO_NAME" = "$i" ]; then
            echo "rpm"
            return
        fi
    done

    echo ""
}

# Reads one key from the "distros" object of package-formats.json.
# Prints its raw token; A format string without quotes, or the literal null. 
# Prints nothing when the key is absent.
lookup_distro_in_map() {
    local wanted="$1"

    awk -v want="$wanted" '
        /"distros"[[:space:]]*:/ { in_distros = 1; next }
        in_distros && /^[[:space:]]*}/ { in_distros = 0 }
        in_distros {
            line = $0
            sub(/^[[:space:]]*"/, "", line)
            quote = index(line, "\"")
            if (quote == 0) { next }

            key = substr(line, 1, quote - 1)
            if (key != want) { next }

            value = substr(line, quote + 1)
            sub(/^[[:space:]]*:[[:space:]]*/, "", value)
            sub(/,[[:space:]]*$/, "", value)
            gsub(/^"|"$/, "", value)
            print value
            exit
        }
    ' "$PACKAGE_FORMATS_PATH"
}

# Fetches the map published with this release. A failure is not fatal: the caller falls back to
# the built-in list. The map is deliberately fetched under the same tag as the artifacts it
# describes, so a script can never pair itself with a stale table.
fetch_package_formats() {
    PACKAGE_FORMATS_PATH=""

    local map_url="$BASE_DOWNLOAD_LINK/$PACKAGE_FORMATS_FILE"
    local map_file="$TEMP_INSTALL_PATH/$PACKAGE_FORMATS_FILE"

    # -f so an HTTP error page is not saved and then parsed as a map.
    curl -sfL --max-time 30 "$map_url" -o "$map_file" 2>/dev/null || return 1

    if [ ! -s "$map_file" ] || ! grep -q '"distros"' "$map_file"; then
        return 1
    fi

    PACKAGE_FORMATS_PATH="$map_file"
    show_info "Loaded the package format map from $map_url"
    return 0
}
###### End of CPU + OS Checks ######

###### Start of Platform independent installation logic ######

if [ -z "$HOME" ]; then
    TEMP_INSTALL_PATH=~/bamm-installation
else    
    TEMP_INSTALL_PATH="$HOME/bamm-installation"
fi

# Credits to Lukechilds (https://gist.github.com/lukechilds/a83e1d7127b78fef38c2914c4ececc3c)

if [ -z "$HOME" ]; then
    TEMP_INSTALL_PATH=~/bamm-installation
else
    TEMP_INSTALL_PATH="$HOME/bamm-installation"
fi

mkdir -p "$TEMP_INSTALL_PATH" || show_error_and_exit "Unable to create temporary installation directory"

cd "$TEMP_INSTALL_PATH" || show_error_and_exit "Unable to navigate to temporary installation directory"

VERSION_TAG=$(get_latest_release) || "Not Found" # Example: v1.0.0A5
RELEASE_VERSION=$(echo "$VERSION_TAG" | sed -r 's/^v//; s/A([0-9]+)/-alpha\1/i') # Example: 1.0.0-alpha5

if [ -z "$VERSION_TAG" ] || [ "$VERSION_TAG" = "Not Found" ]; then
    show_error_and_exit "Unable to determine the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"
fi

BASE_DOWNLOAD_LINK="https://github.com/Static-Codes/BrowserAutomationMaster/releases/download/$VERSION_TAG"
###### End of Platform independent installation logic ######


###### Start of installation logic ######

# The map is fetched here rather than at the top because it needs BASE_DOWNLOAD_LINK, which is
# only known once the release tag has been resolved.
if ! fetch_package_formats; then
    show_warning "Could not download the release package map, falling back to the built-in distribution list."
    show_warning "If your distribution is missing, please make a bug report at $BUG_REPORT_LINK"
fi

PACKAGE_FORMAT=$(resolve_package_format)

if [ -z "$ARCH_SUFFIX" ]; then
    show_error_and_exit "Unsupported CPU architecture: $CPU_ARCH"
fi

if [ "$PACKAGE_FORMAT" = "NOT_SUPPORTED" ]; then
    show_error_and_exit "BAMM does not publish a package for $DISTRO_PRETTY_NAME ($DISTRO_NAME)."
fi

if [ -z "$PACKAGE_FORMAT" ]; then
    show_error_and_exit "Linux distribution $DISTRO_PRETTY_NAME ($DISTRO_NAME) is not currently supported."
fi

case "$PACKAGE_FORMAT" in
    deb|rpm)
        ;;
    *)
        show_error_and_exit "BAMM does not yet publish an installable $PACKAGE_FORMAT package for $DISTRO_PRETTY_NAME."
        ;;
esac

FILENAME="bamm.$RELEASE_VERSION.linux-$ARCH_SUFFIX.$PACKAGE_FORMAT"
FULL_DOWNLOAD_LINK="$BASE_DOWNLOAD_LINK/$FILENAME"

show_info "Installing BAMM $VERSION_TAG for $DISTRO_PRETTY_NAME ($DISTRO_NAME) on $CPU_ARCH"
show_info "Downloading from: $FULL_DOWNLOAD_LINK"

download_release "$FULL_DOWNLOAD_LINK" "$FILENAME" || show_error_and_exit "Unable to download the latest release of BAMM, please make a bug report at $BUG_REPORT_LINK"

if [ "$PACKAGE_FORMAT" = "deb" ]; then
    sudo dpkg -i "$FILENAME" \
        || { sudo apt-get install -f -y && sudo dpkg -i "$FILENAME"; } \
        || show_error_and_exit "Unable to install the downloaded package, please make a bug report at $BUG_REPORT_LINK"
else
    sudo dnf install -y "$FILENAME" \
        || sudo yum install -y "$FILENAME" \
        || sudo rpm -i "$FILENAME" \
        || show_error_and_exit "Unable to install the downloaded package, please make a bug report at $BUG_REPORT_LINK"
fi

rm -rf "$TEMP_INSTALL_PATH" || show_warning "Unable to remove the temporary installation directory $TEMP_INSTALL_PATH, please remove it manually."
show_info "Successfully installed latest release of BAMM ($VERSION_TAG)"
show_info "Installation location: /usr/local/bin/bamm"

###### End of installation logic ######
