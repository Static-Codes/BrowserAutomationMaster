using System.Runtime.InteropServices;
using BrowserAutomationMaster.Core.SystemInfo.RAM;
using static BrowserAutomationMaster.Core.Common.PlatformManager;

namespace BrowserAutomationMaster.Core.Types 
{
    public class HardwareInformation() 
    {
        public string CpuName { get; set; } = "Not Set";
        public uint CpuCoreCount { get; set; } = 0;
        public uint CpuThreadCount { get; set; } = 0;
        public Architecture CurrentArchitecture { get; init; } = RuntimeInformation.OSArchitecture;
        public MemoryInfo? MemoryInfo { get; set; }
        public bool IsSupportedArchitecture() => ValidArchitectures.Contains(CurrentArchitecture);
    } 
}