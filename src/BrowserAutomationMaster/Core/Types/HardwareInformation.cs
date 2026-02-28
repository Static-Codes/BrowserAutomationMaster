using BrowserAutomationMaster.Core.SystemInfo.RAM;
using Hardware.Info;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.PlatformManager;

namespace BrowserAutomationMaster.Core.Types 
{
    public class HardwareInformation() 
    {

        private readonly HardwareInfo _HardwareInfo = new();
        public string CpuName { get; set; } = "Not Set";
        public uint CpuCoreCount { get; set; } = 0;
        public uint CpuThreadCount { get; set; } = 0;
        public Architecture CurrentArchitecture { get; init; } = RuntimeInformation.OSArchitecture;
        public MemoryInfo? MemoryInfo { get; set; }
        public bool IsSupportedArchitecture() => ValidArchitectures.Contains(CurrentArchitecture);
    } 
}