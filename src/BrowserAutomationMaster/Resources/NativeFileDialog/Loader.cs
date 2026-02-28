using System.Runtime.InteropServices;
using BrowserAutomationMaster.Core.Messaging;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Utilities.LibraryUtility;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;

namespace BrowserAutomationMaster.Resources.NativeFileDialog
{

    public static class Loader 
    {
        private static readonly string basePattern = "BrowserAutomationMaster.Resources.NativeFileDialog.runtimes";

        private static readonly Architecture[] supportedArchitectures = ValidArchitectures[1..]; // This returns X64 and ARM64
        
        public static bool NFDIsCallable() => supportedArchitectures.Contains(GlobalUserInfo.HardwareInformation.CurrentArchitecture);

        // <summary>
        // Writes the appropriate NativeFileDialog library to a temp file on disk.
        // </summary>

        public static async Task InitializeNativeFileDialog()
        {
            if (!NFDIsCallable()) 
            {
                Warning.Write(
                    string.Join(NLC, [
                        "A non fatal exception occured:",
                        NLC,
                        "Error Log:",
                        "Unable to load NativeFileDialog library for the current architecture."
                    ])
                );
                return;
            }

            var libName =  (GlobalUserInfo.PlatformInfo.IsWindows, GlobalUserInfo.PlatformInfo.IsMacOS, GlobalUserInfo.PlatformInfo.IsLinux) switch {
                (true, _, _) => "nfd",
                (_, true, _) => "libnfd",
                (_, _, true) => "libnfd",
                _ => throw new PlatformNotSupportedException(
                    "No supported OS found in InitializeNativeFileDialog(), Please ensure SetPlatforms() was successfully executed."
                )
            };

            await Load(basePattern, libName, NativeFunctions.ResolvedName);
        }

    }
}
