using BrowserAutomationMaster.Core.SystemInfo.RAM;
using Hardware.Info;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.PlatformManager;

namespace BrowserAutomationMaster.Core.Types 
{
    public class HardwareInformation() 
    {

        private readonly HardwareInfo _HardwareInfo = new();
        // private void SetCpuInfo() {
        //     _HardwareInfo.RefreshCPUList(includePercentProcessorTime: false, includePerformanceCounter: false);
        //     NumberOfCpu = _HardwareInfo.CpuList.Count;
        //     CpuCoreCount = _HardwareInfo.CpuList.Sum(a => a.CpuCoreList.Count);

        // };

        public string CpuName { get; set; } = "Not Set";
        public int NumberOfCpu { get; set; } = 1;
        public int CpuCoreCount { get; set; } = 1;
        public int CpuThreadCount { get; set; } = 1;
        public Architecture CurrentArchitecture { get; init; } = RuntimeInformation.OSArchitecture;
        public MemoryInfo? MemoryInfo { get; set; }
        public bool IsSupportedArchitecture() => ValidArchitectures.Contains(CurrentArchitecture);
    } 
}