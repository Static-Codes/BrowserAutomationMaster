using BrowserAutomationMaster.Core.Common;
using System.Runtime.CompilerServices;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// BAMM detects the host platform in ProgramFunctions.InitializeAsync via PlatformManager.SetPlatform. <br/>
    /// Without initialization, the PlatformInfo flags stay false. <br/>
    /// This will cause GetAppDataDirectory() to throw a PlatformNotSupportedException.
    /// </summary>
    internal static class PlatformInitializer
    {
        [ModuleInitializer]
        internal static void Initialize() {
            PlatformManager.SetPlatform(GlobalUserInfo);
        }
    }
}
