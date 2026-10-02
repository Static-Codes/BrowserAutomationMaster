using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BrowserAutomationMaster.Core.SystemInfo.OS.Unix 
{

    [UnsupportedOSPlatform("windows")]
    public partial class UnixFilePermissions() 
    {
        // Apple's libc supports both Utf8 and Utf16 but Linux's Glibc only supports Utf8
        // Utf8 is chosen for cross compatability. 
        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        
        // access Function Docs
        // https://pubs.opengroup.org/onlinepubs/009695399/functions/access.html
        // MacOSX.sdk is a symlink to the latest MacOSX SDK, this provides a compile time constant per rosyln's requirements for DllImport.
        private static partial int access(string path, int amode);

        // Apple's libc supports both Utf8 and Utf16 but Linux's Glibc only supports Utf8
        // Utf8 is chosen for cross compatability. 
        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        
        // Direct chmod execution as opposed to spawning an additional process object.
        // If successful, chmod() returns 0.
        // If unsuccessful, chmod() returns -1
        // https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-chmod-change-mode-file-directory#rtchm
        private static partial int chmod(string pathname, UInt32 mode);

        // X_OK is a Bitmask for the libc "Execute" permission, where:
        // 1 = Permission Denied
        // 0 = Permission Granted
        // X_OK Usage + Docs
        // https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-access-determine-whether-file-can-be-accessed
        private const int X_OK = 1;
        

        // Search permission (for a directory) or execute permission (for a file) for the file owner.
        // Docs: https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-chmod-change-mode-file-directory#rtchm
        
        // Since C# interprets Octals as Decimals, 0755 must be written as it's hex representation.
        // Can be written as Convert.ToInt32("0755", 8) aswell
        private const UInt32 READ_WRITE_EXECUTE_MODE = 0x1ED;

        public static bool HasExecutablePermissions(string filePath) {
            return access(filePath, X_OK) == 0;
        }
        
        public static bool SetExecutablePermissions(string filePath) {
            return chmod(filePath, READ_WRITE_EXECUTE_MODE) == 0;
        }
    }
}