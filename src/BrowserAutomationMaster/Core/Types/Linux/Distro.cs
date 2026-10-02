using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using static System.Runtime.InteropServices.Architecture;


namespace BrowserAutomationMaster.Core.Types.Linux 
{
    /// <summary> A Linux distro that BAMM supports. </summary>
    public class Distro(
        string Name,
        WhichDistro[] SupportedDistros,
        IDistroPackageInfo PackageInfo,
        string[] RequiredPackages,
        string[] OptionalPackages,
        InstallationType InstallationType,
        string ShellPath = "/bin/bash",
        string PythonVar = "python3",
        string? BackupReleaseCmd = null,
        string? BackupReleaseCmdArgs = null,
        string? Description = null,
        string[]? DotnetPackages = null
    ) 
    {
        public string Name { get; set; } = Name;
        public IDistroPackageInfo PackageInfo { get; private set; } = PackageInfo;
        public WhichDistro[] SupportedDistros { get; private set; } = SupportedDistros;
        public string[] RequiredPackages { get; private set; } = RequiredPackages;
        public string[] OptionalPackages { get; private set; } = OptionalPackages;
        public InstallationType InstallationType { get; private set; } = InstallationType;
        public DisplayServer DisplayServer { get; private set; } = GetActiveDisplayServer();
        public string ShellPath { get; private set; } = ShellPath;
        public string PythonVar { get; private set; } = PythonVar;
        public string? BackupReleaseCmd { get; private set; } = BackupReleaseCmd;
        public string? BackupReleaseCmdArgs { get; private set; } = BackupReleaseCmdArgs;
        public string? Description { get; private set; } = Description;
        public string[]? DotnetPackages = DotnetPackages;
        
        
        public Architecture[] SupportedArchitectures = [ 
            X64, X86, Arm, Arm, Arm64
        ];

        // The ecosystem the package manager belongs to. 
        // Will report "Unknown" when the distro declares no package manager. 
        public DistroFamily BaseDistro => PackageInfo.PackageManager == WhichDistroSharp.PackageManager.Unknown
            ? DistroFamily.Unknown
            : PackageInfo.Family;

        /// <summary>
        /// The command provided to a terminal that will invoke the package manager, such as: 'apt-get', 'dnf', 'pacman', etc.
        /// </summary>
        ///
        /// <remarks>
        /// Not to be confused with <see cref="IDistroPackageInfo.PackageManager"/>.
        /// This is an enum with each member repesenting the package manager name, such as <c>Apt</c> or <c>Pacman</c>.
        /// 
        /// The two may share the same value, and may differ where the concept and the command have different names.
        /// 
        /// One such example is with Debian; Debian's package manager is <c>Apt</c>, however, the command a user types is <c>apt-get</c>.
        /// </para>
        /// </remarks>
        public string PackageManagerCommand => PackageInfo.PackageManagerCommand;
        
        public string InstallCommand => PackageInfo.InstallCommand;
        public string UninstallCommand => PackageInfo.UninstallCommand;
        public string QueryCommand => PackageInfo.QueryCommand;
        public string QueryArguments => PackageInfo.QueryArguments;

        // A null value here means the query's exit code is used for determining the installation status of a package.
        public string? InstallationKeyword => string.IsNullOrEmpty(PackageInfo.InstallKeyword) ? null : PackageInfo.InstallKeyword;

        public PackageType PackageType => PackageInfo.PackageType;

        /// <summary>
        /// This reference check allows the Unit Test Suite to validate coverage for Distros registered through WhichDistroSharp.
        /// </summary>
        public bool Is(Distro? other) => ReferenceEquals(this, other);

        public bool UsingX11() => DisplayServer == DisplayServer.X11;
        public bool UsingWayland() => DisplayServer == DisplayServer.Wayland;

        public override string ToString()
        {
            var platform = GlobalUserInfo.PlatformInfo.CurrentPlatform;

            return string.Join(NLC, [
                $"Distribution Name: {Name}",
                $"Distribution Base: {BaseDistro}",
                $"Detected Distro: {platform?.PrettyName ?? "Not Detected"}",
                $"Detected ID: {(string.IsNullOrWhiteSpace(platform?.Id) ? "Not Detected" : platform.Id)}",
                $"Detected Version: {(string.IsNullOrWhiteSpace(platform?.VersionId) ? "Not Detected" : platform.VersionId)}",
                $"Package Manager: {PackageManagerCommand}",
                $"Install Command: {PackageManagerCommand} {InstallCommand}",
                $"Uninstall Command: {PackageManagerCommand} {UninstallCommand}",
                $"Query Command: {QueryCommand} {QueryArguments}",
                $"Package Type: {PackageType.GetPackageFileType()}",
                $"Shell Path: {ShellPath}",
            ]);
        }

        public static DisplayServer GetActiveDisplayServer() 
        {
            DisplayServer potentialServer = DisplayServer.None;

            if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null) {
                return DisplayServer.Wayland;
            }

            var xdgSessionType = Environment.GetEnvironmentVariable("XDG_SESSION_TYPE");
            var desktopSession = Environment.GetEnvironmentVariable("DESKTOP_SESSION");
            
            if (xdgSessionType != null) {
                potentialServer = ParseXDGSessionType(xdgSessionType);
            }

            else if (desktopSession != null) {
                potentialServer = ParseDesktopSessionType(desktopSession);
            }

            return potentialServer;
        }

        private static DisplayServer ParseXDGSessionType(string xdgSessionType) 
        {
            return xdgSessionType switch {
                "x11" => DisplayServer.X11,
                "wayland" => DisplayServer.Wayland,
                _ => DisplayServer.None,
            };
        } 

        private static DisplayServer ParseDesktopSessionType(string desktopSession) 
        {
            return desktopSession switch {
                "gnome-xorg" => DisplayServer.X11,
                "gnome-wayland" => DisplayServer.Wayland,
                _ => DisplayServer.None,
            };
        } 
    }
}
