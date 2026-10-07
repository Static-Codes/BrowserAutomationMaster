using BrowserAutomationMaster.Core.Types.Linux;

namespace BrowserAutomationMaster.Core.Types 
{
    public class PlatformInfo()
    {
        public bool IsPiDevice { get; set; } // Raspberry Pi
        public bool IsWindows { get; set; }
        public bool IsMacOS { get; set; }
        public bool IsLinux { get; set; }
        public bool IsUnixLike { get; set; } // Linux + OSX

        public Distro? CurrentDistribution = null;

        /// <summary>
        /// The raw os-release fields for the detected distro that are resolved using WhichDistroSharp
        /// They are populated during the detection phase; This is a one pass process, whereas re-parsing requires an additional pass.
        /// </summary>
        public IPlatform? CurrentPlatform = null;

        public KeyValuePair<string, bool>? RaspiModelInfo { get; set; }

        public string GetRaspiModelName() {
            return (IsPiDevice && RaspiModelInfo != null) ? RaspiModelInfo.Value.Key : "N/A";
        }

        
        public void SetRaspiModel(string Name, bool SupportsGUI)
        {
            if (!IsPiDevice) { return; }
            RaspiModelInfo = new(Name, SupportsGUI);
        }

    }
}