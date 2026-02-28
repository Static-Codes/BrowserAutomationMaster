using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.SystemInfo.OS.Unix;
using BrowserAutomationMaster.Core.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32.System.SystemInformation;
using Windows.Win32;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Helpers.EmbeddedResourceHelper;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;

namespace BrowserAutomationMaster.Core.SystemInfo.RAM
{

    public class MemoryMonitor
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static async Task<MemoryInfo?> GetMemoryInfoAsync()
        {
            return true switch
            {
                _ when GlobalUserInfo.PlatformInfo.IsWindows => CheckForWindows(),
                _ when GlobalUserInfo.PlatformInfo.IsMacOS => await CheckForOSX(),
                _ when GlobalUserInfo.PlatformInfo.IsLinux => CheckForLinux(),
                _ => null
            };
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private static MemoryInfo? CheckForWindows()
        {
            // Lays out the managed memory from c# in a manner that is identical to the unmanaged memory of c++ 
            var memStatus = new MEMORYSTATUSEX() {
                dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>()
            };

            // memStatus is passed as a reference type and is modified by the call to GlobalMemoryStatusEx
            if (!PInvoke.GlobalMemoryStatusEx(ref memStatus)) {
                WriteAndExit(
                    message: $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                             $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                             $"Error log:\nGlobalMemoryStatusEx invoke inside MemoryInfoManager.CheckForWindows() returned false", 
                    status: 1
                );
                return null;
            }

            double total = memStatus.ullTotalPhys / (1024 * 1024);
            double free = memStatus.ullAvailPhys / (1024 * 1024);
            double used = total - free;
            double usedPercent = Math.Round(used / total * 100.0, 2); // 100.0 is required to go from a double to a decimal to prevent the error below
            double freePercent = Math.Round(100.0 - usedPercent, 2);  // The call is ambiguous between the following methods or properties: 'System.Math.Round(double, int)' and 'System.Math.Round(decimal, int)

            return new MemoryInfo()
            {
                TotalMemory = total,
                UsedMemory = used,
                FreeMemory = free,
                UsedPercent = usedPercent,
                FreePercent = freePercent
            };
        }
            
        [SupportedOSPlatform("maccatalyst")]
        private static async Task<MemoryInfo?> CheckForOSX() 
        {
            try 
            {
                var binariesDirectory = DirectoryManager.GetBinariesDirectory();
                var binaryName = "free";
                
                var freeBinaryPath = Path.Combine(binariesDirectory, binaryName);

                if (!Directory.Exists(binariesDirectory)) {
                    DirectoryManager.EnsureDirectoryExists(binariesDirectory);
                }

                if (!File.Exists(freeBinaryPath)) 
                {
                    Console.WriteLine("BAMM bundles free-for-macOS, a MacOS application that allows for streamlined memory detection.");
                    Thread.Sleep(1000);
                    Console.WriteLine($"Please wait while free-for-macOS is written to: {freeBinaryPath}");
                    Thread.Sleep(1000);
                    Console.WriteLine("For more information on free-for-macOS, please see the github repo:");
                    Console.WriteLine(FREE_FOR_MACOS_REPO_LINK);

                    await WriteEmbeddedResourceToDisk(
                        resourceName: binaryName,
                        resourcePattern: FREE_FOR_MACOS_RESOURCE_PATH,
                        outputPath: freeBinaryPath
                    );
                }

                // Checking if the free-for-macOS binary has executable permissions
                var binaryHasPermissions = UnixFilePermissions.HasExecutablePermissions(freeBinaryPath);
                
                // If binaryHasPermissions is true, this changes nothing. 
                // However, if binaryHasPermissions is false, this attempts to give the binary exutable permissions using chmod.
                binaryHasPermissions = binaryHasPermissions || UnixFilePermissions.SetExecutablePermissions(freeBinaryPath);
                

                if (!binaryHasPermissions) 
                {
                    WriteAndExit(
                        message: string.Join(NLC, [
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.",
                            NLC,
                            $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}",
                            "Error log:",
                            $"Unable to give executable permissions to '{freeBinaryPath}'"
                        ]),
                        status: 1
                    );
                }

                // Executing the actual memory check using free-for-macOS
                // https://github.com/zfdang/free-for-macOS
                ExecuteFreeCommand(
                    binaryPath: freeBinaryPath, 
                    argument: "", 
                    out string? output, 
                    out Process? process
                );

                return ProcessFreeCommandOutput(output, process);
            }

            catch (Exception e)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                        $"Error Log:",
                        e.Message,
                    ]),
                    status: 1
                );
                return null;
            }

        }


        private static MemoryInfo? CheckForLinux() 
        {
    
            ExecuteFreeCommand("free", "", out string? output, out Process? process);
            return ProcessFreeCommandOutput(output, process);
            
        }

        private static void ExecuteFreeCommand(string binaryPath, string argument, out string? output, out Process? process) 
        {
            output = null;
            process = null;

            var info = new ProcessStartInfo {
                FileName = binaryPath,
                // Learned what the hell a mebibyte was today and now I'm upset that it's not the standard unit of storage. 
                // Thanks, marketing departments for selling us base-10 dreams on base-2 hardware.
                Arguments = argument, 
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            { 
                process = Process.Start(info);
                using (process) 
                {
                    if (process == null) 
                    { 
                        WriteAndExit(
                            message: string.Join(NLC, [
                                $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.",
                                $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}",
                                "Error log:",
                                "The process associated with the command:",
                                $"'{binaryPath}' {argument}",
                                "returned null."
                            ]),
                            status: 1
                        ); 
                    }
                    output = process!.StandardOutput.ReadToEnd(); // Null check above prevents process from being null at this point thus the !.
                }
            }

            catch (Exception e)
            {
                WriteAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again, " +
                        $"if this issue persists, please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                        $"Error log:{NLC}{NLC}MemoryInfoManager.ExecuteFreeCommand exited with stack trace of:{NLC}{NLC}{e}",
                    status: 1
                );
            }
        }

        private static MemoryInfo? ProcessFreeCommandOutput(string? output, Process? process)
        {
            try 
            {
                // Logging an exception if output or process are null
                if (output == null || process == null) 
                {
                    WriteAndExit(
                        message: string.Join(string.Empty, [
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                            $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}" +
                            $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                            $"Error log:",
                            NLC,
                            NLC,
                            $"MemoryInfoManager.CheckForUnixLike exited with an unknown status code as no valid output was received."
                        ]),
                        status: 1
                    );
                }

                // Using NLC would also would here, but for verbosity "\n" was chosen.
                var lines = output.Split("\n");
                
                
                // Preventing an IndexOutOfRangeException
                if (lines.Length < 3) 
                {
                    WriteAndExit(
                        message: string.Join(string.Empty, [
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                            $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}" +
                            $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                            $"Error log:",
                            NLC,
                            NLC,
                            $"MemoryInfoManager.CheckForUnixLike exited with a status code of {process.ExitCode}, ",
                            $"and no valid output was received."
                        ]),
                        status: 1
                    );
                }

                // Retrieving Main and Swap memory data 
                // This will terminate the program if an exception is thrown.
                (var totalMem, var usedMem, var freeMem) = ProcessMemData(lines, process);
                (var totalSwap, var usedSwap, var freeSwap) = ProcessSwapData(lines, process);


                // Adding the Main and Swap memory values to reflect the accurate amounts in bytes.
                var memTotalBytes = totalMem; // Swap is already included in total
                var memUsedBytes = usedMem + usedSwap;
                var memFreeBytes = freeMem + freeSwap;

                var memTotalMiB = memTotalBytes / 1024;
                var memUsedMiB = memUsedBytes / 1024;
                var memFreeMiB = memFreeBytes / 1024;

                // Calculating used and free percent.
                var usedPercent = Math.Round(memUsedMiB / memTotalMiB * 100, 2);
                var freePercent = Math.Round(100 - usedPercent, 2);

                // Debug ONLY do not uncomment in public releases.
                // Console.WriteLine($"totalMem: {totalMem}");
                // Console.WriteLine($"usedMem: {usedMem}");
                // Console.WriteLine($"freeMem: {freeMem}");
                // Console.WriteLine($"totalSwap: {totalSwap}");
                // Console.WriteLine($"usedSwap: {usedSwap}");
                // Console.WriteLine($"freeSwap: {freeSwap}");
                // Console.WriteLine($"memTotalBytes: {memTotalBytes}");
                // Console.WriteLine($"memUsedBytes: {memUsedBytes}");
                // Console.WriteLine($"memFreeBytes: {memFreeBytes}");
                // Console.WriteLine($"memTotalGiB: {memTotalMiB}");
                // Console.WriteLine($"memUsedGiB: {memUsedMiB}");
                // Console.WriteLine($"memFreeGiB: {memFreeMiB}");
                // Console.WriteLine($"usedPercent: {usedPercent}");
                // Console.WriteLine($"freePercent: {freePercent}");

                return new MemoryInfo()
                {
                    TotalMemory = memTotalMiB,
                    UsedMemory = memUsedMiB,
                    FreeMemory = memFreeMiB,
                    UsedPercent = usedPercent,
                    FreePercent = freePercent
                };
            }

            catch (Exception e)
            {
                WriteAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again, " +
                        $"if this issue persists, please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                        $"Error log:{NLC}{NLC}MemoryInfoManager.CheckForLinux exited with stack trace of:{NLC}{NLC}{e}",
                    status: 1
                );
                return null;
            }
        }

        private static (double totalMem, double usedMem, double freeMem) ProcessMemData(string[] lines, Process process) 
        {
            // Retrieving the memory info from output string
            var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);

            // Preventing an IndexOutOfRangeException
            if (memory.Length < 4) 
            {
                WriteAndExit(
                    message: string.Join(string.Empty, [
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                        $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}" +
                        $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                        $"Error log:",
                        NLC,
                        NLC,
                        $"MemoryInfoManager.ProcessMemData exited with a status code of {process.ExitCode}, ",
                        $"because memory.Length returned a value less than 4."
                    ]),
                    status: 1
                );
            }

            // Setting cache index for use below.
            // On Linux, cache is the 6th entry (5th index) on the second line of the output from the 'free' command.
            // On MacOS, cache is the 5th entry (4th index) on the second line of the output from the 'free-for-macOS' binary.
            var cacheIndex = GlobalUserInfo.PlatformInfo.IsLinux ? 5 : 4;

            // Assigning a flag that will be tested below to determine if the cached RAM amount should be queried.
            var checkCache = true;

            if (cacheIndex > memory.Length) {
                Warning.Write(
                    message: string.Join(string.Empty, [
                        $"BAM Manager (BAMM) was unable to determine the amount of cached system memory.{NLC}",
                        "As such, BAMM can't accurately determine the total available system memory.",
                        $"If this persists, and causes bugs, please make a bug report at {ISSUES_LINK}{NLC}",
                        $"Error log:",
                        NLC,
                        $"cacheIndex in MemoryInfoManager.ProcessMemData is greater than the total number of elements in memory.Length."
                    ])
                );
                checkCache = false;
            }

            

            // Parsing members of memory output
            bool invalidTotalMem = !double.TryParse(memory[1], out double totalMem);
            bool invalidUsedMem = !double.TryParse(memory[2], out double usedMem);
            bool invalidFreeMem = !double.TryParse(memory[3], out double freeMem);
            
            // Assigning a default value that will be tested below to determine if the attempt to query cacheMem failed unexpectedly.
            bool invalidCacheMem = false;

            // Assigning the default value to cacheMem that will only be modified if checkCache is true.
            double cacheMem = 0;

            // invalidCacheMem can only be true if both:
            // chechCache is true
            // The conversion fails

            if (checkCache) {
                invalidCacheMem = !double.TryParse(memory[cacheIndex], out cacheMem); 
            }


            // Checking for invalid states
            if (invalidTotalMem || invalidUsedMem || invalidFreeMem || invalidCacheMem)
            {
                WriteAndExit(
                    message: string.Join(string.Empty, [
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory ",
                        $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}",
                        $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}",
                        $"Error log:",
                        NLC,
                        NLC,
                        $"MemoryInfoManager.HandleMemFromFreeCommand exited with a status code of {process.ExitCode}.",
                        NLC,
                        $"invalidMemTotal: {invalidTotalMem}",
                        $"invalidMemUsed: {invalidUsedMem}",
                        $"invalidMemFree: {invalidFreeMem}",
                        $"invalidMemCache: {invalidCacheMem}"
                    ]),
                    status: 1
                );
            }

            // Adding the cached memory to the free memory amount, since it can be reallocated as needed.
            var adjustedFreeMem = freeMem + cacheMem;

            // OSX Specific logic, since OSX reports in bytes unlike linux which reports in mebibytes
            totalMem = GlobalUserInfo.PlatformInfo.IsMacOS ? totalMem / 1024 : totalMem;
            usedMem = GlobalUserInfo.PlatformInfo.IsMacOS ? usedMem / 1024 : usedMem;
            adjustedFreeMem = GlobalUserInfo.PlatformInfo.IsMacOS ? adjustedFreeMem / 1024 : adjustedFreeMem;

            return (totalMem, usedMem, adjustedFreeMem);
        }

        private static (double totalSwap, double usedSwap, double freeSwap) ProcessSwapData(string[] lines, Process process) 
        {

            // Retrieving the memory info from output string
            var swap = lines[2].Split(" ", StringSplitOptions.RemoveEmptyEntries);
            
            // Preventing an IndexOutOfRangeException
            if (swap.Length < 4) 
            {
                WriteAndExit(
                    message: string.Join(string.Empty, [
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                        $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}" +
                        $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}" +
                        $"Error log:",
                        NLC,
                        NLC,
                        $"MemoryInfoManager.ProcessSwapData exited with a status code of {process.ExitCode}, ",
                        $"and no valid output was received."
                    ]),
                    status: 1
                );
            }

            // Parsing members of swap output
            bool invalidSwapTotal = !double.TryParse(swap[1], out double totalSwap);
            bool invalidSwapUsed = !double.TryParse(swap[2], out double usedSwap);
            bool invalidSwapFree = !double.TryParse(swap[3], out double freeSwap);

            // Checking for invalid states
            if (invalidSwapTotal || invalidSwapUsed || invalidSwapFree)
            {
                WriteAndExit(
                    message: string.Join(string.Empty, [
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory ",
                        $"as the unix 'free' command returned nothing, please try again.{NLC}{NLC}",
                        $"If this issue persists please make a bug report at {ISSUES_LINK}{NLC}{NLC}",
                        $"Error log:",
                        NLC,
                        NLC,
                        $"MemoryInfoManager.ProcessSwapData exited with a status code of {process.ExitCode}.",
                        NLC,
                        $"invalidSwapTotal: {invalidSwapTotal}",
                        $"invalidSwapUsed: {invalidSwapUsed}",
                        $"invalidSwapFree: {invalidSwapFree}"
                    ]),
                    status: 1
                );
            }

            return (totalSwap, usedSwap, freeSwap);


        }

        
    }
}
