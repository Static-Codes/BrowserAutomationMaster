using System.Text.Json;
using BrowserAutomationMaster.Messaging;
using YamlDotNet.Core.Tokens;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;
using static MacPackager.BundleManager;

namespace MacPackager 
{
    public class BuildConfig
    {
        private static readonly string BASE_APPLICATION_DIR = AppContext.BaseDirectory;
        private const string CONFIG_FILE_NAME = "buildConfig.json";
        private static readonly string CONFIG_FILE_PATH = Path.Combine(BASE_APPLICATION_DIR, CONFIG_FILE_NAME);

        private Dictionary<string, string> defaultBuildConfig = new() {
            { "MacOSBinaryPath", "" },
            { "CPUTarget", "x64" },
        };

        private Dictionary<string, string> buildConfig;

        public BuildConfig()
        {
            buildConfig = LoadBuildConfig();
        }

        private string GetValue(string key) 
        {
            var value = 
                buildConfig
                .Where(k => k.Key == key)
                .Select(k => k.Value)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(value))
            {
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
                Warning.Write($"[WARNING]: '{CONFIG_FILE_NAME}' was not found."); 
                Console.WriteLine($"[INFO]: Writing default config to '{CONFIG_FILE_PATH}'.");
                WriteDefaultConfig();
                return defaultBuildConfig.ToDictionary(k => k.Key, k => k.Value ?? string.Empty);
            }
            
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
                        continue;
                    }
                    
                    buildConfig.Add(kvp.Key, kvp.Value ?? string.Empty);
                    
                }
                return buildConfig;

            }
            
            catch (Exception ex)
            {
                    Warning.Write(string.Join(NLC, 
                    [
                        $"[WARNING]: Failed to read or deserialize the buildConfig '{CONFIG_FILE_NAME}'.", 
                        $"[WARNING LOG]: {ex.StackTrace ?? ex.Message}",
                    ]));

                    // Writing the default config as a backup
                    Console.WriteLine("[INFO]: Writing default config.");
                    WriteDefaultConfig();
                    WriteSuccessMessage($"[SUCCESS]: Wrote the default config to `{CONFIG_FILE_NAME}`.");

                    // Binary Path warning
                    Console.WriteLine("[INFO]: The following warning is a requirement for all users regardless of their CPU target.");
                    Warning.Write("[WARNING]: You will have to select \"Change Config Binary Path\" from the main menu, before you are able to build.");
                    
                    // CPU Target warning
                    Console.WriteLine("[INFO]: If you are bundling a macOS binary for Apple Silicon, please read the warning below, otherwise you can ignore it.");
                    Warning.Write("[WARNING]: You will have to select \"Change Config CPU Target\" from the main menu, before you are able to build.");
                    return defaultBuildConfig;
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
            return $"MacOSBinaryPath: {buildConfig["MacOSBinaryPath"]}\nCPUType: {buildConfig["CPUType"]}";
        }
    }
}