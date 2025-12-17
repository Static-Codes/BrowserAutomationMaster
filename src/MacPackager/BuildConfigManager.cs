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
        private static readonly string CONFIG_FILE_PATH = Path.Combine(BASE_APPLICATION_DIR, CONFIG_FILE_NAME);

        private readonly Dictionary<string, string> defaultBuildConfig = new() 
        {
            { "MacOSBinaryPath", "" },
            { "CPUTarget", "x64" },
        };

        private readonly Dictionary<string, string> buildConfig;

        public readonly JsonSerializerOptions serializerOptions = new() { WriteIndented = true };
        public BuildConfigManager()
        {
            buildConfig = LoadBuildConfig();
        }

        // public Dictionary<string, string> GetBuildConfig() { return buildConfig; }

        public string GetValue(string key) 
        {
            var value = 
                buildConfig
                .Where(k => k.Key == key)
                .Select(k => k.Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(value))
            {
                Warning.Write($"[WARNING]: Please ensure that the Build Config has a value for the key '{key}'.");
                Console.WriteLine($"[INFO]: The config is located at '{CONFIG_FILE_PATH}'.");
                Console.WriteLine($"[INFO]: The config can be edited using the \"EditConfig\" menu option.");
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
             
            return value;

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
                        message: "[ERROR]: The operation was cancelled by the user, The BAMM for macOS Packager will exit now.",
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
            try
            {
                // Throws an early (recoverable) exception, if the config file is not present.
                // If this exception is thrown the code outside the conditional is not executed.
                // The Build Config is created within the catch block below.
                if (!File.Exists(CONFIG_FILE_PATH))
                {
                    Warning.Write($"[WARNING]: '{CONFIG_FILE_NAME}' was not found, using default.");
                    return defaultBuildConfig;
                }
            
                // Loads the config (if present)
                string jsonString = File.ReadAllText(CONFIG_FILE_PATH);
                var loadedConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                var buildConfig = new Dictionary<string, string>(defaultBuildConfig.Count);


                foreach (var kvp in defaultBuildConfig)
                {
                    if (loadedConfig != null && loadedConfig.TryGetValue(kvp.Key, out string? value) && value != null)
                    {
                        buildConfig.Add(kvp.Key, value);
                        continue;
                    }
                    
                    buildConfig.Add(kvp.Key, kvp.Value ?? string.Empty);
                    
                }
                return buildConfig;

            }
            
            catch (Exception ex)
            {
                Warning.Write($"[WARNING]: Failed to parse the build config, the file might be corrupted.");
                Warning.Write($"[WARNING LOG]: {ex.Message}");

                return CreateAndReturnDefaultConfig(overwrite: false);
            }
            
        }

        private Dictionary<string, string> CreateAndReturnDefaultConfig(bool overwrite)
        {
            Console.WriteLine("[INFO]: Writing default config.");
            WriteDefaultConfig(overwrite);
            WriteSuccessMessage($"[SUCCESS]: Wrote the default config to `{CONFIG_FILE_NAME}`.");

            // Mandatory warnings
            Warning.Write("[WARNING]: You will have to select \"MacOSBinaryPath\" under \"EditConfig\" before building.");
            Warning.Write("[WARNING]: You will have to select \"CPUTarget\" under \"EditConfig\" if targeting Apple Silicon.");
            
            return defaultBuildConfig;
        }

        public void UpdateValue(string key, string value)
        {
            if (!buildConfig.ContainsKey(key))
            {
                Console.WriteLine($"Error: Key '{key}' does not exist in build config.");
                return;
            }

            switch (key)
            {
                case "MacOSBinaryPath":
                    try
                    {
                        ValidateBinaryType(value);
                        buildConfig[key] = value;
                        WriteSuccessMessage($"[SUCCESS]: Updated '{key}' in '{CONFIG_FILE_PATH}' to '{value}'");
                    }
                    catch (Exception ex)
                    {
                        WriteAndExit
                        (
                            string.Join(' ', 
                            [
                                $"[ERROR]:Validation failed for value `{key}` in `{CONFIG_FILE_PATH}`",
                                $"[ERROR LOG]: '{value}' is not a valid choice.",
                                $"[ERROR STACK]: {ex.StackTrace ?? ex.Message})"
                            ]),
                            status: 1,
                            writePlatformDebugInfo: false
                        );
                    }
                    break;

                case "CPUType":
                    if (value.Equals("x64", OIC) || value.Equals("ARM64", OIC))
                    {
                        buildConfig[key] = value;
                        WriteSuccessMessage($"[SUCCESS]: Updated {key} in `{CONFIG_FILE_PATH}` to {value}");
                        break;
                    }

                    WriteAndExit
                    (
                        string.Join(NLC, 
                        [
                            $"[ERROR]: An invalid value was provided for CPUType while trying to update `{key}` in `{CONFIG_FILE_PATH}`",
                            $"[ERROR LOG]: Invalid value for CPUType: '{value}', Must be 'x64' or 'ARM64'."
                        ]),
                        status: 1,
                        writePlatformDebugInfo: false
                    );
                    break;

                default:
                    buildConfig[key] = value;
                    WriteSuccessMessage($"[SUCCESS]: Updated {key} in `{CONFIG_FILE_PATH}` to {value}");
                    break;
            }
        }

        public override string ToString()
        {
            return @$"
            MacOSBinaryPath: {buildConfig["MacOSBinaryPath"]}
            CPUType: {buildConfig["CPUType"]}
            ";
        }
    }
}