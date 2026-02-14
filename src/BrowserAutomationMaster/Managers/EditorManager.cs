using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Input;
using BrowserAutomationMaster.Managers.AppManager;
using BrowserAutomationMaster.Managers.Common;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Text;


namespace BrowserAutomationMaster.Managers
{

    public class EditorManager()
    {

        private static Editor GetEditorChoice() 
        {
            Dictionary<string, string> installedEditors = GetSupportedEditors();
            var defaultEditorPath = Editor.GetDefaultEditorPath();

            if (installedEditors.Count == 0) 
            {
                Warning.Write("BAM Manager (BAMM) was unable to locate any supported text editors, defaulting to platform default.");
                
                var editorNames = (
                    Platforms.IsWindows ? "Notepad" : "",
                    Platforms.IsMacOS ? "TextEdit" : "",
                    Platforms.IsLinux ? "Vim" : ""
                );

                var editorPaths = (
                    Platforms.IsWindows ? defaultEditorPath : "",
                    Platforms.IsMacOS ? defaultEditorPath : "",
                    Platforms.IsLinux ? defaultEditorPath : ""
                );

                return new Editor()
                { 
                    Names = editorNames,
                    Supports = (Platforms.IsWindows, Platforms.IsWindows, Platforms.IsWindows),
                    EditorPath = editorPaths,
                    // EditorParams = ("", "", "")
                };
            }

            string chosenOption = WriteListFromOptions([.. installedEditors.Keys], "editor", Math.Max(installedEditors.Keys.Count, 3));

            // Since chosenOption is one of the keys, this will likely not be null or throw an error.
            KeyValuePair<string, string>? chosenEditor = installedEditors
                .Where(element => element.Key.Equals(chosenOption))
                .FirstOrDefault();

            // Despite the unlikelihood that chosenEditor will be null, this inherently redudant check, isn't a horrible idea.
            if (chosenEditor == null) 
            {
                Warning.Write($"BAM Manager (BAMM) was unable to open '{chosenOption}'"); 
                Console.WriteLine("Please make a bug report with the following contents.");

                Write(
                    string.Join(NLC, [
                        "Error Log:",
                        "chosenEditor was not found in installedEditors.Keys."
                    ])
                );
                var editorNames = (
                    Platforms.IsWindows ? "Notepad" : "",
                    Platforms.IsMacOS ? "TextEdit" : "",
                    Platforms.IsLinux ? "Xed" : ""
                );

                var editorPaths = (
                    Platforms.IsWindows ? defaultEditorPath : "",
                    Platforms.IsMacOS ? defaultEditorPath : "",
                    Platforms.IsLinux ? defaultEditorPath : ""
                );

                return new Editor()
                { 
                    Names = editorNames,
                    Supports = (Platforms.IsWindows, Platforms.IsWindows, Platforms.IsWindows),
                    EditorPath = editorPaths,
                    // EditorParams = ("", "", "")
                };
            }

            var path = (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch {
                (true, false, false) => (chosenEditor.Value.Value, "", ""),
                (false, true, false) => (chosenEditor.Value.Value, "", ""),
                (false, false, true) => (chosenEditor.Value.Value, "", ""),
                _ => throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms")
            };
            
            // Will either return an Editor object, or throw an exception.
            return GetSelectedEditorObject(chosenEditor.Value.Key, path);
        }
        
        private static Editor GetSelectedEditorObject(string EditorName, (string Windows, string Mac, string Linux) Paths) 
        {
            return (EditorName) switch 
            {
                "Helix" => new Helix(Paths),
                "Nano" => new Nano(),
                "Notepad++" => new NotepadPlusPlus(Paths),
                "PyCharm" => new PyCharm(Paths),
                "Sublime Text" => new Sublime(Paths),
                "Vim (Advanced Users)" => new Vim(),
                "Visual Studio" => new VisualStudio(),
                "VSCode" => new VSCode(Paths.Mac),
                "VSCodium" => new VSCodium(Paths.Mac),
                _ => throw new ArgumentException("Invalid Selection.")
            };
        }
        
        private static Dictionary<string, string> GetSupportedEditors() 
        {
            return (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch {
                (true, false, false) => GetSupportedWindowsEditors(),
                (false, true, false) => GetSupportedMacEditors(),
                (false, false, true) => GetSupportedLinuxEditors(),
                _ => throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms")
            };
        }

        private static Dictionary<string, string> GetSupportedWindowsEditors() 
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
        private static Dictionary<string, string> GetSupportedMacEditors()
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

        private static Dictionary<string, string> GetSupportedLinuxEditors()
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
                        (var commandOutput, _) = RunCommand("/bin/bash", $"-c {command}");

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

        public static async Task OpenFileInEditor(string filePath)
        {
            var editor = GetEditorChoice();
            var psi = await editor.GetProcessInfo(filePath);
            using var process = await ProcessFactory.SpawnProcess(psi, $"choosing an editor to open '{filePath}'");
            (int ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(process);
        }
    }

    public class Editor
    {
        public (string Windows, string Mac, string Linux) Names = ("", "", "");
        public (bool Windows, bool Mac, bool Linux) Supports = (false, false, false);
        public (string Windows, string Mac, string Linux) EditorPath = ("", "", "");
        public (string Windows, string Mac, string Linux) EditorParams = ("", "", "");
        private static (string Windows, string Mac, string Linux) DefaultEditor = (
            @"C:\Windows\System32\notepad.exe", "/System/Applications/TextEdit.app", "vi"
        );

        public static string GetDefaultEditorPath() {
            return (Platforms.IsWindows, Platforms.IsMacOS, Platforms.IsLinux) switch {
                (true, false, false) => DefaultEditor.Windows,
                (false, true, false) => DefaultEditor.Mac,
                (false, false, true) => DefaultEditor.Linux,
                _ => throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms")
            };
        }

        public async Task<ProcessStartInfo> GetProcessInfo(string FilePath)
        {
            // Creates the file if it doesn't already exist.
            if (!File.Exists(FilePath))
            {
                try 
                {
                    var dateTimeObj = DateTime.Now;

                    var fileHeaderStr = string.Join(NLC, [
                        $"// Created at: {dateTimeObj}",
                        $"// File created using BAMM {CurrentVersion}",
                        "// https://github.com/Static-Codes/BrowserAutomationMaster",
                        "",
                        "// Your .BAMC contents goes below this line"
                    ]);

                    var fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeaderStr);

                    using var stream = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite);

                    await stream.WriteAsync(fileHeaderBytes);
                }

                catch (Exception ex) 
                {
                    WriteAndExit
                    (
                        message: 
                            string.Join(NLC, [
                                $"BAM Manager (BAMM) ran into a fatal error, trying to create a file at: {FilePath}.",
                                "Error Log:",
                                $"{ex.Message}"
                            ]),
                        status: 1,
                        writePlatformDebugInfo: true 
                    );
                }
            }

            string editor;
            string editorParams;

            // Determines the editor and its parameters based on platform and user setting
            if (Platforms.IsWindows)
            {
                editor = !string.IsNullOrEmpty(EditorPath.Windows) ? EditorPath.Windows : DefaultEditor.Windows;
                editorParams = !string.IsNullOrEmpty(EditorParams.Windows) ? EditorParams.Windows : string.Empty;
            }

            else if (Platforms.IsMacOS)
            {
                // On macOS, the default 'open' command is used, and the editor is passed via the '-a' flag.
                editor = !string.IsNullOrEmpty(EditorPath.Mac) ? EditorPath.Mac : DefaultEditor.Mac;
                editorParams = !string.IsNullOrEmpty(EditorParams.Mac) ? EditorParams.Mac : string.Empty;
            }

            else if (Platforms.IsLinux)
            {
                editor = !string.IsNullOrEmpty(EditorPath.Linux) ? EditorPath.Linux : DefaultEditor.Linux;
                editorParams = !string.IsNullOrEmpty(EditorParams.Linux) ? EditorParams.Linux : string.Empty;
            }

            else
            {
                throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms");
            }

            ProcessStartInfo psi;

            if (Platforms.IsWindows)
            {
                // Direct execution of the editor executable.
                psi = new ProcessStartInfo
                {
                    FileName = editor,
                    Arguments = $"{editorParams} \"{FilePath}\"",
                    UseShellExecute = true
                };
            }

            else if (Platforms.IsMacOS)
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
                var textBasedEditors = new string[4] {"helix", "nano", "vi", "xed"};

                // If the application is interactive, UseShellExecute ensures proper terminal handling.
                // Allows the system to resolve PATH variables for vim and xed
                var useShellExecute = textBasedEditors.Contains(editor);
                psi = new ProcessStartInfo
                {
                    FileName = editor,
                    Arguments = $"{editorParams} \"{FilePath}\"",
                    UseShellExecute = useShellExecute
                };
            }

            else
            {
                throw new PlatformNotSupportedException("Failed to set all values for members in InternalPlatforms.Platforms");
            }
            
            // Debug only do NOT leave in production release.
            // foreach (var arg in psi.ArgumentList) {
            //     Console.WriteLine(arg);
            // }

            return psi;
        }
    };


    public class Helix : Editor 
    {
        public Helix((string Windows, string Mac, string Linux) EditorPath)
        {
            Names = (
                Windows: "Helix", 
                Mac: "Helix", 
                Linux: "Helix"
            );
            Supports = (
                Windows: true, 
                Mac: true, 
                Linux: true
            );
            this.EditorPath = EditorPath;
            EditorParams = (":open -- ", ":open -- ", ":open -- ");
        }
    }

    public class Nano : Editor 
    {
        public Nano()
        {
            Names = (
                Windows: "", 
                Mac: "Nano", 
                Linux: "Nano"
            );
            Supports = (
                Windows: false, 
                Mac: false, 
                Linux: true
            );
            EditorPath = ("", "nano", "nano");
            EditorParams = ("", "", "");
        }
    }

    public class NotepadPlusPlus : Editor 
    {
        public NotepadPlusPlus((string W, string M, string L) EditorPath)
        {
            Names = (
                Windows: "Notepad++", 
                Mac: "", 
                Linux: ""
            );
            Supports = (
                Windows: true, 
                Mac: false, 
                Linux: false
            );
            this.EditorPath = EditorPath;
            EditorParams = ("", "", "");
        }
    }

    public class PyCharm : Editor 
    {
        public PyCharm((string Windows, string Mac, string Linux) EditorPath)
        {
            Names = (
                Windows: "PyCharm", 
                Mac: "PyCharm", 
                Linux: "PyCharm"
            );
            Supports = (
                Windows: true, 
                Mac: true, 
                Linux: true
            );
            this.EditorPath = EditorPath;
            EditorParams = ("", "", "");
        }
    }

    public class Sublime : Editor 
    {
        public Sublime((string Windows, string Mac, string Linux) EditorPath)
        {
            Names = (Windows: "Sublime Text", Mac: "Sublime Text", Linux: "Sublime Text");
            Supports = (Windows: true, Mac: true, Linux: true);
            this.EditorPath = EditorPath;
            EditorParams = ("", "", "");
        }
    }

    public class Vim : Editor 
    {
        public Vim()
        {
            Names = (
                Windows: "", 
                Mac: "Vim", 
                Linux: "Vim"
            );
            Supports = (
                Windows: false, 
                Mac: true, 
                Linux: true
            );
            EditorPath = (
                Windows: "", 
                Mac: "vi", 
                Linux: "vi"
            );
            EditorParams = ("", "", "");
        }
    }

    public class VisualStudio : Editor 
    {
        public VisualStudio()
        {
            Names = (
                Windows: "Visual Studio", 
                Mac: "", 
                Linux: ""
            );
            Supports = (
                Windows: true, 
                Mac: false, 
                Linux: false
            );
            EditorPath = (
                Windows: @"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe", 
                Mac: "", 
                Linux: ""
            );
            EditorParams = ("", "", "");
        }
    }

    public class VSCode : Editor 
    {
        public VSCode(string MacOSPath)
        {
            Names = (
                Windows: "VSCode", 
                Mac: "VSCode", 
                Linux: "VSCode"
            );
            Supports = (
                Windows: true, 
                Mac: true, 
                Linux: true
            );
            EditorPath = (
                Windows: "code", 
                Mac: MacOSPath, 
                Linux: "code"
            );
            EditorParams = ("", "", "");
        }
    }

    public class VSCodium : Editor 
    {
        public VSCodium(string MacOSPath)
        {
            Names = (
                Windows: "VSCodium", 
                Mac: "VSCodium", 
                Linux: "VSCodium"
            );
            Supports = (
                Windows: true, 
                Mac: true, 
                Linux: true
            );
            EditorPath = (
                Windows: "codium",
                Mac: MacOSPath,
                Linux: "codium"    
            );
            EditorParams = ("", "", "");
        }
    }

}