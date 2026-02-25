using BrowserAutomationMaster.Core.Types.Linux;

namespace BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux
{
    // Ensure DistroManager.DetermineDistro() is updated when a new distro is added.
    public class Distros 
    {
        // python-venv is included by default with python on Arch based distros.
        public readonly static Distro ArchLinux = new(
            Name: "Arch Linux", 
            ID: "arch",
            BaseDistro: DistroBase.ArchLinux, 
            PackageManager: "pacman", 
            InstallCommand: "-S",
            UninstallCommand: "-Rns",
            QueryCommand: "pacman",
            QueryArguments: "-Qi",
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi",
                "base-devel",
            ],
            PackageType: PackageType.PKG_TAR_XZ,
            InstallationType: InstallationType.Binary
        );

        // python-venv is included by default with python on AltLinux.
        public readonly static Distro AltLinux = new(
            Name: "ALT Linux",
            ID: "altlinux",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "epm",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            QueryCommand: "rpm",
            QueryArguments: "-q",
            RequiredPackages: [
                "xclip",
            ],
            OptionalPackages: [
                "python3-dev",
                "gcc-c++",
                "make"
            ],
            PackageType: PackageType.RPM,
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages",
            InstallationType: InstallationType.Binary
        );

        public readonly static Distro Debian = new(
            Name: "Debian",
            ID: "debian", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            QueryCommand: "dpkg-query",
            QueryArguments: "-W -f=${db:Status-Status}",
            InstallationKeyword: "installed",
            RequiredPackages: [
                "xclip",
                "python3-venv"
            ],
            OptionalPackages: [
                "libffi-dev",
                "build-essential",
                "python3-dev",
            ],
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro ElementaryOS = new(
            Name: "elementary OS",
            ID: "elementary", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        // python3-venv is included by default with python3 on Fedora and RHEL based distros.
        public readonly static Distro Fedora = new(
            Name: "Fedora",
            ID: "fedora", 
            BaseDistro: DistroBase.Fedora, 
            PackageManager: "dnf",
            InstallCommand: "install -y",
            UninstallCommand: "remove -y",
            QueryCommand: "rpm",
            QueryArguments: "-q",
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi-devel",
                "python3-devel"
            ],
            PackageType: PackageType.RPM,
            InstallationType: InstallationType.Package
        );

        // build-essential, python3-dev, python3-venv is included by default with BSD based distros.
        public readonly static Distro FreeBSD = new(
            Name: "FreeBSD",
            ID: null, 
            BaseDistro: DistroBase.BSD, 
            PackageManager: "pkg", 
            InstallCommand: "install -y",
            UninstallCommand: "delete -y",
            QueryCommand: "pkg",
            QueryArguments: "info",
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi"
            ],
            PackageType: PackageType.PKG,
            InstallationType: InstallationType.Binary,
            ReleaseIdentifier: "freebsd",
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public readonly static Distro KaliLinux = new(
            Name: "Kali Linux",
            ID: "kali",
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        public readonly static Distro LinuxMint = new(
            Name: "Linux Mint", 
            ID: "linuxmint",
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        public readonly static Distro OpenSUSE = new(
            Name: "openSUSE",
            ID: "opensuse", 
            BaseDistro: DistroBase.Standalone,
            PackageManager: "zypper",
            InstallCommand: "install -y",
            UninstallCommand: "remove -u",
            QueryCommand: "zypper",
            QueryArguments: "search -i",
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi-devel",
                "devel_basis",
                "python3-devel"
            ],
            PackageType: PackageType.RPM,
            InstallationType: InstallationType.Package,
            Description: "Independent RPM-based distribution utilizing the Zypper package manager and YaST configuration tool."
        );

        public readonly static Distro ParrotOS = new(
            Name: "Parrot OS",
            ID: "parrot",
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        // python-venv is included by default with python on PCLinuxOS.
        public readonly static Distro PCLinuxOS = new(
            Name: "PCLinuxOS",
            ID: "pclinuxos",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            QueryCommand: "rpm",
            QueryArguments: "-q",
            RequiredPackages: [
                "xclip",
            ],
            OptionalPackages: [
                "libffi-devel",
                "python3-devel",
                "task-c++-devel"
            ],
            PackageType: PackageType.RPM,
            InstallationType: InstallationType.Package,
            PythonVar: "python",
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages"
        );

        public readonly static Distro PopOS = new(
            Name: "Pop!_OS", 
            ID: "pop",
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        public readonly static Distro Ubuntu = new(
            Name: "Ubuntu", 
            ID: "ubuntu",
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );

        public readonly static Distro Unknown = new(
            Name: "Generic Linux",
            ID: "",
            BaseDistro: DistroBase.Unknown,
            PackageManager: "",
            InstallCommand: "",
            UninstallCommand: "",
            QueryCommand: "",
            QueryArguments: "",
            RequiredPackages: [],
            OptionalPackages: [],
            PackageType: PackageType.UNKNOWN,
            InstallationType: InstallationType.Binary,
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public readonly static Distro ZorinOS = new(
            Name: "Zorin OS",
            ID: "zorin", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: Debian.PackageManager,
            InstallCommand: Debian.InstallCommand,
            UninstallCommand: Debian.UninstallCommand,
            QueryCommand: Debian.QueryCommand,
            QueryArguments: Debian.QueryArguments,
            InstallationKeyword: Debian.InstallationKeyword,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            PackageType: Debian.PackageType,
            InstallationType: Debian.InstallationType
        );
    }


}