using System.Text.Json;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Input;
using static BrowserAutomationMaster.Messaging.Success;
using static MacPackager.BundleManager;

namespace MacPackager 
{
    public class BuildConfigManager
    {
        private static readonly string BASE_APPLICATION_DIR = AppContext.BaseDirectory;
        private const string CONFIG_FILE_NAME = "buildConfig.json";
        public static readonly string CONFIG_FILE_PATH = Path.Combine(BASE_APPLICATION_DIR, CONFIG_FILE_NAME);

        private readonly Dictionary<string, string> defaultBuildConfig = new() 
        {
            { "MacOSBinaryPath", "" },
            { "CPUTarget", "x64" },
        };

        public static readonly string defaultBuildConfigContentString = """
            { "MacOSBinaryPath": "" },
            { "CPUTarget": "x64" }
        """;

        public readonly Dictionary<string, string> buildConfig;

        public readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
        public BuildConfigManager()
        {
            buildConfig = LoadBuildConfig();
        }

        // public Dictionary<string, string> GetBuildConfig() { return buildConfig; }

        public string GetValue(string key, bool failIfEmpty = false)
        {
            // Attempts to
            buildConfig.TryGetValue(key, out var value);

            if (failIfEmpty && string.IsNullOrEmpty(value))
            {
                Warning.Write($"[WARNING]: Please ensure that the build config has a value for the key '{key}'.");
                Console.WriteLine($"[INFO]: The build config is located at '{CONFIG_FILE_PATH}'.");
                Console.WriteLine($"[INFO]: It can be edited using the \"EditConfig\" menu option.");
                Console.WriteLine($"[INFO]: If you prefer using the CLI, you can use the following command.");
                WriteSuccessMessage("[SYNTAX]: bamm-macos-publisher --edit-config");

                WriteAndExit
                (
                    message: 
                        string.Join(NLC, 
                        [
                            $"[ERROR]: Unable to return a value for the key '{key}' in '{CONFIG_FILE_NAME}'.",
                            $"[ERROR LOG]: GetValue(key: {key}) returned a null value."
                        ]), 
                    status: 1, 
                    writePlatformDebugInfo: false
                );
            }
             
            return value ?? string.Empty;

        }

        public string[] GetKeys() 
        {
            if (buildConfig.Keys is null) 
            {
                Warning.Write($"[WARNING]: Please ensure that the Build Config exists.");
                Console.WriteLine($"[INFO]: The config should be located at '{CONFIG_FILE_PATH}'.");
                Console.WriteLine($"[INFO]: If this issue persists, please open The BAMM for macOS Packager, and select \"New Config\".");

                WriteAndExit
                (
                    message: $"[ERROR]: Unable to retrieve the keys from the Build Config.",
                    status: 1, 
                    writePlatformDebugInfo: false
                );
            }

            return [.. buildConfig.Keys];
        }
        public void WriteDefaultConfig(bool overwriteExisting = true)
        {
            // The warning and confirmation are not required if the file doesn't already exist.
            if (!File.Exists(CONFIG_FILE_PATH) && overwriteExisting)
            {
                overwriteExisting = false;
            }

            if (overwriteExisting)
            {
                Warning.Write
                (
                    string.Join(", ", [
                        "[WARNING]: This is a potentially destructive action",
                        "it should only be used if you are experiencing failed builds,"
                    ])
                );

                var choice = AskForInput("Would you like to override the current build config? [y/n]: ");
                if (ConditionRejected(choice)) 
                {
                    WriteAndExit
                    (
                        message: "[ERROR]: The operation was cancelled by the user, press any key to exit The BAMM for macOS Packager.",
                        status: 0,
                        writePlatformDebugInfo: false
                    );
                }
            }

            try
            {
                Console.WriteLine($"[INFO]: Writing default build config.{NLC}");
                string jsonString = JsonSerializer.Serialize(defaultBuildConfig, serializerOptions);
                File.WriteAllText(CONFIG_FILE_PATH, jsonString);
                WriteSuccessMessage($"[SUCCESS]: Wrote default build config to '{CONFIG_FILE_PATH}'.");
            }
            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, 
                    [
                        $"[ERROR]: Unable to write the default build config.", 
                        $"[ERROR LOG]: {ex.StackTrace ?? ex.Message}",
                    ]),
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }
        }

        private Dictionary<string, string> LoadBuildConfig()
        {
            if (!File.Exists(CONFIG_FILE_PATH)) 
            {
                return new Dictionary<string, string>(defaultBuildConfig);
            }

            try
            {
                string jsonString = File.ReadAllText(CONFIG_FILE_PATH);
                return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString) ?? defaultBuildConfig;
            }
            catch
            {
                return new Dictionary<string, string>(defaultBuildConfig);
            }
        }

        public void UpdateConfigContents() 
        {
            try 
            {
                var newContents = JsonSerializer.Serialize(buildConfig, serializerOptions);
                File.WriteAllText(CONFIG_FILE_PATH, newContents);
            }
            catch (Exception ex)
            {
                 WriteAndExit
                (
                    string.Join(NLC, 
                    [
                        $"[ERROR]: Unable to write the updated build config.", 
                        $"[ERROR LOG]: {ex.StackTrace ?? ex.Message}",
                    ]),
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }
        }
        public void UpdateValue(string key, string value)
        {
            if (!buildConfig.ContainsKey(key))
            {
                Console.WriteLine($"[ERROR]: The key '{key}' does not exist in build config.");
                return;
            }

            switch (key)
            {
                case "MacOSBinaryPath":
                    try
                    {   
                        // For uniformity clearing previous output will avoid incorrect spacing.
                        Console.Clear();

                        // Writes the initial text in white.
                        Console.Write("[INFO]: Validating provided binary path: ");

                        // Writes the path to binary in yellow for clarity.
                        Warning.Write(value, noNewLines: true);

                        // New line chars for uniform output.
                        Console.WriteLine(NLC);

                        ValidateBinaryType(value, null);

                        
                        buildConfig[key] = value;

                        UpdateConfigContents();

                        // Green text for the header.
                        WriteSuccessMessage($"[SUCCESS]: Updated ", noNewLines: true);

                        // Displays the key in yellow.
                        Warning.Write(key, noNewLines: true);

                        // Continues the line with green text for uniformity.
                        WriteSuccessMessage($" in build config with the value ", noNewLines: true);

                        // Finishes the line with yellow text for clarity.
                        Warning.Write(value, noNewLines: true);

                        // Empty lines for formatting
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                    catch (Exception ex)
                    {
                        WriteAndExit
                        (
                            string.Join(' ', 
                            [
                                $"[ERROR]: Validation failed for value `{key}` in the build config.",
                                $"[ERROR LOG]: '{value}' is not a valid path.",
                                $"[ERROR STACK]: {ex.StackTrace ?? ex.Message})"
                            ]),
                            status: 1,
                            writePlatformDebugInfo: false
                        );
                    }
                    break;

                case "CPUTarget":

                    if (value.Equals(buildConfig[key])) 
                    {
                        WriteAndExit
                        (
                            $"[ERROR]: Unable to update {key}. The new value provided is equal to the previous value.",
                            status: 1,
                            writePlatformDebugInfo: false
                        );
                    }

                    if (value.Equals("x64") || value.Equals("ARM64"))
                    {
                        buildConfig[key] = value;
                        UpdateConfigContents();
                        WriteSuccessMessage($"[SUCCESS]: Updated {key} in build config to {value}");
                        return;
                    }

                    WriteAndExit
                    (
                        string.Join(NLC, 
                        [
                            $"[ERROR]: An invalid value was provided for CPUTarget while trying to update the build config.",
                            $"[ERROR LOG]: Invalid value for CPUTarget: '{value}', only 'x64' and 'ARM64' are supported, these values are case sensitive."
                        ]),
                        status: 1,
                        writePlatformDebugInfo: false
                    );
                    break;

                default:
                    break;
            }
        }

        public override string ToString()
        {
            return @$"
                -> MacOSBinaryPath: {buildConfig["MacOSBinaryPath"]}
                -> CPUTarget: {buildConfig["CPUTarget"]}
            ".Replace("                ", "");
        }
    }
}