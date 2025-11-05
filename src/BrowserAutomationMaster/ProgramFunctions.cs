using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Managers.Python.BrowserStack;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.AppManager.InstalledApps;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.LocalServerManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.ProcessManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.BrowserVersionManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Messaging.Success;
using static BrowserAutomationMaster.Parsing.Parser;


namespace BrowserAutomationMaster
{
    public class ProgramFunctions
    {
        /// <summary>Handles all of the initial application setup and prerequisite checks.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        public static async Task InitializeAsync(string[] args)
        {
            // Sets PlatformManager.PlatformName to be used across the session duration.
            SetPlatform();

            Console.WriteLine("IsARMel: {0}", Platforms.IsARMel);
            Console.WriteLine("IsARMhf: {0}", Platforms.IsARMhf);
            Console.WriteLine("IsChromeOS: {0}", Platforms.IsChromeOS);
            Console.WriteLine("IsLinux: {0}", Platforms.IsLinux);
            Console.WriteLine("IsOSX: {0}", Platforms.IsOSX);
            Console.WriteLine("IsRaspi: {0}", Platforms.IsRaspi);
            Console.WriteLine("Raspi Model: {0}", Platforms.RaspiModelName);
            Console.WriteLine("IsUnixLike: {0}", Platforms.IsUnixLike);
            Console.WriteLine("IsWindows: {0}", Platforms.IsWindows);

            // Downloads a local copy of:
            // https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/main/src/BrowserAutomationMaster/AppData/packages.json
            await PackageManager.Initalize();

            // BUG FIXXED: DO NOT CHANGE POSITION
            // If GlobalConfig is loaded after PopulateInstallations(), DefaultTheme's colors are used to display installation information.
            GlobalConfig = LoadConfig();

            // Populates AppManager.InstalledApps.AppInfo
            await PopulateInstallations();

            // Populate DeviceManager.Devices
            if (!await PopulateDevices())
                Environment.Exit(0);

            // Populates BrowserVersionManager.browserVersions
            SetBrowserVersions(await GetLatestVersionInfo());
            var versions = GetBrowserVersion();

            // Null check on BrowserVersionManager.browserVersions
            if (versions == null)
                Warning.Write(
                    "Unable to get most browser versions, please ensure you have an active internet connection.\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n"
                );

            CheckForMultipleInstances();

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
                Win.VerifyRootDrive(args);

            // The user will select the version of python they want to use
            HandlePythonVersionSelection(GetInstallations());
            
            await HandleHardwareCheck(args);
        }

        /// <summary>Processes any CLI arguments and returns execution status.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        /// <returns>True if BAMM is to be terminated | False if execution is to continue.</returns>
        public static async Task<bool> HandleCLIArguments(string[] pArgs)
        {
            if (pArgs.Length == 0) 
                return false; // No args, proceed to main menu loop.

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

            // Handles --bs command (does nothing if on chromeOS)
            if (pArgs[0].Equals("--bs", CCIC))
            {
                SetBrowserStackStatus(status: true);
                return false;
            }

            // Handles `--editbsconf` command
            if (pArgs[0].Equals("--editbsconf"))
            {
                HandleBSOverwriteCommand();
                return true;
            }

            // Downloads a local copy of the GUI (If one is not already present) from:
            // https://raw.githubusercontent.com/Static-Codes/BrowserAutomationMaster/refs/heads/gui/gui.zip
            if (pArgs[0].Equals("--gui") && !Directory.Exists(GetGUIDirectoryPath()))
                await HandleGUIDownload();

            // Handles '--gui' command using default port (8008)
            if (pArgs.Length == 1 && pArgs[0].Equals("--gui"))
            {
                await StartServer();
                return true;
            }

            // Handles '--gui --port==X' command where X is a valid integer between 1 and 65535
            if (pArgs.Length == 2 && pArgs[0].Equals("--gui") && IsMatches(GUIPortRegex(), pArgs[1], out string port))
            {
                await StartServer(port);
                return true;
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

            if (pArgs[0].Equals("delete", CCIC))
            {
                if (pArgs.Length == 0)
                    WriteAndExit("Invalid delete command format please specify the path to the file you wish to delete.", 1);
                DeleteFile(pArgs[1]);
                return true;
            }

            // Handles 'help' command variations
            else if (pArgs[0].Equals("help", CCIC))
            {
                HandleHelpCommand(pArgs);
                return true;
            }

            // Handles 'run' command variations
            if (pArgs[0].Equals("run", CCIC))
                return await HandleRunCommand(pArgs);

            // Handles 'uninstall' command
            if (pArgs[0].Equals("uninstall", CCIC))
            {
                UninstallationManager.Uninstall();
                return true;
            }

            // Handles 'validate' command variations
            if (pArgs[0].Equals("validate", CCIC))
            {
                if (pArgs.Length != 2)
                    WriteAndExit("Invalid 'validate' command.\n\nValid Syntax:\nbamm validate \"path/to/file.bamc\"", 1);
                
                if (IsValidFile(pArgs[1]))
                    WriteSuccessMessageAndExit("Selected file has valid syntax.", 0);
                else
                    WriteAndExit("Selected file has invalid syntax.", 1);

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
                var message =
                    "Invalid 'backup' command.\n\n" +
                    "Valid commands:\n" +
                    "bamm backup # backups to the desktop or $HOME directory." +
                    "bamm backup path/to/desired/backupFile.zip # Creates a backup file at the specified location.";

                Write(message);
                ReadKey();
                return;
            }

            if (pArgs.Length == 1)
                ArchiveAppDataDirectory();
                
            if (pArgs.Length == 2)
                ArchiveAppDataDirectory(pArgs[1]);

        }


        ///<summary>Handles 'bamm --editbsconf'</summary>
        ///<param name="pArgs">Program Arguments</param>
        private static void HandleBSOverwriteCommand()
        {
            if (InstanceManager.PromptConfigOverride())
                InstanceManager.WriteConfig(fileNotFound: false);
        }


        /// <summary>Handles variations of 'bamm clear'</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleClearCommand(string[] pArgs)
        {
            if (pArgs.Length != 2)
            {
                Write(
                    "Invalid 'clear' command.\n\nValid commands:\nbamm clear userScripts\nbamm clear compiled\nbamm clear config\n\nPress any key to continue...");
                ReadKey();
                return;
            }

            string targetDir = pArgs[1].ToLower();
            string dirPath = targetDir switch
            {
                "userscripts" => GetUserScriptDirectory(),
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
                DeleteDirectory(dirPath);
        }

        private static async Task<bool> HandleDaemonDownload()
        {
            var msg = "Unable to download the GUI Daemon, any attempt to use the 'Restart GUI' button will throw an error.";
            try
            {
                var content = await RequestManager.NetworkClient.Instance.GetStringAsync(GUI_DAEMON_LINK);

                if (content == null)
                    return WriteErrorAndReturnBool(msg, false);

                var path = GetGUIDaemonPath();
                File.WriteAllText(path, content);
                return File.Exists(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return WriteErrorAndReturnBool(msg, false);
            }
        }

        private static async Task<bool> HandleGUIDownload()
        {
            try
            {
                bool daemonDownloaded = false; // Prevents the requirement for nesting

                if (File.Exists(GetGUIDaemonPath())) // If the Daemon is already downloaded, continue
                    daemonDownloaded = true;

                else // Downloads a local copy of the GUI from ConstantManager.GUI_DAEMON_LINK
                    daemonDownloaded = await HandleDaemonDownload();


                if (!daemonDownloaded) // If the daemonDownload flag isnt true, execution ends.
                    return false;

                if (!File.Exists(GetGUIDaemonPath())) // If the daemon wasn't downloaded, execution ends.
                    return false;

                WriteSuccessMessage("Successfully downloaded the GUI Daemon, downloading GUI now..");
                await Task.Delay(300);

                if (!await DownloadGUI())
                    return false;

                WriteSuccessMessage("Successfully downloaded gui.zip from project repository, please wait while it's extracted.");
                await Task.Delay(300);

                if (!ExtractGUI())
                    return false;

                WriteSuccessMessage("Successfully extracted GUI, please wait while the HTTP Server starts..");
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message:
                        string.Join(
                            string.Empty, [
                                "Unable to download the required GUI files, ",
                                "if this issue persists, ",
                                $"please make a bug report at {ISSUES_LINK}\n\n",
                                $"Error Log:\n{ex.Message}"
                            ]
                        ),
                    status: 1
                );
            }
            return true;
        }

        private static async Task HandleHardwareCheck(string[] pArgs)
        {
            // Skip compatibility checks if the user is not attempting to compile or run scripts.
            string[] nonUserScriptArgs = ["backup", "clear", "help", "uninstall", "validate"];

            string[] bypassCLIArgs = ["--bs", "--nohwc", "--editbsconf"];

            bool bypassCheck1 = pArgs.Any(arg => nonUserScriptArgs.Contains(arg));
            bool bypassCheck2 = pArgs.Any(arg => bypassCLIArgs.Contains(arg));

            bool doHardwareCheck = !bypassCheck1 && !bypassCheck2;

            if (GlobalConfig.ShowUpdateCheck)
                await CheckForUpdate();

            if (doHardwareCheck)
                RuntimeManager.DoRuntimeCheck();
        }
        /// <summary>Runs the main menu loop for BAMM.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>


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


        /// <summary> Handle variations of 'bamm run' </summary>
        /// <param name="pArgs"></param>
        /// <returns></returns>
        private static async Task<bool> HandleRunCommand(string[] pArgs)
        {
            var errorMessage =
                "Invalid 'run' command.\n" +
                "Please provide a valid path to a Python script.\n\n" +
                "Valid Syntax:\n" +
                "bamm run 'path/to/file.py'";

            if (pArgs.Length == 2 && File.Exists(pArgs[1]))
            {
                var runtimeManager = new RuntimeManager(pArgs[1]);
                await runtimeManager.RunScript();
            }

            else { WriteAndExit(errorMessage, 1); }

            return true;
        }

        public static async Task RunMenuLoop(string[] args)
        {
            bool isRunning = true;
            while (isRunning)
            {
                KeyValuePair<MenuOption, string> parserResult = Parser.New();
                switch (parserResult.Key)
                {
                    case MenuOption.Add:
                        string response = Input.AskForInput("Would you like to compile the newly added file? [y/n]:");
                        if (Input.ConditionAccepted(response))
                            await New(parserResult.Value, args);
                        break;

                    case MenuOption.Compile:
                        await New(parserResult.Value, args);
                        break;

                    case MenuOption.Run:
                        RuntimeManager runtimeManager = new(parserResult.Value);
                        await runtimeManager.RunScript();
                        break;

                    case MenuOption.Help:
                        break; // Help menu handles its own loop

                    case MenuOption.Invalid:
                        isRunning = false;
                        break;
                }

                if (isRunning)
                {
                    string input = Input.AskForInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:");
                    if (Input.ConditionAccepted(input))
                        isRunning = false;
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
}
