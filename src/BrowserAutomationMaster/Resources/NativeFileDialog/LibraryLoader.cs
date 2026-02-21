using BrowserAutomationMaster.Core.Types;
using System.Reflection;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Helpers.EmbeddedResourceHelper;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Types.LibraryInfo;

namespace BrowserAutomationMaster.Resources.NativeFileDialog
{

    public static class LibraryLoader 
    {
        private static string? libName = null;
        private static string basePattern = "BrowserAutomationMaster.Resources.NativeFileDialog.runtimes";
        
        // <summary>
        // Writes the appropriate NativeFileDialog library to disk.
        // </summary>

        public static async Task InitializeNativeFileDialog()
        {
            string resourcePattern;

            if (Platforms.IsWindows)
            {
                libName = "nfd.dll";
                resourcePattern = $"{basePattern}.win_x64.nfd.dll";
            }
            else if (Platforms.IsMacOS)
            {
                libName = "libnfd.dylib";
                resourcePattern = $"{basePattern}.osx_x64.libnfd.dylib";
            }
            else if (Platforms.IsLinux)
            {
                libName = "libnfd.so";
                resourcePattern = $"{basePattern}.linux_x64.libnfd.so";
            }
            else
            {
                WriteAndExit(
                    $"[ERROR]: No OS detected for NFD injection, please ensure SetPlatforms is working.", 
                    status: 1
                );
                return;
            }

            var optionalChecks = new Dictionary<string, bool[]>
            {
                // This checks if the file already exists and/or has permissions issues.
                { "File Access Check", [ !File.Exists(libName) || IsFileLocked(libName) == false ] }
            };

            // This will only write the resource to disk if either of the above checks return null.
            await WriteEmbeddedResourceToDisk(
                resourceName: libName,
                resourcePattern: resourcePattern,
                outputPath: Path.Combine(Directory.GetCurrentDirectory(), libName),
                optionalChecks
            );
        }

        private static bool IsFileLocked(string filePath)
        {
            try { 
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None); 
            } 

            catch (IOException) { 
                return true; 
            }

            return false;
        }
    }
}
