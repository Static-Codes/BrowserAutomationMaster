namespace BrowserAutomationMaster.Managers.Types
{
    public partial class AppSettings
    {
        public required Theme ThemeType { get; set; }
        public bool ShowAppCheck { get; set; }
        public bool ShowCpuCheck { get; set; }
        public bool ShowMemoryCheck { get; set; }
        public bool ShowUpdateCheck { get; set; }
        public bool AutoCopyPath { get; set; }
        public bool RunOnCompile { get; set; }
        public bool UseBrowserstack { get; set; }
    }
}