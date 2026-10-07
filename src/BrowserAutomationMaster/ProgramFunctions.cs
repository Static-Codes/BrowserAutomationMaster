using BrowserAutomationMaster.Core.Compilation;
using BrowserAutomationMaster.Core.Messaging;
using BrowserAutomationMaster.Core.SystemInfo.OS;
using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using BrowserAutomationMaster.Core.Python;
using BrowserAutomationMaster.Core.Utilities;
using BrowserAutomationMaster.Core.Types.Linux;
using static BrowserAutomationMaster.Core.Common.ANSI;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Common.ProcessManager;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Compilation.Transpiler;
using static BrowserAutomationMaster.Core.GUI.Server;
using static BrowserAutomationMaster.Core.GUI.BackendFunctions;
using static BrowserAutomationMaster.Core.Helpers.EmbeddedResourceHelper;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Menu;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Generic.InstalledApps;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Core.Parsing.Parser;
using static BrowserAutomationMaster.Core.Utilities.AppSettingsUtility;
using static BrowserAutomationMaster.Core.Utilities.AppUpdateUtility;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;
using static BrowserAutomationMaster.Resources.NativeFileDialog.Loader;

namespace BrowserAutomationMaster;
public class ProgramFunctions
{
    /// <summary>Executes the specified asynchronous task. </summary>
    /// <param name="action">The asynchronous task (function) to invoke.</param>
    /// <returns> The status of the operation, true if the task completed, otherwise false. </returns>
    private static async Task<bool> ExecuteTaskAndReturnTrue(Task task)
    {
        await task;
        return true;
    }


    /// <summary>Handles all of the initial application setup and prerequisite checks.</summary>
    /// <param name="pArgs">Program Arguments (args)</param>
    public static async Task InitializeAsync(string[] args)
    {
        // Sets PlatformManager.PlatformName to be used across the session duration.
        SetPlatform(GlobalUserInfo);

        GlobalUserInfo.HardwareInformation.SetCpuInfo();

        // BUG FIXED: DO NOT CHANGE POSITION
        // If GlobalSettings is loaded after PopulateInstallations(), DefaultTheme's colors are used to display installation information.
        GlobalSettings = Load();

        // Populates AppManager.InstalledApps.AppInfo
        await PopulateInstallations();

        // This is handled here rather than in HandleCLIArguments because the Entry Point calls InitializeAsync() first.
        // The argument may appear in any position.
        bool allowMultipleInstances = args.Any(arg => arg.Equals("--allow-multiple-instances", OIC));

        if (allowMultipleInstances) { 
            WriteSuccessMessage("`--allow-multiple-instances` passed, skipping checks for multiple instances.");
        }

        CheckForMultipleInstances(allowMultipleInstances);

        #pragma warning disable CA1416 // Handled by SetPlatforms()

        if (GlobalUserInfo.PlatformInfo.IsWindows) {
            Win.VerifyRootDrive();
        }

        #pragma warning restore

        // The user will select the version of python they want to use
        HandlePythonVersionSelection(GetInstallations());
        
        await HandleHardwareCheck(args);

        await InitializeNativeFileDialog();
    }


    /// <summary>Handles 'bamm backup'</summary>
    /// <param name="pArgs"></param>
    private static void HandleBackupCommand(string[] pArgs)
    {
        if (pArgs.Length > 2)
        {
            Write(
                string.Join(NLC, [
                    "Invalid 'backup' command.",
                    NLC,
                    "Valid commands:",
                    "bamm backup # backups to the desktop or $HOME directory.",
                    "bamm backup path/to/desired/backupFile.zip # Creates a backup file at the specified location."
                ])
            );
            ReadKey();
            return;
        }

        if (pArgs.Length == 1) { ArchiveAppDataDirectory(); }

        if (pArgs.Length == 2)
        {
            WriteAndExit
            (
                message: string.Join(NLC, [
                    "Currently BAMM does not support custom paths for your backup.",
                    "Please remove the second argument to continue."
                ]), 
                status: 1
            );
            // ArchiveAppDataDirectory(pArgs[1]); // Re-add this later when restore functionality is improved

        }
    }


    /// <summary>Handles variations of 'bamm clear'</summary>
    /// <param name="pArgs">Program Arguments (args)</param>
    private static void HandleClearCommand(string[] pArgs)
    {
        if (pArgs.Length != 2)
        {
            Write
            (
                string.Join(NLC, [
                    "Invalid 'clear' command.",
                    NLC,
                    "Valid commands:",
                    "bamm clear userScripts",
                    "bamm clear compiled",
                    "bamm clear config",
                    NLC,
                    "Press any key to continue..."
                ])
            );
            ReadKey();
            return;
        }

        string targetDir = pArgs[1].ToLower();
        string dirPath = targetDir switch
        {
            "userscripts" => userScriptsDirectory,
            "compiled" => GetCompiledScriptsDirectory(),
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(dirPath))
        {
            Write("Invalid 'clear' target. Use 'userScripts', 'compiled', or 'config'.");
            ReadKey();
            return;
        }

        string input = Input.AskForInput($"Are you sure you want to delete the '{targetDir}' directory? [y/n]:\n");
        
        if (input.Equals("y", OIC)) { DeleteDirectory(dirPath); }
    }

    /// <summary>Processes any CLI arguments and returns execution status.</summary>
    /// <param name="pArgs">Program Arguments (args)</param>
    /// <returns>True if BAMM is to be terminated | False if execution is to continue.</returns>
    public static async Task<bool> HandleCLIArguments(string[] pArgs)
    {
        if (pArgs.Length == 0) { return false; }

        var lArg0 = pArgs[0].ToLower();

        if (TryHandleUserScriptUtility(pArgs, lArg0, out bool scriptResult)) {
            return scriptResult;
        }

        if (TryHandleBamcFileAssociation(pArgs, lArg0, out bool fileResult)) {
            return fileResult;
        }

        ProcessDiagnosticFlags(pArgs);
        await HandleGuiFlagsAsync(pArgs);

        if (pArgs.Any(arg => arg.Equals("--version"))) { HandleVersionFlag(); }

        return await HandleSubcommandsAsync(pArgs);
    }

    private static async Task HandleGuiFlagsAsync(string[] pArgs)
    {
        if (pArgs.Any(arg => arg.Equals("--gui") && !Directory.Exists(userScriptsDirectory)))
        {
            WriteAndExit
            (
                string.Join(NLC, [
                    "Unable to start BAMM's GUI.",
                    "Please start BAMM without any arguments for your first run, unless instructed otherwise.",
                    "Once you see the Main Menu, select \"GUI\".",
                    NLC,
                    "Please note, you are seeing this because either:",
                    "- 1. You are running BAMM for the first time.",
                    "- 2. The userScripts directory has not been created, or has been corrupted.",
                    "After this, you can run BAMM as normal."
                ]),
                status: 1
            );
        }

        if (pArgs.Any(arg => arg.Equals("--gui")) && !HasDisplayVariableSet()) {
            Warning.Write("Unable to query $DISPLAY, BAMM's GUI will not work, please install X11 or Wayland.");
        }

        else if (
            pArgs[0].Equals("--gui") && 
            (!Directory.Exists(GetGUIDirectoryPath()) || !File.Exists(GetGUIDaemonPath()))
        ){
            await HandleGUIExtract();
        }

        if (pArgs.Length == 1 && pArgs[0].Equals("--gui")) {
            Console.WriteLine("Starting HTTP Server for GUI..");
            StartGUIThread();
        }
        
        else if (pArgs.Length == 2 && pArgs[0].Equals("--gui") && IsMatches(GUIPortRegex(), pArgs[1], out string port))
        {
            if (!int.TryParse(port, out int parsedPort) || parsedPort is < 1 or > 65535)
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to start BAMM's GUI.",
                        $"'{port}' is not a valid port.",
                        "Please provide a port between 1 and 65535, for example: bamm --gui --port==42069"
                    ]),
                    status: 1
                );
            }

            Console.WriteLine($"Starting HTTP Server for GUI on port {parsedPort}..");
            StartGUIThread(parsedPort.ToString());
        }
    }

    private static async Task<bool> HandleNewCommandAsync(string[] pArgs)
    {
        if (pArgs.Length == 1)
        {
            await MenuLoopFunctions.New();
            return true;
        }

        if (pArgs.Length == 2)
        {
            var filePath = pArgs[1].Replace("'", "").Replace("\"", "");
            await MenuLoopFunctions.New(filePath);
            return true;
        }

        WriteAndExit
        (
            message: string.Join(NLC, [
                "Invalid open command.",
                "Please use the following commands, where <filename> will be created in the userScripts directory.",
                "bamm new '<filename>'",
                "bamm -n '<filename>'",
            ]),
            status: 1
        );
        return true;
    }

    private static async Task<bool> HandleOpenCommandAsync(string[] pArgs)
    {
        if (pArgs.Length == 2)
        {
            await MenuLoopFunctions.Open();
            return true;
        }

        WriteAndExit
        (
            message: string.Join(NLC, [
                "Invalid open command.",
                "Please use the following commands, where <filename> is a file in the userScripts directory.",
                "bamm open '<filename>'",
                "bamm -o '<filename>'",
            ]),
            status: 1
        );
        return true;
    }

    private static async Task<bool> HandleSubcommandsAsync(string[] pArgs)
    {
        if (pArgs[0].Equals("backup", CCIC))
        {
            HandleBackupCommand(pArgs);
            return true;
        }

        if (pArgs[0].Equals("clear", CCIC))
        {
            HandleClearCommand(pArgs);
            return true;
        }

        if (pArgs[0].Equals("delete", CCIC))
        {
            if (pArgs.Length == 1)
            {
                WriteAndExit("Invalid delete command format please specify the path to the file you wish to delete.", 1);
            }

            DeleteFile(pArgs[1]);
            return true;
        }

        if (pArgs[0].Equals("help", CCIC))
        {
            HandleHelpCommand(pArgs);
            return true;
        }

        if (pArgs[0].Equals("new", CCIC) || pArgs[0].Equals("-n"))
        {
            return await HandleNewCommandAsync(pArgs);
        }

        if (pArgs[0].Equals("open", CCIC) || pArgs[0].Equals("-o"))
        {
            return await HandleOpenCommandAsync(pArgs);
        }

        if (pArgs.Length == 1 && pArgs[0].Equals("restore"))
        {
            RestoreFromBackup();
            return true;
        }

        if (pArgs[0].Equals("run", CCIC))
        {
            return await HandleRunCommand(pArgs);
        }

        if (pArgs[0].Equals("uninstall", CCIC))
        {
            await AppRemovalUtility.Uninstall();
        }

        if (pArgs[0].Equals("validate", CCIC))
        {
            return HandleValidateCommand(pArgs);
        }

        return false;
    }

    private static bool HandleValidateCommand(string[] pArgs)
    {
        if (pArgs.Length != 2)
        {
            WriteAndExit("Invalid 'validate' command.\n\nValid Syntax:\nbamm validate \"path/to/file.bamc\"", status: 1);
        }

        if (IsValidFile(pArgs[1]))
        {
            WriteSuccessMessageAndExit("Selected file has valid syntax.", exitCode: 0);
        }
        else
        {
            WriteAndExit("Selected file has invalid syntax.", status: 1);
        }

        return true;
    }

    private static void HandleVersionFlag()
    {
        Warning.Write
        (
            string.Join(NLC, [
                $"Version: {CurrentVersion}",
                $"Is Latest: {CurrentVersion == LatestVersion}"
            ])
        );
        Environment.Exit(0);
    }

    /// <summary> Handles the embedded GUI extraction workflow. </summary>
    /// <returns> Returns the status of the operation, true if it succeeds, otherwise false. </returns>
    public static async Task<bool> HandleGUIExtract()
    {
        try
        {
            var daemonPath = GetGUIDaemonPath();
            var guiDir = GetGUIDirectoryPath();

            bool daemonOnDisk = File.Exists(daemonPath);
            bool guiDirOnDisk = Directory.Exists(guiDir);

            // If the Daemon and GUI are already downloaded, continue
            if (daemonOnDisk && guiDirOnDisk) { return true; }

            // Retrieves gui.zip and UIDaemon.py from the embedded project resources
            // WriteEmbeddedResourceToDisk will exit if the operation fails
            if (!daemonOnDisk) {
                await WriteEmbeddedResourceToDisk(
                    resourceName: "UIDaemon.py",
                    resourcePattern: UI_DAEMON_RESOURCE_PATH,
                    outputPath: daemonPath
                );
            }

            if (!guiDirOnDisk) 
            {

                // This will overwrite any existing gui.zip archive in the root AppData directory.
                // As such, an additional File.Delete call is not required.
                await WriteEmbeddedResourceToDisk(
                    resourceName: "gui.zip",
                    resourcePattern: GUI_ZIP_RESOURCE_PATH,
                    outputPath: GetGUIZipPath()
                );

                await Task.Delay(300);

                WriteSuccessMessage(
                    string.Join(NLC, [
                        $"Successfully extracted the embedded gui.zip (GUI version {GetGuiVersion() ?? "unknown"}).",
                        "Please wait while it's extracted..."
                    ])
                );

                await Task.Delay(300);

                // Extracts the GUI or writes an error and exits.
                ExtractGUI();
            }

        }

        catch (Exception ex)
        {
            WriteAndExit
            (
                string.Join(NLC, [
                    "Unable to download the required GUI files.",
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1
            );
        }

        return true;
    }


    /// <summary>
    /// Handles the hardware checks ran at startup, unless one of the following commands / flags are passed: <br/>
    ///     - backup <br/>
    ///     - clear <br/>
    ///     - help <br/>
    ///     - restore <br/>
    ///     - uninstall <br/>
    ///     - validate <br/>
    ///     --nohwc <br/>
    ///     --version <br/>
    /// </summary>
    /// <param name="pArgs">The arguments passed to bamm via the CLI.</param>
    private static async Task HandleHardwareCheck(string[] pArgs)
    {
        // Skips compatibility checks if the user is not attempting to compile or run scripts.
        string[] nonUserScriptArgs = ["backup", "clear", "help", "restore", "uninstall", "validate"];

        // Bypasses hardware checks if the user is 
        string[] bypassCLIArgs = ["--nohwc", "--version"];

        bool bypassCheck1 = pArgs.Any(arg => nonUserScriptArgs.Contains(arg));
        bool bypassCheck2 = pArgs.Any(arg => bypassCLIArgs.Contains(arg));

        bool doHardwareCheck = !bypassCheck1 && !bypassCheck2;

        if (GlobalSettings.ShowUpdateCheck) { await CheckForUpdate(); }

        await Runtime.SetMemoryInfo();

        // Avoiding the hardware check at the user's request.
        if (!doHardwareCheck) {
            Warning.Write("Skipping runtime validation.. please note this may have unintended consequences.");
            return;
        }
        
        await Runtime.DoRuntimeCheck();
    }


    /// <summary> Handles variations of 'bamm help' </summary>
    /// <param name="args">The CLI arguments passed to BAMM.</param>
    private static void HandleHelpCommand(string[] pArgs)
    {
        if (pArgs.Length == 1)
        {
            Write(string.Join(
                string.Empty, [
                    "Invalid command: 'bamm help'\n\nTo see available entries for the 'help' command, ",
                    "run bamm without arguments then select the Help tab.\n\n"
                ])
            );
            ReadKey();
        }

        else if (pArgs.Length == 2) {
            Help.ShowCommandDetails(pArgs[1]);
        }
    }


    /// <summary> Handle variations of 'bamm run' </summary>
    /// <param name="args">The CLI arguments passed to BAMM.</param>
    /// <returns>A boolean result indicating a successful or failed execution.</returns>
    private static async Task<bool> HandleRunCommand(string[] pArgs)
    {
        var errorMessage =
            "Invalid 'run' command.\n" +
            "Please provide a valid path to a Python script.\n\n" +
            "Valid Syntax:\n" +
            "bamm run 'path/to/file.py'";

        if (pArgs.Length == 2 && File.Exists(pArgs[1]))
        {
            var runtimeManager = new Runtime(pArgs[1]);
            await runtimeManager.RunScript();
        }

        else 
        { 
            WriteAndExit(errorMessage, 1); 
        }

        return true;
    }


    /// <summary>
    /// Handles:<br/>
    /// - <c>bamm --platform-info</c> <br/>
    /// - <c>bamm --query-display</c> <br/>
    /// - <c>bamm --force-error</c> <br/>
    /// - <c>bamm --show-distro</c> <br/>
    /// </summary>
    /// <param name="args">The CLI arguments passed to BAMM.</param>
    private static void ProcessDiagnosticFlags(string[] args)
    {
        if (args.Any(arg => arg.Equals("--platform-info")))
        {
            var labelText = GlobalUserInfo.PlatformInfo.IsLinux ? "Detected Distro:" : "OS Name:";

            Warning.Write(string.Join(NLC, [
                "---------------- PLATFORM CLASS DEBUG INFO ----------------",
                $"IsLinux: {GlobalUserInfo.PlatformInfo.IsLinux}",
                $"IsMacOS: {GlobalUserInfo.PlatformInfo.IsMacOS}",
                $"IsPiDevice: {GlobalUserInfo.PlatformInfo.IsPiDevice}",
                $"Raspi Model: {GlobalUserInfo.PlatformInfo.GetRaspiModelName()}",
                $"IsUnixLike: {GlobalUserInfo.PlatformInfo.IsUnixLike}",
                $"IsWindows: {GlobalUserInfo.PlatformInfo.IsWindows}",
                $"{labelText}: {GlobalUserInfo.PlatformInfo.CurrentPlatform?.PrettyName ?? "Not Detected"}",
                NLC, 
                NLC,
            ]));
        }

        if (GlobalUserInfo.PlatformInfo.IsUnixLike && args.Any(arg => arg.Equals("--query-display")))
        {
            Console.WriteLine("====================================");
            Console.WriteLine($"$DISPLAY Set: {GlobalUserInfo.PlatformInfo.CurrentDistribution?.DisplayServer != DisplayServer.None}");
            Console.WriteLine($"Active Server: {GlobalUserInfo.PlatformInfo.CurrentDistribution?.DisplayServer.ToString() ?? "Not Set"}");
            Console.WriteLine("===================================={0}{1}", NLC, NLC);
        }

        if (args.Any(arg => arg.Equals("--force-error"))) { WriteAndExit("", 0); }

        if (args.Any(arg => arg.Equals("--show-distro")))
        {
            var distro = GlobalUserInfo.PlatformInfo.CurrentDistribution ?? Distros.Unknown;
            WriteSuccessMessage(distro.ToString());
        }
    }


    /// <summary> Runs the interactive menu loop. </summary>
    /// <param name="args">The CLI arguments passed to BAMM.</param>
    public static async Task RunMenuLoop(string[] args)
    {
        bool isRunning = true;
        while (isRunning)
        {
            KeyValuePair<MenuOption, string> MenuResult = await New();
            
            isRunning = MenuResult.Key switch
            {
                MenuOption.Add => await ExecuteTaskAndReturnTrue(MenuLoopFunctions.Add(MenuResult, args)),
                MenuOption.Compile => await ExecuteTaskAndReturnTrue(New(MenuResult.Value, args)),
                MenuOption.GUI => StartGUIThread(),
                MenuOption.Help => true,
                MenuOption.Invalid => false,
                MenuOption.New => await ExecuteTaskAndReturnTrue(MenuLoopFunctions.New()),
                MenuOption.Open => await ExecuteTaskAndReturnTrue(MenuLoopFunctions.Open()),
                MenuOption.Run => await ExecuteTaskAndReturnTrue(MenuLoopFunctions.Run(MenuResult)),
                _ => isRunning
            };

            if (isRunning)
            {
                string input = Input.AskForInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:");
                if (Input.ConditionAccepted(input)) { isRunning = false; }
            }
        }
    }

    

    /// <summary> Displays the final exit message and waits for user input. </summary>
    public static void Terminate(string? selectedBrowser)
    {
        PreventMemoryLeaks(selectedBrowser);
        Thread.Sleep(300); // Timeout to prevent unexpected behavior
        WriteMessage("\nPress any key to exit...", isSuccess: true);
        ReadKey();
        Environment.Exit(0);
    }

    /// <summary> Handles BAMM being invoked by double-clicking a .BAMC file. </summary>
    /// <param name="args">The arguments passed to BAMM via CLI.</param>
    /// <param name="firstArg">The first argument passed to BAMM via CLI.</param>
    /// <param name="shouldTerminate">If the application will exit due to a failed operation. </param>
    /// <returns>A boolean, true if the operation succeed, otherwise false.</returns>
    private static bool TryHandleBamcFileAssociation(string[] args, string firstArg, out bool shouldTerminate)
    {
        if (args.Length == 1 && firstArg.EndsWith(".bamc") && File.Exists(args[0]))
        {
            _ = new UserScriptUtility(filePath: args[0], method: "add");
            var response = Input.AskForInput("Would you like to continue? [y/n]: ");
            var wantsToContinue = Input.ConditionAccepted(response);
            shouldTerminate = !wantsToContinue;
            return true;
        }

        shouldTerminate = false;
        return false;
    }

    /// <summary> Handles `bamm add`, `bamm compile`, and `bamm run`.</summary>
    /// <param name="args">The arguments passed to BAMM via CLI.</param>
    /// <param name="firstArg">The first argument passed to BAMM via CLI.</param>
    /// <param name="shouldTerminate">If the application will exit due to a failed operation. </param>
    /// <returns>A boolean, true if the operation succeed, otherwise false.</returns>
    private static bool TryHandleUserScriptUtility(string[] args, string firstArg, out bool shouldTerminate)
    {
        var scriptArgs = new string[] { "add", "compile", "run" };
        if (scriptArgs.Contains(firstArg))
        {
            _ = new UserScriptUtility(args[1], args[0]);
            shouldTerminate = true;
            return true;
        }

        shouldTerminate = false;
        return false;
    }

}

public static class MenuLoopFunctions
{
    public static async Task Add(KeyValuePair<MenuOption, string> MenuResult, string[] args) 
    {
        string response = Input.AskForInput("Would you like to compile the newly added file? [y/n]:");
        if (Input.ConditionAccepted(response)) { await Transpiler.New(MenuResult.Value, args); }
    }

    public static async Task New(string? fullFileName = null)
    {
        // If no param is passed the user is prompted for the filename.
        fullFileName ??= Input.AskForInput("Please enter the name of the file you wish to create: ");
        
        while (string.IsNullOrEmpty(fullFileName) || !fullFileName.EndsWith(".bamc", OIC)) 
        {
            Warning.Write("Please enter a valid filename ending in .bamc");
            Console.WriteLine("Example: new_file.bamc");
            fullFileName = Input.AskForInput("Please enter the name of the file you wish to create: ");
        }

        // Sanitizes the filename pre-emptively incase a variation of ".bamc" is present (ex: ".BAMC" or ".BAMc")
        var fileName = Path.GetFileNameWithoutExtension(fullFileName);
        fullFileName = $"{fileName}.bamc";
        var filePath = Path.Combine(userScriptsDirectory, fullFileName);

        await EditorUtility.OpenFileInEditor(filePath);
    }

    public static async Task Open(string? fullFileName = null) 
    {
        fullFileName ??= Input.AskForInput("Please enter the name of the file you wish to open: ");

        while (string.IsNullOrEmpty(fullFileName) || !fullFileName.EndsWith(".bamc", OIC)) 
        {
            Warning.Write("Please enter a valid filename ending in .bamc");
            Console.WriteLine("Example: filename.bamc");
            fullFileName = Input.AskForInput("Please enter the name of the file you wish to open: ");
        }

        // Sanitizes the filename pre-emptively incase a variation of ".bamc" is present (ex: ".BAMC" or ".BAMc")
        var fileName = Path.GetFileNameWithoutExtension(fullFileName);
        fullFileName = $"{fileName}.bamc";

        var filePath = Path.Combine(userScriptsDirectory, fullFileName);

        // // If the file doesn't exist in the userScripts directory:
        // // 1. The file is created 
        // // 2. The user is prompted for their choice of editor to use when opening the selected file.
        // if (!File.Exists(filePath)) {
        //     await New(fullFileName);
        //     return;
        // } 
        
        // If the file does exist in the userScripts directory
        // 1. OpenFileInEditor() creates the file
        // 2. The user is prompted for their choice of editor to use when opening the selected file.
        await EditorUtility.OpenFileInEditor(filePath);
        
    }

    public static async Task Run(KeyValuePair<MenuOption, string> MenuResult)
    {
        Runtime runtimeManager = new(MenuResult.Value);
        // CheckBrowserStackStatus();
        await runtimeManager.RunScript();
    }
}
