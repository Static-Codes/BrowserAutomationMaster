using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using static BrowserAutomationMaster.Managers.AppManager.InstalledApps;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.ProcessManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.BrowserVersionManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Menu;
using static BrowserAutomationMaster.Parsing.Parser;


namespace BrowserAutomationMaster
{
    public class ProgramFunctions
    {
        /// <summary>Handles all of the initial application setup and prerequisite checks.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        public static async Task InitializeAsync(string[] args)
        {
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
            {
                Warning.Write(
                    "Unable to get most browser versions, please ensure you have an active internet connection.\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n"
                );
            }

            Linux.ChromeOSCheck();
            CheckForMultipleInstances();

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
                Win.VerifyRootDrive(args);

            GlobalConfig = LoadConfig();

            // Skip compatibility checks if the user is not attempting to compile or run scripts.
            string[] nonUserScriptArgs = ["clear", "help", "uninstall", "validate"];
            string[] bypassCLIArgs = ["--vlinux-bypass", "--use-browserstack"];
            bool bypassCheck1 = args.Any(arg => nonUserScriptArgs.Contains(arg));
            bool bypassCheck2 = args.Any(arg => bypassCLIArgs.Contains(arg));
            bool doHardwareCheck = !bypassCheck1 && !bypassCheck2;

            if (doHardwareCheck)
            {
                RuntimeManager.DoRuntimeCheck();
                if (GlobalConfig.ShowUpdateCheck)
                    await CheckForUpdate();
            }
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

            // These args will bypass hardware checks.
            var noHWCheckArgs = new string[] {"backup", "clear", "help", "uninstall", "validate"};
            
            // These args will be passed to an instance of UserScriptManager.
            var scriptArgs = new string[] { "add", "backup", "compile", "delete", "run", "validate" };

            var noHardwareCheck = pArgs.Length == 2 && !noHWCheckArgs.Contains(lArg0);
            
            // Flag to ensure all arguments handled by UserScriptManager are processed. 
            var usingUSM = noHardwareCheck && scriptArgs.Contains(lArg0);

            if (usingUSM)
            {
                _ = new UserScriptManager(pArgs[1], pArgs[0]);
                return true;
            }

            // Handles double-clicking a BAMC file (On Windows)
            if (pArgs.Length == 1 && lArg0.EndsWith(".bamc") && File.Exists(pArgs[0]))
            {
                _ = new UserScriptManager(pArgs[0], "add");
                var response = Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ");
                var wantsToContinue = Input.ConditionAccepted(response); // OIC = StringComparison.OrdinalIgnoreCase
                return !wantsToContinue; // Exit if user doesn't want to continue
            }

            // Handles 'backup' command
            if (pArgs[0].Equals("backup", CCIC))
            {
                // ADD A CHECK HERE REGARDING THE USERS ARCHIVING CHOICE (ZIP, GZIP, TAR.GZ, etc)
                ArchiveAppDataDirectory();
            }

            // Handles 'clear' command variations
            if (pArgs[0].Equals("clear", CCIC)) // CCIC = StringComparison.CurrentCultureIgnoreCase
            {
                HandleClearCommand(pArgs);
                return true;
            }

            // Handles 'help' command variations
            if (pArgs[0].Equals("help", CCIC))
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
                    Errors.WriteErrorAndExit("Invalid 'validate' command.\n\nValid Syntax:\nbamm validate \"path/to/file.bamc\"", 1);
                
                if (IsValidFile(pArgs[1]))
                    Success.WriteSuccessMessageAndExit("Selected file has valid syntax.", 0);
                else
                    Errors.WriteErrorAndExit("Selected file has invalid syntax.", 1);

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

                Errors.Write(message);
                ReadKey();
                return;
            }

            if (pArgs.Length == 1)
                ArchiveAppDataDirectory();
                
            if (pArgs.Length == 2)
                ArchiveAppDataDirectory(pArgs[1]);

        }


        /// <summary>Handles variations of 'bamm clear'</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleClearCommand(string[] pArgs)
        {
            if (pArgs.Length != 2)
            {
                Errors.Write(
                    "Invalid 'clear' command.\n\nValid commands:\nbamm clear userScripts\nbamm clear compiled\nbamm clear config\n\nPress any key to continue...");
                ReadKey();
                return;
            }

            string targetDir = pArgs[1].ToLower();
            string dirPath = targetDir switch
            {
                "userscripts" => GetUserScriptDirectory(),
                "compiled" => GetDesiredSaveDirectory(),
                "config" => GetConfigDirectory(),
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(dirPath))
            {
                Errors.Write("Invalid 'clear' target. Use 'userScripts', 'compiled', or 'config'.");
                ReadKey();
                return;
            }

            string input = Input.WriteTextAndReturnRawInput($"Are you sure you want to delete the '{targetDir}' directory? [y/n]:\n");
            if (input.Equals("y", OIC))
                DeleteDirectory(dirPath);
        }


        /// <summary> Handles variations of 'bamm help' </summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleHelpCommand(string[] pArgs)
        {
            if (pArgs.Length == 1)
            {
                Errors.Write(
                    "Invalid command: 'bamm help'\n\nTo see available entries for the 'help' command, run bamm without arguments then select the Help tab.\n\n");
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

            else { Errors.WriteErrorAndExit(errorMessage, 1); }

            return true;
        }


        /// <summary>Runs the main menu loop for BAMM.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>

        public static async Task RunMenuLoop(string[] args)
        {
            bool isRunning = true;
            while (isRunning)
            {
                KeyValuePair<MenuOption, string> parserResult = Parser.New();
                switch (parserResult.Key)
                {
                    case MenuOption.Add:
                        string response = Input.WriteTextAndReturnRawInput("Would you like to compile the newly added file? [y/n]:");
                        if (Input.ConditionAccepted(response))
                            await Transpiler.New(parserResult.Value, args);
                        break;

                    case MenuOption.Compile:
                        await Transpiler.New(parserResult.Value, args);
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
                    string input = Input.WriteTextAndReturnRawInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:");
                    if (Input.ConditionAccepted(input))
                        isRunning = false;
                }
            }
        }

        /// <summary>Displays the final exit message and waits for user input.</summary>
        public static void ExitApplication()
        {
            WriteMessage("\nPress any key to exit...", isSuccess: true);
            ReadKey();
            Environment.Exit(0);
        }
    }
}
