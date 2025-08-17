using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers
{
    public enum X64Instructions
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

        // Minimum cores supported: 2
        // Minimum cores recommended: 4
        // Ensure all requiredInstructions
        // Verify presence of recommendedInstructions, if not inform the user of what they are missing, what it does, and whether or not they want to continue.
        // string[] requiredInstructions = ["x64", "AVX", "SSE2", "SSE3", "SSSE3", "SSE4.1", "SSE4.2"];
        // string[] recommendedInstructions = ["AES", "AVX2", "BMI1", "BMI2", "FMA3", "LZCNT", "PopCnt", "PCLMULQDQ", "TZCNT"];

        public const string X64_EXPLANATION = "X86-64, commonly referred to as x64, is the modern implementation of CPU architecture, it's the reason our system's aren't limited to 4GiB of RAM.";
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
        readonly private static List<X64Instructions> unsupportedInstructions = [];

        private static bool ContainsNeededInstructions()
        {
            if (!X86Base.X64.IsSupported) { unsupportedInstructions.Add(X64Instructions.X64); }
            if (!Avx.IsSupported) { unsupportedInstructions.Add(X64Instructions.AVX); }
            if (!Sse2.IsSupported) { unsupportedInstructions.Add(X64Instructions.SSE2); }
            if (!Sse3.IsSupported) { unsupportedInstructions.Add(X64Instructions.SSE3); }
            if (!Ssse3.IsSupported) { unsupportedInstructions.Add(X64Instructions.SSSE3); }
            if (!Sse41.IsSupported) { unsupportedInstructions.Add(X64Instructions.SSE4); }
            if (!Sse42.IsSupported) {
                if (!unsupportedInstructions.Contains(X64Instructions.SSE4)) {
                    unsupportedInstructions.Add(X64Instructions.SSE4);  // Prevents any issues with a duplicates on SSE4
                }
            }

            // Originally ContainsRecommendedInstructions()
            if (!Aes.IsSupported) { unsupportedInstructions.Add(X64Instructions.AES); }
            if (!Avx2.IsSupported) { unsupportedInstructions.Add(X64Instructions.AVX2); }
            if (!Bmi1.IsSupported) { unsupportedInstructions.Add(X64Instructions.BMI1); } // TZCNT is a part of Bmi1 for some reason whereas Lzcnt isn't
            if (!Bmi2.IsSupported) { unsupportedInstructions.Add(X64Instructions.BMI2); }
            if (!Fma.IsSupported) { unsupportedInstructions.Add(X64Instructions.FMA); }
            if (!Lzcnt.IsSupported) { unsupportedInstructions.Add(X64Instructions.LZCNT); }
            if (!Popcnt.IsSupported) { unsupportedInstructions.Add(X64Instructions.POPCNT); }
            if (!Pclmulqdq.IsSupported) { unsupportedInstructions.Add(X64Instructions.PCLMULQDQ); }

            return unsupportedInstructions.Count == 0; // The application will exit if this returns false
        }

        public static bool IsMissingInstructions() {
            if (!ContainsNeededInstructions()) { return true; }
            return false;
        }

        public bool HasEnoughCores()
        {
            if (Cores < 2) { return false; }
            if (Cores <= 4) {
                if (GlobalConfig.ShowCpuCheck) {
                    Warning.Write(
                        message:
                            $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                            $"this might impact your performance slightly if your CPU is older.\n"
                    );
                }
            }
            else {
                if (GlobalConfig.ShowCpuCheck)
                {
                    Success.WriteSuccessMessage(message:
                    $"BAM Manager (BAMM) has determined your cpu has {Cores} cores, " +
                    $"you should not experience any performance issues directly related to your CPU.\n"
                    );
                }
            }
            return true;
        }

        public static void DisplayMissingInstructions()
        {
            foreach (X64Instructions instruction in unsupportedInstructions) {
                Spectre.Console.AnsiConsole.Write($"{instruction} is unsupported on the current CPU.");
            }
        }

        private static string GetExplanationForInstruction(X64Instructions instruction)
        {
            return instruction switch
            {
                X64Instructions.X64 => X64_EXPLANATION,
                X64Instructions.AES => AES_EXPLANATION,
                X64Instructions.AVX => AVX_EXPLANATION,
                X64Instructions.AVX2 => AVX2_EXPLANATION,
                X64Instructions.BMI1 => BMI_EXPLANATION,
                X64Instructions.BMI2 => BMI_EXPLANATION,
                X64Instructions.FMA => FMA_EXPLANATION,
                X64Instructions.LZCNT => LZCNT_EXPLANATION,
                X64Instructions.PCLMULQDQ => PCLMULQDQ_EXPLANATION,
                X64Instructions.POPCNT => POPCNT_EXPLANATION,
                X64Instructions.SSE2 => SSE2_EXPLANATION,
                X64Instructions.SSE3 => SSE3_EXPLANATION,
                X64Instructions.SSSE3 => SSE3_EXPLANATION,
                X64Instructions.SSE4 => SSE4_EXPLANATION,
                _ => "Invalid instruction provided, this shouldn't be trigger unless there is a bug in CPUInfoManager.GetExplanationForInstruction()",
            };
        }
    }

    public class CPUCoreManager() // This doesn't need to be public
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static int GetCoreCount()
        {
            if (RuntimeManager.IsSupportedWindowsVersion()) {
                return Win.GetPhysicalCoreCount();
            }
            if (RuntimeManager.IsSupportedOSXVersion()) {
                return GetPhysicalCoreCountMacOS(); 
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { 
                return GetPhysicalCoreCountLinux(); 
            }
            Errors.ThrowUnsupportedPlatformException();
            return 0; // This wont be executed, roslyn has no idea an exception has been thrown, so this is required.
        }

        private static int GetPhysicalCoreCountMacOS()
        {
            try
            {
                ProcessStartInfo coreCountProcessInfo = new() {
                    FileName = "/usr/sbin/sysctl",
                    Arguments = "-n hw.physicalcpu",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process? process = Process.Start(coreCountProcessInfo);
                if (process == null) {
                    Errors.WriteErrorAndExit(
                        $"BAM Manager (BAMM) was unable to check the number of physical cores on the current machine, please make sure you are on an admin account.\n\nIf this continues, please make a bug report at {ISSUES_LINK}\n\nError log:\nprocess returned null\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                    return -1; // This is purely to appease the compiler since -> process.StandardOutput has plausibility to be null according to the compiler, this is incorrect given that the function above kills the main thread.
                }
                Process coreCountProcess = new() { StartInfo = coreCountProcessInfo };
                coreCountProcess.Start();
                coreCountProcess.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string errorOutput = process.StandardError.ReadToEnd();


                if (coreCountProcess.ExitCode != 0) {
                    Errors.WriteErrorAndExit(
                        message:   
                            $"BAM Manager (BAMM) was unable to give corecheck.sh executable permissions.\n\n" +
                            $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                            $"Error log:\nchmod failed with exit code {coreCountProcess.ExitCode}", 
                        status: 1
                    );
                }

              
                if (int.TryParse(output, out int coreCount)) { return coreCount; }

                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to determine the amount of physical CPU cores on your system.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\ncorecheck.sh returned the following error:\n{errorOutput}\n" +
                        $"Exit Code: {coreCountProcess.ExitCode}",
                    status: 1
                );
            }
            catch (Exception ex) {
                Errors.WriteErrorAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the amount of physical CPU cores on your system.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n{ex}\n\n{ex.InnerException}",
                    status: 1
                );
            }
            return -1;
        }

        private static int GetPhysicalCoreCountLinux()
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"lscpu -p | egrep -v '^#' | sort -u -t, -k 2,4 | wc -l\"", // lscpu doesnt require sudo privileges on linux but sysctl does since it handles linux kernel data.
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            if (string.IsNullOrEmpty(output) && !string.IsNullOrEmpty(error) || process.ExitCode != 0) {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, " +
                        $"if this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\n{error}", 
                    status: 1
                );
            }
            if (!int.TryParse(output, out int coreCount)) {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, " +
                        $"if this issue persists, please make a bug report at {ISSUES_LINK}\n" +
                        $"Error log:\nManagers.CPUInfoManager.GetPhysicalCoreCountLinux() returned an invalid input.", 
                    status: 1
                );
            }
            return coreCount;
        }
    }
}
