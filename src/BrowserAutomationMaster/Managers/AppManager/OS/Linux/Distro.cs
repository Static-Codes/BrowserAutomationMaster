using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux 
{

    public enum PackageType {
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
            return "." + packageType.ToString().ToLower();
        }
    }

    public enum DistroBase
    {
        ArchLinux,
        BSD,
        Debian,
        Fedora,
        Standalone,
        Unknown,
    }

    public class Distro(
        string Name,
        string? ID, 
        DistroBase BaseDistro, 
        string PackageManager,
        string InstallCommand,
        PackageType PackageType,
        string ShellPath = "/bin/bash",
        string ReleaseFilePath = "/etc/os-release",
        string ReleaseIdentifier = "=",
        string? BackupReleaseCmd = null,
        string? BackupReleaseCmdArgs = null,
        string? Description = null
        
    ) 
    {
        public string Name { get; private set; } = Name;
        public string? ID { get; private set; } = ID;
        public DistroBase BaseDistro { get; private set; } = BaseDistro;
        public string PackageManager { get; private set; } = PackageManager;
        public string InstallCommand { get; private set; } = InstallCommand;
        public PackageType PackageType { get; private set; } = PackageType;
        public string ShellPath { get; private set; } = ShellPath;
        public string ReleaseFilePath { get; private set; } = ReleaseFilePath;
        public string ReleaseIdentifier { get; private set; } = ReleaseIdentifier;
        public string? BackupReleaseCmd { get; private set; } = BackupReleaseCmd;
        public string? BackupReleaseCmdArgs { get; private set; } = BackupReleaseCmdArgs;
        public string? Description { get; private set; } = Description;
        public Architecture[] SupportedArchitectures = [ 
            X64, X86, Arm, Arm, Arm64
        ];
    }


    // Ensure DistroManager.DetermineDistro() is updated when a new distro is added.
    public class Distros 
    {
        public static Distro ArchLinux = new(
            Name: "Arch Linux", 
            ID: "arch",
            BaseDistro: DistroBase.ArchLinux, 
            PackageManager: "pacman", 
            InstallCommand: "-S",
            PackageType: PackageType.PKG_TAR_XZ
        );

        public static Distro AltLinux = new(
            Name: "ALT Linux",
            ID: "altlinux",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "epm",
            InstallCommand: "install",
            PackageType: PackageType.RPM,
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages"
        );

        public static Distro PCLinuxOS = new(
            Name: "PCLinuxOS",
            ID: "pclinuxos",
            BaseDistro: DistroBase.Standalone,
            PackageManager: "apt-get",
            InstallCommand: "install",
            PackageType: PackageType.RPM,
            Description: "A standalone linux distro utilizing apt-get but instead of .deb it uses .rpm Packages"
        );

        public static Distro Debian = new(
            Name: "Debian",
            ID: "debian", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get",
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );

        public static Distro ElementaryOS = new(
            Name: "elementary OS",
            ID: "elementary", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );
        
        public static Distro FreeBSD = new(
            Name: "FreeBSD",
            ID: null, 
            BaseDistro: DistroBase.BSD, 
            PackageManager: "pkg", 
            InstallCommand: "install",
            PackageType: PackageType.PKG,
            ReleaseIdentifier: "freebsd",
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public static Distro KaliLinux = new(
            Name: "Kali Linux",
            ID: "kali",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );

        public static Distro LinuxMint = new(
            Name: "Linux Mint", 
            ID: "linuxmint",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );

        public static Distro ParrotOS = new(
            Name: "Parrot OS",
            ID: "parrot",
            BaseDistro: DistroBase.Debian,
            PackageManager: "apt-get",
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );

        public static Distro PopOS = new(
            Name: "Pop!_OS", 
            ID: "pop",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );

        public static Distro Ubuntu = new(
            Name: "Ubuntu", 
            ID: "ubuntu",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
            
        );

        public static Distro Unknown = new(
            Name: "Generic Linux",
            ID: "unknown",
            BaseDistro: DistroBase.Unknown,
            PackageManager: "unknown",
            InstallCommand: "unknown",
            PackageType: PackageType.UNKNOWN,
            BackupReleaseCmd: "uname",
            BackupReleaseCmdArgs: "-o"
        );

        public static Distro ZorinOS = new(
            Name: "Zorin OS",
            ID: "zorin", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install",
            PackageType: PackageType.DEB
        );
    }


}