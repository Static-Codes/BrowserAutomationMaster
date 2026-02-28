using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Resources.CpuInfoSharp.CpuInfoWrappers;

namespace BrowserAutomationMaster.Resources.CpuInfoSharp 
{
    public static partial class CpuInfoFunctions
    {
        public const string ResolvedName = "cpuinfo";

        [LibraryImport(ResolvedName, EntryPoint = "cpuinfo_initialize")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        [return: MarshalAs(UnmanagedType.U1)] // Bools are returned as unsigned 1 bit ints
        public static partial bool cpuinfo_initialize(); // Is Initialize
        
        [LibraryImport(ResolvedName, EntryPoint = "cpuinfo_get_processors_count")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial uint cpuinfo_get_processors_count(); // Is GetThreadCount

        [LibraryImport(ResolvedName, EntryPoint = "cpuinfo_get_cores")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static partial IntPtr cpuinfo_get_cores(); // Used in GetCoreCount

        [DllImport(ResolvedName, EntryPoint = "cpuinfo_get_packages")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]

        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(cpuinfo_package))]
        public static extern IntPtr cpuinfo_get_packages(); // Used in GetCoreCount

    }
}