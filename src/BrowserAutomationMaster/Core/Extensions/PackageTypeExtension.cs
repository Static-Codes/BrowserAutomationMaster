using BrowserAutomationMaster.Core.Types.Linux;

namespace BrowserAutomationMaster.Core.Extensions 
{
    public static class PackageTypeExtension 
    {
        public static string GetPackageFileType(this PackageType packageType) {
            return "." + packageType.ToString().ToLower().Replace("_", ".");
        }
    }
}