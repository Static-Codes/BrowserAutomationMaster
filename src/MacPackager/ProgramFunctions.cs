using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.AppManager.OS.Win;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Input;
using static BrowserAutomationMaster.Messaging.Success;
using static MacPackager.Menu;

namespace MacPackager
{
    public class ProgramFunctions
    {

        static BuildConfigManager? buildConfigManager;

        /// <summary>Handles all of the initial application setup and prerequisite checks.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        public static void Initialize(string[] args)
        {
            // Sets PlatformManager.PlatformName to be used across the session duration.
            SetPlatform();

            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
            {
                VerifyRootDrive(args);
            }
        }


        /// <summary> Handles the Build menu option and 'build' CLI argument. </summary>
        public static void HandleBuildCommand()
        {
            ReassignNullBuildConfigManager();

            // ReassignNullBuildConfigManager() handles the null check for this.
            string path = buildConfigManager!.GetValue("MacOSBinaryPath");
                    
            if (!File.Exists(path))
            {
                WriteAndExit
                (
                    string.Join(' ', [
                        "[ERROR]:",
                        "The BAMM Packager for macOS was unable to find the provided file, " +
                        $"please ensure the file below exists:{NLC}{path}"
                    ]),
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }

            BundleManager bundleManager = new BundleManager();
            bundleManager.BuildBundle();
        }

        
        /// <summary>Processes any CLI arguments and returns execution status.</summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        /// <returns>True if BAMM is to be terminated | False if execution is to continue.</returns>
        public static bool HandleCLIArguments(string[] pArgs)
        {
            if (pArgs.Length == 0) 
                return false; // No args, proceed to main menu loop.

            // Defining the lowercase representation of pArgs[0] to save memory (Not that its required, but its a good practice)
            var lArg0 = pArgs[0].ToLower();

            // Note: no-hwc is handled in HandleHardwareCheck()

            // Displays platform debug information.
            if (pArgs.Any(arg => arg.Equals("--platform-debug")))
            {
                Warning.Write(string.Join(NLC, [
                    "---------------- PLATFORM CLASS DEBUG INFO ----------------",
                    $"IsARMel: {Platforms.IsARMel}",
                    $"IsARMhf: {Platforms.IsARMhf}",
                    $"IsChromeOS: {Platforms.IsChromeOS}",
                    $"IsLinux: {Platforms.IsLinux}",
                    $"IsOSX: {Platforms.IsOSX}",
                    $"IsRaspi: {Platforms.IsRaspi}",
                    $"Raspi Model: {Platforms.GetRaspiModelName()}",
                    $"IsUnixLike: {Platforms.IsUnixLike}",
                    $"IsWindows: {Platforms.IsWindows}",
                    NLC, 
                    NLC,
                ]));
            }

            // Queries whether or not
            if (Platforms.IsUnixLike && pArgs.Any(arg => arg.Equals("--query-display")))
            {
                Console.WriteLine("====================================");
                Console.WriteLine("$DISPLAY Set: {0}", HasDisplayVarSet());
                Console.WriteLine("===================================={0}{1}", NLC, NLC);
            }

            // Handles `--edit-config` command
            if (pArgs[0].Equals("--edit-config"))
            {
                if (buildConfigManager is null) 
                {
                    WriteAndExit(
                        string.Join(NLC, [
                            "Invalid 'validate' command.", 
                            "Valid Syntax:",
                            "bamm-macos-publisher validate path/to/apple-binary"
                        ]), 
                        status: 1, 
                        writePlatformDebugInfo: false
                    );
                }

                // Will return true
                return HandleEditConfigCommand();
            }

            // Forces an explicit error, which is useful for debugging.
            if (pArgs.Any(arg => arg.Equals("--force-error")))
            {
                WriteAndExit
                (
                    message: string.Empty, 
                    status: 0, 
                    writePlatformDebugInfo: true
                );
            }

            // Displays the latest release version of BAMM.
            else if (pArgs.Any(arg => arg.Equals("--version"))) 
            {
                Warning.Write($"[INFO]: Latest Version Available: {CurrentVersion == LatestVersion}");
                Environment.Exit(0);
            }


            // Handles 'help' command variations
            else if (pArgs[0].Equals("help", CCIC) || pArgs[0].Equals("--help"))
            {
                HandleHelpCommand(pArgs);
                return true;
            }

            // Handles 'validate' command variations
            if (pArgs[0].Equals("validate", CCIC))
            {
                if (pArgs.Length != 2)
                {
                    
                    Write(message: "[ERROR]: Invalid 'validate' command.");
                    Console.WriteLine("[INFO]: See validate syntax below.");
                    WriteSuccessMessage("[SYNTAX]: bamm-macos-publisher validate path/to/apple-binary");
                    Environment.Exit(1);
                }

                BundleManager.ValidateBinaryType(pArgs[1]);
                return true;
            }

            return false;
        }


        public static bool HandleEditConfigCommand() 
        {
            ReassignNullBuildConfigManager();

            bool isRunning = true;
            while (isRunning)
            {
                // ReassignNullBuildConfigManager() handles the null check for this.
                var commands = buildConfigManager!.GetKeys();

                // This is used in the menu text.
                var noun = "key";

                // The default pageSize for WriteListFromOptions is 3, so that is the fallback value.
                var pageSize = Math.Max(3, commands.Length);

                var selection = WriteListFromOptions(commands, noun, pageSize);

                var selectedKeysValue = buildConfigManager.GetValue(selection);

                var newValue = AskForInput("Please enter a new value for the specified key: ");

                Console.Clear();

                // White text for the message type header.
                Console.Write($"[INFO]: Current value for key ");

                // Writes the key name in yellow
                Warning.Write(selection, noNewLines: true);
                
                // Closes the line with white text
                Console.Write(": \"");

                // Changes an empty or null string to a more verbose NOT SET
                if (string.IsNullOrEmpty(selectedKeysValue)) 
                {
                    selectedKeysValue = "NOT SET";
                }
                
                // Outputs red text for clarity to indicate to the user this action will change the value.
                Write($"{selectedKeysValue}", noNewLines: true);

                // Writes the closing quote in white and adds a newline that was removed from the call above.
                Console.WriteLine('"');
                Console.WriteLine(NLC);

                // White text for the message type header.
                Console.Write($"[INFO]: New value for key ");
                
                // Writes the key name in yellow
                Warning.Write(selection, noNewLines: true);
                
                // Writes the text between the key and value in white
                Console.Write(": \"");

                // Outputs green text for clarity 
                WriteSuccessMessage($"{newValue}", noNewLines: true);

                // Writes the closing quote in white and adds a newline that was removed from the call above.
                Console.WriteLine('"');
                Console.WriteLine(NLC);

                // Displays a warning
                Warning.Write
                (
                    string.Join(", ", [
                        "[WARNING]: Please note, while this change can be reversed", 
                        "it may cause a previously working build process to fail",
                        "if an incorrect value is provided."
                    ])
                );

                // Trailing new-line char for uniform output.
                Console.Write(NLC);

                // Asks for confirmation
                var confirmation = AskForInput($"[CONFIRM]: Are you sure you want to update the value? [y/n]: ");

                if (ConditionAccepted(confirmation))
                {

                    buildConfigManager.UpdateValue(selection, selectedKeysValue);
                    WriteSuccessMessage($"[SUCCESS]: Updated value of key '{selection}' from to '{newValue}'.");
                }
                
                else 
                {
                    Write("[ERROR]: The operation was cancelled by the user.");
                }
                
                var choice = AskForInput("[CONFIRM]: Would you like to continue editing the Build Config? [y/n]: ");
                isRunning = ConditionAccepted(choice);
            }

            return true;
        }

        /// <summary> Handles variations of 'bamm help' </summary>
        /// <param name="pArgs">Program Arguments (args)</param>
        private static void HandleHelpCommand(string[] pArgs)
        {
            if (pArgs.Length == 1)
            {
                Write($"[ERROR]: Invalid command '{pArgs[0]}'{NLC}");
                
                Console.WriteLine(
                    string.Join(' ', [
                        "[INFO]:",
                        "To see available entries for the 'help' // '--help' command,",
                        "run the packager without arguments then select the Help option in the main menu."
                    ])
                );

                ReadKey();
            }

            else if (pArgs.Length == 2)
            {
                ShowCommandDetails(pArgs[1]);
            }
        }

        public static void HandleNewConfigCommand() 
        {
            ReassignNullBuildConfigManager(forceRefresh: true);

            if (buildConfigManager is null) 
            {
                Write("[ERROR]: An exception occured while attempt to load the Build Config Manager");
                WriteAndExit
                (
                    message: "[ERROR LOG]: Variable 'buildConfigManager' failed a null check.",
                    status: 1, 
                    writePlatformDebugInfo: false
                );
            }

            buildConfigManager.WriteDefaultConfig(overwriteExisting: true);
            
            // Mandatory warnings
            Warning.Write("[WARNING]: You will have to select \"MacOSBinaryPath\" under \"EditConfig\" before building.");
            Warning.Write("[WARNING]: You will have to select \"CPUTarget\" under \"EditConfig\" if targeting Apple Silicon.");
        }

        public static BuildConfigManager ReassignNullBuildConfigManager(bool forceRefresh = false)
        {
            if (buildConfigManager is null || forceRefresh) 
            {
                buildConfigManager = new BuildConfigManager();
            }
            return buildConfigManager;
        }

        public static void RunMenuLoop(string[] args)
        {
            bool isRunning = true;
            while (isRunning)
            {
                MenuOption result = NewMenu();
                switch (result)
                {
                    case MenuOption.BuildPackage:
                        break;

                    case MenuOption.EditConfig:
                        HandleEditConfigCommand();
                        break;

                    case MenuOption.GUI:
                        WriteAndExit
                        (
                            message:
                                string.Join(' ', [
                                    "[ERROR]:",
                                    "The BAMM for macOS Packager does not currently have a Graphical User Interface,",
                                    "this command currently serves as a placeholder for future updates."
                                ]),
                            status: 0,
                            writePlatformDebugInfo: false
                        );
                        break; // Purely to appease the c# static compiler.

                    case MenuOption.NewConfig:
                        HandleNewConfigCommand();
                        break;

                    case MenuOption.Help:
                        break; // Help menu handles its own loop

                    case MenuOption.Invalid:
                        isRunning = false;
                        break;
                }

                if (isRunning)
                {
                    string input = AskForInput($"{NLC}[CONFIRM]: Would you like to exit The BAMM for macOS Packager? [y/n]:");
                    if (ConditionAccepted(input))
                    {
                        isRunning = false;
                    }
                }
            }
        }

        /// <summary>Displays the final exit message and waits for user input.</summary>
        public static void Terminate()
        {
            // PreventMemoryLeaks(selectedBrowser);
            Thread.Sleep(300); // Timeout to prevent unexpected behavior
            WriteMessage("\nPress any key to exit...", isSuccess: true);
            ReadKey();
            Environment.Exit(0);
        }
    }
}
