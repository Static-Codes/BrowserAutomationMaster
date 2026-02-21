using BrowserAutomationMaster.Core.OS.Unix.Linux;
using BrowserAutomationMaster.Core.Python;
using BrowserAutomationMaster.Core.Messaging;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static System.Runtime.InteropServices.Architecture;


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
        public Architecture CurrentArchitecture { get; private set; } = RuntimeInformation.OSArchitecture;
        public KeyValuePair<string, bool>? RaspiModelInfo { get; set; }

        public string GetRaspiModelName()
        {
            if (!IsRaspi) {
                return string.Empty;
            }
            
            if (RaspiModelInfo == null) {
                return string.Empty;
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