using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using System.Reflection;
using System.Text;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace MacPackager
{
    public struct BundleStructure
    {
        public required string DirectoryName { get; init; }

        public required SubDirectory Subdirectory { get; init; }
    }

    public struct SubDirectory()
    {
        public required string DirectoryName { get; init; }

        public required List<DirectoryContents> DirectoryContents { get; init; }
        public List<SubDirectory> SubDirectories = [];

        // Meant to act similiarly to string.Empty, and will be used for comparisons.
        public static SubDirectory Empty
        {
            get
            {
                return new SubDirectory
                {
                    DirectoryName = "",
                    DirectoryContents = new List<DirectoryContents>()
                    {
                        new DirectoryContents 
                        {
                            FileName = "",
                            FileContents = null
                        }
                    }
                };
            }
        }
    }

    public struct DirectoryContents 
    {
        public required string FileName { get; init; }
        public required MemoryStream? FileContents { get; init; }
    }

    public class BundleManager
    {

        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
        // Uses $HOME on Unix-based machines and %USERPROFILE% on Windows-based machines
        private static readonly string USER_PROFILE_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string PARENT_DIRECTORY = Path.Combine(USER_PROFILE_DIR, "MACOS_RELEASE");
        private static readonly string BINARY_NAME = "bamm";
        
        // Directory Structure:
        //    BAMM.app/
        //    └── Contents/
        //        ├── Info.plist
        //        ├── MacOS/
        //        │   └── bamm
        //        └── Resources/
        //            └── AppIcon.icns

        // Apparently IReadOnlyList is not just semantic
        // It removes all reference to the original members that are unavailable on readonly types.
        private readonly IReadOnlyList<BundleStructure> bundleStructure =
        [
            // MACOS_RELEASE/BAMM.app
            new BundleStructure()
            {
                DirectoryName = "BAMM.app",
                
                // MACOS_RELEASE/BAMM.app/Contents
                Subdirectory = new SubDirectory
                {
                    DirectoryName = "Contents",
                    
                    // MACOS_RELEASE/BAMM.app/Contents/Info.plist
                    DirectoryContents = new List<DirectoryContents>
                    {
                        new DirectoryContents()
                        {
                            FileName = "Info.plist",
                            // Normally I'd avoid using sync calls of async functions, but I dont really have a choice here.
                            FileContents = PlistManager.GetPlistContent().GetAwaiter().GetResult()
                        }
                    },
                    
                    SubDirectories = new List<SubDirectory>
                    {
                        // MACOS_RELEASE/BAMM.app/Contents/MacOS/
                        new SubDirectory
                        {
                            DirectoryName = "MacOS",
                            // This uses used by the CFBundleExecutable key in Info.plist
                            DirectoryContents = new List<DirectoryContents>
                            {
                                new DirectoryContents()
                                {
                                    FileName = "bamm",
                                    // FileContents = "WRITE FUNCTION HERE TO BUILD THE LATEST RELEASE, THEN STREAM THE FILE CONTENTS, AND WRITE THE STREAMED OBJECT, BOTH BAMM AND BAMM-SILICON ARE REQUIRED"
                                    FileContents = null
                                }
                            }
                        },
                        
                        // MACOS_RELEASE/BAMM.app/Contents/Resources/
                        new SubDirectory
                        {
                            DirectoryName = "Resources",
                            DirectoryContents = new List<DirectoryContents>
                            {
                                new DirectoryContents()
                                {
                                    FileName = "AppIcon.icns", 
                                    FileContents = GetAppIconStream()
                                },
                            }
                        }
                    }
                }
            }
        ];

        // Apparently IReadOnlyList isn't just semantic, it provides methods associated with a readonly element.
        private IReadOnlyList<BundleStructure> GetBundleStructure() => bundleStructure;

        public void BuildBundle()
        {
            var bundleDirectoryStructure = GetBundleStructure();

            // Ensures MACOS_RELEASE/ exists
            DirectoryManager.EnsureDirectoryExists(PARENT_DIRECTORY);

            foreach (var bundle in bundleDirectoryStructure)
            {
                // Start building from PARENT_DIRECTORY/BAMM.app
                BuildDirectory(PARENT_DIRECTORY, bundle.DirectoryName, bundle.Subdirectory);
            }
        }

        // THIS IS RECURSIVE, HANDLE ACCORDINGLY.
        private void BuildDirectory(string parentPath, string currentDirName, SubDirectory subStructure)
        {
            // PARENT_DIRECTORY/BAMM.app
            var currentDirPath = Path.Combine(parentPath, currentDirName);
            Console.WriteLine("[INFO]: Creating base bundle directory at: {currentDirPath}");
            DirectoryManager.EnsureDirectoryExists(currentDirPath);
            

            // Inserts /MACOS_RELEASE/BAMM.app/Contents/Info.plist
            if (subStructure.DirectoryContents != null)
            {
                foreach (var file in subStructure.DirectoryContents)
                {
                    var filePath = Path.Combine(currentDirPath, file.FileName);
                    
                    
                    if (file.FileContents == null) 
                    {
                        Warning.Write($"[WARNING]: {file.FileName} was not provided any contents, skipping.");
                        continue;
                    }

                    try 
                    {
                        DisplayStatus(filePath, completed: false); 
                        File.WriteAllBytes(filePath, file.FileContents.ToArray());
                    }

                    catch (Exception ex)
                    {
                        WriteAndExit(
                            string.Join(NLC, [ 
                                "A fatal error occured while writing a file to the application bundle.",
                                $"File Location: {filePath}",
                                $"Error Log: {ex.StackTrace ?? ex.Message}",
                            ]), 
                            status: 1
                        );
                    }

                    DisplayStatus(filePath, completed: true);

                }
            }

            // Inserts:
            // 1. -> /MACOS_RELEASE/BAMM.app/Contents/MacOS/bamm
            // 2. -> /MACOS_RELEASE/BAMM.app/Contents/Resources/AppIcon.icns
            if (subStructure.SubDirectories != null)
            {
                foreach (var subDir in subStructure.SubDirectories)
                {
                    // currentDirPath overwrites the previously value of parentPath, then the child is created with the appropriate structure.
                    BuildDirectory(currentDirPath, subDir.DirectoryName, subDir);
                }
            }
        }

        private void DisplayStatus(string filePath, bool completed = false)
        {
            if (filePath.EndsWith("Info.plist")) 
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Writing the required metadata for the application bundle to: {filePath}");
                    return;
                }

                Success.WriteSuccessMessage($"[SUCCESS]: Wrote the required metadata for the application bundle to: {filePath}");
                return;
            }

            else if (filePath.EndsWith(BINARY_NAME))
            {
                if (!completed) 
                {
                    Console.WriteLine($"[INFO]: Copying the standalone binary to the application bundle at: {filePath}");
                    return;
                }

                Console.WriteLine($"[SUCCESS]: Copied the standalone binary to the application bundle at: {filePath}");
                return;
            }

            else if (filePath.EndsWith("AppIcon.icns"))
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Writing the icon for the application bundle to: {filePath}");
                    return;
                }

                Console.WriteLine($"[SUCCESS]: Wrote the icon for the application bundle to: {filePath}");
                return;
            }

            else 
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Adding a file to the application bundle at: {filePath}");
                    return;
                }

                Console.WriteLine($"[SUCCESS]: Added a file to the application bundle at: {filePath}");
                return;
            }
        }

        public static MemoryStream GetAppIconStream()
        {
            var resourceName = "AppIcon.icns";
            var resourcePattern = "MacPackager.AppIcon.icns";

            var bundleManager = new BundleManager();
            using var stream = bundleManager.GetEmbeddedResource(resourceName, resourcePattern);
            var memoryStream = new MemoryStream();
            
            // Obligitory cleanup to prevent a corrupted output.
            try 
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
            }

            catch (Exception ex) 
            {
                WriteAndExit(
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        "Error Log:",
                        ex.StackTrace ?? ex.Message,
                    ]), 
                    status: 1
                );
            }
            return memoryStream;

            // File.WriteAllBytes("AppIcon.icns", Encoding.UTF8.GetBytes(reader.ReadToEnd()));
        }

        private Stream GetEmbeddedResource(string resourceName, string resourcePattern) 
        {
            // var resourcePattern = "*MacPackager*.*AppIcon*.icns";
            Stream? resourceStream = null;

            try
            {
                resourceStream = assembly.GetManifestResourceStream(resourcePattern);

                if (resourceStream == null) 
                {
                    WriteAndExit(string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        "Error Log:",
                        "resourceStream returned null"
                    ]), status: 1);
                }
            }
            catch (Exception ex)
            {
                WriteAndExit(string.Join(NLC, [
                    $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                    $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                ]), status: 1);
            }

            return resourceStream;
        }

    }
}