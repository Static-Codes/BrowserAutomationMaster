using BrowserAutomationMaster.Managers;

namespace MacPackager
{
    internal class Program 
    {
        static void Main(string[] args)
        {
            PlatformManager.SetPlatform();
            var bundleManager = new BundleManager();
            bundleManager.BuildBundle();
            
            // Path to test:
            // "/home/nerdy/repos/BrowserAutomationMaster/src/BrowserAutomationMaster/bin/Release/net8.0/osx-x64/publish/bamm"

        }
    }
}
