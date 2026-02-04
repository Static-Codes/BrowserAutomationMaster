using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.ProgramFunctions;
using static Publisher.PlatformSelection;
using static Publisher.SourceControl;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Managers;
using Publisher;
using System.Runtime.InteropServices;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

if (Platforms.IsRaspi || Platforms.IsARMel || Platforms.IsARMhf || Platforms.IsChromeOS) {
    Warning.Write("Your system was determined to be potentially underpowered for the purposed of compiling BAMM from source.");
    Warning.Write("If you experience build related issues, please try a more powerful system.");
};

var archiveFileType = SetArchiveFileType();
string? archiveFilePath;
string? workingDir;
string[]? packagingOptions;

// Download latest release of source
if (archiveFileType != "Skip Compilation and Start Packaging")
{
    archiveFilePath = await DownloadSourceOfLatestRelease(args, archiveFileType);

    if (archiveFilePath == null) 
    {
        WriteAndExit(
            message: "Unable to locate the BAMM codebase archive, please try again.", 
            status: 1
        );
    }


    var archiveManager = new ArchiveManager(archiveFileType, archiveFilePath);

    // Removes the starting "v" in "v1.0.0A(X)"
    var tagWithoutVersionMarker = LatestTag != null ? LatestTag[1..] : "Source";
    
    var codebaseSourceDir = Path.Join(
        GetSourceDirectory(),
        $"BrowserAutomationMaster-{tagWithoutVersionMarker}/"
    );

    // While this exits in the event an exception is thrown:
    // Directory.Exists(codebaseSourceDir) can potentially return false.
    if (!archiveManager.UnarchiveFile(args, codebaseSourceDir)) 
    {
        WriteAndExit(
            message: "Unable to locate the BAMM codebase source directory, please try again.", 
            status: 1
        );
    }

    workingDir = Path.Join(codebaseSourceDir, "src/BrowserAutomationMaster");
    packagingOptions = [
        "Debian Package (.deb)",
        "Fedora Package (.rpm)",
        "Arch Package (.pkg.tar.xz)",
        "Gentoo Package (.tbz2)",
        "Standalone Binary",
        "Windows Installer"
    ];
} 
else 
{
    archiveFilePath = Input.AskForInput("Enter the path to the standalone binary: ");
    workingDir = Path.GetDirectoryName(archiveFilePath);
    packagingOptions = [
        "Arch Package (.pkg.tar.xz)",
        "Gentoo Package (.tbz2)",
        "Windows Installer"
    ];
}






string desiredBuildProcess = Input.WriteListFromOptions(packagingOptions, "build process", pageSize: packagingOptions.Length);

Packager.SetSelectedOS(desiredBuildProcess, out string? selectedOS);

var availableArches = GetAvailableArchitectures(selectedOS)
                      .Select(arch => arch.ToString())
                      .ToArray();
                      
var selectedArch = Enum.Parse<Architecture>(
    Input.WriteListFromOptions(availableArches, "architecture")
);

var RID = GetRID(selectedOS, selectedArch);

if (RID == null) 
{
    WriteAndExit(
        message:"Unable to determine the Runtime ID for the specified system, please try again.", 
        status: 1
    );
}

var platformOption = new PlatformOption() {
    OSName = selectedOS,
    ArchitectureInfo = new(selectedArch, RID)
};

var packager = new Packager(platformOption);

(var result, var binaryPath) = await packager.HandlePackaging(desiredBuildProcess, workingDir!);

Console.WriteLine("Compilation Complete: {0}", result);
Console.WriteLine("Path: {0}", binaryPath);