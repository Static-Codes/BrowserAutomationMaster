using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using BrowserAutomationMaster.Core.Types.Linux;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.PlatformManager;

namespace BrowserAutomationMaster.Core.Types 
{
    public class PlatformInfo()
    {
        public bool IsARMel { get; set; } // 32 Bit ARMv7 (el = EABI Little Endian)
        public bool IsARMhf { get; set; } // 32 Bit ARMv7 (hf = Hard Float)
        public bool IsChromeOS { get; set; }
        public bool IsRaspi { get; set; } // Raspberry Pi
        public bool IsWindows { get; set; }
        public bool IsMacOS { get; set; }
        public bool IsLinux { get; set; }
        public bool IsUnixLike { get; set; } // Linux + OSX

        public Distro? CurrentDistribution = null;
        public KeyValuePair<string, bool>? RaspiModelInfo { get; set; }

        public string GetRaspiModelName()
        {
            if (!IsRaspi) {
                return "N/A";
            }
            
            if (RaspiModelInfo == null) {
                return "N/A";
            }
            
            return RaspiModelInfo.Value.Key;
        }

        
        public void SetRaspiModel(string Name, bool SupportsGUI)
        {
            if (!IsRaspi) {
                return;
            }
            RaspiModelInfo = new(Name, SupportsGUI);
        }

    }
}