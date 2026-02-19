using BrowserAutomationMaster.Managers.Utilities;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.Common.ANSI;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.Common.RegexManager;
using static BrowserAutomationMaster.Managers.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public partial class AppSettings
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

    public partial class SettingNameParser
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
    
    public readonly struct SettingOverrideResult
    {
        public string PropertyName { get; init; }
        public string ColorType { get; init; }
        public string ColorValue { get; init; }
    }

    public class Settings()
    {
        public static AppSettings GlobalSettings { get; set; } = new()
        {
            AutoCopyPath = false,
            RunOnCompile = false,
            ShowAppCheck = false,
            ShowCpuCheck = true,
            ShowMemoryCheck = true,
            ShowUpdateCheck = true,
            ThemeType = ThemeUtility.DefaultTheme,
            UseBrowserstack = false,
        };

        private static readonly Dictionary<string, Dictionary<string, string>> rawSections = new()
        {
            {
                "[messaging]", new()
                {
                    { "show_app_check", "false" },
                    { "show_cpu_check", "true" },
                    { "show_memory_check", "true" },
                    { "show_update_check", "false" },
                }
            },

            {
                "[interface]", new()
                {
                    { "theme_type", "dark" }, // Also supports "light"
                }
            },

            {
                "[compilation]", new()
                {
                    { "auto_copy_path", "false" },
                    { "run_on_compile", "false" },
                    { "use_browserstack", "false" },
                }
            },

            // This section uses a seperate syntax for @Override, it's handled in LoadConfig();
            {
                "[overrides]", new()
            },
        };
        
        public static string SettingsFilePath { get; private set; } = Path.Combine(AppDataDirectory, "settings.ini");

        public static readonly BindingFlags BindingAttr = BindingFlags.Instance | BindingFlags.Public;
        
        private static string BuildFileContent()
        {
            var settingsContents = new StringBuilder();
            var sortedSections = rawSections.OrderBy(s => s.Key);

            foreach (var sectionEntry in sortedSections)
            {
                var sectionName = sectionEntry.Key;
                var properties = sectionEntry.Value;

                settingsContents.AppendLine($"{sectionName}");

                if (sectionName != "[overrides]") {
                    foreach (var property in properties) {
                        settingsContents.AppendLine($"{property.Key} = {property.Value}");
                    }
                    continue; // This continue statement negates the requirement for an else block below.
                }

                settingsContents.AppendLine("; Only use this if you experience issues with one of the existing themes.");
                settingsContents.AppendLine();
                settingsContents.AppendLine(" ; @Override ForegroundColor = #FFFFFF");
                settingsContents.AppendLine(" ; @Override SuccessColor = #EEEEEE");
                settingsContents.AppendLine(" ; @Override WarningColor = #CCCCCC");
                settingsContents.AppendLine(" ; @Override ErrorColor = #AAAAAA");
                settingsContents.AppendLine(" ; @Override HighlightBackground = #010101");
                settingsContents.AppendLine(" ; @Override HighlightForeground = #020202");
                settingsContents.AppendLine(" ; @Override AccentColor = #040404");
                settingsContents.AppendLine();
                settingsContents.AppendLine(" ; @Override ForegroundColor = RGB(10, 10, 0)");
                settingsContents.AppendLine(" ; @Override SuccessColor = RGB(20, 20, 20)");
                settingsContents.AppendLine(" ; @Override WarningColor = RGB(30, 30, 30)");
                settingsContents.AppendLine(" ; @Override ErrorColor = RGB(40, 40, 40)");
                settingsContents.AppendLine(" ; @Override HighlightBackground = RGB(50, 50, 50)");
                settingsContents.AppendLine(" ; @Override HighlightForeground = RGB(60, 60, 60)");
                settingsContents.AppendLine(" ; @Override AccentColor = RGB(70, 70, 70)");
                settingsContents.AppendLine(" ; @Override ForegroundColor = FFFF/FFFF/FFFF");
                settingsContents.AppendLine();
                settingsContents.AppendLine(" ; @Override SuccessColor = 0000/0000/0000");
                settingsContents.AppendLine(" ; @Override WarningColor = 0000/0000/0000");
                settingsContents.AppendLine(" ; @Override ErrorColor = 0000/0000/0000");
                settingsContents.AppendLine(" ; @Override HighlightBackground = 0000/0000/0000");
                settingsContents.AppendLine(" ; @Override HighlightForeground = 0000/0000/0000");
                settingsContents.AppendLine(" ; @Override AccentColor = 0000/0000/0000");

                // Adds trailing newline to all sections except the final one
                if (sectionEntry.Key != sortedSections.Last().Key) {
                    settingsContents.AppendLine();
                }
            }

            return settingsContents.ToString();
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
                var field = typeof(ThemeUtility).GetField(themeName, bindingAttr) ?? throw exc;
                return field.GetValue(null);
            }

            else if (targetType.IsEnum) {
                return Enum.Parse(targetType, value, ignoreCase: true);
            }

            throw new InvalidCastException(
                $"Cannot convert value '{value}' to type '{targetType.Name}', as it's currently not supported.{NLC}" +
                "Please add this feature in ConfigManager.DoCast()");
        }

        private static void EnsureSettingsExist()
        {
            if (!SettingsFileExists())
            {
                try
                {
                    string settingsContents = BuildFileContent();
                    ValidateConfigContents(settingsContents);
                    File.WriteAllText(SettingsFilePath, settingsContents);
                    return;
                }

                catch (Exception ex)
                {
                    WriteAndExit(
                        string.Join(NLC, [
                            "Failed to create config file at:",
                            SettingsFilePath,
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
                string settingsContents = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
                ValidateConfigContents(settingsContents);
            }
            catch (Exception ex)
            {
                WriteMessage(ex.Message);
                Write("Failed to validate config.ini, writing default values.");
                string settingsContents = BuildFileContent();
                ValidateConfigContents(settingsContents);
                File.WriteAllText(SettingsFilePath, settingsContents); // Fixed: was ConfigDirectory, should be SettingsFilePath
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
                    [.. 
                        trimmedLine[tagLength..] // Removes the overrideTag
                        .Split('=') // Splits the line into PropertyName, PropertyValue.
                        .Select(part => part.Trim())
                    ];
            }

            int index = trimmedLine.IndexOf('=');
            
            // Returns the parts of the lines if the index is greater than one, or the whole line if not
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
                        propsAndFuncs.Add(propKvp.Key, ThemeRegex());
                    }

                    else if (bool.TryParse(propKvp.Value, out bool _)) {
                        propsAndFuncs.Add(propKvp.Key, BoolRegex());
                    }

                    // Add support for integer properties if needed
                    else if (int.TryParse(propKvp.Value, out int _)) {
                        propsAndFuncs.Add(propKvp.Key, IntRegex());
                    }
                }
            }
            return propsAndFuncs;
        }

        public static AppSettings Load()
        {
            EnsureSettingsExist();

            var settingsContents = File.ReadAllText(SettingsFilePath, Encoding.UTF8);
            string? currentSection = null;
            var splitLines = settingsContents.Split('\n');

            foreach (string originalLine in splitLines)
            {
                string trimmedLine = SettingNameParser.RemoveCommentIfPresent(originalLine.Replace('\r', ' ').Trim());
                
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
            return GlobalSettings;
        }

        private static void LogOverrideError(string originalLine, string[] splitLines)
        {
            string message = string.Join(NLC, [
                "", // Leading NLC char inserted here
                "Invalid override format.",
                "Expected: '@Override PropertyName = value'",
                $"Where PropertyName is one of the supported color properties and value is a valid color format." +
                $"For more information please visit {DOCUMENTATION_LINK}"
            ]);

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
        private static SettingOverrideResult? ParseOverrideLine(string line)
        {
            var match = OverrideRegex().Match(line); // Will fail if a line has a comment (This is expected)
            if (!match.Success) {
                return null;
            }

            string propertyName = match.Groups["PropertyName"].Value;

            if (match.Groups["Hex"].Success) {
                return new SettingOverrideResult() { 
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

        private static void ProcessConfigProperty(string trimmedLine, string originalLine, string[] splitLines)
        {
            string[] parts = trimmedLine.Split('=', 2);
            if (parts.Length != 2) { 
                return;
            }

            var propName = SettingNameParser.ConvertSnakeToPascal(parts[0].Trim());
            var propValue = parts[1].Trim();

            var lineNumber = Array.IndexOf(splitLines, originalLine) + 1;
            var property = typeof(AppSettings).GetProperty(propName, BindingAttr);

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
                property.SetValue(GlobalSettings, castedValue);
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
                property.SetValue(GlobalSettings.ThemeType, ToColor(value.Value));
            }
        }

        private static bool SettingsFileExists()
        {
            return !string.IsNullOrEmpty(SettingsFilePath) && File.Exists(SettingsFilePath);
        }

        private static void ValidateConfigContents(string settingsContents)
        {
            var splitLines = settingsContents.Split('\n');
            string? currentSection = null;
            var encounteredSections = new HashSet<string>();
            var propsAndFuncs = GetPropsAndFuncs();

            for (int i = 0; i < splitLines.Length; i++)
            {
                string originalLine = splitLines[i];
                string trimmedLine = SettingNameParser.RemoveCommentIfPresent(
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
                var invalidThemeType = typeof(Theme).GetProperty(propName, BindingAttr)?.GetValue(GlobalSettings.ThemeType) == null;

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

        private static void ValidateConfigContents(FileStream settingsContents)
        {

        }
    }

     // Since System.Globalization.TextInfo.ToTitleCase is more complicated than required
    public static class StringExtensions 
    {
        public static string ToTitle(this string value) => string.Concat(char.ToUpper(value[0]), value[1..]);
        public static string ToTitle(this string[] values) => string.Concat(
            values.Select(val => val.ToTitle())
        );    
    }
}