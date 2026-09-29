using BrowserAutomationMaster.Core.Common;
using System.Runtime.CompilerServices;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// The application detects the host platform in ProgramFunctions.InitializeAsync via
    /// PlatformManager.SetPlatform. Tests exercise types such as Parser and DirectoryManager
    /// directly, so without this initializer their PlatformInfo flags stay false and
    /// GetAppDataDirectory() throws PlatformNotSupportedException.
    /// A module initializer runs once before any test in the assembly executes.
    /// </summary>
    internal static class PlatformInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PlatformManager.SetPlatform(GlobalUserInfo);
        }
    }
}
