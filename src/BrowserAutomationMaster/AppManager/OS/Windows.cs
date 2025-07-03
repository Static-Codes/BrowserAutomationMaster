using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Messaging;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace BrowserAutomationMaster.AppManager.OS
{
    [SupportedOSPlatform("windows")]
    public static partial class Windows
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
            catch { Errors.WriteErrorAndExit("BAM Manager was unable to query Windows Registry, please try again; if this issue persists, it's likely a bug.", 1); }
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
                    Errors.WriteErrorAndExit("BAM Manager (BAMM) was developed to be ran on the C: drive.\n\nRunning this application on a different drive caused too many unforseeable bugs, so i've decided to prevent it from happening all together.\n\nIf you are contributing to development, you can bypass this restriction by passing the argument '--ignore-drive-root'.", 1);
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

                (int pyExitCode, string pyOutput, string pyError) pyLauncherResult = RunCommand("py", "--list-paths"); // Runs py(.exe) --list-paths

                if (pyLauncherResult.pyExitCode == 0 && !string.IsNullOrWhiteSpace(pyLauncherResult.pyOutput))
                {
                    MatchCollection matches = PrecompiledPythonPathRegex().Matches(pyLauncherResult.pyOutput);

                    foreach (Match match in matches)
                    {
                        string potentialPath = match.Value.Trim();

                        if (potentialPath.Contains(@"\Microsoft\WindowsApps\python.exe")) { continue; } // Excludes WindowsApp PyLauncher
                        string versionOutput = GetIntepreterVersion(potentialPath, "--version");
                        if (versionOutput.StartsWith("Python 3.", StringComparison.OrdinalIgnoreCase)) { discoveredPython3Paths.Add(potentialPath); }
                        else if (versionOutput.StartsWith("Python 2.", StringComparison.OrdinalIgnoreCase)) { discoveredPython2Paths.Add(potentialPath); }
                    }
                }

                // Remove duplicates if present.
                discoveredPython3Paths = [.. discoveredPython3Paths.Distinct(StringComparer.OrdinalIgnoreCase)];
                discoveredPython2Paths = [.. discoveredPython2Paths.Distinct(StringComparer.OrdinalIgnoreCase)];

                // Warn about potential instability when both python 2.X and 3.X are present.
                if (discoveredPython2Paths.Count > 0) {
                    Warning.Write("While BAM Manager (BAMM) can run with both Python 2.X and 3.X installed, it may cause instability.\nIf possible please uninstall python 2.X, or use a virtual machine.");
                }

                // Handle Python 3 paths found
                if (discoveredPython3Paths.Count == 0) {
                    Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\nIf this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nNo valid Python 3 interpreter found in system PATH after checking with 'py.exe'.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                    return string.Empty; // Should not be reached
                }

                return SelectPythonPath([.. discoveredPython3Paths]);
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\nIf this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nNo valid Python 3 interpreter found in system PATH after checking with 'py.exe'.\nException returned: {e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return string.Empty;
            }
        }
        private static string SelectPythonPath(string[] python3Paths)
        {
            if (python3Paths.Length == 0)
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\nIf this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nAppManager.OS.Windows.SelectPythonPath was passed an empty array.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return string.Empty;
            }

            if (python3Paths.Length == 1) { return python3Paths[0]; }

            string choicesMessage = "Multiple Python 3 interpreters found.\n";
            for (int i = 0; i < python3Paths.Length; i++) { choicesMessage += $"{i + 1}. {python3Paths[i]}\n"; }

            string promptMessage = choicesMessage + $"Please select the number correlating to your desired intepreter version.\nBetween [1-{python3Paths.Length}]:\n";

            while (true)
            {
                string rawChoice = Input.WriteTextAndReturnRawInput(promptMessage) ?? "";
                if (int.TryParse(rawChoice, out int choice) && choice >= 1 && choice <= python3Paths.Length) { return python3Paths[choice - 1]; }
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
            catch (Exception ex) { return (-1, string.Empty, $"Exception running 'cmd.exe /c {command} {arguments}': {ex.Message}"); }
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
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to determine the system environment variable for python 3.X.\nIf this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster/issues\n\nError log:\nAppManager.OS.Windows.GetIntepreterVersion returned the following exception:\n{e.Message}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
                return string.Empty;
            }
        }

        #endregion

        #region P/Invoke GetLogicalProcessorInformationEx -> GetPhysicalCoreCount()
        private enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore, // Only processor core is actually required this is a dirty workaround to use less overhead.
            //RelationNumaNode,
            //RelationCache,
            //RelationProcessorPackage,
            //RelationGroup,
            //RelationAll = 0xffff
        }

        [StructLayout(LayoutKind.Explicit)] // LayoutKind.Explicit is required to use FieldOffsetAttribute(s)
        private struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            [FieldOffset(0)] public LOGICAL_PROCESSOR_RELATIONSHIP Relationship; // In this nuanced case, only RelationProcessCore is needed but the fields in the struct remain the same, regardless of this change.
            [FieldOffset(4)] public uint Size; // Total structure size with variable length data included.  Will be used to keep track of the position in the current buffer.
        }

        [DllImport("kernel32.dll", SetLastError = true)] // SetLastError will be overwritten as each new error is added to the stack, if the error if fatal, it will be displayed and the application will exit.
        [return: MarshalAs(UnmanagedType.Bool)] // Ensure b4b compatibility between c bytes (4 byte bool) and c# bytes (1 byte bool)

        // c# implemenation of https://learn.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getlogicalprocessorinformationex
        private static extern bool GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP RelationshipType, [In, Out] byte[] Buffer, ref uint ReturnedLength);

        public static int GetPhysicalCoreCount()
        {
            uint bufferSize = 0;
            // Uses the bufferSize as a reference value, returns false if an InvalidOperation is reached.
            bool success = GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, null!, ref bufferSize);

            if (!success && Marshal.GetLastWin32Error() != 122)
            {  // 122 is the err code for ERROR_INSUFFICIENT_BUFFER
                Errors.WriteErrorAndExit($"BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, if this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster\n\nError log:\n\nAppManager.OS.Windows.GetPhysicalCoreCount() Failed to get logical processor information buffer size, the last Win32 Error was:\n{Marshal.GetLastWin32Error()}\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }

            if (bufferSize == 0) {
                Errors.WriteErrorAndExit($"BAMM Manager (BAMM) was unable to determine the number of physical CPU cores present in your system, if this issue persists, please make a bug report at https://github.com/static-codes/BrowserAutomationMaster\n\nError log:\nAppManager.OS.Windows.GetPhysicalCoreCount() returned a buffer size of 0.\n\n{Messaging.Debug.GetPlatformInfoForErrorLog()}", 1);
            }

            byte[] buffer = new byte[bufferSize];
            success = GetLogicalProcessorInformationEx(LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore, buffer, ref bufferSize);

            if (!success) { throw new Exception($"Failed to get logical processor information. Win32 Error: {Marshal.GetLastWin32Error()}"); }
            int physicalCoreCount = 0;

            IntPtr currentPtr = Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0); // Get pointer to start of buffer
            IntPtr bufferEndPtr = IntPtr.Add(currentPtr, (int)bufferSize);

            // Debug values
            // Console.WriteLine("\n--- Debugging GetLogicalProcessorInformationEx Entries ---");
            // Console.WriteLine($"Total buffer size: {bufferSize} bytes");

            while (currentPtr.ToInt64() < bufferEndPtr.ToInt64())
            {
                // The Relationship is used to count the number of physical cores, and the size of the current entry is used to properly pointer to the next structure in the buffer
                SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX currentInfoExHeader = (SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX)Marshal.PtrToStructure(currentPtr, typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX))!;

                // Debug values
                // Console.WriteLine($"\n  Entry at offset {currentPtr.ToInt64() - Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0).ToInt64()}:");
                // Console.WriteLine($"    Relationship: {currentInfoExHeader.Relationship}");
                // Console.WriteLine($"    Entry Size: {currentInfoExHeader.Size}");

                if (currentInfoExHeader.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.RelationProcessorCore) { physicalCoreCount++; }
                currentPtr = IntPtr.Add(currentPtr, (int)currentInfoExHeader.Size); // Move to the next structure in the buffer
            }

            return physicalCoreCount;
        }

        #endregion
    }
}