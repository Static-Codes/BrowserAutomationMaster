using BrowserAutomationMaster.Core.Types.Linux;

namespace BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux
{
    /// <summary> The distros that BAMM supports. </summary>
    /// <remarks>
    /// Adding a distro takes two edits: 
    /// 1. A static field here
    /// 2. An entry in the table at <c>Distros/package-managers.json</c> in the WhichDistroSharp repository. 
    /// 
    /// This file stores data that is unique to BAMM; A display name, package dependency lists and binary installation type.
    ///  
    /// The package manager and the package format are defined by WhichDistroSharp in <see cref="Distro.SupportedDistros"/>.<br/><br/>
    /// 
    /// Several upstream distros can map to a single entry, however, BAMM will treat them as the same base.
    /// 
    /// Example:
    /// Debian claims Raspbian and OpenSUSE
    /// OpenSUSE claims Leap and Tumbleweed.
    /// 
    /// Note: An upstream member may NOT be claimed twice; Attempting to do so will throw an exception.
    /// </remarks>
    public class Distros 
    {
        // python-venv is included by default with python on Arch based distros.
        public readonly static Distro ArchLinux = new(
            Name: "Arch Linux",
            SupportedDistros: [WhichDistro.Arch],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Arch)!,
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi",
                "base-devel",
            ],
            InstallationType: InstallationType.Binary
        );

        // python-venv is included by default with python on AltLinux.
        public readonly static Distro AltLinux = new(
            Name: "ALT Linux",
            SupportedDistros: [WhichDistro.Altlinux],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Altlinux)!,
            RequiredPackages: [
                "xclip",
            ],
            OptionalPackages: [
                "python3-dev",
                "gcc-c++",
                "make"
            ],
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages",
            InstallationType: InstallationType.Binary
        );

        public readonly static Distro Debian = new(
            Name: "Debian",
            SupportedDistros: [WhichDistro.Debian, WhichDistro.Raspbian],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Debian)!,
            RequiredPackages: [
                "xclip",
                "python3-venv"
            ],
            OptionalPackages: [
                "libffi-dev",
                "build-essential",
                "python3-dev",
            ],
            InstallationType: InstallationType.Package
        );

        public readonly static Distro ElementaryOS = new(
            Name: "elementary OS",
            SupportedDistros: [WhichDistro.Elementary],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Elementary)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        // python3-venv is included by default with python3 on Fedora and RHEL based distros.
        public readonly static Distro Fedora = new(
            Name: "Fedora",
            SupportedDistros: [WhichDistro.Fedora],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Fedora)!,
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi-devel",
                "python3-devel"
            ],
            InstallationType: InstallationType.Package
        );

        // build-essential, python3-dev, python3-venv is included by default with BSD based distros.
        public readonly static Distro FreeBSD = new(
            Name: "FreeBSD",
            SupportedDistros: [WhichDistro.Freebsd],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Freebsd)!,
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi"
            ],
            InstallationType: InstallationType.Binary,
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public readonly static Distro KaliLinux = new(
            Name: "Kali Linux",
            SupportedDistros: [WhichDistro.Kali],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Kali)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro LinuxMint = new(
            Name: "Linux Mint",
            SupportedDistros: [WhichDistro.Linuxmint],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Linuxmint)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro OpenSUSE = new(
            Name: "openSUSE",
            SupportedDistros: [WhichDistro.Opensuse, WhichDistro.OpensuseLeap, WhichDistro.OpensuseTumbleweed],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Opensuse)!,
            RequiredPackages: [
                "xclip"
            ],
            OptionalPackages: [
                "libffi-devel",
                "devel_basis",
                "python3-devel"
            ],
            InstallationType: InstallationType.Package,
            Description: "Independent RPM-based distribution utilizing the Zypper package manager and YaST configuration tool."
        );

        public readonly static Distro ParrotOS = new(
            Name: "Parrot OS",
            SupportedDistros: [WhichDistro.Parrot],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Parrot)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        // python-venv is included by default with python on PCLinuxOS.
        // Semantic Override Information:
        // - PCLinuxOS uses Standalone packaging, so its upstream value is ignored.
        // - PCLinuxOS uses the upstream value for it's package manager (apt-get)
        // - PCLinux uses the RPM package format, therefore Debian packaging rules do NOT apply.
        public readonly static Distro PCLinuxOS = new(
            Name: "PCLinuxOS",
            SupportedDistros: [WhichDistro.Pclinuxos],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Pclinuxos)!,
            RequiredPackages: [
                "xclip",
            ],
            OptionalPackages: [
                "libffi-devel",
                "python3-devel",
                "task-c++-devel"
            ],
            InstallationType: InstallationType.Package,
            PythonVar: "python",
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages"
        );

        public readonly static Distro PopOS = new(
            Name: "Pop!_OS",
            SupportedDistros: [WhichDistro.Pop],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Pop)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro Ubuntu = new(
            Name: "Ubuntu",
            SupportedDistros: [WhichDistro.Ubuntu],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Ubuntu)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro Unknown = new(
            Name: "Generic Linux",
            SupportedDistros: [],
            PackageInfo: PackageManagerInfo.Unknown,
            RequiredPackages: [],
            OptionalPackages: [],
            InstallationType: InstallationType.Binary,
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public readonly static Distro ZorinOS = new(
            Name: "Zorin OS",
            SupportedDistros: [WhichDistro.Zorin],
            PackageInfo: PackageManagerInfo.GetInfo(WhichDistro.Zorin)!,
            RequiredPackages: Debian.RequiredPackages,
            OptionalPackages: Debian.OptionalPackages,
            InstallationType: InstallationType.Package
        );
    }
}
