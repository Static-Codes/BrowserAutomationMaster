using BrowserAutomationMaster.Managers.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public partial class Config
    {
        public required Theme ThemeType { get; set; }
        public bool ShowAppCheck { get; set; }
        public bool ShowCpuCheck { get; set; }
        public bool ShowMemoryCheck { get; set; }
        public bool ShowUpdateCheck { get; set; }
        public bool AutoCopyPath { get; set; }
        public bool RunOnCompile { get; set; }
        public bool UseBrowserstack { get; set; }
    }

    public static class StringExtensions {
        public static string ToTitle(this string value) => string.Concat(char.ToUpper(value[0]), value[1..]);
        public static string ToTitle(this string[] values) => string.Concat(
            values.Select(val => val.ToTitle())
        );    
    }

    public partial class ConfigParser
    {
        public static string ConvertSnakeToPascal(string snake_case) => snake_case.Split('_').ToTitle();
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
            ShowAppCheck = false,
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
                    KeyValuePair.Create("show_app_check", "false"),
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

                if (sectionName != "[overrides]") {
                    foreach (var property in properties) {
                        configContents.AppendLine($"{property.Key} = {property.Value}");
                    }
                    continue; // This continue statement negates the requirement for an else block below.
                }

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
            if (targetType == typeof(bool)) {
                return bool.Parse(value);
            }

            else if (targetType == typeof(int)) {
                return int.Parse(value);
            }

            else if (targetType == typeof(string)) {
                return value;
            }

            else if (targetType == typeof(Theme)) {
                var themeName = value.ToTitle() + "Theme";
                var bindingAttr = BindingFlags.Public | BindingFlags.Static;
                
                var exc = new ArgumentException($"Theme '{value}' not found in ThemeManager (expected field '{themeName}').");
                var field = typeof(ThemeManager).GetField(themeName, bindingAttr) ?? throw exc;
                return field.GetValue(null);
            }

            else if (targetType.IsEnum) {
                return Enum.Parse(targetType, value, ignoreCase: true);
            }

            throw new InvalidCastException(
                $"Cannot convert value '{value}' to type '{targetType.Name}', as it's currently not supported.{NLC}" +
                "Please add this feature in ConfigManager.DoCast()");
        }

        private static void EnsureConfigExists()
        {
            if (!ConfigDirectoryExists())
            {
                try {
                    Directory.CreateDirectory(ConfigDirectory);
                }

                catch (Exception ex)
                {
                    WriteAndExit(
                        string.Join(NLC, [
                            "Failed to create config file at:",
                            ConfigFilePath,
                            $"Please make a bug report at {ISSUES_LINK}",
                            "Error Log:",
                            ex.Message,
                        ]),
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
                    WriteAndExit(
                        string.Join(NLC, [
                            "Failed to create config file at:",
                            ConfigFilePath,
                            $"Please make a bug report at {ISSUES_LINK}",
                            "Error Log:",
                            ex.Message,
                        ]),
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
                Write("Failed to validate config.ini, writing default values.");
                string configContents = BuildConfigContents();
                ValidateConfigContents(configContents);
                File.WriteAllText(ConfigFilePath, configContents); // Fixed: was ConfigDirectory, should be ConfigFilePath
            }
        }

        private static string[]? GetPartsOfLine(string trimmedLine, string originalLine)
        {
            if (OverrideLineHasComment(originalLine)) {
                return null; // Returning null means the line was skipped this is intended.
            }

            ReadOnlySpan<char> span = trimmedLine.AsSpan();

            var overrideTag = "@Override ";
            var tagLength = overrideTag.Length;

            // Skips "@Override " as its only needed to identify the command type.
            if (span.StartsWith(overrideTag)) 
            {
                return
                    trimmedLine[tagLength..] // Removes the overrideTag
                    .Split('=') // Splits the line into PropertyName, PropertyValue.
                    .Select(part => part.Trim())
                    .ToArray();
            }

            int index = trimmedLine.IndexOf('=');
            
            return index > 0 ? [trimmedLine[..index], trimmedLine[(index + 1)..]] : [trimmedLine];
        }

        private static Dictionary<string, Regex> GetPropsAndFuncs()
        {
            var propsAndFuncs = new Dictionary<string, Regex>();

            // Builds propsAndFuncs dynamically
            foreach (var sectionName in rawSections.Keys)
            {
                foreach (var propKvp in rawSections[sectionName])
                {
                    if (propKvp.Key.Equals("theme_type")) {
                        propsAndFuncs.Add(propKvp.Key, RegexManager.ThemeRegex());
                    }

                    else if (bool.TryParse(propKvp.Value, out bool _)) {
                        propsAndFuncs.Add(propKvp.Key, RegexManager.BoolRegex());
                    }

                    // Add support for integer properties if needed
                    else if (int.TryParse(propKvp.Value, out int _)) {
                        propsAndFuncs.Add(propKvp.Key, RegexManager.IntRegex());
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
                
                if (string.IsNullOrWhiteSpace(trimmedLine)) {
                    continue;
                }
                
                // Handles section headers
                if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']')) {
                    currentSection = trimmedLine;
                    continue;
                }

                if (currentSection == null) {
                    continue;
                }

                // Handles overrides (if present)
                if (currentSection == "[overrides]")
                {
                    if (OverrideLineHasComment(trimmedLine)) {
                        continue;
                    }

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
            string message = $"{NLC}Invalid override format. Expected '@Override PropertyName = value' " +
                            $"where PropertyName is one of the supported color properties and value is a valid color format.{NLC}{NLC}" +
                            "For more information please check {}";

            Write
            (
                GenerateErrorMessage(
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
            if (parts.Length != 2) { 
                return;
            }

            var propName = ConfigParser.ConvertSnakeToPascal(parts[0].Trim());
            var propValue = parts[1].Trim();

            var lineNumber = Array.IndexOf(splitLines, originalLine) + 1;
            var property = typeof(Config).GetProperty(propName, BindingAttr);

            if (property == null)
            {
                WriteAndExit(
                    GenerateErrorMessage(
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
                    WriteAndExit(
                        GenerateErrorMessage(
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
                WriteAndExit(
                    GenerateErrorMessage(
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
            if (!result.HasValue) {
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

            // If a line is null, it is treated as a comment, and skipped.
            if (line == null) { 
                return true; 
            }
            
            foreach (char c in line)
            {
                if (char.IsWhiteSpace(c)) {
                    continue;
                }

                if (c.Equals(commentChar)) {
                    hasComment = true;
                    break;
                }
            }

            return hasComment;
        } 
        private static ConfigOverrideResult? ParseOverrideLine(string line)
        {
            var match = RegexManager.OverrideRegex().Match(line); // Will fail if a line has a comment (This is expected)
            if (!match.Success) {
                return null;
            }

            string propertyName = match.Groups["PropertyName"].Value;

            if (match.Groups["Hex"].Success) {
                return new ConfigOverrideResult() { 
                    PropertyName = propertyName, 
                    ColorType = "Hex", 
                    ColorValue = match.Groups["Hex"].Value 
                };
            }
            
            else if (match.Groups["RGB"].Success) {
                return new() { 
                    PropertyName = propertyName, 
                    ColorType = "RGB", 
                    ColorValue = match.Groups["RGB"].Value 
                };
            }

            else if (match.Groups["XTerm"].Success) {
                return new() { 
                    PropertyName = propertyName, 
                    ColorType = "XTerm", 
                    ColorValue = match.Groups["XTerm"].Value 
                };
            }

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

                    if (!rawSections.ContainsKey(sectionName)) {
                        WriteAndExit(
                            GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
                                lineNumber: i + 1, // Fixed: line numbers should be 1-based
                                issueText: $"Unknown section detected: `{sectionName}` is not a valid section."
                            ),
                            status: 1
                        );
                    }

                    if (encounteredSections.Contains(sectionName)) {
                        WriteAndExit(
                            GenerateErrorMessage(
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
                    continue;
                }
                
                if (currentSection == null)
                {
                    WriteAndExit(
                        GenerateErrorMessage(
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
                    WriteAndExit(
                        GenerateErrorMessage(
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

                var invalidPropName = !rawSections[currentSection].Any(pair => pair.Key.Equals(propName));
                var invalidThemeType = typeof(Theme).GetProperty(propName, BindingAttr)?.GetValue(GlobalConfig.ThemeType) == null;

                // Invalid propertyNames
                if (invalidPropName && invalidThemeType) 
                {
                    WriteAndExit
                    (
                        GenerateErrorMessage(
                            fileName: "config.ini",
                            originalLine,
                            lineNumber: i + 1,
                            issueText: $"Unknown property `{propName}` in section `{currentSection}`."
                        ),
                        status: 1
                    );
                }
                    
                // Secondary check
                var validPropName = propsAndFuncs.TryGetValue(propName, out Regex? func);
                var validPropValue = func != null && func.IsMatch(trimmedLine);
                    
                switch (validPropName, validPropValue) 
                {
                    case (true, true):
                        continue;

                    case (true, false):
                        WriteAndExit
                        (
                            GenerateErrorMessage(
                                fileName: "config.ini",
                                originalLine,
                                lineNumber: i + 1,
                                issueText: $"Invalid value '{propValue}' for property `{propName}`."
                            ),
                            status: 1
                        );
                        break; // This isn't executed, rosyln is unaware that WriteAndExit [DoesNotReturn].
                }

                // Skipping validation on override sections.
                if (currentSection == "[overrides]") {
                    continue;
                }

                WriteAndExit(
                    GenerateErrorMessage(
                        fileName: "config.ini",
                        originalLine,
                        lineNumber: i + 1,
                        issueText: $"No validation rule found for property `{propName}` in section `{currentSection}`."
                    ),
                    status: 1
                );
            }
        }    

        private static void ValidateConfigContents(FileStream configContents)
        {

        }
    }
}