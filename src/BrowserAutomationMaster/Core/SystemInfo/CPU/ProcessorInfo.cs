using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Windows.Win32;
using Windows.Win32.System.SystemInformation;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Common.ProcessFactory;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.SystemInfo.CPU.RequiredInstructions;
using static BrowserAutomationMaster.Core.Utilities.AppSettingsUtility;

namespace BrowserAutomationMaster.Core.SystemInfo.CPU
{
    public class ProcessorInfo()
    {
        public int Cores { get; set; } = GetCoreCount();
        
        // Minimum cores supported: 2
        // Minimum cores recommended: 4
        // Ensure all requiredInstructions
        // Verify presence of recommendedInstructions
        // If not present, inform the user of:
        // - What they are missing
        // - What it does
        // - Whether or not they want to continue.

        public const string X64_EXPLANATION = "X86-64, commonly referred to as x64, is the modern implementation of the x86 CPU architecture, it's the reason our system's aren't limited to 4GB of RAM.";
        public const string AES_EXPLANATION = "Accelerates encryption/decryption, important for HTTPS and secure web communication.";
        public const string AVX_EXPLANATION = "Advanced Vector Extensions introduced a major advancement in SIMD capabilities, by introducing 256bit registers.\nFor modern browser automation, especially with complex pages, WebGL, or video, AVX support is critical.";
        public const string AVX2_EXPLANATION = "Advanced Vector Extensions 2 expanded greatly on AVX and remains the backbone of the modern internet regarding displaying and rendering web content.\nChrome versions starting from 142 are explicitly requiring AVX2 support for full compatibility. ";
        public const string BMI_EXPLANATION = "A set of instructions for more efficient bitwise operations.";
        public const string FMA_EXPLANATION = "Improves performance for floating-point calculations by combining multiplication and addition into a single instruction, common in graphics and scientific computing.";
        public const string LZCNT_EXPLANATION = "Leading Zero Count ";
        public const string PCLMULQDQ_EXPLANATION = "Carry-less Mutiplication (CLMUL) is used for certain cryptographic operations (Like GCM in AES128)";
        public const string POPCNT_EXPLANATION = "Population Count is a subset of the SSE4.2 instruction set, it's responsible for counting the number of set bits in a machine word.\nFor example (assuming 8-bit words for simplicity), popcount(00100110) is 3 and popcount(01100000) is 2.";
        public const string SSE2_EXPLANATION = "Streaming SIMD Extensions 2 is a fundamental SIMD (Single Instruction, Multiple Data) instruction set.\nIt's used heavily for floating-point calculations, multimedia processing, and many other general-purpose computations.\nModern compilers assume SSE2 is present for almost all code. Without it, modern browser binaries simply won't run.";
        public const string SSE3_EXPLANATION = "Streaming SIMD Extensions 3 is the next iteration of SSE2, it provides additional instructions that are still used today in modern CPUs.";
        public const string SSSE3_EXPLANATION = "Supplemental Streaming SIMD Extensions 3 much like SSE3 is yet another instruction set responsible for integer processing, data manipulation, and general codec operations.";
        public const string SSE4_EXPLANATION = "Streaming SIMD Extensions 4.X include instructions for string processing, dot products, and other operations that speed up many common tasks. Modern JavaScript engines (V8, SpiderMonkey) and rendering engines can leverage these for performance.";
        readonly private static List<RequiredInstructions> unsupportedInstructions = [];

        private static bool ContainsNeededInstructions()
        {
            Action Add(RequiredInstructions instruction) => () => 
            {
                var needsAttention = instruction.Equals(SSE3) || instruction.Equals(SSE4);

                if (!needsAttention) {
                    unsupportedInstructions.Add(instruction);
                }

                // Prevents duplicates
                else if (needsAttention && !unsupportedInstructions.Contains(instruction)) {
                    unsupportedInstructions.Add(instruction);
                }
            };

            var conditionPairs = new Dictionary<bool, RequiredInstructions>() 
            {
                { X86Base.X64.IsSupported, X64 },
                { Avx.IsSupported, AVX },
                { Sse2.IsSupported, SSE2 },
                { Sse3.IsSupported, SSE3 },
                { Ssse3.IsSupported, SSE3 },
                { Sse41.IsSupported, SSE4 },
                { Sse42.IsSupported, SSE4 },
                { Aes.IsSupported, AES },
                { Avx2.IsSupported, AVX2 },
                { Bmi1.IsSupported, BMI1 },
                { Bmi2.IsSupported, BMI2 },
                { Fma.IsSupported, FMA },
                { Lzcnt.IsSupported, LZCNT },
                { Popcnt.IsSupported, POPCNT },
                { Pclmulqdq.IsSupported, PCLMULQDQ },
            };

            foreach (var conditionPair in conditionPairs)
            {
                if (!conditionPair.Key) {
                    Add(conditionPair.Value);
                }
            }

            return unsupportedInstructions.Count == 0; // The application will exit if this returns false
        }
        
        public static void DisplayMissingInstructions()
        {
            var message = "Would you like to learn more about this instruction? [y/n]: ";
            
            foreach (RequiredInstructions instruction in unsupportedInstructions) 
            {
                Spectre.Console.AnsiConsole.Write($"{instruction} is unsupported on the current CPU.");

                if (Input.ConditionAccepted(Input.AskForInput(message))) {
                    Warning.Write(GetExplanationForInstruction(instruction));
                }
            }
        }

        private static string GetExplanationForInstruction(RequiredInstructions instruction)
        {
            return instruction switch
            {
                X64 => X64_EXPLANATION,
                AES => AES_EXPLANATION,
                AVX => AVX_EXPLANATION,
                AVX2 => AVX2_EXPLANATION,
                BMI1 => BMI_EXPLANATION,
                BMI2 => BMI_EXPLANATION,
                FMA => FMA_EXPLANATION,
                LZCNT => LZCNT_EXPLANATION,
                PCLMULQDQ => PCLMULQDQ_EXPLANATION,
                POPCNT => POPCNT_EXPLANATION,
                SSE2 => SSE2_EXPLANATION,
                SSE3 => SSE3_EXPLANATION,
                SSSE3 => SSE3_EXPLANATION,
                SSE4 => SSE4_EXPLANATION,
                _ => "Invalid instruction provided, this shouldn't be trigger unless there is a bug in ProcessorInfo.GetExplanationForInstruction()",
            };
        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static int GetCoreCount()
        {
            return (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch {
                (true, _, _) => GetPhysicalCoreCountWindows(),
                (_, true, _) => GetPhysicalCoreCountMacOS(),
                (_, _, true) => GetPhysicalCoreCountLinux(),
                _ => GetPhysicalCoreException()
            };
        }

        private static (int, int) GetCPUTopologyLinux()
        {
            const string cpuInfoPath = "/proc/cpuinfo";

            if (!File.Exists(cpuInfoPath))
            {
                Warning.Write(
                    string.Join(NLC, [
                        "Unable to access /proc/cpuinfo to determine CPU topology.",
                        $"Continuing under the assumption the current system is single socket (may cause issues).{NLC}",
                        $"Error Log: /proc/cpuinfo was not found. Please make a bug report at {ISSUES_LINK}"
                    ]
                ));
                return (-1, -1);
            }

            var uniqueSocketIDs = new HashSet<string>();
            var uniqueCoreCombos = new HashSet<string>();

            string? currentPhysicalID = null;
            string? currentCoreID = null;

            try
            {
                var lines = File.ReadLines(cpuInfoPath);

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        if (currentPhysicalID != null && currentCoreID != null) {
                            uniqueCoreCombos.Add($"{currentPhysicalID}-{currentCoreID}");
                        }

                        // Resets IDs before the next logical processor block.
                        currentPhysicalID = null;
                        currentCoreID = null;
                        continue;
                    }

                    if (line.StartsWith("physical id")) {
                        currentPhysicalID = line.Split(':')[1].Trim();
                        if (currentPhysicalID != null) {
                            uniqueSocketIDs.Add(currentPhysicalID);
                        }
                    }
                    else if (line.StartsWith("core id")) {
                        currentCoreID = line.Split(':')[1].Trim();
                    }
                }

                if (currentPhysicalID != null && currentCoreID != null)
                {
                    // Note from me in the past: uniqueSocketIDs was already populated inside the loop, DO NOT reintroduce a critical logic flaw!
                    uniqueCoreCombos.Add($"{currentPhysicalID}-{currentCoreID}");
                }
            }
            catch (Exception ex)
            {
                Warning.Write(
                    string.Join(NLC, [
                        "Unable to read /proc/cpuinfo due to an exception.",
                        $"Continuing under the assumption the current system is single socket.{NLC}",
                        $"Error Log:{NLC}{ex.Message}"
                    ]
                ));
                return (-1, -1);
            }

            // Return the count of unique sockets and unique cores.
            return (uniqueSocketIDs.Count, uniqueCoreCombos.Count);
        }

        private static int GetPhysicalCoreCountMacOS()
        {
            string actionString = "determine the amount of physical CPU cores on your system";

            var psi = new ProcessStartInfo
            {
                FileName = "/usr/sbin/sysctl",
                Arguments = "-n hw.physicalcpu",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = SpawnProcess(psi, actionString, writeSTDInOut: false, runSync: true, timeout: 10).Result;
            (int ExitCode, List<string> STDOut, List<string> STDErr) = GetProcessResponse(process).Result;

            var response = GetProcessResponse(process).Result;

            return (int)HandleSingleLineProcessOutput(actionString, STDOut, typeof(int));

        }

        private static int GetPhysicalCoreCountLinux()
        {
            var socketNotFoundMsg = string.Join(NLC, [
                $"Unable to determine the amount of physical CPU cores on your system.",
                "Error Log:",
                "socketCount returned -1, indicating a failure to query /proc/cpuinfo",
                $"If this issue persists please make a bug report at {ISSUES_LINK}"  
            ]);

            var socketErrorMsg = string.Join(NLC, [
                $"BAMM does not support multi socket systems.",
                "Please disable one of these sockets in your bios or use a different machine."
            ]);

            var coresNotFoundMsg = string.Join(NLC, [
                $"Unable to determine the amount of physical CPU cores on your system.",
                "Error Log:",
                "coreCount returned 0, indicating a failure to query /proc/cpuinfo",
                $"Unless you have created a way to run a pc without a CPU, please make a bug report at {ISSUES_LINK}"  
            ]);


            (var socketCount, var coreCount) = GetCPUTopologyLinux();

            if (socketCount == -1) {
                WriteAndExit(socketNotFoundMsg, 1);
            }

            if (socketCount > 1) {
                WriteAndExit(socketErrorMsg, 1);
            }

            if (coreCount == 0) {
                WriteAndExit(coresNotFoundMsg, 1);
            }

            return coreCount;

        }

        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        // Unsafe accessor required for casting null to SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        public unsafe static int GetPhysicalCoreCountWindows()
        {
            int physicalCoreCount = 1;
            try
            {
                uint bufferSize = 0;

                // This is expected to fail, it requires a 2 pass system
                // firstResult: returns the bufferSize of the given CPU topology
                // secondResult: uses the bufferSize as a ref object and iterates over the structs, counts RelationProcessCore(s)number

                // firstResult: returns the bufferSize of the given CPU topology
                bool firstResult = PInvoke.GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                    (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)null,
                    ref bufferSize
                );


                // 122 is the err code for ERROR_INSUFFICIENT_BUFFER (it wont import for some reason)
                if (!firstResult && Marshal.GetLastWin32Error() != 122) 
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system.",
                            $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                            "Error Log:",
                            $"AppManager.OS.Windows.GetPhysicalCoreCount() Failed to get logical processor information buffer size.",
                            "Win32 Error:",
                            Marshal.GetLastWin32Error()
                        ]),
                        status: 1
                    );
                }

                // If the buffer is empty, a fatal error has occured.
                if (bufferSize == 0) {
                    WriteAndExit(
                        message: string.Join(NLC, [
                            "BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system.",
                            $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                            "Error Log:",
                            "AppManager.OS.Windows.GetPhysicalCoreCount() returned a buffer size of 0."
                        ]),
                        status: 1
                    );
                }

                // Allocates N bytes from bufferSize
                var buffer = Marshal.AllocHGlobal((int)bufferSize); 

                // secondResult: uses the bufferSize as a ref object and iterates over the structs, counts RelationProcessCore(s)number
                bool secondResult = PInvoke.GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                    (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)buffer,
                    ref bufferSize
                );

                if (!secondResult) {
                    throw new Exception($"Failed to get logical processor information. Win32 Error: {Marshal.GetLastWin32Error()}");
                }

                uint bytesParsed = 0;

                nint currentPtr = buffer;

                // Debug values
                // Spectre.Console.AnsiConsole.Write("\n--- Debugging GetLogicalProcessorInformationEx Entries ---");
                // Spectre.Console.AnsiConsole.Write($"Total buffer size: {bufferSize} bytes");
                // Spectre.Console.AnsiConsole.Write(bufferSize);
                while (bytesParsed < bufferSize)
                {
                    // Deserializes the raw bytes of the currentPtr to the SYSTEM_PROCESSOR_INFORMATION_EX struct
                    var currentInfoExHeader = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(currentPtr);

                    // Debug values
                    // Spectre.Console.AnsiConsole.Write($"Bytes parsed: {bytesParsed}");
                    // Spectre.Console.AnsiConsole.Write($"\n  Entry at offset {currentPtr.ToInt64() - Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0).ToInt64()}:");
                    // Spectre.Console.AnsiConsole.Write($"    Relationship: {currentInfoExHeader.Relationship}");
                    // Spectre.Console.AnsiConsole.Write($"    Entry Size: {currentInfoExHeader.Size}");

                    if (currentInfoExHeader.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore) {
                        physicalCoreCount++;
                    }

                    // Move to the next structure in the buffer
                    currentPtr += (nint)currentInfoExHeader.Size; // I SPENT 10 minutes before I realized wasn't being incremented.
                    bytesParsed += currentInfoExHeader.Size;
                }
            }

            catch (Exception ex) 
            {
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                
                WriteAndExit(
                    message: string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to determine the number of physical CPU cores present.",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                        "Error Log:",
                        errorMessage
                    ]),
                    status: 1
                );
            }

            return physicalCoreCount;
        }

        private static int GetPhysicalCoreException() 
        {
            ThrowUnsupportedPlatformException();
            // This wont be executed but due to the nature of rosyln, this is required, but will never be executed.
            return 1;
        }

        
    

        public static string GetCPUName()
        {
            string? processName;
            string? processArgs;

            if (Platforms.IsWindows) {
                return GetCPUName();
            }

            else if (Platforms.IsMacOS) {
                processName = "/bin/bash";
                processArgs = "-c \"sysctl -n machdep.cpu.brand_string\"";
            }

            else if (Platforms.IsLinux) {
                processName = "/bin/bash";
                processArgs = "-c \"lscpu | grep 'Model name:' | sed -r 's/Model name:\\s{1,}//g'\"";
            }

            else {
                throw new PlatformNotSupportedException("Failed to set all values for members in PlatformInfo.Platforms");
            }

            var psi = new ProcessStartInfo()
            {
                FileName = processName,
                Arguments = processArgs,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            try
            {
                using var process = ProcessFactory.SpawnProcess(psi, "get cpu name", runSync: true, timeout: 10, writeSTDInOut: false).Result;
                (int ExitCode, List<string> STDOut, List<string> STDErr) = ProcessFactory.GetProcessResponse(process).Result;

                if (ExitCode == 0 || STDOut.Count == 1 || STDErr.Count == 0) {
                    return STDOut[0];
                }

            }
            catch (Exception e) 
            {
                Warning.Write(
                    string.Join(NLC, [
                        $"Unable to determine CPU name.", 
                        NLC,
                        "Error Log:",
                        e.Message
                    ])
                );
            }

            return "Unknown";
        }

        // Used for syntax purposes 
        public static bool IsMissingInstructions() 
        {
            if (!ContainsNeededInstructions()) {
                return true;
            }

            return false;
        }

        public static object HandleSingleLineProcessOutput(string actionString, List<string> STDOut, Type returnType)
        {
            string failureMessage = string.Join(NLC, [
                $"BAM Manager (BAMM) was unable to {actionString}.",
                $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                NLC,
                $"Error Log:{NLC}",
            ]);

            switch (STDOut.Count)
            {
                case 0:
                    failureMessage += "Command returned no output.";
                    WriteAndExit(failureMessage, 1);
                    break;

                case 1 when returnType.Equals(typeof(int)):
                    return int.TryParse(STDOut[0], out int res) ? res : 0;

                case 1 when returnType.Equals(typeof(string)):
                    return !string.IsNullOrEmpty(STDOut[0]) ? STDOut[0] : string.Empty;

                // Fallback for cases where an unsupported returnType is provided.
                case 1:
                    failureMessage += "Invalid returnType passed to HandleSingleLineProcessOutput()";
                    WriteAndExit(failureMessage, 1);
                    break;

                default:
                    failureMessage += string.Join(NLC, [
                        "Command returned an invalid.",
                        NLC,
                        "Output:",
                        string.Join(NLC, STDOut)
                    ]);

                    WriteAndExit(failureMessage, 1);
                    break;
            }
            return -1;
        }

        public bool HasEnoughCores()
        {
            if (Cores < 2) {
                return false;
            }

            if (Cores <= 4 && GlobalSettings.ShowCpuCheck) {
                Warning.Write(
                    $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                    $"this might impact your performance slightly if your CPU is older.\n"
                );
            }

            else if (GlobalSettings.ShowCpuCheck) {
                WriteSuccessMessage(
                    $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                    $"you should not experience any performance issues directly related to your CPU.\n"
                );
            }
                
            
            return true;
        }

        
    }

}