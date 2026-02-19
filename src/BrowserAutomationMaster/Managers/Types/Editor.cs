using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.Utilities.AppUpdateUtility;
using static BrowserAutomationMaster.Managers.Messaging.Errors;
using System.Diagnostics;
using System.Text;


namespace BrowserAutomationMaster.Managers.Types
{    
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
                        "// Your .BAMC contents goes below this line",
                        ""
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