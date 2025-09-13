using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Helpers
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
        
        public static readonly List<ApplicationNames> validPythonVersions = [
            ApplicationNames.Python3_X, ApplicationNames.Python3_9, ApplicationNames.Python3_10, 
            ApplicationNames.Python3_11, ApplicationNames.Python3_12, ApplicationNames.Python3_13, 
            ApplicationNames.Python3_14
        ];

        //readonly List<ApplicationNames> validBrowsersApps = [ApplicationNames.Brave, ApplicationNames.Chrome, ApplicationNames.Firefox];
        readonly List<ApplicationNames> validBrowsersApps = [ApplicationNames.Chrome, ApplicationNames.Firefox];


        readonly static string NoBrowsersMessage = @"BAM Manager (BAMM) was unable to detect any valid browser installations.

Supported browsers include:

    - Chrome
    - Firefox".Replace("\r", ""); // Carriage returns cause issues with Spectre Console on Windows... odd?

        readonly static string NoPythonMessage = @"BAM Manager (BAMM) was unable to detect any valid python installations.

Supported versions include:

- Python 3.9.X
- Python 3.10.X
- Python 3.11.X
- Python 3.12.X
- Python 3.13.X
- Python 3.14.X".Replace("\r", ""); // Carriage returns cause issues with Spectre Console on Windows... odd?

        public Installations(List<AppInfo> detectedApplications)
        {
            var pythonVerMap = new Dictionary<string, ApplicationNames>
            {
                { "Python 3.9", ApplicationNames.Python3_9 },
                { "Python 3.10", ApplicationNames.Python3_10 },
                { "Python 3.11", ApplicationNames.Python3_11 },
                { "Python 3.12", ApplicationNames.Python3_12 },
                { "Python 3.13", ApplicationNames.Python3_13 },
                { "Python 3.14", ApplicationNames.Python3_14 }
            };

            AppNames = [];


            void Add(ApplicationNames app) 
            {
                if (!AppNames.Contains(app))
                    AppNames.Add(app);
            }


            /// <summary>Attempts to get the enum member associated with the python version string <summary>
            /// <param name="name">The string representation of the Python version.</param>
            /// <param name="app">The returned enum member</param>
            /// <returns>Either the ApplicationNames member associated or ApplicationNames.Python3_X which will throw an exception later down the stack.</returns>
            bool GetEnumMemberFromString(string name, out ApplicationNames app)
            {
                app = pythonVerMap.GetValueOrDefault(name, ApplicationNames.Python3_X);
                return app != ApplicationNames.Python3_X;
            }

            void CheckApp(AppInfo app, bool pythonOnly, string? version = null)
            {
                if (app == null || app.Name == null || app.Name.Length == 0)
                    return;

                //if (app.Name.Contains("brave", CCIC))
                //Add(ApplicationNames.Brave);

                else if (!pythonOnly && app.Name.Contains("chrome", CCIC))
                    Add(ApplicationNames.Chrome);

                else if (!pythonOnly && app.Name.Contains("firefox", CCIC))
                    Add(ApplicationNames.Firefox);

                else if (version == null && GetEnumMemberFromString(app.Name, out ApplicationNames appName))
                    Add(appName);

                // Unix Specific Recursive Case
                // To prevent an infinite loop, version must have a value to continue
                else if (IsUnixLike && version != null && GetEnumMemberFromString(version, out ApplicationNames appName2))
                    Add(appName2);

                else if (app.Name.StartsWith("python3"))
                {
                    var foundVersion = GetMissingPyVersion();

                    if (string.IsNullOrEmpty(foundVersion))
                        Add(ApplicationNames.Python3_X); // This will raise an error once Transpiler.New is executed.

                    else if (GetEnumMemberFromString(foundVersion, out ApplicationNames appNameNested))
                        CheckApp(app, pythonOnly: true, version: foundVersion);

                }
            }
            
            void CheckAndAdd(List<AppInfo> appsInfo, string? verNum = null, bool pythonOnly = false)
            {
                foreach (AppInfo app in detectedApplications)
                    CheckApp(app, pythonOnly: false);
            }

            CheckAndAdd(detectedApplications);

            if (!AppNames.Intersect(validBrowsersApps).Any())
                Errors.WriteAndExit(NoBrowsersMessage, 1);

            if (!AppNames.Intersect(validPythonVersions).Any())
                Errors.WriteAndExit(NoPythonMessage, 1);
        }

        public Installations() // Empty constructor used as a fallback.
        {
            Errors.WriteAndExit(NoBrowsersMessage, 1);
            AppNames = []; // This wont be reached, its purely to appease the compilers static nature.
        }

        public static string GetMissingPyVersion()
        {
            if (!IsUnixLike)
                return string.Empty;

            var whichPyResp = Linux.RunCommand("which", "python3");

            if (string.IsNullOrEmpty(whichPyResp))
                return string.Empty;

            var pyVersionResp = Linux.RunCommand("python3", "--version");

            Match pyVersionMatch = RegexManager.PyVersionRegex.Match(pyVersionResp);

            return pyVersionMatch.Success ? pyVersionMatch.Value : string.Empty;

        }

    }
}
