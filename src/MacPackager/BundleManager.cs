using BrowserAutomationMaster.Managers;

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
                            FilePath = "",
                            FileContents = ""
                        }
                    }
                };
            }
        }
    }

    public struct DirectoryContents 
    {
        public required string FilePath { get; init; }
        public required string FileContents { get; init; }
    }

    public class BundleManager
    {
        // Uses $HOME on Unix-based machines and %USERPROFILE% on Windows-based machines
        private static readonly string USER_PROFILE_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string PARENT_DIRECTORY = Path.Combine(USER_PROFILE_DIR, "MACOS_RELEASE");
        
        // Directory Structure:
        //    BAMM.app/
        //    └── Contents/
        //        ├── Info.plist
        //        ├── MacOS/
        //        │   └── bamm
        //        └── Resources/
        //            └── AppIcon.icns
        private readonly List<BundleStructure> bundleStructure =
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
                            FilePath = "Info.plist",
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
                                    FilePath = "bamm",
                                    FileContents = "WRITE FUNCTION HERE TO BUILD THE LATEST RELEASE, THEN STREAM THE FILE CONTENTS, AND WRITE THE STREAMED OBJECT, BOTH BAMM AND BAMM-SILICON ARE REQUIRED"
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
                                    FilePath = "AppIcon.icns", 
                                    FileContents = "INSERT CONTENTS FOR AppIcon.icns"
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
            DirectoryManager.EnsureDirectoryExists(currentDirPath);
            
            // Inserts /MACOS_RELEASE/BAMM.app/Contents/Info.plist
            if (subStructure.DirectoryContents != null)
            {
                foreach (var file in subStructure.DirectoryContents)
                {
                    var filePath = Path.Combine(currentDirPath, file.FilePath);
                    Console.WriteLine($"Writing file: {filePath}");
                    File.WriteAllText(filePath, file.FileContents);
                }
            }

            // Inserts 
            // /MACOS_RELEASE/BAMM.app/Contents/MacOS/bamm 
            // /MACOS_RELEASE/BAMM.app/Contents/Resources/AppIcon.icns
            if (subStructure.SubDirectories != null)
            {
                foreach (var subDir in subStructure.SubDirectories)
                {
                    // currentDirPath overwrites the previously value of parentPath, then the child is created with the appropriate structure.
                    BuildDirectory(currentDirPath, subDir.DirectoryName, subDir);
                }
            }
        }

    }
}