###### CPU ARCHITECTURES ######
CPU_ARCH=$(uname -m) || "Not Found"
IS_I386=false
IS_X64=false
IS_ARMV7=false
IS_ARMV8=false

###### OS VARS ######
IS_CHROMEOS=false
IS_DEBIAN=false
IS_FEDORA=false
IS_RASPI=false
IS_OSX=false


###### CPU + OS CHECKS ######
if [ $CPU_ARCH = "x86_64" ]; then
    IS_X64=true
fi

elif [ $CPU = "armv7l"]; then
    IS_ARMV7=true
fi

elif [ $CPU = "aarch64"]; then
    IS_ARMV8=true
fi

if [ $(uname -s) = "Mac" ]; then
    IS_OSX=true
fi

if [[ "Raspberry Pi" =~ $(cat "/proc/cpuinfo") ]]; then
    IS_RASPI=true
fi

if [[ "cros_" =~ $(cat "/proc/cmdline") ]]; then
    IS_CHROMEOS=true
fi


