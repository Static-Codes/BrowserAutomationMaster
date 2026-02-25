using BrowserAutomationMaster.Core.Extensions;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.Constants;
using static System.Runtime.InteropServices.Architecture;


namespace BrowserAutomationMaster.Core.Types.Linux 
{
    public class Distro(
        string Name,
        string? ID, 
        DistroBase BaseDistro, 
        string PackageManager,
        string InstallCommand,
        string UninstallCommand,
        string QueryCommand,
        string QueryArguments,
        string[] RequiredPackages,
        string[] OptionalPackages,
        PackageType PackageType,
        InstallationType InstallationType,
        string ShellPath = "/bin/bash",
        string PythonVar = "python3",
        string ReleaseFilePath = "/etc/os-release",
        string ReleaseIdentifier = "=",
        string? BackupReleaseCmd = null,
        string? BackupReleaseCmdArgs = null,
        string? Description = null,
        string? InstallationKeyword = null,
        string[]? DotnetPackages = null
    ) 
    {
        public string Name { get; set; } = Name;
        public string? ID { get; private set; } = ID;
        public DistroBase BaseDistro { get; private set; } = BaseDistro;
        public string PackageManager { get; private set; } = PackageManager;
        public string InstallCommand { get; private set; } = InstallCommand;
        public string UninstallCommand { get; private set; } = UninstallCommand;
        public string QueryCommand { get; private set; } = QueryCommand;
        public string QueryArguments { get; private set; } = QueryArguments;
        public string? InstallationKeyword { get; private set; } = InstallationKeyword;
        public string[] RequiredPackages { get; private set; } = RequiredPackages;
        public string[] OptionalPackages { get; private set; } = OptionalPackages;
        public PackageType PackageType { get; private set; } = PackageType;
        public InstallationType InstallationType { get; private set; } = InstallationType;
        public DisplayServer DisplayServer { get; private set; } = GetActiveDisplayServer();
        public string ShellPath { get; private set; } = ShellPath;
        public string PythonVar { get; private set; } = PythonVar;
        public string ReleaseFilePath { get; private set; } = ReleaseFilePath;
        public string ReleaseIdentifier { get; private set; } = ReleaseIdentifier;
        public string? BackupReleaseCmd { get; private set; } = BackupReleaseCmd;
        public string? BackupReleaseCmdArgs { get; private set; } = BackupReleaseCmdArgs;
        public string? Description { get; private set; } = Description;
        public string[]? DotnetPackages = DotnetPackages;
        
        
        public Architecture[] SupportedArchitectures = [ 
            X64, X86, Arm, Arm, Arm64
        ];

        public bool UsingX11() => DisplayServer == DisplayServer.X11;
        public bool UsingWayland() => DisplayServer == DisplayServer.Wayland;

        public override string ToString()
        {
            return string.Join(NLC, [
                $"Distribution Name: {Name}",
                $"Distribution Base: {BaseDistro}",
                $"Package Manager: {PackageManager}",
                $"Install Command: {PackageManager} {InstallCommand}",
                $"Package Type: {PackageType.GetPackageFileType()}",
                $"Shell Path: {ShellPath}",
                $"Release File: {ReleaseFilePath}",
            ]);
        }

        public static DisplayServer GetActiveDisplayServer() 
        {
            DisplayServer potentialServer = DisplayServer.None;

            if (Environment.GetEnvironmentVariable("$WAYLAND_DISPLAY") != null) {
                return DisplayServer.Wayland;
            }


            if (Environment.GetEnvironmentVariable("XDG_SESSION_TYPE") != null) {
                potentialServer = ParseXDGSessionType();
            }

            else if (Environment.GetEnvironmentVariable("DESKTOP_SESSION") != null) {
                potentialServer = ParseDesktopSessionType();
            }

            return potentialServer;
        }

        private static DisplayServer ParseXDGSessionType() 
        {
            var data = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            return data switch {
                "x11" => DisplayServer.X11,
                "wayland" => DisplayServer.Wayland,
                _ => DisplayServer.None,
            };
        } 

        private static DisplayServer ParseDesktopSessionType() 
        {
            var data = Environment.GetEnvironmentVariable("DESKTOP_SESSION");
            return data switch {
                "gnome-xorg" => DisplayServer.X11,
                "gnome-wayland" => DisplayServer.Wayland,
                _ => DisplayServer.None,
            };
        } 
    }
}