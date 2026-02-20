using BrowserAutomationMaster.Core.Common;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Core.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using BrowserAutomationMaster.Core.OS.Generic;


namespace BrowserAutomationMaster.Core.Types
{
    public partial class Installations
    {
        public List<AppNames> AppNames { get; set; }
        
        public static readonly List<AppNames> validPythonVersions = 
        [
            Types.AppNames.Python3_X, Types.AppNames.Python3_8, Types.AppNames.Python3_9,
            Types.AppNames.Python3_10, Types.AppNames.Python3_11, Types.AppNames.Python3_12,
            Types.AppNames.Python3_13, Types.AppNames.Python3_14
        ];

        // Uncomment if Brave support is reintroduced.
        //readonly List<AppNames> validBrowsersApps = [ AppNames.Brave, AppNames.Chrome, AppNames.Firefox ];
        readonly List<AppNames> validBrowsersApps = [Types.AppNames.Chrome, Types.AppNames.Firefox ];

        readonly static string NoBrowsersMessage = string.Join(NLC, [
            "BAM Manager (BAMM) was unable to detect any valid browser installations.",
            "",
            "Supported browsers include:",
            "",
            "    - Chrome",
            "    - Firefox"
        ]);

        readonly static string NoPythonMessage = string.Join(NLC, [
            "BAM Manager (BAMM) was unable to detect any valid python installations.",
            "",
            "Supported versions include:",
            "",
            "    - Python 3.9.X",
            "    - Python 3.10.X",
            "    - Python 3.11.X",
            "    - Python 3.12.X",
            "    - Python 3.13.X",
            "    - Python 3.14.X"
        ]);

        private readonly Dictionary<string, AppNames> pythonVerMap = new()
        {
            { "Python 3.8", Types.AppNames.Python3_8 },
            { "Python 3.9", Types.AppNames.Python3_9 },
            { "Python 3.10", Types.AppNames.Python3_10 },
            { "Python 3.11", Types.AppNames.Python3_11 },
            { "Python 3.12", Types.AppNames.Python3_12 },
            { "Python 3.13", Types.AppNames.Python3_13 },
            { "Python 3.14", Types.AppNames.Python3_14 }
        };

        private void Add(AppNames app)
        {
            if (!AppNames.Contains(app)) {
                AppNames.Add(app);
            }
        }


        /// <summary>Attempts to get the enum member associated with the python version string <summary>
        /// <param name="name">The string representation of the Python version.</param>
        /// <param name="app">The returned enum member</param>
        /// <returns>Either the AppNames member associated or AppNames.Python3_X which will throw an exception later down the stack.</returns>
        private bool GetEnumMemberFromString(string name, out AppNames app)
        {
            var collection = pythonVerMap.Where(map => name.StartsWith(map.Key));
            
            if (!collection.Any())
            {
                app = Types.AppNames.Python3_X;
                return false;
            }

            app = collection.First().Value;
            return app != Types.AppNames.Python3_X;
        }

        private void CheckApp(AppInfo app, bool pythonOnly, string? version = null)
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
            //     Add(AppNames.Brave);
            // }

            // else if (!pythonOnly && app.Name.Contains("chrome", CCIC)) {
            //     Add(AppNames.Chrome);
            // }
            
            // Delete this line if the above two checks are reintroduced.
            if (!pythonOnly && app.Name.Contains("chrome", CCIC)) {
                Add(Types.AppNames.Chrome);
            }

            else if (!pythonOnly && app.Name.Contains("firefox", CCIC)) {
                Add(Types.AppNames.Firefox);
            }

            else if (version == null && GetEnumMemberFromString(app.Name, out AppNames appName)) {
                Add(appName);
            }

            // Unix Specific Recursive Case
            // To prevent an infinite loop, version must have a value to continue
            else if (Platforms.IsUnixLike && version != null && GetEnumMemberFromString(version, out AppNames appName2)) {
                Add(appName2);
            }

            else if (app.Name.StartsWith("python3"))
            {
                var foundVersion = GetMissingPythonVersion();

                if (string.IsNullOrEmpty(foundVersion)) {
                    Add(Types.AppNames.Python3_X); // This will raise an error once Transpiler.New is executed.
                }

                else if (GetEnumMemberFromString(foundVersion, out AppNames _)) {
                    CheckApp(app, pythonOnly: true, version: foundVersion);
                }

            }
        }
            
        private void CheckAndAdd(List<AppInfo> detectedApplications, string? verNum = null, bool pythonOnly = false)
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

        public static string GetMissingPythonVersion(string pythonVar = "python3")
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
            var missingVersion = GetMissingPythonVersion(pythonVar: "python");

            if (string.IsNullOrEmpty(missingVersion)) {
                WriteAndExit(NoPythonMessage, 1);
            }

            // This will return bool if successful, however:
            // As a fallback AppNames.Python3_X is returned, thus no check is required.
            GetEnumMemberFromString(missingVersion, out AppNames appName);
            Add(appName);
        }

    }
}