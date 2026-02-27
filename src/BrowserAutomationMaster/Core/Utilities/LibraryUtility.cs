using BrowserAutomationMaster.Core.Messaging;
using System.Reflection;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Helpers.EmbeddedResourceHelper;
using static System.Runtime.InteropServices.Architecture;

namespace BrowserAutomationMaster.Core.Utilities 
{
    public class LibraryUtility 
    {

        private static readonly Dictionary<string, string> libPaths = [];
        private static bool isResolverRegistered = false;

        public static bool FileIsLocked(string filePath)
        {
            try { 
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None); 
            } 

            catch (IOException) { 
                return true; 
            }

            return false;
        }

        public static string GetFullLibraryFileName(string libName) {

            var extension = (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch
            {
                (true, _, _) => "dll",
                (_, true, _) => "dylib",
                (_, _, true) => "so",
                _ => throw new PlatformNotSupportedException(
                    "No supported OS found in GetFullLibraryFileName(), Please ensure SetPlatforms() was successfully executed."
                )
            };
            return $"{libName}.{extension}";
        }

        public static string GetRID() 
        {
            var arch = Platforms.CurrentArchitecture switch {
                X64 => "x64",
                Arm64 => "arm64",
                Arm => "arm",
                _ => throw new PlatformNotSupportedException(
                    "Unsupported architecture found in GetRID()"
                )
            };

            var platform = (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch {
                (true, _, _) => "win",
                (_, true, _) => "osx",
                (_, _, true) => "linux",
                _ => throw new PlatformNotSupportedException(
                    "No supported OS found in GetRID(), Please ensure SetPlatforms() was successfully executed."
                )
            };

            // The underscore is chosen as MSBuild maps - to _
            return $"{platform}_{arch}";
        }

        /// <summary>
        /// Resolves the resource path of the library, writes it to disk, then registers it.
        /// </summary>
        /// <param name="basePattern">
        ///     The pattern that will be used to resolve the base directory of the library. <br/>
        ///     This includes everything before ".runtimes.RID.libname"
        /// </param>
        /// <param name="libName">
        ///     The name of the library to resolve and load. <br/>
        ///     This must match the runtime filename exactly without the extension.
        /// </param>
        /// <param name="resolvedName">
        ///     This is the name the resolver will attempt to find and register. <br/>
        ///     Do not include a file extension.<br/>
        ///     Note: <br/>
        ///     This may be the same or different than libName depending on the naming conventions used for the runtimes.
        /// </param>
        public static async Task Load(string basePattern, string libName, string resolvedName) 
        {
            var fileName = GetFullLibraryFileName(libName);

            var resourcePattern = $"{basePattern}.{GetRID()}.{fileName}";

            // This will only write the resource to disk if either of the above checks return null.
            var tempPath = Path.GetTempFileName();
            await WriteEmbeddedResourceToDisk(
                resourceName: libName,
                resourcePattern: resourcePattern,
                outputPath: tempPath
            );

            // This will only write the resource to disk if either of the above checks return null
            RegisterLibrary(resolvedName, tempPath);
        }

        public static void RegisterLibrary(string libName, string libPath) 
        {
            if (libPaths.ContainsKey(libName)) {
                throw new InvalidOperationException($"Library '{libName}' is already registered.");
            }

            try 
            {
                libPaths[libName] = libPath;

                if (!isResolverRegistered) {
                    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), Resolver);
                    isResolverRegistered = true;
                }
            }
            catch (Exception ex) 
            {
                Console.Write(ex.StackTrace);
                Environment.Exit(1);
            }
        }
        
        private static IntPtr Resolver(string libName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            var libraryResolved = libPaths.TryGetValue(libName, out var foundPath);

            if (!libraryResolved || foundPath == null) {
                Errors.Write($"Unable to resolve library '{libName}'");
                return IntPtr.Zero;
            }

            if (FileIsLocked(foundPath)) {
                Errors.Write($"Unable to resolve library '{libName}', file is inaccessible.");
                return IntPtr.Zero;
            }
            try {
                return NativeLibrary.Load(foundPath);
            }

            catch (Exception ex) 
            {
                Errors.Write(
                    string.Join(NLC, [
                        $"Unable to resolve library '{libName}'",
                        "Error Log:",
                        ex.Message
                    ])
                );
            }
            return IntPtr.Zero;
        }
    }
}