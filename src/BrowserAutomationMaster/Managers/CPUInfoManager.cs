using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RequiredCPUInstruction;

namespace BrowserAutomationMaster.Managers
{
    public enum RequiredCPUInstruction
    {
        X64,
        AES,
        AVX,
        AVX2,
        BMI1,
        BMI2,
        FMA,
        LZCNT,
        PCLMULQDQ,
        POPCNT,
        SSE2,
        SSE3,
        SSSE3,
        SSE4
    }

    public class CPUInfoManager()
    {
        public int Cores { get; set; } = CPUCoreManager.GetCoreCount();
        public Architecture Architecture { get; init; } = RuntimeInformation.OSArchitecture;

        // Minimum cores supported: 2
        // Minimum cores recommended: 4
        // Ensure all requiredInstructions
        // Verify presence of recommendedInstructions, if not inform the user of what they are missing, what it does, and whether or not they want to continue.
        // string[] requiredInstructions = ["x64", "AVX", "SSE2", "SSE3", "SSSE3", "SSE4.1", "SSE4.2"];
        // string[] recommendedInstructions = ["AES", "AVX2", "BMI1", "BMI2", "FMA3", "LZCNT", "PopCnt", "PCLMULQDQ", "TZCNT"];

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
        readonly private static List<RequiredCPUInstruction> unsupportedInstructions = [];

        private static bool ContainsNeededInstructions()
        {
            Action Add(RequiredCPUInstruction instruction) => () => 
            {
                var needsAttention = instruction.Equals(SSE3) || instruction.Equals(SSE4);

                if (!needsAttention)
                    unsupportedInstructions.Add(instruction);

                // Prevents duplicates
                else if (needsAttention && !unsupportedInstructions.Contains(instruction))
                    unsupportedInstructions.Add(instruction);
            };

            var conditionPairs = new Dictionary<bool, RequiredCPUInstruction>() 
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
                if (!conditionPair.Key)
                    Add(conditionPair.Value);
            }

            return unsupportedInstructions.Count == 0; // The application will exit if this returns false
        }

        // Used for syntax purposes 
        // if (IsMissingInstructions) is more clear
        public static bool IsMissingInstructions() {
            if (!ContainsNeededInstructions()) 
                return true;

            return false;
        }

        public bool HasEnoughCores()
        {
            if (Cores < 2)
                return false;

            if (Cores <= 4 && GlobalConfig.ShowCpuCheck)
                Warning.Write(
                    $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                    $"this might impact your performance slightly if your CPU is older.\n"
                );

            else if (GlobalConfig.ShowCpuCheck)
                Success.WriteSuccessMessage(
                    $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                    $"you should not experience any performance issues directly related to your CPU.\n"
                );
                
            
            return true;
        }

        public static void DisplayMissingInstructions()
        {
            foreach (RequiredCPUInstruction instruction in unsupportedInstructions) {
                Spectre.Console.AnsiConsole.Write($"{instruction} is unsupported on the current CPU.");
            }
        }

        
        private static string GetExplanationForInstruction(RequiredCPUInstruction instruction)
        {
            return instruction switch
            {
                RequiredCPUInstruction.X64 => X64_EXPLANATION,
                RequiredCPUInstruction.AES => AES_EXPLANATION,
                RequiredCPUInstruction.AVX => AVX_EXPLANATION,
                RequiredCPUInstruction.AVX2 => AVX2_EXPLANATION,
                RequiredCPUInstruction.BMI1 => BMI_EXPLANATION,
                RequiredCPUInstruction.BMI2 => BMI_EXPLANATION,
                RequiredCPUInstruction.FMA => FMA_EXPLANATION,
                RequiredCPUInstruction.LZCNT => LZCNT_EXPLANATION,
                RequiredCPUInstruction.PCLMULQDQ => PCLMULQDQ_EXPLANATION,
                RequiredCPUInstruction.POPCNT => POPCNT_EXPLANATION,
                RequiredCPUInstruction.SSE2 => SSE2_EXPLANATION,
                RequiredCPUInstruction.SSE3 => SSE3_EXPLANATION,
                RequiredCPUInstruction.SSSE3 => SSE3_EXPLANATION,
                RequiredCPUInstruction.SSE4 => SSE4_EXPLANATION,
                _ => "Invalid instruction provided, this shouldn't be trigger unless there is a bug in CPUInfoManager.GetExplanationForInstruction()",
            };
        }
    }

    public class CPUCoreManager() 
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static int GetCoreCount()
        {
            if (IsWindows)
                return Win.GetPhysicalCoreCount();
            
            if (IsUnixLike) 
                return GetPhysicalCoreCountUnixLike();  
            
            Errors.ThrowUnsupportedPlatformException();
            return 0; // This wont be executed, roslyn has no idea an exception has been thrown, so this is required.
        }
        private static int GetPhysicalCoreCountUnixLike()
        {
            string actionString = "determine the amount of physical CPU cores on your system";

            var psi = (IsLinux) switch
            {
                true => new ProcessStartInfo()
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"lscpu -p | egrep -v '^#' | sort -u -t, -k 2,4 | wc -l\"", // lscpu doesnt require sudo privileges on linux but sysctl does since it handles linux kernel data.
                },

                false => new ProcessStartInfo()
                {
                    FileName = "/usr/sbin/sysctl",
                    Arguments = "-n hw.physicalcpu",
                }
            };

            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;

            using Process process = ProcessFactory.SpawnProcess(psi, actionString, writeSTDInOut: false, runSync: true, timeout: 10).Result;
            (var ExitCode, var STDOut, var STDErr) = ProcessFactory.GetProcessResponse(process).Result;

            var response = ProcessFactory.GetProcessResponse(process).Result;

            return (int)HandleSingleLineProcessOutput(actionString, STDOut, typeof(int));

        }

        public static object HandleSingleLineProcessOutput(string actionString, List<string> STDOut, Type returnType)
        {
            string failureMessage =
            $"BAM Manager (BAMM) was unable to {actionString}, " +
            $"if this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
            "Error log:\n";

            switch (STDOut.Count)
            {
                case 0:
                    failureMessage += "Command returned no output.";
                    Errors.WriteAndExit(failureMessage, 1);
                    break;

                case 1 when returnType.Equals(typeof(int)):
                    return int.TryParse(STDOut[0], out int res) ? res : 0;

                case 1 when returnType.Equals(typeof(string)):
                    return !string.IsNullOrEmpty(STDOut[0]) ? STDOut[0] : string.Empty;

                // Fallback for cases where an unsupported returnType is provided.
                case 1:
                    failureMessage += "Invalid returnType passed to HandleSingleLineProcessOutput()";
                    Errors.WriteAndExit(failureMessage, 1);
                    break;

                default:
                    failureMessage += $"Command returned invalid output.\n\nOutput:\n{string.Join("\n", STDOut)}";
                    Errors.WriteAndExit(failureMessage, 1);
                    break;
            }
            return -1;
        }
    }
}
