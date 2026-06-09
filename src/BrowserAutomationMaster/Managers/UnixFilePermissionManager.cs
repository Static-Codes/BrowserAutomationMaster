// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace BrowserAutomationMaster.Managers 
{

    [UnsupportedOSPlatform("windows")]
    public partial class UnixFilePermissionManager() 
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