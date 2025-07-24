using BrowserAutomationMaster.Messaging;
using Esprima.Ast;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Managers
{

    public partial class Config
    {
        public Theme? ThemeType { get; set; }
        public bool ShowCpuCheck { get; set; }
        public bool ShowMemoryCheck { get; set; }
        public bool ShowUpdateCheck { get; set; }
        public bool AutoCompile { get; set; }
        public bool AutoCopyPath { get; set; }
    }
    
    public partial class ConfigParser()
    {
        //[GeneratedRegex("^.\\S*=\\S.*$")]

        [GeneratedRegex("^.*=.*(true|false)$")]
        public static partial Regex BoolRegex();

        [GeneratedRegex("^.*=.*(dark|light)$")]
        public static partial Regex ThemeRegex();
        [GeneratedRegex("^.*=.*(\\d)$")]
        public static partial Regex IntRegex();

        public static string ConvertSnakeToPascal(string snake_case)
        {
            return string.Join(
                string.Empty,
                snake_case
                    .Split('_')
                    .Select(
                        s => char.ToUpper(s[0]) + s[1..]
                    )
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
    public class ConfigManager
    {
        private static readonly Dictionary<string, List<KeyValuePair<string, string>>> rawSections = new()
        {
            {
                "[messaging]", new List<KeyValuePair<string, string>>()
                {
                    KeyValuePair.Create("show_cpu_check", "true"),
                    KeyValuePair.Create("show_memory_check", "true"),
                    KeyValuePair.Create("show_update_check", "true"),
                    //KeyValuePair.Create("", ""),
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
                    KeyValuePair.Create("auto_compile", "false"),
                    KeyValuePair.Create("auto_copy_path", "false"),
                    //KeyValuePair.Create("", "")
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
                var propertys = sectionEntry.Value;

                configContents.AppendLine($"{sectionName}");

                foreach (var property in propertys)
                {
                    configContents.AppendLine($"{property.Key} = {property.Value}");
                }

                // Adds trailing spaces to all lines except the final.
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

        public static void EnsureConfigExists()
        {
            if (!ConfigDirectoryExists())
            {
                try
                {
                    Directory.CreateDirectory(ConfigDirectory);
                    //Success.WriteSuccessMessage(
                    //    $"Successfully created config directory.\n" +
                    //    $"Location: {ConfigDirectory}"
                    //);
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
                    File.WriteAllText(ConfigFilePath, configContents); // Null check is done in ConfigFileExists
                    //Success.WriteSuccessMessage(
                    //    $"Successfully created config directory.\n" +
                    //    $"Location: {ConfigDirectory}"
                    //);
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
                string configContents = File.ReadAllText(path: ConfigFilePath!, encoding: Encoding.UTF8);
                ValidateConfigContents(configContents);
            }
            catch
            {
                Errors.WriteErrorAndContinue("Failed to validate config.ini, writing default values.");
                string configContents = BuildConfigContents();
                ValidateConfigContents(configContents);
                File.WriteAllText(ConfigDirectory, configContents);
            }
        }
        private static Dictionary<string, Regex> GetPropsAndFuncs()
        {
            var propsAndFuncs = new Dictionary<string, Regex>();
            Action Add(string propName, Regex func) => () => {
                propsAndFuncs.Add(propName, func);
            };

            // Builds propsAndFuncs dynamically
            foreach (var sectionName in rawSections.Keys)
            {
                foreach (var propName in rawSections[sectionName])
                {
                    if (propName.Key.Equals("theme_type"))
                    {
                        Add(propName: "theme_type", func: ConfigParser.ThemeRegex())();
                    }
                    else if (bool.TryParse(propName.Value, out bool res))
                    {
                        Add(propName.Key, ConfigParser.BoolRegex())();
                    }
                }
            }
            return propsAndFuncs;
        }
        private static void ValidateConfigContents(string configContents)
        {
            // ;            Comments
            // [            Key start  ||
            // ]            Key end
            // name = value
            // (check if previous trimmed line ends with ]
            // if it does check for [ or ] in the current line, if this is true then an error is thrown
            // if no error is thrown add it to the section

            var splitLines = configContents.Split('\n');
            string? currentSection = null;
            var encounteredSections = new HashSet<string>();
            var propsAndFuncs = GetPropsAndFuncs();

            for (int i = 0; i < splitLines.Length; i++)
            {
                string originalLine = splitLines[i]; // Original is used for errors
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
                                lineNumber: i,
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
                                lineNumber: i,
                                issueText: $"Duplicate section detected: `{sectionName}` has already been defined."
                            ),
                            status: 1
                        );
                    }

                    currentSection = sectionName;
                    encounteredSections.Add(sectionName);
                }

                // Handles lines within a given section
                else
                {
                    if (currentSection == null)
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i,
                                issueText: "Content found before any section header. All configuration must be within a section."
                            ),
                            status: 1
                        );
                    }

                    // Validate property format (name = value)
                    string[] parts = trimmedLine.Split('=', 2);
                    if (parts.Length != 2)
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i,
                                issueText: "Invalid property format, expected 'name = value'."
                            ),
                            status: 1
                        );
                    }

                    string propName = parts[0].Trim();
                    string propValue = parts[1].Trim();
                    

                    // Validate if the property is expected in the current section
                    if (!rawSections[currentSection].Any(pair => pair.Key.Equals(propName)))
                    {
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i,
                                issueText: $"Unknown property `{propName}` in section `{currentSection}`."
                            ),
                            status: 1
                        );
                    }

                    propsAndFuncs.TryGetValue(propName, out Regex? func);
                    if (func == null) {
                        // This wont be executed but the out parameter cannot be used the null forgiveness operator.
                        Errors.WriteErrorAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                line: originalLine,
                                lineNumber: i,
                                issueText: $"Unknown property `{propName}` in section `{currentSection}`."
                            ),
                            status: 1
                        );
                    }

                    if (!ConfigParser.IsValidLine(trimmedLine, func))
                    {
                        Errors.WriteErrorAndExit($"Invalid value passed to: `{propName}`", 1);
                    }

                }
            }
        }

        public static Config LoadConfig()
        {
            EnsureConfigExists();

            var configContents = File.ReadAllText(ConfigFilePath!, Encoding.UTF8);

            Config config = new();
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

                        // Reflection was the only the way I found to access all properties of my class dynamically.
                        PropertyInfo? property = typeof(Config).GetProperty(propName, bindingAttr);
                              

                        if (property == null)
                        {
                            // This error SHOULD be caught by ValidateConfigContents() but a fallback is nice.
                            Errors.WriteErrorAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    line: originalLine,
                                    lineNumber: Array.IndexOf(splitLines, originalLine),
                                    $"Property '{propName}' not found or not settable in Config class."
                                ),
                                status: 1
                            );
                        }


                        try
                        {
                            // Casts the property's string value to the property's type.
                            object? castedValue = DoCast(propValue, property.PropertyType);
                            if (castedValue == null) {
                                Errors.WriteErrorAndExit(
                                    Errors.GenerateErrorMessage(
                                        fileName: "config.ini",
                                        line: originalLine,
                                        lineNumber: Array.IndexOf(splitLines, originalLine),
                                        $"Failed to convert value '{propValue}' for property '{propName}'."
                                    ),
                                    status: 1
                                );
                            }
                            property.SetValue(config, castedValue);
                        }
                        catch (Exception ex)
                        {
                            Errors.WriteErrorAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    line: originalLine,
                                    lineNumber: Array.IndexOf(splitLines, originalLine),
                                    $"Failed to convert value '{propValue}' for property '{propName}': {ex.Message}"
                                ),
                                status: 1
                            );
                        }
                    }
                    
                }
            }
            return config;
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
                var name = string.Join("", themeName);
                FieldInfo? field = 
                    typeof(ThemeManager).GetField(name, bindingAttr) ?? 
                    throw new ArgumentException($"Theme '{value}' not found in ThemeManager (expected field '{themeName}').");

                // I can't wrap my head around why:
                // return field doesnt work
                // but...
                // field.GetValue(null) does??

                return field.GetValue(null);
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(
                    enumType: targetType,
                    value,
                    ignoreCase: true
                );
            }
            else
            {
                throw new InvalidCastException(
                    $"Cannot convert value '{value}' to type '{targetType.Name}', as its currently not supported.\n" +
                    "Please add this feature in ConfigManager.ConvertValue()"
                );
            }
        }
    }
}
