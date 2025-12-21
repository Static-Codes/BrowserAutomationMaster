using System.Diagnostics;
using static BrowserAutomationMaster.Managers.AppManager.OS.Win;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using BrowserAutomationMaster.Managers.AppManager;

namespace BrowserAutomationMaster.Managers
{
    public class EditorManager()
    {

        public static Dictionary<string, string> GetSupportedEditors() {
            return (Platforms.IsWindows, Platforms.IsOSX, Platforms.IsLinux) switch {
                (true, false, false) => GetSupportedWindowsEditors(),
                (false, true, false) => GetSupportedMacEditors(),
                (false, false, true) => GetSupportedLinuxEditors(),
                _ => throw new PlatformNotSupportedException("Invalid OS.")
            };
        }

        public static Dictionary<string, string> GetSupportedWindowsEditors() 
        {
            if (InstalledApps.AppInfoList.Count == 0) 
            {
                Console.WriteLine($"A fatal error occured, please make a bug report with the following error at {ISSUES_LINK}");
                WriteAndExit
                (
                    string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to query for supported text editors.",
                        "Error Log:",
                        "InstalledApps.AppInfoList.Count return a zero value, indicating no applications were detected, this is a huge bug and needs to addressed."
                    ]),
                    status: 1,
                    writePlatformDebugInfo: true
                );
            }

            var supportedApps = new Dictionary<string, string>
            {
                { "Notepad++", @"C:\Program Files\Notepad++\notepad++.exe" },
                { "PyCharm", "pycharm64.exe" },
                { "Sublime", @"C:\Program Files\Sublime Text\sublime_text.exe" },
                { "VSCode", @"%APPDATA%\Local\Programs\Microsoft VS Code\Code.exe" },
                { "VSCodium", @"%APPDATA%\Local\Programs\VSCodium\VSCodium.exe" },
            };

            Dictionary<string, string> installedEditors = supportedApps
                .Where(app => File.Exists(app.Value))
                .ToDictionary();

            // This isn't a secure check because the input is not sanitized.
            // This isnt an issue currently due to the nature of this application, however in a hypothetical situation the following is true: 
            // A malicious registry key could be inserted pointing to a malicious executable placed in "C:\Program Files\JetBrains\PyCharm*\pycharm.exe"
            // This could cause BAMM to attempt to open the incorrect executable, if it was coded correctly it would then .
            // This is purely hypothetical once again but definitely noteworthy for a hardened solution in the future. 
            var pyCharmEntry = 
                InstalledApps.AppInfoList.Where(
                    app => app.Path
                        .StartsWith(@"C:\Program Files\JetBrains\PyCharm") && 
                        app.Path.EndsWith("pycharm64.exe")
                    ).FirstOrDefault();
                

            if (pyCharmEntry != null) 
            {
                installedEditors.Add(pyCharmEntry.Name, pyCharmEntry.Path);
            }

            return installedEditors;
        }

        // Doesn't check for vim, textedit, or xcode.
        // public static async Task<string[]> GetSupportedMacEditors() 
        // {
        //     var actionText = "checking for the supported text editors";
        //     var errorMessage = $"An error occured while {actionText}.";

        //     var supportedMacEditors = new string[4] {
        //         "PyCharm.app",
        //         "Sublime Text.app",
        //         "VSCodium.app",
        //         "Visual Studio Code.app"
        //     };

        //     var psi = new ProcessStartInfo() {
        //         FileName = "/bin/bash",
        //         ArgumentList = {
        //             "-c",
        //             "ls -1",
        //             "/Applications/"
        //         },
        //         RedirectStandardError = true,
        //         RedirectStandardInput = true,
        //         RedirectStandardOutput = true,
        //     };

        //     (int ExitCode, List<string> STDOut, List<string> STDErr) = (-1, [], []);

        //     try {
        //         var process = await ProcessFactory.SpawnProcess(psi, "checking for supported text editors", writeSTDInOut: false, timeout: 10);
        //         (ExitCode, STDOut, STDErr) = await ProcessFactory.GetProcessResponse(process);

        //         if (ExitCode == -1 || ExitCode > 0) {
        //             WriteAndExit($"{errorMessage}{NLC}Exit Code: {ExitCode}", 1);
        //         }

        //         if (STDErr.Count > 0 && STDOut.Count == 0){
        //             WriteAndExit($"{errorMessage}{NLC}{NLC}Error Log:{NLC}{string.Join(NLC, STDErr)}", 1);
        //         }

        //         if (STDOut.Count == 0) {
        //             var errorLog = $"The command '{string.Join(' ', psi.Arguments)}' returned no output";
        //             WriteAndExit($"{errorMessage}{NLC}{NLC}Error Log:{NLC}{errorLog}", 1);
        //         }

        //     }
        //     catch (Exception ex) {
        //         WriteAndExit($"{errorMessage}{NLC}{NLC}Error Log:{NLC}{ex.StackTrace}", 1);
        //     }

        //     return [.. STDOut.Where(app => supportedMacEditors.Contains(app))];
            
        // }

        // Doesn't check for vim, textedit, or xcode.
        public static Dictionary<string, string> GetSupportedMacEditors()
        {
            var supportedAppNames = new Dictionary<string, string>
            {
                { "PyCharm", "PyCharm.app" },
                { "Sublime", "Sublime Text.app" },
                { "VSCode", "Visual Studio Code.app" },
                { "VSCodium", "VSCodium.app" },
            };
            
            var applicationsPath = "/Applications/";

            // This SHOULD NEVER BE EXECUTED, IF IT IS A HUGE PROBLEM IS PRESENT.
            if (!Directory.Exists(applicationsPath))
            {
                WriteAndExit($"The expected directory '{applicationsPath}' does not exist.", 1);
                return [];
            }

            var installedEditors = supportedAppNames
                .Where(app => Directory.Exists(Path.Combine(applicationsPath, app.Value)))
                .ToDictionary();

            return installedEditors;
        }

        public static Dictionary<string, string> GetSupportedLinuxEditors()
        {
            const char DIR_ESC = '/';
            const char WILDCARD = '*';
            const char CMD_ESC_CHAR = '"';
            

            var potentialEditors = new Dictionary<string, string>()
            {
                { "Helix", "/usr/lib/helix"},
                { "Nano", "/usr/bin/nano"},
                { "PyCharm", "*/bin/pycharm" },
                { "Sublime Text", "/opt/sublime_text/sublime_text"},
                { "VSCode", "/usr/share/code/bin/code"},
                { "VSCodium", "/usr/bin/codium"},
                { "Vim (Advanced Users)", "vi" }
            };

            static string? GetLinuxSearchCommand(string argument)
            {

                bool hasWildcard = argument.Contains(WILDCARD);
                bool hasDirEsc = argument.Contains(DIR_ESC);

                if (hasWildcard && hasDirEsc)
                {
                    return string.Join(' ', [
                        CMD_ESC_CHAR,
                        "find",
                        "/opt",
                        "/usr/bin",
                        "/usr/share",
                        "/usr/local",
                        "-wholename",
                        argument,
                        "-print",
                        "-quit",
                        CMD_ESC_CHAR
                    ]);
                }
                else if (hasDirEsc)
                {
                    return File.Exists(argument) ? "Found" : "Not Found";
                }
                else
                {
                    // Assumed to be a simple command name like 'vi' (Vim) 
                    return CommandExists(argument) ? "Found" : "Not Found";
                }
            }

            var foundEditors = new Dictionary<string, string>();

            foreach (var editor in potentialEditors)
            {
                string editorKey = editor.Key;
                string editorPathValue = editor.Value;

                var commandResultType = GetLinuxSearchCommand(editorPathValue);

                if (commandResultType == "Found") {
                    foundEditors.Add(editorKey, editorPathValue);
                } else if (commandResultType == "Not Found") {
                    continue;
                } else if (commandResultType is not null) {
                    // Means the find command was successfully generated 
                    string command = commandResultType;

                    // The command is only executed if the editorPathValue is not a direct path to a file and the path contains a wildcard.
                    if (editorPathValue.Contains(WILDCARD) || !File.Exists(editorPathValue))
                    {
                        // DEBUGGING ONLY DO NOT REMOVE
                        // Console.WriteLine($"Executing:{NLC}/bin/bash -c {command}");
                        var commandOutput = RunCommand("/bin/bash", $"-c {command}");

                        if (!string.IsNullOrEmpty(commandOutput))
                        {
                            string foundPath = commandOutput.Trim();
                            foundEditors.Add(editorKey, foundPath);
                        }
                    }
                } else {
                    // commandResultType is null a direct path check
                    if (File.Exists(editorPathValue))
                    {
                        foundEditors.Add(editorKey, editorPathValue);
                    }
                }
            }
            return foundEditors;
        }

        // Refactor with a custom struct/class
        public string[] SupportedEditors = [
            // Open source fork of Visual Studio Code
            "Codium", 
            // Windows notepad
            "Notepad",
            // Fork/Rewrite of Windows notepad (I believe the command is npp)
            "Notepad++", 
            // JetBrains IDE
            "PyCharm",
            // Cross Platform IDE
            "Sublime Text",
            // The bloated older brother to visual studio code.
            "Visual Studio",
            // A much sleeker version of visual studio, which is cross. (`code <filename>` to open the file)
            "Visual Studio Code",
            // Built in text-editor in linux (not to be confused with xcode which calls on xed and is not supported.)
            "Xed", 
        ];
    };

    // Left for reference after hours of proper debugging.
    // public class Editor()
    // {
    //     public required (string Windows, string Mac, string Linux) Names;
    //     public required (bool Windows, bool Mac, bool Linux) Supports;
    //     private (string Windows, string Mac, string Linux) ShellNames = ("cmd.exe", "/bin/bash", "/bin/bash");
    //     private (string Windows, string Mac, string Linux) ShellParams = ("/c", "-c", "-c");
    //     public (string Windows, string Mac, string Linux)? EditorPath;
    //     public (string Windows, string Mac, string Linux)? EditorParams;
    //     private (string Windows, string Mac, string Linux) DefaultEditor = ("notepad.exe", "/System/Applications/TextEdit.app", "xed");


    //     /// <summary> 
    //     /// <description><description>
    //     /// <param name="FilePath">The path of the newly created file that the user wishes to open</param>
    //     /// </summary>
    //     // MacOS will return an error if .bamc isn't associated with a file extension. 
    //     // Parse the STDErr for string (Error Domain=NSOSStatusErrorDomain Code=-10814)
    //     // vim is loaded by default on macos and linux under "vi", dont forget to add this as an option with a caveat its for advanced users.
    //     public ProcessStartInfo GetProcessInfo(string FilePath)
    //     {
    //         if (!File.Exists(FilePath))
    //         {
    //             WriteAndExit("Unable to open the specified file, it has yet to be created, please try again.", 1);
    //         }

    //         var psi = (Platforms.IsWindows, Platforms.IsOSX, Platforms.IsLinux) switch {
    //             (true, false, false) => new ProcessStartInfo() { 
    //                 FileName = ShellNames.Windows, // cmd.exe
    //                 ArgumentList = {
    //                     ShellParams.Windows, // /c
    //                     EditorPath.HasValue ? EditorPath.Value.Windows : DefaultEditor.Windows, // The supplied path or the default editor.
    //                     EditorParams.HasValue ? EditorParams.Value.Windows : "", // If the editor requires any params to open the file
    //                 },

    //             },
    //             (false, true, false) => new ProcessStartInfo() { 
    //                 FileName = ShellNames.Mac, // /bin/bash
    //                 ArgumentList = {
    //                     ShellParams.Mac, // -c
    //                     EditorPath.HasValue ? EditorPath.Value.Mac : DefaultEditor.Mac, // The supplied path or the default editor.
    //                     EditorParams.HasValue ? EditorParams.Value.Mac : "", // If the editor requires any params to open the file
    //                 },
    //             },
    //             (false, false, true) => new ProcessStartInfo() { 
    //                 FileName = ShellNames.Linux, // /bin/bash
    //                 ArgumentList = {
    //                     ShellParams.Linux, // -c
    //                     EditorPath.HasValue ? EditorPath.Value.Linux : DefaultEditor.Linux, // The supplied path or the default editor.
    //                     EditorParams.HasValue ? EditorParams.Value.Linux : "", // If the editor requires any params to open the file
    //                 },

    //             },
    //             _ => throw new PlatformNotSupportedException("Unsupported OS.")
    //         };

    //         psi.RedirectStandardError = true;
    //         psi.RedirectStandardInput = true;
    //         psi.RedirectStandardOutput = true;
    //         return psi;
    //     }
    // };

    public class Editor
    {
        public required (string Windows, string Mac, string Linux) Names;
        public required (bool Windows, bool Mac, bool Linux) Supports;
        public (string Windows, string Mac, string Linux)? EditorPath;
        public (string Windows, string Mac, string Linux)? EditorParams;
        private static (string Windows, string Mac, string Linux) DefaultEditor = (
            @"C:\Windows\System32\notepad.exe", "/System/Applications/TextEdit.app", "xed"
        );

        public static string GetDefaultEditor() {
            return (Platforms.IsWindows, Platforms.IsOSX, Platforms.IsLinux) switch {
                (true, false, false) => DefaultEditor.Windows,
                (false, true, false) => DefaultEditor.Mac,
                (false, false, true) => DefaultEditor.Linux,
                _ => throw new PlatformNotSupportedException("Invalid OS.")
            };
        }

        public ProcessStartInfo GetProcessInfo(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException("Unable to open the specified file, it has yet to be created.", FilePath);
            }

            string editor;
            string editorParams;

            // Determines the editor and its parameters based on platform and user setting
            if (Platforms.IsWindows)
            {
                editor = EditorPath.HasValue ? EditorPath.Value.Windows : DefaultEditor.Windows;
                editorParams = EditorParams.HasValue ? EditorParams.Value.Windows : string.Empty;
            }
            else if (Platforms.IsOSX)
            {
                // On macOS, the default 'open' command is used, and the editor is passed via the '-a' flag.
                editor = EditorPath.HasValue ? EditorPath.Value.Mac : DefaultEditor.Mac;
                editorParams = EditorParams.HasValue ? EditorParams.Value.Mac : string.Empty;
            }
            else if (Platforms.IsLinux)
            {
                editor = EditorPath.HasValue ? EditorPath.Value.Linux : DefaultEditor.Linux;
                editorParams = EditorParams.HasValue ? EditorParams.Value.Linux : string.Empty;
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS.");
            }

            ProcessStartInfo psi;

            if (Platforms.IsWindows)
            {
                // Direct execution of the editor executable.
                psi = new ProcessStartInfo
                {
                    FileName = editor,
                    ArgumentList = { editorParams, FilePath },
                    UseShellExecute = true
                };
            }
            else if (Platforms.IsOSX)
            {
                // Uses the 'open' command which launches .app bundles and handles path association.
                psi = new ProcessStartInfo
                {
                    FileName = "open",
                    ArgumentList = {
                        "-a", // Flag to specify the application to open the file with.
                        editor,
                        "--args", // Passes any subsequent arguments directly to the opened application.
                        editorParams,
                        FilePath
                    },
                    UseShellExecute = true
                };
            }
            else if (Platforms.IsLinux)
            {
                var specialEditors = new string[2] {"vi", "xed"};

                // If the application is interactive, UseShellExecute ensures proper terminal handling.
                // Allows the system to resolve PATH variables for vim and xed
                var useShellExecute = specialEditors.Contains(editor);
                psi = new ProcessStartInfo
                {
                    FileName = editor,
                    ArgumentList = { editorParams, FilePath },
                    UseShellExecute = useShellExecute
                };
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported OS.");
            }

            // Sets output redirection to false by default, as it often causes GUI based applications to fail to correctly launch.
            psi.RedirectStandardError = false;
            psi.RedirectStandardInput = false;
            psi.RedirectStandardOutput = false;
            
            return psi;
        }
    }
}