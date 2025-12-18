using Windows.Win32.System.SystemInformation;
using Windows.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public struct MemoryInfo
    {
        public required double? TotalMemory { get; set; }
        public required double? UsedMemory { get; set; }
        public required double? FreeMemory { get; set; }
        public required double? UsedPercent { get; set; }
        public required double? FreePercent { get; set; }
    }

    public class MemoryInfoManager
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static MemoryInfo? RunCheck()
        {
            return true switch
            {
                _ when Platforms.IsWindows => CheckForWindows(),
                _ when Platforms.IsOSX => CheckForOSX(),
                _ when Platforms.IsLinux => CheckForLinux(),
                _ => null
            };
        }


        [SupportedOSPlatform("windows10.0.10240")]
        private static MemoryInfo? CheckForWindows()
        {
            // Lays out the managed memory from c# in a manner that is identical to the unmanaged memory of c++ 
            MEMORYSTATUSEX memStatus = new() {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX))
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

            double total = (double)(memStatus.ullTotalPhys / (1024 * 1024));
            double free = (double)(memStatus.ullAvailPhys / (1024 * 1024));
            double used = total - free;
            double usedPercent = Math.Round((used / total) * 100.0, 2); // 100.0 is required to go from a double to a decimal to prevent the error below
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
            

        private static MemoryInfo? CheckForOSX() {
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

            var scriptDirectory = Path.GetTempPath(); // Creates a temp file for {scriptFileName}
            var scriptFileName = "memcheck.sh";
            var scriptFilePath = Path.Combine(scriptDirectory, scriptFileName);

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
                    WriteAndExit(
                        message: $"BAM Manager (BAMM) was unable to give {scriptFileName} executable permissions.\n\n" +
                                 $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                                 $"Error log:\nchmod failed with exit code {chmodProcess.ExitCode}",
                        status: 1);
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
                    WriteAndExit(
                        message: $"BAM Manager (BAMM) was unable to give {scriptFileName} executable permissions.\n\n" +
                                 $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                                 $"Error log:\nsed failed with exit code {sedProcess.ExitCode}", 
                        status: 1
                    );
                }


                ProcessStartInfo scriptRunInfo = new() {
                    FileName = scriptFilePath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Process? process = Process.Start(scriptRunInfo);

                if (process == null) {
                    WriteAndExit(
                        message: $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                                 $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                                 "Error log:\n" +
                                 $"Process associated with {scriptFileName} returned null, but it successfully received +x privileges.",
                        status: 1
                    ); 
                }

                string output = process!.StandardOutput.ReadToEnd(); // Null check above thus the null forgiveness operator.
                string errorOutput = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode != 0) {
                    WriteAndExit(
                        message: 
                            "BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                            $"If this continues, please make a bug report at {ISSUES_LINK}\n\nError log:\n" +
                            $"{scriptFileName} returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}",
                        status: 1
                    );
                }

                // Handles the cross system issues caused by pasting a unix script on a windows machine
                var lines = output.Split(["\n", "\r"], StringSplitOptions.RemoveEmptyEntries); 
                //foreach (string line in lines) { Spectre.Console.AnsiConsole.Write(line); } // Used for debug only do not forget to comment this out.

                if (lines.Length < 3) { return null; }

                if (double.TryParse(lines[0], out double total) && 
                    double.TryParse(lines[1], out double used) && 
                    double.TryParse(lines[2], out double free)
                ) 
                {
                    var usedPercent = Math.Round(used / total * 100.0, 2); // 100 is required to go from a double to a decimal to prevent this error
                    var freePercent = Math.Round(100.0 - usedPercent, 2);  // The call is ambiguous between the following methods or properties: 'System.Math.Round(double, int)' and 'System.Math.Round(decimal, int)

                    return new MemoryInfo()
                    {
                        TotalMemory = total,
                        UsedMemory = used,
                        FreeMemory = free,
                        UsedPercent = usedPercent,
                        FreePercent = freePercent
                    };
                }
                WriteAndExit(
                    message: $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                    $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error log:\n{scriptFileName} returned the following error:\n{errorOutput}\nExit Code: {process.ExitCode}",
                    status: 1
                );
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message: $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                             $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                             $"Error log:\n{ex.Message}",
                    status: 1);
            }

            return null;
        }

        private static MemoryInfo? CheckForLinux() {
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
                    if (process == null) { 
                        WriteAndExit(
                            message: 
                                $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again.\n\n" +
                                $"If this issue persists please make a bug report at {ISSUES_LINK}\n\n" +
                                $"Error log:\nfree -m command process returned null.",
                            status: 1
                        ); 
                    }
                    output = process!.StandardOutput.ReadToEnd(); // Null check above prevents process from being null at this point thus the !.
                }

                var lines = output.Split("\n");
                if (lines.Length == 0) {
                    WriteAndExit(
                        message: 
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                            $"as the linux 'free' command returned nothing, please try again.\n\n" +
                            $"If this issue persists please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, " +
                            $"and no valid output was received.",
                        status: 1
                    );
                }

                var memory = lines[1].Split(" ", StringSplitOptions.RemoveEmptyEntries);
                if (memory.Length == 0) {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory " +
                            $"as the linux 'free' command returned nothing, please try again.\n\n" +
                            $"If this issue persists please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}, " +
                            $"and no valid output was received.",
                        status: 1
                    );
                }

                bool invalidTotal = !double.TryParse(memory[1], out double total);
                bool invalidUsed = !double.TryParse(memory[2], out double used);
                bool invalidFree = !double.TryParse(memory[3], out double free);

                if (invalidTotal || invalidUsed || invalidFree)
                {
                    WriteAndExit(
                        message: 
                            $"BAM Manager (BAMM) was unable to determine the amount of available system memory as the linux 'free' command returned " +
                            $"unexpected output for 'total', please try again.\n\n" +
                            $"If this issue persists please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\n\nRuntimeManager.GetMemoryInfo for linux exited with a status code of {process.ExitCode}," +
                            $" and no valid output was received.", 
                        status: 1
                    );
                }

                var usedPercent = Math.Round(used / total * 100, 2);
                var freePercent = Math.Round(100 - usedPercent, 2);

                return new MemoryInfo()
                {
                    TotalMemory = total,
                    UsedMemory = used,
                    FreeMemory = free,
                    UsedPercent = usedPercent,
                    FreePercent = freePercent
                };
            }
            catch (Exception e)
            {
                WriteAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the amount of available system memory, please try again, " +
                        $"if this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n\nRuntimeManager.GetMemoryInfo for linux exited with stack trace of:\n\n{e}",
                    status: 1
                );
                return null;
            }
        }
    }
}
