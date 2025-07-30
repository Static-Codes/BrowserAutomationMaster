using BrowserAutomationMaster.Messaging;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Managers
{
    public partial class Config
    {
        public required Theme ThemeType { get; set; }
        public bool ShowCpuCheck { get; set; }
        public bool ShowMemoryCheck { get; set; }
        public bool ShowUpdateCheck { get; set; }
        public bool AutoCopyPath { get; set; }
        public bool RunOnCompile { get; set; }
    }

    public partial class ConfigParser
    {
        [GeneratedRegex("^.*=.*(true|false)$")]
        public static partial Regex BoolRegex();

        [GeneratedRegex("^.*=.*(dark|light)$")]
        public static partial Regex ThemeRegex();

        [GeneratedRegex("^.*=.*(\\d+)$")]
        public static partial Regex IntRegex();

        public static string ConvertSnakeToPascal(string snake_case)
        {
            return string.Join(
                string.Empty,
                snake_case
                    .Split('_')
                    .Select(s => char.ToUpper(s[0]) + s[1..])
            );
        }

        public static bool IsValidLine(string line, Regex regexName)
        {
            return regexName.IsMatch(line);
        }

        public static string RemoveCommentIfPresent(string line)
        {
            if (line.Contains(';'))
            {
                int index = line.IndexOf(';');
                return line[..index];
            }
            return line;
        }
    }

    public class ConfigManager()
    {
        public static Config GlobalConfig { get; private set; } = new()
        {
            AutoCopyPath = false,
            RunOnCompile = false,
            ShowCpuCheck = true,
            ShowMemoryCheck = true,
            ShowUpdateCheck = false,
            ThemeType = ThemeManager.DefaultTheme
        };

        private static readonly Dictionary<string, List<KeyValuePair<string, string>>> rawSections = new()
        {
            {
                "[messaging]", new List<KeyValuePair<string, string>>()
                {
                    KeyValuePair.Create("show_cpu_check", "true"),
                    KeyValuePair.Create("show_memory_check", "true"),
                    KeyValuePair.Create("show_update_check", "false"),
                }
            },
            {
                "[interface]", new List<KeyValuePair<string, string>>()
                {
                    KeyValuePair.Create("theme_type", "dark"), // Also supports "light"
                }
            },
            {
                "[compilation]", new List<KeyValuePair<string, string>>()
                {
                    KeyValuePair.Create("auto_copy_path", "false"),
                    KeyValuePair.Create("run_on_compile", "false"),
                }
            },
        };

        private static string ConfigDirectory { get; set; } = DirectoryManager.GetConfigDirectory();
        public static string ConfigFilePath { get; private set; } = Path.Combine(ConfigDirectory, "config.ini");

        private static string BuildConfigContents()
        {
            var configContents = new StringBuilder();
            var sortedSections = rawSections.OrderBy(s => s.Key);

            foreach (var sectionEntry in sortedSections)
            {
                var sectionName = sectionEntry.Key;
                var properties = sectionEntry.Value;

                configContents.AppendLine($"{sectionName}");

                foreach (var property in properties)
                {
                    configContents.AppendLine($"{property.Key} = {property.Value}");
                }

                // Adds trailing newline to all sections except the final one
                if (sectionEntry.Key != sortedSections.Last().Key)
                {
                    configContents.AppendLine();
                }
            }

            return configContents.ToString();
        }

        private static bool ConfigDirectoryExists()
        {
            return !string.IsNullOrEmpty(ConfigDirectory) && Directory.Exists(ConfigDirectory);
        }

        private static bool ConfigFileExists()
        {
            return !string.IsNullOrEmpty(ConfigFilePath) && File.Exists(ConfigFilePath);
        }

        private static object? DoCast(string value, Type targetType)
        {
            if (targetType == typeof(bool))
            {
                return bool.Parse(value);
            }
            else if (targetType == typeof(int))
            {
                return int.Parse(value);
            }
            else if (targetType == typeof(string))
            {
                return value;
            }
            else if (targetType == typeof(Theme))
            {
                var themeName = char.ToUpper(value[0]) + value[1..] + "Theme";
                var bindingAttr = BindingFlags.Public | BindingFlags.Static;

                FieldInfo? field = typeof(ThemeManager).GetField(themeName, bindingAttr);

                if (field == null)
                {
                    throw new ArgumentException($"Theme '{value}' not found in ThemeManager (expected field '{themeName}').");
                }

                return field.GetValue(null);
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, ignoreCase: true);
            }
            else
            {
                throw new InvalidCastException(
                    $"Cannot convert value '{value}' to type '{targetType.Name}', as it's currently not supported.\n" +
                    "Please add this feature in ConfigManager.DoCast()"
                );
            }
        }

        public static void EnsureConfigExists()
        {
            if (!ConfigDirectoryExists())
            {
                try
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }
                catch (Exception ex)
                {
                    Errors.WriteErrorAndExit(
                        "Failed to create config directory.\n'" +
                        $"{ConfigDirectory}'\nError: {ex.Message}",
                        status: 1
                    );
                }
            }

            if (!ConfigFileExists())
            {
                try
                {
                    string configContents = BuildConfigContents();
                    ValidateConfigContents(configContents);
                    File.WriteAllText(ConfigFilePath, configContents);
                    return;
                }
                catch (Exception ex)
                {
                    Errors.WriteErrorAndExit(
                        "Failed to create config file.\n'" +
                        $"{ConfigFilePath}'\nError: {ex.Message}\n" +
                        $"Please make a bug report at {ConstantManager.ISSUES_LINK}",
                        status: 1
                    );
                }
            }

            try
            {
                string configContents = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                ValidateConfigContents(configContents);
            }
            catch
            {
                Errors.WriteErrorAndContinue("Failed to validate config.ini, writing default values.");
                string configContents = BuildConfigContents();
                ValidateConfigContents(configContents);
                File.WriteAllText(ConfigFilePath, configContents); // Fixed: was ConfigDirectory, should be ConfigFilePath
            }
        }

        private static Dictionary<string, Regex> GetPropsAndFuncs()
        {
            var propsAndFuncs = new Dictionary<string, Regex>();

            // Builds propsAndFuncs dynamically
            foreach (var sectionName in rawSections.Keys)
            {
                foreach (var propKvp in rawSections[sectionName])
                {
                    if (propKvp.Key.Equals("theme_type"))
                    {
                        propsAndFuncs.Add(propKvp.Key, ConfigParser.ThemeRegex());
                    }
                    else if (bool.TryParse(propKvp.Value, out bool _))
                    {
                        propsAndFuncs.Add(propKvp.Key, ConfigParser.BoolRegex());
                    }
                    // Add support for integer properties if needed
                    else if (int.TryParse(propKvp.Value, out int _))
                    {
                        propsAndFuncs.Add(propKvp.Key, ConfigParser.IntRegex());
                    }
                }
            }
            return propsAndFuncs;
        }

        public static Config LoadConfig()
        {
            EnsureConfigExists();

            var configContents = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
            string? currentSection = null;
            var splitLines = configContents.Split('\n');

            foreach (string originalLine in splitLines)
            {
                string trimmedLine = ConfigParser.RemoveCommentIfPresent(
                    originalLine.Replace('\r', ' ').Trim()
                );

                if (string.IsNullOrWhiteSpace(trimmedLine)) { continue; }

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    currentSection = trimmedLine;
                }
                else if (currentSection != null)
                {
                    string[] parts = trimmedLine.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var rawPropName = parts[0].Trim();
                        var bindingAttr = BindingFlags.Public | BindingFlags.Instance;

                        var propName = ConfigParser.ConvertSnakeToPascal(rawPropName);
                        var propValue = parts[1].Trim();

                        PropertyInfo? property = typeof(Config).GetProperty(propName, bindingAttr);

                        if (property == null)
                        {
                            Errors.WriteErrorAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    line: originalLine,
                                    lineNumber: Array.IndexOf(splitLines, originalLine) + 1,
                                    $"Property '{propName}' not found or not settable in Config class."
                                ),
                                status: 1
                            );
                        }

                        try
                        {
                            object? castedValue = DoCast(propValue, property.PropertyType);
                            if (castedValue == null)
                            {
                                Errors.WriteErrorAndExit(
                                    Errors.GenerateErrorMessage(
                                        fileName: "config.ini",
                                        line: originalLine,
                                        lineNumber: Array.IndexOf(splitLines, originalLine) + 1,
                                        $"Failed to convert value '{propValue}' for property '{propName}'."
                                    ),
                                    status: 1
                                );
                            }
                            property.SetValue(GlobalConfig, castedValue);
                        }
                        catch (Exception ex)
                        {
                            Errors.WriteErrorAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    line: originalLine,
                                    lineNumber: Array.IndexOf(splitLines, originalLine) + 1,
                                    $"Failed to convert value '{propValue}' for property '{propName}': {ex.Message}"
                                ),
                                status: 1
                            );
                        }
                    }
                }
            }
            return GlobalConfig;
        }

        private static void ValidateConfigContents(string configContents)
        {
            var splitLines = configContents.Split('\n');
            string? currentSection = null;
            var encounteredSections = new HashSet<string>();
            var propsAndFuncs = GetPropsAndFuncs();

            for (int i = 0; i < splitLines.Length; i++)
            {
                string originalLine = splitLines[i];
                string trimmedLine = ConfigParser.RemoveCommentIfPresent(
                    originalLine.Replace('\r', ' ').Trim()
                );

                if (string.IsNullOrWhiteSpace(trimmedLine)) { continue; }

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    string sectionName = trimmedLine;

                    if (!rawSections.ContainsKey(sectionName))
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1, // Fixed: line numbers should be 1-based
                                issueText: $"Unknown section detected: `{sectionName}` is not a valid section."
                            ),
                            status: 1
                        );
                    }

                    if (encounteredSections.Contains(sectionName))
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1,
                                issueText: $"Duplicate section detected: `{sectionName}` has already been defined."
                            ),
                            status: 1
                        );
                    }

                    currentSection = sectionName;
                    encounteredSections.Add(sectionName);
                }
                else
                {
                    if (currentSection == null)
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1,
                                issueText: "Content found before any section header. All configuration must be within a section."
                            ),
                            status: 1
                        );
                    }

                    string[] parts = trimmedLine.Split('=', 2);
                    if (parts.Length != 2)
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1,
                                issueText: "Invalid property format, expected 'name = value'."
                            ),
                            status: 1
                        );
                    }

                    string propName = parts[0].Trim();
                    string propValue = parts[1].Trim();

                    if (!rawSections[currentSection].Any(pair => pair.Key.Equals(propName)))
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1,
                                issueText: $"Unknown property `{propName}` in section `{currentSection}`."
                            ),
                            status: 1
                        );
                    }

                    if (propsAndFuncs.TryGetValue(propName, out Regex? func))
                    {
                        if (!ConfigParser.IsValidLine(trimmedLine, func))
                        {
                            Errors.WriteErrorAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    line: originalLine,
                                    lineNumber: i + 1,
                                    issueText: $"Invalid value '{propValue}' for property `{propName}`."
                                ),
                                status: 1
                            );
                        }
                    }
                    else
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i + 1,
                                issueText: $"No validation rule found for property `{propName}` in section `{currentSection}`."
                            ),
                            status: 1
                        );
                    }
                }
            }
        }
    }
}