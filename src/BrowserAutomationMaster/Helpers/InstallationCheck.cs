using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Helpers
{
    public enum ApplicationNames
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

    public partial class Installations
    {

        public List<ApplicationNames> AppNames { get; set; }
        
        public static readonly List<ApplicationNames> validPythonVersions = 
        [
            ApplicationNames.Python3_X, ApplicationNames.Python3_8, ApplicationNames.Python3_9, 
            ApplicationNames.Python3_10, ApplicationNames.Python3_11, ApplicationNames.Python3_12, 
            ApplicationNames.Python3_13, ApplicationNames.Python3_14
        ];

        //readonly List<ApplicationNames> validBrowsersApps = [ ApplicationNames.Brave, ApplicationNames.Chrome, ApplicationNames.Firefox ];
        readonly List<ApplicationNames> validBrowsersApps = [ ApplicationNames.Chrome, ApplicationNames.Firefox ];


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

        readonly Dictionary<string, ApplicationNames> pythonVerMap = new()
        {
                { "Python 3.8", ApplicationNames.Python3_8 },
                { "Python 3.9", ApplicationNames.Python3_9 },
                { "Python 3.10", ApplicationNames.Python3_10 },
                { "Python 3.11", ApplicationNames.Python3_11 },
                { "Python 3.12", ApplicationNames.Python3_12 },
                { "Python 3.13", ApplicationNames.Python3_13 },
                { "Python 3.14", ApplicationNames.Python3_14 }
        };

        void Add(ApplicationNames app)
        {
            if (!AppNames.Contains(app)) {
                AppNames.Add(app);
            }
        }


        /// <summary>Attempts to get the enum member associated with the python version string <summary>
        /// <param name="name">The string representation of the Python version.</param>
        /// <param name="app">The returned enum member</param>
        /// <returns>Either the ApplicationNames member associated or ApplicationNames.Python3_X which will throw an exception later down the stack.</returns>
        bool GetEnumMemberFromString(string name, out ApplicationNames app)
        {
            var collection = pythonVerMap.Where(map => name.StartsWith(map.Key));
            
            if (!collection.Any())
            {
                app = ApplicationNames.Python3_X;
                return false;
            }

            app = collection.First().Value;
            return app != ApplicationNames.Python3_X;
        }

        void CheckApp(AppInfo app, bool pythonOnly, string? version = null)
        {
            if (app == null || app.Name == null || app.Name.Length == 0) {
                return;
            }
            
            // Arch specific case (May also work for UnixLike machines that are unconventional)
            if (app.Name.Equals("python") && Platforms.IsLinux)  {
                HandleArchLinuxPythonCheck();
            }

            // Uncomment these two when readding brave support
            // if (app.Name.Contains("brave", CCIC)) {
            //     Add(ApplicationNames.Brave);
            // }

            // else if (!pythonOnly && app.Name.Contains("chrome", CCIC)) {
            //     Add(ApplicationNames.Chrome);
            // }
            
            // Delete this line if the above two checks are reintroduced.
            if (!pythonOnly && app.Name.Contains("chrome", CCIC)) {
                Add(ApplicationNames.Chrome);
            }

            else if (!pythonOnly && app.Name.Contains("firefox", CCIC)) {
                Add(ApplicationNames.Firefox);
            }

            else if (version == null && GetEnumMemberFromString(app.Name, out ApplicationNames appName)) {
                Add(appName);
            }

            // Unix Specific Recursive Case
            // To prevent an infinite loop, version must have a value to continue
            else if (Platforms.IsUnixLike && version != null && GetEnumMemberFromString(version, out ApplicationNames appName2)) {
                Add(appName2);
            }

            else if (app.Name.StartsWith("python3"))
            {
                var foundVersion = GetMissingPyVersion();

                if (string.IsNullOrEmpty(foundVersion)) {
                    Add(ApplicationNames.Python3_X); // This will raise an error once Transpiler.New is executed.
                }

                else if (GetEnumMemberFromString(foundVersion, out ApplicationNames _)) {
                    CheckApp(app, pythonOnly: true, version: foundVersion);
                }

            }
        }
            
        void CheckAndAdd(List<AppInfo> detectedApplications, string? verNum = null, bool pythonOnly = false)
        {
            foreach (AppInfo app in detectedApplications) {
                CheckApp(app, pythonOnly: pythonOnly, version: verNum);
            }
        }

        public Installations(List<AppInfo> detectedApplications)
        {

            AppNames = [];

            CheckAndAdd(detectedApplications);

            if (!AppNames.Intersect(validBrowsersApps).Any()) {
                WriteAndExit(NoBrowsersMessage, 1);
            }

            if (!AppNames.Intersect(validPythonVersions).Any() && !Platforms.IsChromeOS) {
                WriteAndExit(NoPythonMessage, 1);
            }
        }

        public Installations() // Empty constructor used as a fallback.
        {
            WriteAndExit(NoBrowsersMessage, 1);
            AppNames = []; // This wont be reached, its purely to appease the compilers static nature.
        }

        public static string GetMissingPyVersion(string pythonVar = "python3")
        {
            if (!Platforms.IsUnixLike) {
                return string.Empty;
            }

            (var whichPyResp, _) = RunCommand("which", pythonVar);

            if (string.IsNullOrEmpty(whichPyResp)) {
                return string.Empty;
            }

            (var pyVersionResp, _) = RunCommand(pythonVar, "--version");

            Match pyVersionMatch = RegexManager.PyVersionRegex.Match(pyVersionResp);

            return pyVersionMatch.Success ? pyVersionMatch.Value : string.Empty;

        }

        private void HandleArchLinuxPythonCheck() 
        {
            var missingVersion = GetMissingPyVersion(pythonVar: "python");

            if (string.IsNullOrEmpty(missingVersion)) {
                WriteAndExit(NoPythonMessage, 1);
            }

            // This will return bool if successful, however:
            // As a fallback ApplicationNames.Python3_X is returned, thus no check is required.
            GetEnumMemberFromString(missingVersion, out ApplicationNames appName);
            Add(appName);
        }

    }
}
