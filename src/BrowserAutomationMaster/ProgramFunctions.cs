using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
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
                return false; // No args, proceed to interactive loop.

            string[] nonUserScriptArgs = ["clear", "help", "uninstall", "validate"];
            string[] validCLIArgs = ["add", "compile", "delete", "run", "validate"];

            bool isCLI = pArgs.Length == 2 && !nonUserScriptArgs.Contains(pArgs[0].ToLower());
            bool validCLIArg = isCLI && validCLIArgs.Contains(pArgs[0]);

            if (validCLIArg)
            {
                _ = new UserScriptManager(pArgs[1], pArgs[0]);
                return true;
            }

            // Handles double-clicking a BAMC file (On Windows)
            if (pArgs.Length == 1 && pArgs[0].ToLower().EndsWith(".bamc") && File.Exists(pArgs[0]))
            {
                _ = new UserScriptManager(pArgs[0], "add");
                string input = Input.WriteTextAndReturnRawInput("Would you like to continue? [y/n]: ");
                bool wantsToContinue = input.Trim().Equals("y", OIC); // OIC = StringComparison.OrdinalIgnoreCase
                return !wantsToContinue; // Exit if user doesn't want to continue
            }

            // Handles 'clear' command variations
            if (pArgs.Length > 0 && pArgs[0].Equals("clear", CCIC)) // CCIC = StringComparison.CurrentCultureIgnoreCase
            {
                HandleClearCommand(pArgs);
                return true;
            }

            // Handles 'help' command variations
            if (pArgs.Length > 0 && pArgs[0].Equals("help", CCIC))
            {
                HandleHelpCommand(pArgs);
                return true;
            }

            // Handles 'run' command variations
            if (pArgs.Length > 0 && pArgs[0].Equals("run", CCIC))
            {
                if (pArgs.Length == 2 && File.Exists(pArgs[1]))
                {
                    var runtimeManager = new RuntimeManager(pArgs[1]);
                    await runtimeManager.RunScript();
                }
                else
                {
                    Errors.WriteErrorAndExit(
                       message: "Invalid 'run' command.\nPlease provide a valid path to a Python script.\n\nValid Syntax:\nbamm run \"path/to/file.py\"",
                       status: 1);
                }
                return true; // Exit after handling
            }

            // Handles 'uninstall' command
            if (pArgs.Length == 1 && pArgs[0].Equals("uninstall", CCIC))
            {
                new UninstallationManager().Uninstall();
                return true; // Exit after handling
            }

            // Handles 'validate' command variations
            if (pArgs.Length > 0 && pArgs[0].Equals("validate", CCIC))
            {
                if (pArgs.Length == 2)
                {
                    if (IsValidFile(pArgs[1]))
                        Success.WriteSuccessMessageAndExit("Selected file has valid syntax.", 0);
                    else
                        Errors.WriteErrorAndExit("Selected file has invalid syntax.", 1);
                }
                else
                {
                    Errors.WriteErrorAndExit("Invalid 'validate' command.\n\nValid Syntax:\nbamm validate \"path/to/file.bamc\"", 1);
                }
                return true; // Exit after handling
            }

            return false; // No recognized CLI args, proceed to interactive mode
        }


        /// <summary>Handles variations of 'bamm clear'</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleClearCommand(string[] pArgs)
        {
            if (pArgs.Length != 2)
            {
                Errors.WriteErrorAndContinue(
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
                Errors.WriteErrorAndContinue("Invalid 'clear' target. Use 'userScripts', 'compiled', or 'config'.");
                ReadKey();
                return;
            }

            string input = Input.WriteTextAndReturnRawInput($"Are you sure you want to delete the '{targetDir}' directory? [y/n]:\n");
            if (input.Equals("y", OIC))
                DeleteDirectory(dirPath);
        }


        /// <summary>Handles variations of 'bamm help'</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleHelpCommand(string[] pArgs)
        {
            if (pArgs.Length == 1)
            {
                Errors.WriteErrorAndContinue(
                    "Invalid command: 'bamm help'\n\nTo see available entries for the 'help' command, run bamm without arguments then select the Help tab.\n\n");
                ReadKey();
            }
            else if (pArgs.Length == 2)
            {
                Help.ShowCommandDetails(pArgs[1]);
            }
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
                        string compileInput = Input.WriteTextAndReturnRawInput("Would you like to compile the newly added file? [y/n]:");
                        if (compileInput.Trim().Equals("y", OIC))
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
                    string input = Input.WriteTextAndReturnRawInput("\nWould you like to exit BAM Manager (BAMM)? [y/n]:") ?? "n";
                    if (input.Trim().Equals("y", OIC))
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
