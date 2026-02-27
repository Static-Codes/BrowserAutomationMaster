using static BrowserAutomationMaster.Core.Utilities.LibraryUtility;

namespace BrowserAutomationMaster.Resources.CpuInfoSharp
{

    public static class Loader
    {
        private static readonly string basePattern = "BrowserAutomationMaster.Resources.CpuInfoSharp.runtimes";
        private static readonly string libName = "libcpuinfo";

        /// <summary>
        /// Loads and initializes the runtime for pytorch's cpuinfo.
        public static async Task InitializeCpuInfo() 
        {
            await Load(basePattern, libName, CpuInfoFunctions.ResolvedName);
            CpuInfoWrappers.Initialize();
        }  
    }

}