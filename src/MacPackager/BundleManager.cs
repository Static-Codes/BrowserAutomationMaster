using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using System.Reflection;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace MacPackager
{
    readonly struct BundleStructure
    {
        public required string DirectoryName { get; init; }

        public required SubDirectory Subdirectory { get; init; }
    }

    readonly struct SubDirectory()
    {
        public required string DirectoryName { get; init; }

        public required List<DirectoryContents> DirectoryContents { get; init; }
        public required List<SubDirectory> SubDirectories { get; init; }

        // Meant to act similiarly to string.Empty, and will be used for comparisons.
        public static SubDirectory Empty
        {
            get
            {
                return new SubDirectory
                {
                    DirectoryName = "",
                    DirectoryContents =
                    [
                        new DirectoryContents 
                        {
                            FileName = "",
                            FileContents = null
                        }
                    ],
                    SubDirectories = []
                };
            }
        }
    }

    readonly struct DirectoryContents 
    {
        public required string FileName { get; init; }
        public required MemoryStream? FileContents { get; init; }
    }

    public class BundleManager
    {
        private static readonly string filePath = Input.AskForInput("[INPUT]: Please enter the path to a valid standalone binary of BAMM compiled for macOS: ");
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();
        // Uses $HOME on Unix-based machines and %USERPROFILE% on Windows-based machines
        private static readonly string USER_PROFILE_DIR = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        private static readonly string PARENT_DIRECTORY = Path.Combine(USER_PROFILE_DIR, "MACOS_RELEASE");
        private static readonly string BINARY_NAME = "bamm";
        
        
        
        private Dictionary<string, string?> defaultBuildConfig = new() {
            { "MacOSBinaryPath", "" },
            { "CPUType", "x86_64" },
        };
        
        
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
                    DirectoryContents =
                    [
                        new DirectoryContents()
                        {
                            FileName = "Info.plist",
                            // Normally I'd avoid using sync calls of async functions, but I dont really have a choice here.
                            FileContents = PlistManager.GetPlistContent().GetAwaiter().GetResult()
                        }
                    ],
                    
                    SubDirectories =
                    [
                        // MACOS_RELEASE/BAMM.app/Contents/MacOS/
                        new SubDirectory
                        {
                            DirectoryName = "MacOS",
                            // This uses used by the CFBundleExecutable key in Info.plist
                            DirectoryContents =
                            [
                                new DirectoryContents()
                                {
                                    FileName = "bamm",
                                    // FileContents = "WRITE FUNCTION HERE TO BUILD THE LATEST RELEASE, THEN STREAM THE FILE CONTENTS, AND WRITE THE STREAMED OBJECT, BOTH BAMM AND BAMM-SILICON ARE REQUIRED"
                                    FileContents = GetMacOSBinaryContents(filePath)
                                }
                            ],
                            SubDirectories = []
                        },
                        
                        // MACOS_RELEASE/BAMM.app/Contents/Resources/
                        new SubDirectory
                        {
                            DirectoryName = "Resources",
                            DirectoryContents =
                            [
                                new DirectoryContents()
                                {
                                    FileName = "AppIcon.icns", 
                                    FileContents = GetAppIconStream()
                                },
                            ],
                            SubDirectories = []
                        }
                    ]
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

            WriteSuccessMessage("[SUCCESS]: Completed the BAMM for macOS application bundle process");
            
            if (ClipboardHelper.TrySetText(PARENT_DIRECTORY))
            {
                WriteSuccessMessageAndExit("[SUCCESS]: Copied the path of the application bundle to your clipboard.", 0);   
            }

            Warning.Write($"[WARNING]: Failed to copy the path of the application bundle to your clipboard, it can be found at: {PARENT_DIRECTORY}");
        }

        // THIS IS RECURSIVE, HANDLE ACCORDINGLY.
        private static void BuildDirectory(string parentPath, string currentDirName, SubDirectory subStructure)
        {
            // PARENT_DIRECTORY/BAMM.app
            var currentDirPath = Path.Combine(parentPath, currentDirName);
            
            if (parentPath.EndsWith("MACOS_RELEASE")) {
                Console.WriteLine($"[INFO]: Creating base application bundle directory at: {currentDirPath}");
            } else {
                Console.WriteLine($"[INFO]: Creating new directory for application bundle at: {currentDirPath}");
            }

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
                        WriteAndExit
                        (
                            string.Join(NLC, [ 
                                "A fatal error occured while writing a file to the application bundle.",
                                $"File Location: {filePath}",
                                $"Error Log: {ex.StackTrace ?? ex.Message}",
                            ]), 
                            status: 1,
                            writePlatformDebugInfo: false
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

        private static void DisplayStatus(string filePath, bool completed = false)
        {
            if (filePath.EndsWith("Info.plist")) 
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Writing the required metadata for the application bundle to: {filePath}");
                    return;
                }

                WriteSuccessMessage($"[SUCCESS]: Wrote the required metadata for the application bundle to: {filePath}");
                return;
            }

            else if (filePath.EndsWith(BINARY_NAME))
            {
                if (!completed) 
                {
                    Console.WriteLine($"[INFO]: Copying the standalone binary to the application bundle at: {filePath}");
                    return;
                }

                WriteSuccessMessage($"[SUCCESS]: Copied the standalone binary to the application bundle at: {filePath}");
                return;
            }

            else if (filePath.EndsWith("AppIcon.icns"))
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Writing the icon for the application bundle to: {filePath}");
                    return;
                }

                WriteSuccessMessage($"[SUCCESS]: Wrote the icon for the application bundle to: {filePath}");
                return;
            }

            else 
            {
                if (!completed)
                {
                    Console.WriteLine($"[INFO]: Adding a file to the application bundle at: {filePath}");
                    return;
                }

                WriteSuccessMessage($"[SUCCESS]: Added a file to the application bundle at: {filePath}");
                return;
            }
        }

        // Calls GetEmbeddedResource("AppIcon.icns", "MacPackager.AppIcon.icns")
        public static MemoryStream GetAppIconStream()
        {
            var resourceName = "AppIcon.icns";
            var resourcePattern = "MacPackager.AppIcon.icns";

            using var stream = GetEmbeddedResource(resourceName, resourcePattern);
            var memoryStream = new MemoryStream();
            
            // Obligitory cleanup to prevent a corrupted output.
            try 
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;
            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        "Error Log:",
                        ex.StackTrace ?? ex.Message,
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }


            // DEBUGGING ONLY
            // File.WriteAllBytes("AppIcon.icns", Encoding.UTF8.GetBytes(reader.ReadToEnd()));

            return memoryStream;
        }

        // Currently only used for AppIcon.icns, however, will work for all embedded project resources if needbe.
        private static Stream GetEmbeddedResource(string resourceName, string resourcePattern) 
        {
            // var resourcePattern = "*MacPackager*.*AppIcon*.icns";
            Stream? resourceStream = null;

            try
            {
                resourceStream = assembly.GetManifestResourceStream(resourcePattern);

                if (resourceStream == null) 
                {
                    WriteAndExit
                    (
                        string.Join(NLC, [
                            $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                            "Error Log:",
                            "resourceStream returned null"
                        ]), 
                        status: 1,
                        writePlatformDebugInfo: false
                    );
                }
            }
            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            return resourceStream;
        }


        // Validates that the file at filePath is a valid macOS binary, if so, it returns a MemoryStream of its contents.
        private static MemoryStream GetMacOSBinaryContents(string filePath)
        {   
            // Ensures the file exists, and is a valid macOS binary
            // Will error out if an exception occurs
            ValidateBinaryType(filePath);

            Stream? stream = null;

            Console.WriteLine("[INFO]: Opened a stream object to read the contents of the selected file.");

            try
            {
                stream = new FileStream(filePath, FileMode.Open);

                if (stream is null)
                {
                    throw new EndOfStreamException("Stream returned a null value.");
                }

                WriteSuccessMessage($"[SUCCESS]: Read {stream.Length} bytes from the selected file.");
            }

            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to read the contents of: {filePath}",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            Console.WriteLine("[INFO]: Creating a temporary MemoryStream object.");
            using var memoryStream = new MemoryStream();
            WriteSuccessMessage($"[SUCCESS]: Created the required MemoryStream object.");

            Console.WriteLine("[INFO]: Copying the generic Stream object to the more useful MemoryStream object.");
            Warning.Write("[WARNING]: This transfers around ~70MB of data, this may take up to five minutes on slow machines, please be patient.");
            
            try 
            {
                stream.CopyTo(memoryStream); // This copies ~70MB of data, it may take awhile
            }

            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to copying the generic Stream object.",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            WriteSuccessMessage("[SUCCESS]: Sending macOS binary contents to the BAMM for macOS Packager.");
            return memoryStream;
        }


        // Reads the first 7 bytes of the file at the specified path, checking for Apple's Magic Numbers (0xcffaedfe) 
        // https://en.wikipedia.org/wiki/Mach-O#Header
        public static void ValidateBinaryType(string filePath) 
        {
            if (Path.HasExtension(filePath)) 
            {
                WriteAndExit
                (
                    message: "[ERROR]: The provided file is not a valid standalone binary for macOS, it contains a file extension.", 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            if (!File.Exists(filePath)) 
            {
                // Writes the initial text in red for emphasis.
                Write("[ERROR]: The provided file does not exist at: ", noNewLines: true);

                // Writes the provided filePath in yellow for clarity.
                Warning.Write(filePath, noNewLines: true);

                // Trailing new-line char for uniform output.
                Console.Write(NLC);
                Environment.Exit(1);
            }

            
            Stream? binaryStream = null;
            try
            {
                binaryStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4);
                
                if (binaryStream is null)
                {
                    WriteAndExit
                    (
                        string.Join(NLC, [
                            "[ERROR]: An exception occured while trying to validate the provided file.",
                            "Error Log: binaryStream is null."
                        ]), 
                        status: 1,
                        writePlatformDebugInfo: false
                    );
                }

                WriteSuccessMessage("[SUCCESS]: Read the first 4 bytes of the provided file.");
            }

            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to validate the provided file.",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }



            if (binaryStream.Length < 4)
            {
                WriteAndExit
                (
                    message: "[ERROR]: The length of the provided file is less than 4 bytes, this indicates it is not a valid binary.", 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            byte[] first4Bytes = new byte[4];
            try 
            {
                Console.WriteLine($"[INFO]: Copying the first 4 bytes of the provided file to a byte array.");
                binaryStream.Read(first4Bytes, 0, 4);
                WriteSuccessMessage("[SUCCESS: Copied the first 4 bytes of the provided file to a byte array.");
            }

            catch (Exception ex) {
                WriteAndExit
                (
                    string.Join(NLC, 
                    [
                        $"[ERROR]: An exception occured while trying to validate the provided file.",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            Console.WriteLine("[INFO]: Disposing of leftover stream.");
            binaryStream.Dispose();
            WriteSuccessMessage("[SUCCESS]: Disposed of leftover stream.");


            Console.WriteLine("[INFO]: Comparing the 4 copied bytes to documented Apple Magic Numbers for x64 CPU Architecture (0xcffaedfe).");
            var appleMagicNumbers = new byte[4] { 0xcf, 0xfa, 0xed, 0xfe };
            var isMachOBinary = first4Bytes.SequenceEqual(appleMagicNumbers);

            if (isMachOBinary) {
                WriteSuccessMessage("[SUCCESS]: The provided file is a valid macOS binary!");
                return;
            }

            WriteAndExit
            (
                "[ERROR]: The provided file is not a valid macOS binary, as it did not match the documented Apple Magic Numbers for x64 CPU Architecture (0xcffaedfe).", 
                status: 1,
                writePlatformDebugInfo: false
            );
            
        }
    }
}