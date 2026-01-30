using static System.Runtime.InteropServices.Architecture;

namespace Publisher
{
    class PlatformSelection() 
    {
        public class PlatformOption(Architecture Architecture, OSName OperatingSystem) 
        {
            Architecture Architecture = Architecture;
            OSName OperatingSystem = OperatingSystem;
        }

        public enum OSName 
        {
            Windows,
            macOS,
            ArchLinux,
            Debian,
            Fedora,
            //BSD,
            //Gentoo,
        }
    }

}