using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers
{
    public class EditorManager()
    {
        // https://pypl.github.io/IDE.html

        struct Editor 
        {
            public string Name { get; set; }
            public bool SupportsWindows { get; set; }
            public bool SupportsMac { get; set; }
            public bool SupportsLinux { get; set; }
        }

        public Editor[] Editors = [
            
        ];

        // Refactor with a custom struct/class
        public string[] SupportedEditors = [
            "Atom",

            // Open source fork of Visual Studio Code
            "Codium", 
            // JetBrains IDE
            "Eclipse",
            // Windows notepad
            "Notepad",
            // Fork/Rewrite of Windows notepad (I believe the command is npp)
            "Notepad++", 
            // Cross Platform IDE
            "Sublime Text",
            // The bloated older brother to visual studio code.
            "Visual Studio",
            // A much sleeker version of visual studio, which is cross. (`code <filename>` to open the file)
            "Visual Studio Code",
            // Xcode calls on xed (shown below), use (`xed -x <filename>` to open the file)
            "Xcode",
            // Built in text-editor in linux (not to be confused with xcode which calls on xed.)
            "Xed", 
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