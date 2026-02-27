using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Resources.CpuInfoSharp.CpuInfoFunctions;

namespace BrowserAutomationMaster.Resources.CpuInfoSharp
{
    public static class CpuInfoWrappers
    {
        public const int CPUINFO_PACKAGE_NAME_MAX = 64;
        
        // [StructLayout(LayoutKind.Explicit, Size = 88)]
        // public unsafe struct cpuinfo_package 
        // {
        //     /** SoC or processor chip model name */
        //     [FieldOffset(0)]
        //     public fixed byte name[CPUINFO_PACKAGE_NAME_MAX];
            
        //     /** Index of the first logical processor on this physical package */
        //     [FieldOffset(64)]
        //     public uint processor_start;
            
        //     /** Number of logical processors on this physical package */
        //     [FieldOffset(68)]
        //     public uint processor_count;
            
        //     /** Index of the first core on this physical package */
        //     [FieldOffset(72)]
        //     public uint core_start;
            
        //     /** Number of cores on this physical package */
        //     [FieldOffset(76)]
        //     public uint core_count;
            
        //     /** Index of the first cluster of cores on this physical package */
        //     [FieldOffset(80)]
        //     public uint cluster_start;
            
        //     /** Number of clusters of cores on this physical package */
        //     [FieldOffset(84)]
        //     public uint cluster_count;
        // };

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct cpuinfo_package 
        {
            /** SoC or processor chip model name */
            public fixed byte name[CPUINFO_PACKAGE_NAME_MAX];
            
            /** Index of the first logical processor on this physical package */
            public uint processor_start;
            
            /** Number of logical processors on this physical package */
            public uint processor_count;
            
            /** Index of the first core on this physical package */
            public uint core_start;
            
            /** Number of cores on this physical package */
            public uint core_count;
            
            /** Index of the first cluster of cores on this physical package */
            public uint cluster_start;
            
            /** Number of clusters of cores on this physical package */
            public uint cluster_count;
        };
        public static bool Initialize() {
            return cpuinfo_initialize();
        }

        public static uint GetThreadCount() {
            return cpuinfo_get_processors_count();
        }

        public static unsafe uint GetCoreCount() {
            IntPtr responsePtr = cpuinfo_get_cores();

            if (responsePtr == IntPtr.Zero) {
                Console.WriteLine("NULL PTR");
                return 0;
            }

            // This should return 64. If it returns 128, your integers will never align.
            int offset = (int)Marshal.OffsetOf<cpuinfo_package>("processor_start");
            Console.WriteLine($"Offset of processor_start: {offset}"); 

            // This should be 88.
            int size = Marshal.SizeOf<cpuinfo_package>();
            Console.WriteLine($"Total Struct Size: {size}");

            cpuinfo_package package = Marshal.PtrToStructure<cpuinfo_package>(responsePtr);

            Console.WriteLine("package.core_count: {0}", package.core_count);
            Console.WriteLine("package.processor_count: {0}", package.processor_count);
            return package.core_count;
        }
    }
}