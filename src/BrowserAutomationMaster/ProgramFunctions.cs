using System.Text.Json;
using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.OS;
using BrowserAutomationMaster.Managers.OS.Unix.Linux;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Managers.Python.BrowserStack;
using BrowserAutomationMaster.Managers.SystemInfo;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Managers.Common.ProcessManager;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.Common.RegexManager;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.EmbeddedResourceManager;
using static BrowserAutomationMaster.Managers.LocalServerManager;
using static BrowserAutomationMaster.Managers.OS.Generic.InstalledApps;
using static BrowserAutomationMaster.Managers.OS.Unix.Linux.Functions;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Messaging.Success;
using static BrowserAutomationMaster.Parsing.Parser;


namespace BrowserAutomationMaster
{
    public static class ByteArrayExtensions
    {
        public static async Task<T?> Deserialize<T>(this byte[] data) where T : class
        {
            using var stream = new MemoryStream(data);
            return await JsonSerializer.DeserializeAsync(stream, typeof(T)) as T;
        }
    }
    
    public class ProgramFunctions
    {
        /// <summary>Handles all of the initial application setup and prerequisite checks.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        public static async Task InitializeAsync(string[] args)
        {
            // Sets PlatformManager.PlatformName to be used across the session duration.
            SetPlatform();

            // BUG FIXED: DO NOT CHANGE POSITION
            // If GlobalConfig is loaded after PopulateInstallations(), DefaultTheme's colors are used to display installation information.
            GlobalConfig = LoadConfig();

            // Populates AppManager.InstalledApps.AppInfo
            await PopulateInstallations();

            CheckForMultipleInstances();


            #pragma warning disable CA1416 // Handled by SetPlatforms()

            if (Platforms.IsWindows) {
                Win.VerifyRootDrive();
            }

            #pragma warning restore

            // The user will select the version of python they want to use
            HandlePythonVersionSelection(GetInstallations());
            
            await HandleHardwareCheck(args);
        }

        /// <summary>Processes any CLI arguments and returns execution status.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        /// <returns>True if BAMM is to be terminated | False if execution is to continue.</returns>
        public static async Task<bool> HandleCLIArguments(string[] pArgs)
        {
            if (pArgs.Length == 0) {
                return false; // No args, proceed to main menu loop.
            }

            // Defining the lowercase representation of pArgs[0] to save memory (Not that its required, but its a good practice)
            var lArg0 = pArgs[0].ToLower();
            
            // These args will be passed to an instance of UserScriptManager. (These come from the main menu)
            var scriptArgs = new string[] { "add", "compile", "run", };
            
            // Flag to ensure all arguments handled by UserScriptManager are processed. 
            var usingUSM = scriptArgs.Contains(lArg0);

            if (usingUSM)
            {
                _ = new UserScriptManager(pArgs[1], pArgs[0]);
                return true;
            }

            // Note: no-hwc is handled in HandleHardwareCheck()
            // Handles double-clicking a BAMC file (On Windows)
            if (pArgs.Length == 1 && lArg0.EndsWith(".bamc") && File.Exists(pArgs[0]))
            {
                _ = new UserScriptManager(pArgs[0], "add");
                var response = Input.AskForInput("Would you like to continue? [y/n]: ");
                var wantsToContinue = Input.ConditionAccepted(response); // OIC = StringComparison.OrdinalIgnoreCase
                return !wantsToContinue; // Exit if user doesn't want to continue
            }

            if (pArgs.Any(arg => arg.Equals("--platform-debug")))
            {
                Warning.Write(string.Join(NLC, [
                    "---------------- PLATFORM CLASS DEBUG INFO ----------------",
                    $"IsARMel: {Platforms.IsARMel}",
                    $"IsARMhf: {Platforms.IsARMhf}",
                    $"IsChromeOS: {Platforms.IsChromeOS}",
                    $"IsLinux: {Platforms.IsLinux}",
                    $"IsMacOS: {Platforms.IsMacOS}",
                    $"IsRaspi: {Platforms.IsRaspi}",
                    $"Raspi Model: {Platforms.GetRaspiModelName()}",
                    $"IsUnixLike: {Platforms.IsUnixLike}",
                    $"IsWindows: {Platforms.IsWindows}",
                    NLC, 
                    NLC,
                ]));
            }

            if (Platforms.IsUnixLike && pArgs.Any(arg => arg.Equals("--query-display"))){
                Console.WriteLine("====================================");
                Console.WriteLine("$DISPLAY Set: {0}", HasDisplayVarSet());
                Console.WriteLine("===================================={0}{1}", NLC, NLC);
            }

            // Handles --bs command (does nothing if on chromeOS)
            if (pArgs[0].Equals("--bs", CCIC))
            {
                SetBrowserStackStatus(status: true);
                WriteSuccessMessage("Argument `--bs` found, runtime execution will be done through BrowserStack.");
                return false;
            }

            // Handles `--editbsconf` command
            if (pArgs[0].Equals("--editbsconf"))
            {
                HandleBSOverwriteCommand();
                return true;
            }

            if (pArgs.Any(arg => arg.Equals("--force-error"))) {
                WriteAndExit("", 0);
            }

            if (pArgs.Any(arg => arg.Equals("--show-distro"))) 
            {
                var distro = Platforms.CurrentDistribution ?? Distros.Unknown;
                WriteSuccessMessage(distro.ToString());
            }
            
            if (pArgs.Any(arg => arg.Equals("--gui") && !Directory.Exists(userScriptsDirectory)))
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        "Unable to start BAMM's GUI.",
                        "Please start BAMM without any arguments for your first run, unless instructed otherwise.",
                        $"Once you see the Main Menu, select \"GUI\".",
                        NLC,
                        "Please note, you are seeing this because either:",
                        "- 1. You are running BAMM for the first time.",
                        "- 2. The userScripts directory has not been created, or has been corrupted.",
                        "After this, you can run BAMM as normal."
                    ]),
                    status: 1
                );
            }

            // If no display is set and the user attempts to user the GUI, browserstack will be set.
            if (pArgs.Any(arg => arg.Equals("--gui")) && !HasDisplayVarSet())
            {
                Warning.Write($"Unable to query $DISPLAY, BAMM's GUI will not work.");
                SetBrowserStackStatus(true);
            }

            // Writes the GUI to disk if not already present.
            else if (
                pArgs[0].Equals("--gui") && 
                !Directory.Exists(GetGUIDirectoryPath()) || 
                !File.Exists(GetGUIDaemonPath())
            ) {
                await HandleGUIDownload();
            }

            // Handles '--gui' command using default port (8008)
            if (pArgs.Length == 1 && pArgs[0].Equals("--gui"))
            {
                await StartServer();
            }

            // Handles '--gui --port==X' command where X is a valid integer between 1 and 65535
            else if (pArgs.Length == 2 && pArgs[0].Equals("--gui") && IsMatches(GUIPortRegex(), pArgs[1], out string port))
            {
                await StartServer(port);
            }

            else if (pArgs.Any(arg => arg.Equals("--version"))) 
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

            // Handles 'backup' command
            if (pArgs[0].Equals("backup", CCIC))
            {
                // ADD A CHECK HERE REGARDING THE USERS ARCHIVING CHOICE (ZIP, GZIP, TAR.GZ, etc)
                HandleBackupCommand(pArgs);
                return true;
            }

            // Handles 'clear' command variations
            if (pArgs[0].Equals("clear", CCIC)) // CCIC = StringComparison.CurrentCultureIgnoreCase
            {
                HandleClearCommand(pArgs);
                return true;
            }

            // Handles invalid case of `bamm delete`
            if (pArgs.Length == 1 && pArgs[0].Equals("delete", CCIC))
            {
                WriteAndExit("Invalid delete command format please specify the path to the file you wish to delete.", 1);
            }

            // Handles `bamm delete path/to/file.bamc`
            else if (pArgs[0].Equals("delete", CCIC))
            {
                DeleteFile(pArgs[1]);
                return true;
            }

            // Handles 'help' command variations
            else if (pArgs[0].Equals("help", CCIC))
            {
                HandleHelpCommand(pArgs);
                return true;
            }

            // Handles `new` and `-n` commands.
            if (pArgs[0].Equals("new", CCIC) || pArgs[0].Equals("-n")) 
            {
                if (pArgs.Length == 1) 
                {
                    await MenuLoopFunctions.New();
                    return true;
                }

                else if (pArgs.Length == 2) 
                {
                    var filePath = pArgs[1].Replace("'", "").Replace("\"",""); // Sanitizes path argument
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
            }

            // Handles `open` and `-o` commands.
            if (pArgs[0].Equals("open", CCIC) || pArgs[0].Equals("-o")) 
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
            }

            // Handles `restore` command variations
            if (pArgs.Length == 1 && pArgs[0].Equals("restore")) {
                RestoreFromBackup();
                return true;
            }

            // Handles 'run' command variations
            if (pArgs[0].Equals("run", CCIC))
            {
                return await HandleRunCommand(pArgs);
            }

            // Handles 'uninstall' command
            if (pArgs[0].Equals("uninstall", CCIC))
            {
                // This will exit regardless of success status so no return is neccessary.
                await UninstallationManager.Uninstall();
            }

            // Handles 'validate' command variations
            if (pArgs[0].Equals("validate", CCIC))
            {
                if (pArgs.Length != 2)
                {
                    WriteAndExit("Invalid 'validate' command.\n\nValid Syntax:\nbamm validate \"path/to/file.bamc\"", 1);
                }

                if (IsValidFile(pArgs[1]))
                {
                    WriteSuccessMessageAndExit("Selected file has valid syntax.", 0);
                }

                else
                {
                    WriteAndExit("Selected file has invalid syntax.", 1);
                }
                return true;
            }

            return false;
        }

        /// <summary></summary>
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

            if (pArgs.Length == 1)
            {
                ArchiveAppDataDirectory();
            }

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


        ///<summary>Handles 'bamm --editbsconf'</summary>
        ///<param name="pArgs">Program Arguments</param>
        private static void HandleBSOverwriteCommand()
        {
            if (InstanceManager.PromptConfigOverride()) {
                InstanceManager.WriteConfig(fileNotFound: false);
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
                "compiled" => GetDesiredSaveDirectory(),
                "config" => GetBAMConfigDirectory(),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(dirPath))
            {
                Write("Invalid 'clear' target. Use 'userScripts', 'compiled', or 'config'.");
                ReadKey();
                return;
            }

            string input = Input.AskForInput($"Are you sure you want to delete the '{targetDir}' directory? [y/n]:\n");
            if (input.Equals("y", OIC))
            {
                DeleteDirectory(dirPath);
            }
        }


        public static async Task<bool> HandleGUIDownload()
        {
            try
            {
                var daemonPath = GetGUIDaemonPath();
                var guiDir = GetGUIDirectoryPath();

                bool daemonOnDisk = File.Exists(daemonPath);
                bool guiDirOnDisk = Directory.Exists(guiDir);

                // If the Daemon and GUI are already downloaded, continue
                if (daemonOnDisk && guiDirOnDisk) {
                    return true;
                }

                // Retrieves gui.zip and UIDaemon.py from the embedded project resources
                // WriteEmbeddedResourceToDisk will exit if the operation fails
                if (!daemonOnDisk) {
                    await WriteEmbeddedResourceToDisk(
                        resourceName: "UIDaemon.py",
                        resourcePattern: "BrowserAutomationMaster.Resources.UIDaemon.py",
                        outputPath: daemonPath
                    );
                }

                if (!guiDirOnDisk) 
                {
                    await WriteEmbeddedResourceToDisk(
                        resourceName: "gui.zip",
                        resourcePattern: "BrowserAutomationMaster.Resources.gui.zip",
                        outputPath: GetGUIZipPath()
                    );

                    await Task.Delay(300);

                    WriteSuccessMessage(
                        string.Join(NLC, [
                            "Successfully downloaded gui.zip from BAMM's gui branch.",
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

        private static async Task HandleHardwareCheck(string[] pArgs)
        {
            // Skip compatibility checks if the user is not attempting to compile or run scripts.
            string[] nonUserScriptArgs = ["backup", "clear", "help", "restore", "uninstall", "validate"];

            string[] bypassCLIArgs = ["--bs", "--nohwc", "--editbsconf", "--version"];

            bool bypassCheck1 = pArgs.Any(arg => nonUserScriptArgs.Contains(arg));
            bool bypassCheck2 = pArgs.Any(arg => bypassCLIArgs.Contains(arg));

            bool doHardwareCheck = !bypassCheck1 && !bypassCheck2;

            if (GlobalConfig.ShowUpdateCheck) {
                await CheckForUpdate();
            }

            if (doHardwareCheck) {
                await Runtime.DoRuntimeCheck();
                return;
            }

            // Fixed bug where passing --nohwc will cause the CPU core count to be skipped all together.
            // To ensure this is working as intended:
            // dotnet run --nohwc --force-error
            else 
            {
                CPUInfoManager cpuInfoManager = new();
                Runtime.SetCoreCount(cpuInfoManager.Cores);
            }

            await Runtime.SetMemoryInfo();
            
        }


        /// <summary> Handles variations of 'bamm help' </summary>
        /// <param name="pArgs">Program Arguments (args)</param>
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
            else if (pArgs.Length == 2)
            {
                Help.ShowCommandDetails(pArgs[1]);
            }
        }

        /// <summary> </summary>
        /// <param name=""><param>
        /// <returns></returns>
        // private static async Task<bool> HandleRestoreCommand(string[] pArgs)
        // {
        //     var backupFile = GetDefaultBackupPath();

        // }
        
        /// <summary> Handle variations of 'bamm run' </summary>
        /// <param name="pArgs"></param>
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

        public static async Task RunMenuLoop(string[] args)
        {
            bool isRunning = true;
            while (isRunning)
            {
                KeyValuePair<MenuOption, string> MenuResult = await New();
                switch (MenuResult.Key)
                {
                    case MenuOption.Add:
                        await MenuLoopFunctions.Add(MenuResult, args);
                        break;

                    case MenuOption.Compile:
                        await Transpiler.New(MenuResult.Value, args);
                        break;

                    case MenuOption.GUI:
                        await StartServer();
                        break;

                    case MenuOption.Help:
                        break; // Help menu handles its own loop

                    case MenuOption.Invalid:
                        isRunning = false;
                        break;
                    
                    case MenuOption.New:
                        await MenuLoopFunctions.New();
                        break;

                    case MenuOption.Open:
                        await MenuLoopFunctions.Open();
                        break;
                        
                    case MenuOption.Run:
                        await MenuLoopFunctions.Run(MenuResult);
                        break;

                }

                if (isRunning)
                {
                    string input = Input.AskForInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:");
                    if (Input.ConditionAccepted(input))
                    {
                        isRunning = false;
                    }
                }
            }
        }

        /// <summary>Displays the final exit message and waits for user input.</summary>
        public static void Terminate(string? selectedBrowser)
        {
            PreventMemoryLeaks(selectedBrowser);
            Thread.Sleep(300); // Timeout to prevent unexpected behavior
            WriteMessage("\nPress any key to exit...", isSuccess: true);
            ReadKey();
            Environment.Exit(0);
        }
    }

    public static class MenuLoopFunctions
    {
        public static async Task Add(KeyValuePair<MenuOption, string> MenuResult, string[] args) 
        {
            string response = Input.AskForInput("Would you like to compile the newly added file? [y/n]:");
            if (Input.ConditionAccepted(response))
            {
                await Transpiler.New(MenuResult.Value, args);
            }
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

            await EditorManager.OpenFileInEditor(filePath);
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
            await EditorManager.OpenFileInEditor(filePath);
            
        }

        public static async Task Run(KeyValuePair<MenuOption, string> MenuResult)
        {
            Runtime runtimeManager = new(MenuResult.Value);
            // CheckBrowserStackStatus();
            await runtimeManager.RunScript(GetBrowserStackStatus());
        }
    }
}
