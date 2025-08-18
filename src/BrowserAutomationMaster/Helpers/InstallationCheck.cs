using BrowserAutomationMaster.Managers.AppManager;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Checks
{
    public enum ApplicationNames
    {
        //Brave,
        Chrome,
        Firefox,
        Python3_X, // This flag is for MacOS since the default python installer is not a .app bundle
        Python3_9, // Display warning that packages might not be compatible, stick to 3.10 or 3.11
        Python3_10,
        Python3_11,
        Python3_12, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_13, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_14, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
    }

    public partial class Installations
    {
        public List<ApplicationNames> AppNames { get; set; }
        
        readonly List<ApplicationNames> validPythonVersions = [
            ApplicationNames.Python3_X, ApplicationNames.Python3_9, ApplicationNames.Python3_10, 
            ApplicationNames.Python3_11, ApplicationNames.Python3_12, ApplicationNames.Python3_13, 
            ApplicationNames.Python3_14
        ];

        //readonly List<ApplicationNames> validBrowsersApps = [ApplicationNames.Brave, ApplicationNames.Chrome, ApplicationNames.Firefox];
        readonly List<ApplicationNames> validBrowsersApps = [ApplicationNames.Chrome, ApplicationNames.Firefox];


        readonly static string NoBrowsersMessage = @"BAM Manager (BAMM) was unable to detect any valid browser installations.

Supported browsers include:

    - Brave
    - Chrome
    - Firefox";

        readonly static string NoPythonMessage = @"BAM Manager (BAMM) was unable to detect any valid python installations.

Supported versions include:

- Python 3.9.X
- Python 3.10.X
- Python 3.11.X
- Python 3.12.X
- Python 3.13.X
- Python 3.14.X";

        public Installations(List<AppInfo> detectedApplications)
        {
            AppNames = [];
            //AppNames = detectedApplications ?? [];
            foreach (AppInfo app in detectedApplications)
            {
                if (app == null) { continue; }
                if (app.Name == null) { continue; }
                if (app.Name.Length == 0) { continue; }
                //Spectre.Console.AnsiConsole.Write(app.Name);
                //if (app.Name.ToLower().Contains("brave")) {
                //    if (!AppNames.Contains(ApplicationNames.Brave)) {
                //        AppNames.Add(ApplicationNames.Brave);
                //    }
                //}

                else if (app.Name.Contains("chrome", CCIC)) {
                    if (!AppNames.Contains(ApplicationNames.Chrome)) {
                        AppNames.Add(ApplicationNames.Chrome);
                    }
                }

                else if (app.Name.Contains("firefox", CCIC)) {
                    if (!AppNames.Contains(ApplicationNames.Firefox)) {
                        AppNames.Add(ApplicationNames.Firefox);
                    }
                }

                else if (app.Name.StartsWith("Python 3.9")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_9)) {
                        AppNames.Add(ApplicationNames.Python3_9);
                    }
                }

                else if (app.Name.StartsWith("Python 3.10")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_10)) {
                        AppNames.Add(ApplicationNames.Python3_10);
                    }
                }

                else if (app.Name.StartsWith("Python 3.11")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_11)) {
                        AppNames.Add(ApplicationNames.Python3_11);
                    }
                }

                else if (app.Name.StartsWith("Python 3.12")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_12)) {
                        AppNames.Add(ApplicationNames.Python3_12);
                    }
                }

                else if (app.Name.StartsWith("Python 3.13")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_13)) {
                        AppNames.Add(ApplicationNames.Python3_13);
                    }
                }

                else if (app.Name.StartsWith("Python 3.14")) {
                    if (!AppNames.Contains(ApplicationNames.Python3_14)) {
                        AppNames.Add(ApplicationNames.Python3_14);
                    }
                }

                // Mac Specific Case
                else if (app.Name.StartsWith("python3")) { 
                    if (!AppNames.Contains(ApplicationNames.Python3_X)) {
                        AppNames.Add(ApplicationNames.Python3_X);
                    }
                }
            }
            if (!AppNames.Intersect(validBrowsersApps).Any()) { Errors.WriteErrorAndExit(NoBrowsersMessage, 1); }
            if (!AppNames.Intersect(validPythonVersions).Any()) { Errors.WriteErrorAndExit(NoPythonMessage, 1); }
        }
        public Installations() // Empty constructor used as a fallback.
        {
            Errors.WriteErrorAndExit(NoBrowsersMessage, 1);
            AppNames = []; // This wont be reached, its purely to appease the compilers static nature.
        }

    }
}
