using static BrowserAutomationMaster.Managers.UpdateManager;

namespace Publisher.Build 
{
    class BuildInfo 
    {
        public readonly static string AppName = "bamm";
        public readonly static string AppVersion = CurrentVersion[1..];  // Removes the leading "v" in the version tag.
        public readonly static string AppDescription = "A English-like scripting language for Selenium Automation that compiles into Python 3.9+ code.";
        public readonly static string AppExtendedDescription = "BAM Manager (BAMM) is a Dynamic Scripting Language (DSL) that simplifies the process of writing automation tests in Selenium using Python 3.9+";
        public readonly static string AppLicenseType = "MIT";
        
    }
}