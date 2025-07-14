using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Messaging;
using Microsoft.Win32;
using Windows.Win32;
using System.Runtime.InteropServices;
using Windows.Win32.System.SystemInformation;

namespace BrowserAutomationMaster.Managers.AppManager.OS
{
    [SupportedOSPlatform("windows6.1.7601")] // >= 6.1.7601
    public static partial class Win
    {
        public static List<AppInfo> GetApps()
        {
            var apps = new List<AppInfo>();
            try
            {
                apps.AddRange(QueryRegistryForApps(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
                apps.AddRange(QueryRegistryForApps(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"));
                apps.AddRange(QueryRegistryForApps(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"));
            }
            catch { 
                Errors.WriteErrorAndExit(
                    message: "BAM Manager was unable to query Windows Registry, please try again; if this issue persists, it's likely a bug.",
                    status: 1
                );
            }
            return apps;
        }

        private static List<AppInfo> QueryRegistryForApps(RegistryHive hive, string subKeyPath)
        {
            var list = new List<AppInfo>();
            using (RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
            using (RegistryKey? key = baseKey.OpenSubKey(subKeyPath))
            {
                if (key == null)
                    return list;

                foreach (var subkeyName in key.GetSubKeyNames())
                {
                    using RegistryKey? subkey = key.OpenSubKey(subkeyName);
                    if (subkey == null) { continue; }
                    string? name = subkey?.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(name)) { continue; }

                    string? version = subkey?.GetValue("DisplayVersion") as string;
                    string? publisher = subkey?.GetValue("Publisher") as string;

                    list.Add(new AppInfo
                    {
                        Name = name,
                        Version = version ?? "Not Found",
                        Publisher = publisher ?? "Not Found"
                    });
                }
            }
            return list;
        }

        public static void VerifyRootDrive(string[] args)
        {
            try
            {
                if (args.Contains("--ignore-drive-root")) { return; }
                string? rootDrive = Path.GetPathRoot(AppContext.BaseDirectory);

                if (rootDrive == null || !rootDrive.StartsWith("C:"))
                {
                    Errors.WriteErrorAndExit(
                        message: 
                            "BAM Manager (BAMM) was developed to be ran on the C: drive.\n\n" +
                            "Running this application on a different drive caused too many unforseeable bugs.\n\n" +
                            "If you are contributing to development, you can bypass this restriction by passing the argument" +
                            "'--ignore-drive-root'.", 
                        status: 1
                    );
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        #region Python Version Functions for Windows Users

        // Regex to find paths starting with a drive letter, containing path separators, and ending with python.exe
        // Example: "-V:3.12 * C:\Users\UserName\AppData\Local\Programs\Python\Python312\python.exe" -> "C:\Users\UserName\AppData\Local\Programs\Python\Python312\python.exe"
        [GeneratedRegex(@"[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*python\.exe", RegexOptions.IgnoreCase)]
        private static partial Regex PrecompiledPythonPathRegex();
        public static string GetInterpreterPath()
        {
            try
            {
                List<string> discoveredPython3Paths = [];
                List<string> discoveredPython2Paths = [];

                (int pyExitCode, string pyOutput, string pyError) = RunCommand("py", "--list-paths"); // Runs py(.exe) --list-paths

                if (pyExitCode == 0 && !string.IsNullOrWhiteSpace(pyOutput))
                {
                    MatchCollection matches = PrecompiledPythonPathRegex().Matches(pyOutput);

                    foreach (Match match in matches)
                    {
                        string potentialPath = match.Value.Trim();

                        // Excludes WindowsApp PyLauncher
                        if (potentialPath.Contains(@"\Microsoft\WindowsApps\python.exe")) { continue; } 
                        string versionOutput = GetIntepreterVersion(potentialPath, "--version");
                        if (versionOutput.StartsWith("Python 3.", StringComparison.OrdinalIgnoreCase)) { 
                            discoveredPython3Paths.Add(potentialPath); 
                        }
                        else if (versionOutput.StartsWith("Python 2.", StringComparison.OrdinalIgnoreCase)) { 
                            discoveredPython2Paths.Add(potentialPath); 
                        }
                    }
                }

                // Remove duplicates if present.
                discoveredPython3Paths = [.. discoveredPython3Paths.Distinct(StringComparer.OrdinalIgnoreCase)];
                discoveredPython2Paths = [.. discoveredPython2Paths.Distinct(StringComparer.OrdinalIgnoreCase)];

                // Warn about potential instability when both python 2.X and 3.X are present.
                if (discoveredPython2Paths.Count > 0) {
                    Warning.Write(
                        message:
                            "While BAM Manager (BAMM) can run with both Python 2.X and 3.X installed, " +
                            "it may cause instability.\nIf possible please uninstall python 2.X, or use a virtual machine."
                    );
                }

                // Handle Python 3 paths found
                if (discoveredPython3Paths.Count == 0) {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\n" +
                            $"If this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                            $"Error log:\nNo valid Python 3 interpreter found in system PATH after checking with 'py.exe'.", 
                        status: 1
                    );
                }

                return SelectPythonPath([.. discoveredPython3Paths]);
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\n" +
                        $"If this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error log:\nNo valid Python 3 interpreter found in system PATH after checking with 'py.exe'." +
                        $"\nException returned: {e.Message}", 
                    status: 1
                );
                return string.Empty;
            }
        }
        private static string SelectPythonPath(string[] python3Paths)
        {
            if (python3Paths.Length == 0)
            {
                Errors.WriteErrorAndExit(
                    
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\n" +
                        $"If this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error log:\nAppManager.OS.Win.SelectPythonPath was passed an empty array.", 
                    status: 1
                );
                return string.Empty;
            }

            if (python3Paths.Length == 1) { return python3Paths[0]; }

            string choicesMessage = "Multiple Python 3 interpreters found.\n";
            for (int i = 0; i < python3Paths.Length; i++) { choicesMessage += $"{i + 1}. {python3Paths[i]}\n"; }

            string promptMessage = choicesMessage + 
                $"Please select the number correlating to your desired intepreter version.\n" +
                $"Between [1-{python3Paths.Length}]:\n";

            while (true)
            {
                string rawChoice = Input.WriteTextAndReturnRawInput(promptMessage) ?? "";
                if (int.TryParse(rawChoice, out int choice) && choice >= 1 && choice <= python3Paths.Length) { 
                    return python3Paths[choice - 1]; 
                }
                Warning.Write($"Invalid input. Please enter a number between 1 and {python3Paths.Length}.");
            }
        }

        private static (int exitCode, string output, string error) RunCommand(string command, string arguments = "")
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}" + (arguments != null ? $" {arguments}" : ""),
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = new() { StartInfo = startInfo };
            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return (process.ExitCode, output, error);
            }
            catch (Exception ex) { 
                return (
                    exitCode: -1, 
                    output: string.Empty, 
                    error: $"Exception running 'cmd.exe /c {command} {arguments}': {ex.Message}"
                ); 
            }
        }

        private static string GetIntepreterVersion(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = new() { StartInfo = startInfo };
            try
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\n" +
                        $"If this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error log:\nAppManager.OS.Win.GetIntepreterVersion returned the following exception:\n{e.Message}", 
                    status: 1
                );
                return string.Empty;
            }
        }

        #endregion

        #region P/Invoke GetLogicalProcessorInformationEx -> GetPhysicalCoreCount()

        // Unsafe accessor required for casting null to SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        public unsafe static int GetPhysicalCoreCount()
        {
            try
            {
                uint bufferSize = 0;

                // This is expected to fail, it requires a 2 pass system
                // firstResult: returns the bufferSize of the given CPU topology
                // secondResult: uses the bufferSize as a ref object and iterates over the structs, counts RelationProcessCore(s)number
                bool firstResult = PInvoke.GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                    (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)null,
                    ref bufferSize
                );


                // 122 is the err code for ERROR_INSUFFICIENT_BUFFER (it wont import for some reason)
                if (!firstResult && Marshal.GetLastWin32Error() != 122)
                {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, " +
                            $"if this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\nError log:\n\n" +
                            $"AppManager.OS.Windows.GetPhysicalCoreCount() Failed to get logical processor information buffer size," +
                            $" the last Win32 Error was:\n{Marshal.GetLastWin32Error()}",
                        status: 1
                    );
                }

                // If the buffer is empty, a fatal error has occured.
                if (bufferSize == 0)
                {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, " +
                            $"if this issue persists, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                            $"Error log:\nAppManager.OS.Windows.GetPhysicalCoreCount() returned a buffer size of 0.",
                        status: 1
                    );
                }

                var buffer = Marshal.AllocHGlobal((int)bufferSize); // Allocates N bytes from bufferSize

                bool secondResult = PInvoke.GetLogicalProcessorInformationEx(
                    LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore,
                    (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX*)buffer,
                    ref bufferSize
                );

                if (!secondResult)
                {
                    throw new Exception($"Failed to get logical processor information. Win32 Error: {Marshal.GetLastWin32Error()}");
                }

                int physicalCoreCount = 0;
                uint bytesParsed = 0;

                nint currentPtr = buffer;

                // Debug values
                // Console.WriteLine("\n--- Debugging GetLogicalProcessorInformationEx Entries ---");
                // Console.WriteLine($"Total buffer size: {bufferSize} bytes");
                // Console.WriteLine(bufferSize);
                while (bytesParsed < bufferSize)
                {
                    // Deserializes the raw bytes of the currentPtr to the SYSTEM_PROCESSOR_INFORMATION_EX struct
                    var currentInfoExHeader = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX>(currentPtr);

                    // Debug values
                    // Console.WriteLine($"Bytes parsed: {bytesParsed}");
                    // Console.WriteLine($"\n  Entry at offset {currentPtr.ToInt64() - Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0).ToInt64()}:");
                    // Console.WriteLine($"    Relationship: {currentInfoExHeader.Relationship}");
                    // Console.WriteLine($"    Entry Size: {currentInfoExHeader.Size}");

                    if (currentInfoExHeader.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore)
                    {
                        physicalCoreCount++;
                    }

                    // Move to the next structure in the buffer
                    currentPtr += (nint)currentInfoExHeader.Size; // I SPENT 10 minutes before I realized wasn't being incremented.
                    bytesParsed += currentInfoExHeader.Size;
                }

                return physicalCoreCount;
            }
            catch (Exception ex) {
                string errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                Errors.WriteErrorAndExit(
                    message:
                        "BAM Manager (BAMM) was unable to determine the number of physical CPU cores present, if this issue persists, " +
                        $"please make a bug report at {ConstantManager.ISSUES_LINK}\n\nError log:\n{errorMessage}.",
                    status: 1
                );
            }
            return 0; // Wont be reached.
        }

        #endregion
    }
}