using System.Text.Json;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static MacPackager.BundleManager;

namespace MacPackager 
{
    public class BuildConfig
    {
        private const string CONFIG_FILE_NAME = "buildConfig.json";

        private Dictionary<string, string> defaultBuildConfig = new() {
            { "MacOSBinaryPath", "" },
            { "CPUTarget", "x64" },
        };

        private Dictionary<string, string> buildConfig;

        public BuildConfig()
        {
            buildConfig = LoadBuildConfig();
        }

        private void WriteDefaultConfig()
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(defaultBuildConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(CONFIG_FILE_NAME, jsonString);
            }
            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, 
                    [
                        $"[ERROR]: Unable to write the default build config.", 
                        "Error Log:",
                        ex.StackTrace ?? ex.Message
                    ]),
                    status: 1,
                    writePlatformDebugInfo: false
                );
            }
        }

        private Dictionary<string, string> LoadBuildConfig()
        {
            if (File.Exists(CONFIG_FILE_NAME))
            {
                try
                {
                    // Loads the config (if present)
                    string jsonString = File.ReadAllText(CONFIG_FILE_NAME);
                    var loadedConfig = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);

                    var buildConfig = new Dictionary<string, string>(defaultBuildConfig.Count);


                    foreach (var kvp in defaultBuildConfig)
                    {
                        if (loadedConfig != null && loadedConfig.TryGetValue(kvp.Key, out string? value) && value != null)
                        {
                            buildConfig.Add(kvp.Key, value);
                        }
                        else
                        {
                            buildConfig.Add(kvp.Key, kvp.Value ?? string.Empty);
                        }
                    }
                    return buildConfig;
                }
                catch (Exception ex)
                {
                    Warning.Write(string.Join(NLC, 
                    [
                        $"[WARNING]: Failed to read or deserialize the buildConfig '{CONFIG_FILE_NAME}'.", 
                        "Error Log:",
                        ex.StackTrace ?? ex.Message
                    ]));

                    // Writing the default config as a backup
                    Console.WriteLine("[INFO]: Writing default config.");
                    WriteDefaultConfig();
                    WriteSuccessMessage($"[SUCCESS]: Wrote the default config to `{CONFIG_FILE_NAME}`.");

                    // Binary Path warning
                    Console.WriteLine("[INFO]: The following warning is a requirement for all users regardless of their CPU target.");
                    Warning.Write("[WARNING]: You will have to select \"Change Config Binary Path\" from the main menu, before you are able to build.");
                    
                    // CPU Target warning
                    Console.WriteLine("[INFO] If you are bundling a macOS binary for Apple Silicon, please read the warning below, otherwise you can ignore it.");
                    Warning.Write("[WARNING]: You will have to select \"Change Config CPU Target\" from the main menu, before you are able to build.");
                    return defaultBuildConfig;
                }
            }
            else
            {
                Console.WriteLine($"Warning: '{CONFIG_FILE_NAME}' not found. Writing default config.");
                WriteDefaultConfig();
                return defaultBuildConfig.ToDictionary(k => k.Key, k => k.Value ?? string.Empty);
            }
        }

        private void UpdateValue(string key, string value)
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
                        WriteSuccessMessage($"[SUCCESS]: Updated {key} in `{CONFIG_FILE_PATH}` to {value}");
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

                    Console.WriteLine($"Error: Invalid value for CPUType: '{value}'. Must be 'x64' or 'ARM64'.");
                    WriteAndExit
                        (
                            string.Join(NLC, 
                            [
                                $"[ERROR]: An invalid value was provided for CPUType occured while trying to update `{key}` in `{CONFIG_FILE_PATH}`",
                                "Error Log:",
                                $"Validation failed for value '{value}'. {ex.Message}"
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
            return $"MacOSBinaryPath: {buildConfig["MacOSBinaryPath"]}\nCPUType: {buildConfig["CPUType"]}";
        }
    }
}