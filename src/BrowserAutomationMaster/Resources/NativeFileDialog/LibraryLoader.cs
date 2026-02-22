using System.Reflection;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Helpers.EmbeddedResourceHelper;
using static BrowserAutomationMaster.Core.Messaging.Errors;

namespace BrowserAutomationMaster.Resources.NativeFileDialog
{

    public static class LibraryLoader 
    {
        private static string? libName = null;
        private static readonly string basePattern = "BrowserAutomationMaster.Resources.NativeFileDialog.runtimes";

        public static void RegisterResolver(string extractedPath)
        {
            NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), (libraryName, assembly, searchPath) =>
            {
                if (libraryName == "nfd") {
                    return NativeLibrary.Load(extractedPath);
                }
                throw new Exception($"Invalid libraryName passed to RegisterResolver, expected 'nfd', received '{libraryName}'");
            });
        }

        // <summary>
        // Writes the appropriate NativeFileDialog library to a temp file on disk.
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
            var tempPath = Path.GetTempFileName();
            await WriteEmbeddedResourceToDisk(
                resourceName: libName,
                resourcePattern: resourcePattern,
                outputPath: tempPath,
                optionalChecks
            );

            RegisterResolver(tempPath);
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
