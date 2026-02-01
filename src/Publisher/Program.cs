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


var archiveFileType = SetArchiveFileType();

// Download latest release of source
var archiveFilePath = await DownloadSourceOfLatestRelease(args, archiveFileType);

if (archiveFilePath == null) {
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

var workingDir = Path.Join(codebaseSourceDir, "src/BrowserAutomationMaster");

string[] packagingOptions = [
    "Debian Package (.deb)",
    "Fedora Package (.rpm)",
    "Arch Package (.pkg.tar.xz)",
    "Gentoo Package (.tbz2)",
    "Standalone Binary",
    "Windows Installer"
];

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
(var result, var binaryPath) = await packager.HandlePackaging(desiredBuildProcess, workingDir);

Console.WriteLine(result);
Console.WriteLine(binaryPath);