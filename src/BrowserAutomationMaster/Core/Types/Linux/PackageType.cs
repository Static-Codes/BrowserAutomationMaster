namespace BrowserAutomationMaster.Core.Types.Linux 
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

}