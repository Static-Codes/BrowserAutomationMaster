using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using BrowserAutomationMaster.AppManager;
using BrowserAutomationMaster.Messaging;

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
                //Console.WriteLine(app.Name);
                //if (app.Name.ToLower().Contains("brave")) {
                //    if (!AppNames.Contains(ApplicationNames.Brave)) {
                //        AppNames.Add(ApplicationNames.Brave);
                //    }
                //}

                else if (app.Name.ToLower().Contains("chrome")) {
                    if (!AppNames.Contains(ApplicationNames.Chrome)) {
                        AppNames.Add(ApplicationNames.Chrome);
                    }
                }

                else if (app.Name.ToLower().Contains("firefox")) {
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

    // public static class WindowsInstallationCheck
    // {
        //readonly static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //// Handles both x64 and x86 based systems
        //readonly static string ProgramFilesPath = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        //public readonly static string BravePath = Path.Combine(ProgramFilesPath, "BraveSoftware", "Brave-Browser", "Application", "brave.exe");
        //public readonly static string ChromePath = Path.Combine(ProgramFilesPath, "Google", "Chrome", "Application", "chrome.exe");
        //public readonly static string FirefoxPath = Path.Combine(ProgramFilesPath, "Mozilla Firefox", "firefox.exe");
        //readonly static string PythonBasePath = Path.Combine(AppDataPath, "Programs", "Python");
        //readonly static string Python39Path = Path.Combine(PythonBasePath, "Python39", "python.exe");
        //readonly static string Python310Path = Path.Combine(PythonBasePath, "Python310", "python.exe");
        //readonly static string Python311Path = Path.Combine(PythonBasePath, "Python311", "python.exe");
        //readonly static string Python312Path = Path.Combine(PythonBasePath, "Python312", "python.exe");
        //readonly static string Python313Path = Path.Combine(PythonBasePath, "Python313", "python.exe");
        //readonly static string Python314Path = Path.Combine(PythonBasePath, "Python314", "python.exe");


        //readonly static bool BravePresent = File.Exists(BravePath);
        //readonly static bool ChromePresent = File.Exists(ChromePath);
        //readonly static bool FirefoxPresent = File.Exists(FirefoxPath);
        //readonly static bool PythonBasePresent = Path.Exists(PythonBasePath);
        //readonly static bool Python39Present = File.Exists(Python39Path);
        //readonly static bool Python310Present = File.Exists(Python310Path);
        //readonly static bool Python311Present = File.Exists(Python311Path);
        //readonly static bool Python312Present = File.Exists(Python312Path);
        //readonly static bool Python313Present = File.Exists(Python313Path);
        //readonly static bool Python314Present = File.Exists(Python314Path);
        //readonly static List<AppInfo> AppNames = [];
        //public readonly static List<ApplicationNames> BrowserApps = [ApplicationNames.Brave, ApplicationNames.Chrome, ApplicationNames.Firefox];
        //public readonly static List<ApplicationNames> PythonApps = [ApplicationNames.Python3_9, ApplicationNames.Python3_10, ApplicationNames.Python3_11, ApplicationNames.Python3_12, ApplicationNames.Python3_13, ApplicationNames.Python3_14];


        //readonly static string NoBrowsersMessage = $"""
        //BAM Manager (BAMM) was unable to detect any valid browser installations.\n\nValid Installation Locations:

        //Brave: '{BravePath}'
        //Chrome: '{ChromePath}'
        //Firefox: '{FirefoxPath}'
        //""";

        //readonly static string NoPythonMessage = $"""
        //BAM Manager (BAMM) was unable to detect any valid python installations.\n\nValid Installation Locations:

        //Python 3.9.X: '{Python39Path}'
        //Python 3.10.X: '{Python310Path}'
        //Python 3.11.X: '{Python311Path}'
        //Python 3.12.X: '{Python312Path}'
        //Python 3.13.X: '{Python313Path}'
        //Python 3.14.X: '{Python314Path}'
        //""";


        //public static Installations Run() {   
        // bool browserFound = false;
        // bool pythonFound = false;
        //    if (BravePresent || ChromePresent || FirefoxPresent) { browserFound = true; } // Setting this flag to true in one line is the easiest solution.
        //    if (BravePresent) { AppNames.Add(new AppInfo { Name = "Brave" }); }
        //    if (ChromePresent) { AppNames.Add(new AppInfo { Name = "Google Chrome" }); }
        //    if (FirefoxPresent) { AppNames.Add(new AppInfo { Name = "Firefox" }); }


        //    if (PythonBasePresent && Python39Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }
        //    if (PythonBasePresent && Python310Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }
        //    if (PythonBasePresent && Python311Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }
        //    if (PythonBasePresent && Python312Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }
        //    if (PythonBasePresent && Python313Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }
        //    if (PythonBasePresent && Python314Present) { AppNames.Add(new AppInfo { Name = ApplicationNames.Python3_9.ToString().Replace("Python3_", "Python 3.") }); }

        //    if (AppNames.Any(x=>x.Name.StartsWith("Python 3."))) { pythonFound = true; }

        //    if (!browserFound) { Errors.WriteErrorAndExit(NoBrowsersMessage, 1); }
        //    if (!pythonFound) { Errors.WriteErrorAndExit(NoPythonMessage, 1); }
        //    return new Installations(AppNames);
        //}


    // }
    //public static class Installations
    //{
    //    readonly static string NoBrowsersMessage = $"""
    //    BAM Manager (BAMM) was unable to detect any valid browser installations.\n\nSupported browsers include:

    //    - Brave
    //    - Chrome
    //    - Firefox
    //    """;

    //    readonly static string NoPythonMessage = $"""
    //    BAM Manager (BAMM) was unable to detect any valid python installations.\n\nSupported browsers include:

    //    - Python 3.9.X
    //    - Python 3.10.X
    //    - Python 3.11.X
    //    - Python 3.12.X
    //    - Python 3.13.X
    //    - Python 3.14.X
    //    """;
    //    static List<AppInfo> App

    //    public static Installations Run()
    //    {
    //        bool browserFound = false;
    //        bool pythonFound = false;
    //        List<AppInfo> installedApps = InstalledApps.GetInstalledApps();
    //        if (installedApps.Any(x => x.Name.StartsWith("Python 3."))) { pythonFound = true; }
    //        if (installedApps.Any(x => x.Name.StartsWith("Brave") || x.Name.StartsWith("Firefox") || x.Name.StartsWith("Google Chrome"))) { browserFound = true; }
    //        if (!browserFound) { Errors.WriteErrorAndExit(NoBrowsersMessage, 1); }
    //        if (!pythonFound) { Errors.WriteErrorAndExit(NoPythonMessage, 1); }
    //        return Installations(installedApps);
    //    }
    //}
}
