using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.Messaging;

namespace BrowserAutomationMaster.Core.SystemInfo.RAM
{
    public struct MemoryInfo
    {
        public required double? TotalMemory { get; set; }
        public required double? UsedMemory { get; set; }
        public required double? FreeMemory { get; set; }
        public required double? UsedPercent { get; set; }
        public required double? FreePercent { get; set; }
    }
}