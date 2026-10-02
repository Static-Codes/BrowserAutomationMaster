using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.Types.Linux;
using Publisher;
using System.Runtime.InteropServices;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Utilities.AppUpdateUtility;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using static BrowserAutomationMaster.ProgramFunctions;
using static Publisher.PlatformSelection;
using static Publisher.SourceControl;

// Logic from Main application around colored text.
SetPlatform(GlobalUserInfo);
await InitializeAsync(["--nohwc"]);

bool[] invalidStates = [
    GlobalUserInfo.PlatformInfo.IsPiDevice,
    GlobalUserInfo.PlatformInfo.IsARMel,
    GlobalUserInfo.PlatformInfo.IsARMhf,
    GlobalUserInfo.PlatformInfo.IsChromeOS
];

if (invalidStates.Any(invalidState => invalidState)) {
    Warning.Write("Your system was determined to be potentially underpowered for the purposed of compiling BAMM from source.");
    Warning.Write("If you experience build related issues, please try a more powerful system.");
};

var archiveFileType = SetArchiveFileType();
string? archiveFilePath;
string? workingDir;
string appVersion;
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


    var archiveManager = new ArchiveHelper(archiveFileType, archiveFilePath);

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
        "Alt Linux Package (.rpm)",
        "Arch Package (.pkg.tar.xz)",
        "Debian Package (.deb)",
        "Fedora Package (.rpm)",
        "Gentoo Package (.tbz2)",
        "PCLinuxOS Package (.rpm)",
        "Standalone Binary",
        "Windows Installer"
    ];

    appVersion = LatestTag!;
}

else 
{
    archiveFilePath = Input.AskForInput("Enter the path to the standalone binary: ");
    workingDir = Path.GetDirectoryName(archiveFilePath);
    packagingOptions = [
        "Alt Linux Package (.rpm)",
        "Arch Package (.pkg.tar.xz)",
        "Gentoo Package (.tbz2)",
        "PCLinuxOS Package (.rpm)",
        "Windows Installer"
    ];

    appVersion = CurrentVersion;
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

(var result, var binaryPath) = await packager.HandlePackaging(desiredBuildProcess, workingDir!, appVersion);

Console.WriteLine("Compilation Complete: {0}", result);
Console.WriteLine("Path: {0}", binaryPath);

// Emitted for every release, not only for the Linux package builds, because the map describes the
// release rather than any single artifact in it. The Linux installer fetches this from the same
// release tag as the package it installs, so a script can never pair itself with a stale table.
if (result) {
    EmitPackageFormatMap(appVersion, workingDir!);
}

/// <summary>
/// Writes package-formats.json next to the built artifacts so it can be attached to the release.
/// </summary>
/// <remarks>
/// Kept in the Publisher rather than in a build script because this is where the release tag is
/// known: the file records the tag it was generated for, and the installer fetches it under that
/// same tag.
/// </remarks>
static void EmitPackageFormatMap(string releaseTag, string workingDir)
{
    try {
        var map = DistroPackageMap.Build();

        var targetDirectory = File.Exists(workingDir)
            ? Path.GetDirectoryName(workingDir)
            : workingDir;

        targetDirectory ??= Directory.GetCurrentDirectory();

        var mapPath = Path.Combine(targetDirectory, DistroPackageMap.FileName);

        File.WriteAllText(mapPath, DistroPackageMap.Serialize(releaseTag));

        Console.WriteLine($"Wrote the package format map for {map.Count} distributions to: {mapPath}");
        Console.WriteLine($"Attach '{DistroPackageMap.FileName}' to the release for {releaseTag} alongside the packages.");
    }
    catch (Exception ex) {
        // Non fatal. A release without the map still works: the installer falls back to its
        // built-in distribution list. Failing the build here would block a release that users can
        // install today.
        Warning.Write($"Unable to write the package format map: {ex.Message}");
        Warning.Write("The Linux installer will fall back to its built-in distribution list.");
    }
}
