using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster.Managers
{
    public class MemoryInfoManager
    {
        public static Dictionary<string, double> RunCheck()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { return CheckForWindows(); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) { return CheckForOSX(); }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { return CheckForLinux64(); }
            throw new PlatformNotSupportedException("Unsupported OS.");
        }

        #region "P/Invoke win32 api functions
        [StructLayout(LayoutKind.Sequential)]  // Ensures memory safety during invoke, by explicitly declaring struct members are to be placed in memory in the exact order and size they are received as.
                                               //   (Ensures b4b compatibility (no data is loss between the translation of valid c# bytes to valid c bytes)
        private struct MEMORYSTATUSEX // typedef struct _MEMORYSTATUSEX { 
        {
            public uint dwLength;                   // DWORD dwLength;             (In this case its going to be a 32 bit unsigned int since its running on win10/win11)
            public uint dwMemoryLoad;               // DWORD dwMemoryLoad;
            public ulong ullTotalPhys;              // DWORDLONG ullTotalPhys;     (Total available sysmem in bytes)
            public ulong ullAvailPhys;              // DWORDLONG ullAvailPhys;     (Total free sysmem in bytes)
            public ulong ullTotalPageFile;          // DWORDLONG ullTotalPageFile
            public ulong ullAvailPageFile;          // DWORDLONG ullAvailPageFile;
            public ulong ullTotalVirtual;           // DWORDLONG ullTotalVirtual;
            public ulong ullAvailVirtual;           // DWORDLONG ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;   // DWORDLONG ullAvailExtendedVirtual;
        }
        [DllImport("kernel32.dll", SetLastError = true)] // SetLastError likely wont be needed but it's nice to have anyways.
        [return: MarshalAs(UnmanagedType.Bool)] // First time working with Unmanaged Code even though its still in a Managed Environment, C returns a 4 byte bool where as C# natively supports 1 byte bool(s).
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer); // The actual function thats required

        #endregion

        private static Dictionary<string, double> CheckForWindows()
        {
            MEMORYSTATUSEX memStatus = new()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) // Lays out the managed memory from c# in a manner that is identical to the unmanaged memory of c++ 
            };

            // memStatus is passed as a reference type and is modified by the call to GlobalMemoryStatusEx
            if (!GlobalMemoryStatusEx(ref memStatus)) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nGlobalMemoryStatusEx invoke inside MemoryInfoManager.CheckForWindows() returned false.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return [];
            }

            double total = (double)(memStatus.ullTotalPhys / (1024 * 1024));
            double free = (double)(memStatus.ullAvailPhys / (1024 * 1024));
            double used = total - free;
            double usedPercent = Math.Round((used / total) * 100.0, 2); // 100.0 is required to go from a double to a decimal to prevent the error below
            double freePercent = Math.Round(100.0 - usedPercent, 2);  // The call is ambiguous between the following methods or properties: 'System.Math.Round(double, int)' and 'System.Math.Round(decimal, int)

            return new Dictionary<string, double>() {
                { "totalMemoryMB", total },
                { "usedMemoryMB",  used },
                { "freeMemoryMB",  free },
                { "usedPercent", usedPercent },
                { "freePercent", freePercent }
            };
        }
            

        private static Dictionary<string, double> CheckForOSX() {
            string scriptFileContents = @"#!/bin/bash

    BYTES_IN_MB=$((1024 * 1024))
    PAGESIZE_BYTES=$(pagesize)
    TOTAL_MEM_BYTES=$(sysctl -n hw.memsize)
    TOTAL_MEM_MB=$((TOTAL_MEM_BYTES / BYTES_IN_MB))
    VM_STAT_OUTPUT=$(vm_stat)

    get_page_count() {
        echo ""$VM_STAT_OUTPUT"" | awk -v metric=""^$1:"" '$0 ~ metric {gsub(/\./,"""",$3); print $3; exit}' | grep -o '[0-9]*'
    }

    FREE_PAGES=$(get_page_count ""Pages free"")
    INACTIVE_PAGES=$(get_page_count ""Pages inactive"")
    SPECULATIVE_PAGES=$(get_page_count ""Pages speculative"")
    PURGEABLE_PAGES=$(get_page_count ""Pages purgeable"")

    TOTAL_PAGES=$((TOTAL_MEM_BYTES / PAGESIZE_BYTES))
    AVAILABLE_PAGES=$(( ${FREE_PAGES:-0} + ${INACTIVE_PAGES:-0} + ${SPECULATIVE_PAGES:-0} + ${PURGEABLE_PAGES:-0} ))
    USED_PAGES=$((TOTAL_PAGES - AVAILABLE_PAGES))
    USED_PAGES=$((USED_PAGES < 0 ? 0 : USED_PAGES))

    USED_MEM_MB=$(((USED_PAGES * PAGESIZE_BYTES) / BYTES_IN_MB))
    FREE_MEM_MB=$((TOTAL_MEM_MB - USED_MEM_MB))

    echo $TOTAL_MEM_MB
    echo $USED_MEM_MB
    echo $FREE_MEM_MB";

            string scriptDirectory = Path.GetTempPath(); // Creates a temp file for memcheck.sh
            string scriptFileName = "memcheck.sh";
            string scriptFilePath = Path.Combine(scriptDirectory, scriptFileName);

            try
            {
                File.WriteAllText(scriptFilePath, scriptFileContents);

                ProcessStartInfo chmodStartInfo = new()
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"chmod +x \"{scriptFilePath}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

                Process chmodProcess = new() { StartInfo = chmodStartInfo };
                chmodProcess.Start();
                chmodProcess.WaitForExit();

                if (chmodProcess.ExitCode != 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to give memcheck.sh executable permissions, please make sure you are on an admin account.\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nchmod failed with exit code {chmodProcess.ExitCode}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                ProcessStartInfo sedProcessInfo = new()
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"sed -i '' 's/\\r$//' \"{scriptFilePath}\"\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                Process sedProcess = new() { StartInfo = sedProcessInfo };
                sedProcess.Start();
                sedProcess.WaitForExit();

                if (sedProcess.ExitCode != 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to give memcheck.sh executable permissions, please make sure you are on an admin account.\n\nIf this continues, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nsed failed with exit code {sedProcess.ExitCode}.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }


                ProcessStartInfo scriptRunInfo = new() {
                    FileName = scriptFilePath,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Process? process = Process.Start(scriptRunInfo);

                if (process == null) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nProcess associated with memcheck.sh returned null, but it successfully received +x privileges.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                string output = process!.StandardOutput.ReadToEnd(); // Null check above thus the null forgiveness operator.
                string errorOutput = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nMemcheck.sh returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries); // Handles the cross system issues caused by pasting a unix script on a windows machine
                //foreach (string line in lines) { Console.WriteLine(line); } // Used for debug only do not forget to comment this out.

                if (lines.Length < 3) { return []; }
                if (double.TryParse(lines[0], out double total) && double.TryParse(lines[1], out double used) && double.TryParse(lines[2], out double free)) {
                    var usedPercent = Math.Round(used / total * 100.0, 2); // 100 is required to go from a double to a decimal to prevent this error
                    var freePercent = Math.Round(100.0 - usedPercent, 2);  // The call is ambiguous between the following methods or properties: 'System.Math.Round(double, int)' and 'System.Math.Round(decimal, int)

                    return new Dictionary<string, double>() {
                        { "totalMemoryMB", total },
                        { "usedMemoryMB",  used },
                        { "freeMemoryMB",  free },
                        { "usedPercent", usedPercent },
                        { "freePercent", freePercent }
                    };
                }
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nMemcheck.sh returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return [];
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this continues, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\n{ex.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return [];
            }
        }

        private static Dictionary<string, double> CheckForLinux64() {
            var output = "";

            var info = new ProcessStartInfo {
                FileName = "free",
                Arguments = "-m", // Learned what the hell a mebibyte was today and now I'm upset that it's not the standard unit of storage. Thanks, marketing departments for selling us base-10 dreams on base-2 hardware.
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            { 
                Process? process = Process.Start(info);
                using (process) {
                    if (process == null) { Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\nfree -m command process returned null.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1); }
                    output = process!.StandardOutput.ReadToEnd(); // Null check above prevents process from being null at this point thus the !.
                }

                var lines = output.Split("\n");
                if (lines.Length == 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned nothing, please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, and no valid output was received.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (memory.Length == 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned unexpected output, please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, and no valid output was received.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                if (!double.TryParse(memory[1], out double total)) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned unexpected output for 'total', please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, and no valid output was received.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }
                if (!double.TryParse(memory[2], out double used)) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned unexpected output for 'used', please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, and no valid output was received.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }
                if (!double.TryParse(memory[3], out double free)) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned unexpected output for 'free', please try again.\n\nIf this issue persists please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, and no valid output was received.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                }

                var usedPercent = Math.Round(used / total * 100, 2);
                var freePercent = Math.Round(100 - usedPercent, 2);

                return new Dictionary<string, double>(){
                        { "totalMemoryMB", total },
                        { "usedMemoryMB",  used },
                        { "freeMemoryMB",  free },
                        { "usedPercent", usedPercent },
                        { "freePercent", freePercent }
                };
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again, if this issue persists, please make a bug report at https://github.com/Static-Codes/BrowserAutomationMaster/issues\n\nError log:\n\nRuntimeManager.GetMemoryInfo for linux exited with stack trace of:\n\n{e}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return [];
            }
        }
    }
}
