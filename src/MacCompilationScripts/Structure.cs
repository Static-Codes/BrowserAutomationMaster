using MacCompilationScripts.Messaging;
using System.IO.Compression;

namespace MacCompilationScripts
{
    public static class Structure
    {
        readonly static string projectName = "bamm.app";
        static string? projectDirectory;
        static string? contentsDirectory;
        static string? macOSDirectory;
        static string? resourcesDirectory;
        public static void CreateDirectoryStructure(string versionNumber)
        {
            projectDirectory = Path.Combine($"BAMM-OSX64 Build {versionNumber}", projectName);
            contentsDirectory = Path.Combine(projectDirectory, "Contents");
            macOSDirectory = Path.Combine(contentsDirectory, "MacOS");
            resourcesDirectory = Path.Combine(contentsDirectory, "Resources");
            Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, projectDirectory));
            if (!Directory.Exists(projectDirectory))
            {
                Errors.WriteErrorAndExit($"Unable to create project directory:\n\n{projectDirectory}", 1);
            }
            Directory.CreateDirectory(contentsDirectory);
            if (!Directory.Exists(contentsDirectory))
            {
                Errors.WriteErrorAndExit($"Unable to create Content directory:\n\n{contentsDirectory}", 1);
            }
            Directory.CreateDirectory(macOSDirectory);
            if (!Directory.Exists(macOSDirectory))
            {
                Errors.WriteErrorAndExit($"Unable to create MacOS directory:\n\n{macOSDirectory}", 1);
            }
            Directory.CreateDirectory(resourcesDirectory);
            if (!Directory.Exists(resourcesDirectory))
            {
                Errors.WriteErrorAndExit($"Unable to create Resources directory:\n\n{resourcesDirectory}", 1);
            }
        }

        public static void AddAppBundle(string bundleFilePath)
        {
            try
            {
                string outputPath = Path.Combine(macOSDirectory!, "bamm");
                File.Copy(bundleFilePath, outputPath);
                Success.WriteSuccessMessage($"\nSuccessfully added bundle contents to:\n{outputPath}\n");
                
            }
            catch { 
                Errors.WriteErrorAndExit("Unable to bundle app contents, please ensure proper syntax and recompile.", 1); 
                return; // Added to appease c#'s static compiler
            }
        }

        public static void AddPlistFile(string versionNumber) // Refactor to include functionality for CLI args.
        {
            try
            {
                string? appVersion = Input.WriteTextAndReturnRawInput("Please enter the desired version number (Example: 1.0.0A):");
                string? minOSVersion = Input.WriteTextAndReturnRawInput("Please enter the desired minimum version of MacOS for this build, or hit enter to use the default of 11.0:");

                if (string.IsNullOrEmpty(appVersion)) { appVersion = "[Unspecified Version]"; }
                if (string.IsNullOrEmpty(minOSVersion)) { minOSVersion = "11.0"; }

                // Assuming the application gets to this point, contentsDirectory is guaranteed to be a non null value.
                string plistFilePath = Path.Combine(contentsDirectory!, "Info.plist");

                string plistContents = @$"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>CFBundleExecutable</key>
    <string>bamm</string>

    <key>CFBundleIdentifier</key>
    <string>com.static.bamm</string>

    <key>CFBundleName</key>
    <string>BAMM v{appVersion}</string>

    <key>CFBundleDisplayName</key>
    <string>BAMM v{appVersion}</string>

    <key>CFBundlePackageType</key>
    <string>APPL</string> <!-- Identifies this as an application -->

    <key>CFBundleShortVersionString</key>
    <string>{appVersion}</string>

    <key>CFBundleVersion</key>
    <string>{versionNumber}</string>

    <!-- Icon File (Optional for now, but good to have the key) -->
    <key>CFBundleIconFile</key>
    <string>myappicon.icns</string> <!-- **EDIT THIS**: Name of your .icns file in Contents/Resources (e.g., AppIcon.icns) -->
                                     <!-- If you don't have an icon yet, macOS will use a generic one. -->

    <!-- Other Important Keys -->
    <key>LSMinimumSystemVersion</key>
    <string>{minOSVersion}</string> <!-- **EDIT THIS**: Minimum macOS version your app supports.
                                  10.13 (High Sierra) is a reasonable baseline for .NET Core apps.
                                  For .NET 6+, 10.15 (Catalina) might be safer. For .NET 8, maybe 11.0 (Big Sur).
                                  Check .NET documentation for official minimums. -->

    <key>NSHighResolutionCapable</key>
    <true/> <!-- Enable support for Retina displays -->

    <!-- For GUI apps, you might need NSPrincipalClass, but for a self-contained .NET single-file executable,
         the OS launching CFBundleExecutable is usually enough.
         If it's a console app you want to pop a terminal, this is fine.
         If it's supposed to be a windowed app without a console, more might be needed (often handled by UI frameworks).
    -->
    <!-- <key>NSPrincipalClass</key> -->
    <!-- <string>NSApplication</string> -->

</dict>
</plist>";
                File.WriteAllText(plistFilePath, plistContents);
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit($"Unable to compile Info.plist, please see the error below:\n\n{ex}", 1);
            }
        }
        
        public static void ZipBuild() {
            string fullProjectDirectory = Path.Combine(AppContext.BaseDirectory, projectDirectory!);
            string parentDirectory = Path.GetDirectoryName(fullProjectDirectory) ?? AppContext.BaseDirectory; // Unlikely to return a null value but BaseDirectory is a backup.
            

            string zipFileName = "bamm.app.zip"; //Path.GetFileName(fullProjectDirectory) + ".zip"; // "bamm.app.zip"
            string zipFilePath = Path.Combine(parentDirectory, zipFileName);
            if (Path.Exists(zipFilePath)) {
                Console.WriteLine($"Deleting existing zip file:\n{Path.Combine(AppContext.BaseDirectory, projectDirectory!, "bamm.app.zip")}");
                File.Delete(zipFilePath);
            }

            try { ZipFile.CreateFromDirectory(fullProjectDirectory, zipFilePath); }
            catch { Errors.WriteErrorAndExit($"Unable to zip:\n{fullProjectDirectory!}\n\nPlease manually zip this directory if required.", 1); }

        }

        public static void CompileBuild(string bundleFilePath, string versionNumber)
        {
            CreateDirectoryStructure(versionNumber);
            AddAppBundle(bundleFilePath);
            AddPlistFile(versionNumber);
            // If the application reaches this point projectDirectory is guaranteed to be a non null value.
            Success.WriteSuccessMessage($"\nSuccessfully compiled:\n{Path.Combine(AppContext.BaseDirectory, projectDirectory!)}\n");
            ZipBuild();
            Success.WriteSuccessMessage($"\nSuccessfully zipped:\n{Path.Combine(AppContext.BaseDirectory, projectDirectory!, "bamm.app.zip")}");
            Console.WriteLine("\nPress any key to exit..");
            Console.ReadKey();
        }
    }
}
