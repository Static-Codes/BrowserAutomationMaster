using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Managers.AppManager.OS.Linux 
{

    public enum DistroBase
    {
        ArchLinux,
        BSD,
        Debian,
        Fedora,
        Unknown,
    }

    public class Distro(
        string Name,
        string? ID, 
        DistroBase BaseDistro, 
        string PackageManager,
        string InstallCommand
    ) 
    {
        public string Name { get; set; } = Name;
        public string? ID { get; set; } = ID;
        public DistroBase BaseDistro { get; set; } = BaseDistro;
        public string PackageManager { get; set; } = PackageManager;
        public string InstallCommand { get; set; } = InstallCommand;
        public Architecture[] SupportedArchitectures = [ 
            X64, X86, Arm, Arm, Arm64
        ];
    }


    // Ensure DistroManager.DetermineDistroFromID() is updated when a new distro is added.
    public class Distros 
    {
        public static Distro ArchLinux = new(
            Name: "Arch Linux", 
            ID: "arch",
            BaseDistro: DistroBase.ArchLinux, 
            PackageManager: "pacman", 
            InstallCommand: "-S"
        );

        public static Distro Debian = new(
            Name: "Debian",
            ID: "debian", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );

        public static Distro ElmentaryOS = new(
            Name: "elementary OS",
            ID: "elementary", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );
        
        // Free BSD doesn't use /etc/os-release they opt for uname or /etc/motd
        public static Distro FreeBSD = new(
            Name: "FreeBSD",
            ID: null, 
            BaseDistro: DistroBase.BSD, 
            PackageManager: "pkg", 
            InstallCommand: "install"
        );

        public static Distro KaliLinux = new(
            Name: "Kali Linux",
            ID: "kali",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );

        public static Distro LinuxMint = new(
            Name: "Linux Mint", 
            ID: "linuxmint",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );

        public static Distro ParrotOS = new(
            Name: "Parrot OS",
            ID: "parrot",
            BaseDistro: DistroBase.Debian,
            PackageManager: "apt-get",
            InstallCommand: "install"
        );

        public static Distro PopOS = new(
            Name: "Pop!_OS", 
            ID: "pop",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );

        public static Distro Ubuntu = new(
            Name: "Ubuntu", 
            ID: "ubuntu",
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
            
        );

        public static Distro Unknown = new(
            Name: "Generic Linux",
            ID: null,
            BaseDistro: DistroBase.Unknown,
            PackageManager: "unknown",
            InstallCommand: "unknown"
        );

        public static Distro ZorinOS = new(
            Name: "Zorin OS",
            ID: "zorin", 
            BaseDistro: DistroBase.Debian, 
            PackageManager: "apt-get", 
            InstallCommand: "install"
        );
    }


}