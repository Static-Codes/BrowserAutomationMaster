using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers
{
    public class EditorManager()
    {
        // https://pypl.github.io/IDE.html
        public string[] SupportedEditors = [
            "Atom",
            "Codium", // Open source fork of 
            "Eclipse",
            "Notepad", // Windows notepad
            "Notepad++", // I believe the command is npp
            "Sublime",
            "Visual Studio", // The bloated older brother to visual studio code.
            "Visual Studio Code", // A much sleeker version of visual studio. (`code <filename>` to open the file)
            "Xcode", // Xcode isn't Calls on xed (shown below), use (`xed -x <filename>` to open the file)
            "Xed", // Built in text-editor in linux (not to be confused with xcode which calls on xed.)
            ""
        ];

        /// <summary>
        /// 
        /// </summary>
        /// <returns>A string representing the default editor associated with the current operating system.</returns>
        public string GetDefaultEditor()
        {
            return (Platforms.IsWindows, Platforms.IsOSX, Platforms.IsLinux) switch {
                (true, false, false) => "notepad",
                (false, true, false) => "xed",
                (false, false, true) => "xed",
                _ => throw new PlatformNotSupportedException("Unsupported OS.")
            };
        }
    };
    
}