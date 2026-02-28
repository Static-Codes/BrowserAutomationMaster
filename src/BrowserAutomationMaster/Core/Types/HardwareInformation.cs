using BrowserAutomationMaster.Core.SystemInfo.RAM;
using Hardware.Info;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.PlatformManager;

namespace BrowserAutomationMaster.Core.Types 
{
    public class HardwareInformation() 
    {
        private static readonly HardwareInfo _HardwareInfo = new();
        public string CpuName { get; set; } = "Not Set";
        public int NumberOfCpu { get; set; } = 1;
        public int CpuCoreCount { get; set; } = 1;
        public int CpuThreadCount { get; set; } = 1;
        public Architecture CurrentArchitecture { get; init; } = RuntimeInformation.OSArchitecture;
        public MemoryInfo? MemoryInfo { get; set; }
        public bool IsSupportedArchitecture() => ValidArchitectures.Contains(CurrentArchitecture);

        public void SetCpuInfo() {
            _HardwareInfo.RefreshCPUList(includePercentProcessorTime: false, includePerformanceCounter: false);

            if (_HardwareInfo.CpuList.Count == 0) {
                throw new Exception("BrowserAutomationMaster.Core.Types.HardwareInformation._HardwareInfo.CpuList.Count returned 0");
            }

            CpuName = _HardwareInfo.CpuList[0].Name;
            NumberOfCpu = _HardwareInfo.CpuList.Count;
            CpuCoreCount = (int)_HardwareInfo.CpuList.Sum(a => a.NumberOfCores);
            CpuThreadCount = Environment.ProcessorCount;
        }
    } 
}