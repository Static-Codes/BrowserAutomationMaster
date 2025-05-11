using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    public enum ApplicationNames
    {
        Brave,
        Chrome,
        Firefox,
        Python3_9, // Display warning that packages might not be compatible, stick to 3.10 or 3.11
        Python3_10,
        Python3_11,
        Python3_12, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_13, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
        Python3_14, // Display warning that all packages might not be compatible, compile with 3.10 or 3.11
    }

    public partial class Installations
    {
        public List<ApplicationNames> InstalledApps {get; set;}
        public Installations(List<ApplicationNames> detectedApplications)
        {
            InstalledApps = detectedApplications ?? [];
        }
        public Installations() // Empty constructor used as a fallback.
        {
            InstalledApps = [];
        }

    }
    public static class InstallationCheck
    {
        readonly static string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        // Handles both x64 and x86 based systems
        readonly static string ProgramFilesPath = Environment.GetEnvironmentVariable("ProgramW6432") ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        public readonly static string BravePath = Path.Combine(ProgramFilesPath, "BraveSoftware", "Brave-Browser", "Application", "brave.exe");
        public readonly static string ChromePath = Path.Combine(ProgramFilesPath, "Google", "Chrome", "Application", "chrome.exe");
        public readonly static string FirefoxPath = Path.Combine(ProgramFilesPath, "Mozilla Firefox", "firefox.exe");
        readonly static string PythonBasePath = Path.Combine(AppDataPath, "Programs", "Python");
        readonly static string Python39Path = Path.Combine(PythonBasePath, "Python39", "python.exe");
        readonly static string Python310Path = Path.Combine(PythonBasePath, "Python310", "python.exe");
        readonly static string Python311Path = Path.Combine(PythonBasePath, "Python311", "python.exe");
        readonly static string Python312Path = Path.Combine(PythonBasePath, "Python312", "python.exe");
        readonly static string Python313Path = Path.Combine(PythonBasePath, "Python313", "python.exe");
        readonly static string Python314Path = Path.Combine(PythonBasePath, "Python314", "python.exe");
        

        readonly static bool BravePresent = File.Exists(BravePath);
        readonly static bool ChromePresent = File.Exists(ChromePath);
        readonly static bool FirefoxPresent = File.Exists(FirefoxPath);
        readonly static bool PythonBasePresent = Path.Exists(PythonBasePath);
        readonly static bool Python39Present = File.Exists(Python39Path);
        readonly static bool Python310Present = File.Exists(Python310Path);
        readonly static bool Python311Present = File.Exists(Python311Path);
        readonly static bool Python312Present = File.Exists(Python312Path);
        readonly static bool Python313Present = File.Exists(Python313Path);
        readonly static bool Python314Present = File.Exists(Python314Path);
        readonly static List<ApplicationNames> AppNames = [];
        public readonly static List<ApplicationNames> BrowserApps = [ApplicationNames.Brave, ApplicationNames.Chrome, ApplicationNames.Firefox];
        public readonly static List<ApplicationNames> PythonApps = [ApplicationNames.Python3_9, ApplicationNames.Python3_10, ApplicationNames.Python3_11, ApplicationNames.Python3_12, ApplicationNames.Python3_13, ApplicationNames.Python3_14];


        readonly static string NoBrowsersMessage = $"""
            BAM Manager (BAMM) was unable to detect any valid browser installations.\n\nValid Installation Locations:
            
            Brave: '{BravePath}'
            Chrome: '{ChromePath}'
            Firefox: '{FirefoxPath}'
        """;

        readonly static string NoPythonMessage = $"""
            BAM Manager (BAMM) was unable to detect any valid browser installations.\n\nValid Installation Locations:

            Python 3.9.X: '{Python39Path}'
            Python 3.10.X: '{Python310Path}'
            Python 3.11.X: '{Python311Path}'
            Python 3.12.X: '{Python312Path}'
            Python 3.13.X: '{Python313Path}'
            Python 3.14.X: '{Python314Path}'
        """;


        public static Installations Run() {
            //Console.WriteLine(ProgramFiles);
            Console.WriteLine(ProgramFilesPath);

            Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            if (BravePresent) {  AppNames.Add(ApplicationNames.Brave); }
            if (ChromePresent) { AppNames.Add(ApplicationNames.Chrome); }
            if (FirefoxPresent) { AppNames.Add(ApplicationNames.Firefox); }
            if (PythonBasePresent && Python39Present) { AppNames.Add(ApplicationNames.Python3_9); }
            if (PythonBasePresent && Python310Present) { AppNames.Add(ApplicationNames.Python3_10); }
            if (PythonBasePresent && Python311Present) { AppNames.Add(ApplicationNames.Python3_11); }
            if (PythonBasePresent && Python312Present) { AppNames.Add(ApplicationNames.Python3_12); }
            if (PythonBasePresent && Python313Present) { AppNames.Add(ApplicationNames.Python3_13); }
            if (PythonBasePresent && Python314Present) { AppNames.Add(ApplicationNames.Python3_14); }
            if (!AppNames.Any(x => BrowserApps.Contains(x))) { Errors.WriteErrorAndExit(NoBrowsersMessage, 1); }
            if (!AppNames.Any(x => PythonApps.Contains(x))) { Errors.WriteErrorAndExit(NoPythonMessage, 1); }
            return new Installations(AppNames);
        }
    }
}
