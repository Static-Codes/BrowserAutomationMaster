using BrowserAutomationMaster.Messaging;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;

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
        public bool UseBrowserstack { get; set; }
    }

    public partial class ConfigParser
    {
        [GeneratedRegex("^.*=.*(true|false)$")]
        public static partial Regex BoolRegex();

        [GeneratedRegex("^.*=.*(dark|light)$")]
        public static partial Regex ThemeRegex();

        [GeneratedRegex("^.*=.*(\\d+)$")]
        public static partial Regex IntRegex();


        //[GeneratedRegex("^@Override\\s+(?<PropertyName>ForegroundColor|SuccessColor|WarningColor|ErrorColor|HighlightBackground|HighlightForeground|AccentColor)\\s*=\\s*(?:(?<Hex>#[A-Fa-f0-9]+)|(?<RGB>RGB\\((?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d)\\))|(?<XTerm>[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}))$")]
        [GeneratedRegex(
            "^@Override\\s+" +
            "(?<PropertyName>" +
                "ForegroundColor|SuccessColor|WarningColor|ErrorColor|" +
                "HighlightBackground|HighlightForeground|AccentColor" +
            ")\\s*=\\s*" +
            "(?:" +
                "(?<Hex>#[A-Fa-f0-9]+)" +
                "|" +
                "(?<RGB>RGB\\(" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d)" +
                "\\))" +
                "|" +
                "(?<XTerm>[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4})" +
            ")$"
        )]
        public static partial Regex OverrideRegex();

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
            if (line.Contains(" ; "))
            {
                int index = line.IndexOf(" ; ");
                return line[..index];
            }
            return line;
        }
    }
    
    public readonly struct ConfigOverrideResult
    {
        public string PropertyName { get; init; }
        public string ColorType { get; init; }
        public string ColorValue { get; init; }
    }

    public class ConfigManager()
    {
        public static Config GlobalConfig { get; set; } = new()
        {
            AutoCopyPath = false,
            RunOnCompile = false,
            ShowCpuCheck = true,
            ShowMemoryCheck = true,
            ShowUpdateCheck = true,
            ThemeType = ThemeManager.DefaultTheme,
            UseBrowserstack = false,
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
                    KeyValuePair.Create("use_browserstack", "false"),
                }
            },
            {
                "[overrides]", new List<KeyValuePair<string, string>>()
                // This section uses a seperate syntax for @Override, it's handled in LoadConfig();
            },
        };

        private static string ConfigDirectory { get; set; } = DirectoryManager.GetBAMConfigDirectory();
        public static string ConfigFilePath { get; private set; } = Path.Combine(ConfigDirectory, "config.ini");

        public static readonly BindingFlags BindingAttr = BindingFlags.Instance | BindingFlags.Public;
        
        private static string BuildConfigContents()
        {
            var configContents = new StringBuilder();
            var sortedSections = rawSections.OrderBy(s => s.Key);

            foreach (var sectionEntry in sortedSections)
            {
                var sectionName = sectionEntry.Key;
                var properties = sectionEntry.Value;

                configContents.AppendLine($"{sectionName}");

                if (sectionName != "[overrides]")
                {
                    foreach (var property in properties)
                    {
                        configContents.AppendLine($"{property.Key} = {property.Value}");
                    }
                }
                else
                {
                    configContents.AppendLine("; Only use this if you experience issues with one of the existing themes.");
                    configContents.AppendLine();
                    configContents.AppendLine(" ; @Override ForegroundColor = #FFFFFF");
                    configContents.AppendLine(" ; @Override SuccessColor = #EEEEEE");
                    configContents.AppendLine(" ; @Override WarningColor = #CCCCCC");
                    configContents.AppendLine(" ; @Override ErrorColor = #AAAAAA");
                    configContents.AppendLine(" ; @Override HighlightBackground = #010101");
                    configContents.AppendLine(" ; @Override HighlightForeground = #020202");
                    configContents.AppendLine(" ; @Override AccentColor = #040404");
                    configContents.AppendLine();
                    configContents.AppendLine(" ; @Override ForegroundColor = #FFFFFF");
                    configContents.AppendLine(" ; @Override SuccessColor = #EEEEEE");
                    configContents.AppendLine(" ; @Override WarningColor = #CCCCCC");
                    configContents.AppendLine(" ; @Override ErrorColor = #AAAAAA");
                    configContents.AppendLine(" ; @Override HighlightBackground = #010101");
                    configContents.AppendLine(" ; @Override HighlightForeground = #020202");
                    configContents.AppendLine(" ; @Override AccentColor = #040404");
                    configContents.AppendLine();
                    configContents.AppendLine(" ; @Override ForegroundColor = RGB(10, 10, 0)");
                    configContents.AppendLine(" ; @Override SuccessColor = RGB(20, 20, 20)");
                    configContents.AppendLine(" ; @Override WarningColor = RGB(30, 30, 30)");
                    configContents.AppendLine(" ; @Override ErrorColor = RGB(40, 40, 40)");
                    configContents.AppendLine(" ; @Override HighlightBackground = RGB(50, 50, 50)");
                    configContents.AppendLine(" ; @Override HighlightForeground = RGB(60, 60, 60)");
                    configContents.AppendLine(" ; @Override AccentColor = RGB(70, 70, 70)");
                    configContents.AppendLine(" ; @Override ForegroundColor = FFFF/FFFF/FFFF");
                    configContents.AppendLine();
                    configContents.AppendLine(" ; @Override SuccessColor = 0000/0000/0000");
                    configContents.AppendLine(" ; @Override WarningColor = 0000/0000/0000");
                    configContents.AppendLine(" ; @Override ErrorColor = 0000/0000/0000");
                    configContents.AppendLine(" ; @Override HighlightBackground = 0000/0000/0000");
                    configContents.AppendLine(" ; @Override HighlightForeground = 0000/0000/0000");
                    configContents.AppendLine(" ; @Override AccentColor = 0000/0000/0000");

                }

                // Adds trailing newline to all sections except the final one
                if (sectionEntry.Key != sortedSections.Last().Key) {
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

                var exc = new ArgumentException($"Theme '{value}' not found in ThemeManager (expected field '{themeName}').");
                FieldInfo? field = typeof(ThemeManager).GetField(themeName, bindingAttr) ?? throw exc;
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
        private static void EnsureConfigExists()
        {
            if (!ConfigDirectoryExists())
            {
                try
                {
                    Directory.CreateDirectory(ConfigDirectory);
                }
                catch (Exception ex)
                {
                    Errors.WriteAndExit(
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
                    Errors.WriteAndExit(
                        "Failed to create config file.\n'" +
                        $"{ConfigFilePath}'\nError: {ex.Message}\n" +
                        $"Please make a bug report at {ISSUES_LINK}",
                        status: 1
                    );
                }
            }

            try
            {
                string configContents = File.ReadAllText(ConfigFilePath, Encoding.UTF8);
                ValidateConfigContents(configContents);
            }
            catch (Exception ex)
            {
                WriteMessage(ex.Message);
                Errors.Write("Failed to validate config.ini, writing default values.");
                string configContents = BuildConfigContents();
                ValidateConfigContents(configContents);
                File.WriteAllText(ConfigFilePath, configContents); // Fixed: was ConfigDirectory, should be ConfigFilePath
            }
        }
        private static string[]? GetPartsOfLine(string trimmedLine, string originalLine)
        {
            // Learned about spans and now I feel the need to refactor all string usage to ReadonlySpan<char>
            // Will be added to a future release
            if (OverrideLineHasComment(originalLine)) {
                return null; // Returning null means the line was skipped this is intended.
            }

            ReadOnlySpan<char> span = trimmedLine.AsSpan();
            string[] parts;

            // Skips "@Override " as its only needed to identify the command type.
            if (span.StartsWith("@Override "))
            {
                parts = [.. trimmedLine[10..].Split('=').Select(part => part.Trim())];
                return parts;
            }

            int index = trimmedLine.IndexOf('=');
            if (index > 0)
                parts = [trimmedLine[..index], trimmedLine[(index + 1)..]];
            else
                parts = [trimmedLine];
            return parts;
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
                string trimmedLine = ConfigParser.RemoveCommentIfPresent(originalLine.Replace('\r', ' ').Trim());
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                // Handles section headers
                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']')) {
                    currentSection = trimmedLine;
                    continue;
                }

                if (currentSection == null) 
                    continue;

                // Handles overrides (if present)
                if (currentSection == "[overrides]")
                {
                    if (OverrideLineHasComment(trimmedLine)) 
                        continue;

                    ProcessOverrideLine(trimmedLine, originalLine, splitLines);
                }

                // Handles regular config properties
                else {
                    ProcessConfigProperty(trimmedLine, originalLine, splitLines);
                }
            }
            return GlobalConfig;
        }
        private static void LogOverrideError(string originalLine, string[] splitLines)
        {
            string message = "\nInvalid override format. Expected '@Override PropertyName = value' " +
                            "where PropertyName is one of the supported color properties and value is a valid color format.\n\n" +
                            "For more information please check {}";

            Errors.Write(
                Errors.GenerateErrorMessage(
                    fileName: "config.ini",
                    originalLine,
                    lineNumber: Array.IndexOf(splitLines, originalLine) + 1,
                    issueText: message
                )
            );
        }
        private static void ProcessConfigProperty(string trimmedLine, string originalLine, string[] splitLines)
        {
            string[] parts = trimmedLine.Split('=', 2);
            if (parts.Length != 2) return;

            var propName = ConfigParser.ConvertSnakeToPascal(parts[0].Trim());
            var propValue = parts[1].Trim();

            var lineNumber = Array.IndexOf(splitLines, originalLine) + 1;
            var property = typeof(Config).GetProperty(propName, BindingAttr);

            if (property == null)
            {
                Errors.WriteAndExit(
                    Errors.GenerateErrorMessage(
                        fileName: "config.ini",
                        originalLine,
                        lineNumber,
                        issueText: $"Property '{propName}' not found in Config class."
                    ),
                    status: 1
                );
            }

            try
            {
                var castedValue = DoCast(propValue, property.PropertyType);
                if (castedValue == null)
                {
                    Errors.WriteAndExit(
                        Errors.GenerateErrorMessage(
                            fileName: "config.ini",
                            originalLine,
                            lineNumber,
                            $"Failed to convert value '{propValue}' for property '{propName}'."),
                        status: 1);
                }
                property.SetValue(GlobalConfig, castedValue);
            }
            catch (Exception ex)
            {
                Errors.WriteAndExit(
                    Errors.GenerateErrorMessage(
                        "config.ini",
                        originalLine,
                        lineNumber,
                        $"Failed to convert value '{propValue}' for property '{propName}': {ex.Message}"),
                    status: 1);
            }
        }
        private static void ProcessOverrideLine(string trimmedLine, string originalLine, string[] splitLines)
        {
            var result = ParseOverrideLine(trimmedLine);
            if (!result.HasValue)
            {
                LogOverrideError(originalLine, splitLines);
                return;
            }

            var exc = new ArgumentException($"Theme class does not contain a property '{result.Value.PropertyName}'.");
            var property = typeof(Theme).GetProperty(result.Value.PropertyName, BindingAttr) ?? throw exc;
            var value = ToSpectreColor(result.Value.ColorType, result.Value.ColorValue);

            if (value != null) {
                property.SetValue(GlobalConfig.ThemeType, ToColor(value.Value));
            }
        }
        public static bool OverrideLineHasComment(string line)
        {
            bool hasComment = false;
            char commentChar = ';';
            if (line == null) { return true; } // If a line is null returning true will have it skipped.
            foreach (char c in line)
            {
                if (char.IsWhiteSpace(c)) { continue; }
                if (c.Equals(commentChar)) {
                    hasComment = true;
                    break;
                }
            }
            return hasComment;
        } 
        private static ConfigOverrideResult? ParseOverrideLine(string line)
        {
            var match = ConfigParser.OverrideRegex().Match(line); // Will fail if a line has a comment (This is expected)
            if (!match.Success)
                return null;

            string propertyName = match.Groups["PropertyName"].Value;

            if (match.Groups["Hex"].Success)
                return new ConfigOverrideResult() { 
                    PropertyName = propertyName, 
                    ColorType = "Hex", 
                    ColorValue = match.Groups["Hex"].Value 
                };
            
            else if (match.Groups["RGB"].Success)
                return new() { 
                    PropertyName = propertyName, 
                    ColorType = "RGB", 
                    ColorValue = match.Groups["RGB"].Value 
                };

            else if (match.Groups["XTerm"].Success)
                return new() { 
                    PropertyName = propertyName, 
                    ColorType = "XTerm", 
                    ColorValue = match.Groups["XTerm"].Value 
                };

            return null;
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

                if (string.IsNullOrWhiteSpace(trimmedLine)) { 
                    continue; 
                }

                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                {
                    string sectionName = trimmedLine;

                    if (!rawSections.ContainsKey(sectionName))
                    {
                        Errors.WriteAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
                                lineNumber: i + 1, // Fixed: line numbers should be 1-based
                                issueText: $"Unknown section detected: `{sectionName}` is not a valid section."
                            ),
                            status: 1
                        );
                    }

                    if (encounteredSections.Contains(sectionName))
                    {
                        Errors.WriteAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
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
                        Errors.WriteAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
                                lineNumber: i + 1,
                                issueText: "Content found before any section header. All configuration must be within a section."
                            ),
                            status: 1
                        );
                    }

                    var parts = GetPartsOfLine(trimmedLine, originalLine);

                    if (parts == null) {
                        continue; // Returning null means the line was skipped this is intended.
                    }


                    if (parts.Length != 2)
                    {
                        Errors.WriteAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
                                lineNumber: i + 1,
                                issueText: "Invalid property format, expected 'name = value'."

                            ),
                            status: 1
                        );

                    }


                    string propName = parts[0].Trim();
                    string propValue = parts[1].Trim();

                    // Debug only
                    //Console.WriteLine(currentSection);
                    //Console.WriteLine(propName);
                    //Console.WriteLine(propValue);
                    //Console.WriteLine();

                    if (!rawSections[currentSection].Any(pair => pair.Key.Equals(propName))) // Handles all sections but overrides
                    {
                        
                        if (typeof(Theme).GetProperty(propName, BindingAttr)?.GetValue(GlobalConfig.ThemeType) == null) // Handles overrides
                        {
                            Errors.WriteAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    originalLine,
                                    lineNumber: i + 1,
                                    issueText: $"Unknown property `{propName}` in section `{currentSection}`."
                                ),
                                status: 1
                            );
                        }
                    }

                    if (propsAndFuncs.TryGetValue(propName, out Regex? func))
                    {
                        if (!ConfigParser.IsValidLine(trimmedLine, func))
                        {
                            Errors.WriteAndExit(
                                Errors.GenerateErrorMessage(
                                    fileName: "config.ini",
                                    originalLine,
                                    lineNumber: i + 1,
                                    issueText: $"Invalid value '{propValue}' for property `{propName}`."
                                ),
                                status: 1
                            );
                        }
                    }

                    else if (currentSection == "[overrides]") { 
                        continue; 
                    }
                    
                    else
                    {
                        Errors.WriteAndExit(
                            Errors.GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
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