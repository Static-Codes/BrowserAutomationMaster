using System.Diagnostics;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers
{
    public class EditorManager()
    {
        // https://pypl.github.io/IDE.html

        // public class Editor 
        // {
        //     public required string[] Names { get; set; }
        //     public required bool SupportsWindows { get; set; }
        //     public required bool SupportsMac { get; set; }
        //     public required bool SupportsLinux { get; set; }
        //     public required string[] Commands { get; set; }
        // }

        



        // public Editor[] Editors = 
        // [
        //     new Editor()
        //     { 
        //         Names = ["VSCodium", "VSCodium", "VSCodium"],
        //         SupportsWindows = true,
        //         SupportsMac = true,
        //         SupportsLinux = true,
        //         Commands = ["codium", "codium", "codium"]
        //     },

        //     new Editor()
        //     {
        //         Names = ["Notepad"],
        //         SupportsWindows = true,
        //         SupportsMac = false,
        //         SupportsLinux = false,
        //         Commands = ["notepad"]
        //     },

        //     new Editor()
        //     {
        //         Names = ["Notepad++"],
        //         SupportsWindows = true,
        //         SupportsMac = false,
        //         SupportsLinux = false,
        //         Commands = ["npp"]
        //     },
        // ];


        public static void GetSupportedWindowsEditors() {

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
        public static string[] GetSupportedMacEditors()
        {
            var supportedAppNames = new[] {
                "PyCharm.app",
                "Sublime Text.app",
                "VSCodium.app",
                "Visual Studio Code.app"
            };
            
            var applicationsPath = "/Applications/";

            // This SHOULD NEVER BE EXECUTED, IF IT IS A HUGE PROBLEM IS PRESENT.
            if (!Directory.Exists(applicationsPath))
            {
                WriteAndExit($"The expected directory '{applicationsPath}' does not exist.", 1);
                return [];
            }

            var installedEditors = supportedAppNames
                .Select(bundleName => Path.Combine(applicationsPath, bundleName))
                .Where(bundlePath => Directory.Exists(bundlePath))
                .ToArray();

            return installedEditors;
        }

        public static void GetSupportedLinuxEditors() {

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
        private (string Windows, string Mac, string Linux) DefaultEditor = ("notepad.exe", "TextEdit", "xed");

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