using Publisher;
using static Publisher.PlatformSelection;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.ProgramFunctions;
using BrowserAutomationMaster.Messaging;
using System.Runtime.InteropServices;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);


string[] packagingOptions = [
    "Debian Package (.deb)",
    "Fedora Package (.rpm)",
    "Arch Package (.pkg.tar.xz)",
    "Gentoo Package (.tbz2)",
    "Standalone Binary"
];

string desiredBuildProcess = Input.WriteListFromOptions(packagingOptions, "build process");

string[] availableOSNames = [.. GetAvailableOSNames() ];
string selectedOS = Input.WriteListFromOptions(availableOSNames, "operating system");

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
await packager.BuildStandaloneBinary();