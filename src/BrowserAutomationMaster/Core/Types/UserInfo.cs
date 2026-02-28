namespace BrowserAutomationMaster.Core.Types 
{
    public class UserInfo 
    {
        public HardwareInformation HardwareInformation { get; set; } = new();
        public PlatformInfo PlatformInfo { get; set; } = new PlatformInfo();

    }

}