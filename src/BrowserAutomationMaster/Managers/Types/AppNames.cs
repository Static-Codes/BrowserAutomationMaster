

namespace BrowserAutomationMaster.Managers.Types
{
    public enum AppNames
    {
        //Brave,
        Chrome,
        Firefox,
        Python3_X, // This flag is for MacOS since the default python installer is not a .app bundle
        Python3_8, // THIS IS ONLY FOR CHROMEOS (Ubuntu 22.04.5 [Focal Fossa])
        Python3_9, // Display warning that packages might not be compatible, stick to 3.10 or 3.11
        Python3_10,
        Python3_11,
        Python3_12, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_13, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_14, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
    }
}
