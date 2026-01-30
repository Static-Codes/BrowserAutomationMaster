using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux 
{

    public enum PackageType 
    {
        DEB,
        PKG_TAR_XZ, // Arch
        PKG, // FreeBSD
        RPM,
        TBZ2, // Gentoo
        UNKNOWN
    };

    public static class PackageTypeExtensions 
    {
        public static string GetPackageFileType(this PackageType packageType) {
            return "." + packageType.ToString().ToLower().Replace("_", ".");
        }
    }

    public enum InstallationType 
    {
        Binary,
        Package,
    }

    public enum DistroBase
    {
        ArchLinux,
        BSD,
        Debian,
        Fedora,
        OpenSUSE,
        Standalone,
        Unknown,
    }

    public class Distro(
        string Name,
        string? ID, 
        DistroBase BaseDistro, 
        string PackageManager,
        string InstallCommand,
        string UninstallCommand,
        PackageType PackageType,
        InstallationType InstallationType,
        string ShellPath = "/bin/bash",
        string ReleaseFilePath = "/etc/os-release",
        string ReleaseIdentifier = "=",
        string? BackupReleaseCmd = null,
        string? BackupReleaseCmdArgs = null,
        string? Description = null
        
    ) 
    {
        public string Name { get; set; } = Name;
        public string? ID { get; private set; } = ID;
        public DistroBase BaseDistro { get; private set; } = BaseDistro;
        public string PackageManager { get; private set; } = PackageManager;
        public string InstallCommand { get; private set; } = InstallCommand;
        public string UninstallCommand { get; private set; } = UninstallCommand;
        public PackageType PackageType { get; private set; } = PackageType;
        public InstallationType InstallationType { get; private set; } = InstallationType;
        public string ShellPath { get; private set; } = ShellPath;
        public string ReleaseFilePath { get; private set; } = ReleaseFilePath;
        public string ReleaseIdentifier { get; private set; } = ReleaseIdentifier;
        public string? BackupReleaseCmd { get; private set; } = BackupReleaseCmd;
        public string? BackupReleaseCmdArgs { get; private set; } = BackupReleaseCmdArgs;
        public string? Description { get; private set; } = Description;
        public Architecture[] SupportedArchitectures = [ 
            X64, X86, Arm, Arm, Arm64
        ];

        public override string ToString()
        {
            return string.Join(NLC, [
                $"Distribution Name: {Name}",
                $"Distribution Base: {BaseDistro}",
                $"Package Manager: {PackageManager}",
                $"Install Command: {PackageManager} {InstallCommand}",
                $"Package Type: {PackageTypeExtensions.GetPackageFileType(PackageType)}",
                $"Shell Path: {ShellPath}",
                $"Release File: {ReleaseFilePath}",
            ]);
        }
    }

    


    // Ensure DistroManager.DetermineDistro() is updated when a new distro is added.
    public class Distros 
    {
        public readonly static Distro ArchLinux = new(
            Name: "Arch Linux", 
            ID: "arch",
            BaseDistro: DistroBase.ArchLinux, 
            PackageManager: "pacman", 
            InstallCommand: "-S",
            UninstallCommand: "-Rns",
            PackageType: PackageType.PKG_TAR_XZ,
            InstallationType: InstallationType.Binary
        );

        public readonly static Distro AltLinux = new(
            Name: "ALT Linux",
            ID: "altlinux",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "epm",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.RPM,
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages",
            InstallationType: InstallationType.Binary
        );

        public readonly static Distro PCLinuxOS = new(
            Name: "PCLinuxOS",
            ID: "pclinuxos",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.RPM,
            InstallationType: InstallationType.Package,
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages"
        );

        public readonly static Distro Debian = new(
            Name: "Debian",
            ID: "debian", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        // public readonly static Distro ElementaryOS = DistroManager.GetDistro.FromBase("elementary OS", DistroBase.Debian)
        public readonly static Distro ElementaryOS = new(
            Name: "elementary OS",
            ID: "elementary", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro Fedora = new(
            Name: "Fedora",
            ID: "fedora", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "dnf",
            InstallCommand: "install -y",
            UninstallCommand: "remove -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro FreeBSD = new(
            Name: "FreeBSD",
            ID: null, 
            BaseDistro: DistroBase.BSD, 
            PackageManager: "pkg", 
            InstallCommand: "install -y",
            UninstallCommand: "delete -y",
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
            PackageManager: "apt-get", 
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro LinuxMint = new(
            Name: "Linux Mint", 
            ID: "linuxmint",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro OpenSUSE = new(
            Name: "openSUSE",
            ID: "opensuse", 
            BaseDistro: DistroBase.Standalone,
            PackageManager: "zypper",
            InstallCommand: "install -y",
            UninstallCommand: "remove -u",
            PackageType: PackageType.RPM,
            InstallationType: InstallationType.Package,
            Description: "Independent RPM-based distribution utilizing the Zypper package manager and YaST configuration tool."
        );

        public readonly static Distro ParrotOS = new(
            Name: "Parrot OS",
            ID: "parrot",
            BaseDistro: DistroBase.Debian,
            PackageManager: "apt-get",
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro PopOS = new(
            Name: "Pop!_OS", 
            ID: "pop",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );

        public readonly static Distro Ubuntu = new(
            Name: "Ubuntu", 
            ID: "ubuntu",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
            
        );

        public readonly static Distro Unknown = new(
            Name: "Generic Linux",
            ID: "unknown",
            BaseDistro: DistroBase.Unknown,
            PackageManager: "unknown",
            InstallCommand: "unknown",
            UninstallCommand: "unknown",
            PackageType: PackageType.UNKNOWN,
            InstallationType: InstallationType.Binary,
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public readonly static Distro ZorinOS = new(
            Name: "Zorin OS",
            ID: "zorin", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install -y",
            UninstallCommand: "remove --purge -y",
            PackageType: PackageType.DEB,
            InstallationType: InstallationType.Package
        );
    }


}