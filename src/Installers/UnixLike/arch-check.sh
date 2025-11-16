#!/bin/bash

###### CPU ARCHITECTURES ######
CPU_ARCH=$(uname -m) || "Not Found"
IS_X64=false
IS_ARMV7=false
IS_ARMV8=false

###### OS VARS ######
DEBIAN_DISTROS=("debian" "ubuntu" "pop" "linuxmint" "kali" "raspbian")
FEDORA_DISTROS=("fedora" "centos" "rhel" "almalinux" "rocky" "oracle")

OS_ID=$(cat /etc/os-release | grep "^ID=") || "Not Found"

if [ $OS_ID = "Not Found" ] || [ -z $OS_ID ]; then
    echo "Unable to determine the operating system currently in use, OS_ID is null."
    return 1
fi

DISTRO_NAME=$(echo $OS_ID | sed 's/ID=[ ]*//') || "Not Found"

if [ $DISTRO_NAME = "Not Found" ] || [ -z $OS_ID ]; then
    echo "Unable to determine the operating system currently in use, DISTRO_NAME is null."
    return 1
fi

echo "Welcome to the BAMM installer for $DISTRO_NAME!"

IS_CHROMEOS=false
IS_DEBIAN=false
IS_FEDORA=false
IS_RASPI=false
IS_OSX=false


###### CPU + OS CHECKS ######
if [ $CPU_ARCH = "x86_64" ]; then
    IS_X64=true
fi

if [ $CPU_ARCH = "armv7l" ]; then
    IS_ARMV7=true
fi

if [ $CPU_ARCH = "aarch64" ]; then
    IS_ARMV8=true
fi

if [ "$(uname -s)" = "Darwin" ]; then
    IS_OSX=true
fi

if [[ $(cat "/proc/cpuinfo") =~ "Raspberry Pi" ]]; then
    IS_RASPI=true
fi

if [[ $(cat "/proc/cmdline") =~ "cros_" ]]; then
    IS_CHROMEOS=true
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

echo "============================================"
echo "X64: $IS_X64"
echo "ARMV7: $IS_ARMV7"
echo "ARMV8: $IS_ARMV8"
echo "ChromeOS: $IS_CHROMEOS"
echo "Debian Based: $IS_DEBIAN"
echo "Fedora Based: $IS_FEDORA"
echo "Raspberry Pi: $IS_RASPI"
echo "Mac: $IS_OSX"
echo "============================================"