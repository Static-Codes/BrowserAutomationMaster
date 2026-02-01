using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BrowserAutomationMaster.Managers 
{

    [UnsupportedOSPlatform("windows")]
    public partial class UnixFilePermissionManager() 
    {
        
        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        // access Function Docs
        // https://pubs.opengroup.org/onlinepubs/009695399/functions/access.html
        // MacOSX.sdk is a symlink to the latest MacOSX SDK, this provides a compile time constant per rosyln's requirements for DllImport.
        private static partial int access(string path, int amode);


        [LibraryImport("libc", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        // Direct chmod execution as opposed to spawning an additional process object.
        // If successful, chmod() returns 0.
        // If unsuccessful, chmod() returns -1
        // https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-chmod-change-mode-file-directory#rtchm
        private static partial int chmod(string pathname, uint mode);

        // X_OK is a Bitmask for the libc "Execute" permission, where:
        // 1 = Permission Denied
        // 0 = Permission Granted
        // X_OK Usage + Docs
        // https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-access-determine-whether-file-can-be-accessed
        private const int X_OK = 1;
        

        // Search permission (for a directory) or execute permission (for a file) for the file owner.
        // Docs: https://www.ibm.com/docs/en/zos/2.1.0?topic=functions-chmod-change-mode-file-directory#rtchm
        
        // Since C# interprets Octals as Decimals, 0755 must be written as it's hex representation.
        private const uint READ_WRITE_EXECUTE_MODE = 0x1ED;

        public static bool HasExecutablePermissions(string filePath) {
            return access(filePath, X_OK) == 0;
        }
        
        public static bool SetExecutablePermissions(string filePath) {
            return chmod(filePath, READ_WRITE_EXECUTE_MODE) == 0;
        }
    }
}