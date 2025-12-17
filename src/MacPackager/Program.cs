
using static MacPackager.ProgramFunctions;

Console.Title = $"BAMM for macOS Packager (BETA)";

Initialize(args);

bool shouldExit = HandleCLIArguments(args);

if (!shouldExit)
{
    RunMenuLoop(args);
}

// var bundleManager = new BundleManager();
// bundleManager.BuildBundle();
            
// Path to test:
// "/home/nerdy/repos/BrowserAutomationMaster/src/BrowserAutomationMaster/bin/Release/net8.0/osx-x64/publish/bamm"

    

