using Spectre.Console;
using System.Text;
using static BrowserAutomationMaster.Managers.ConfigManager;

namespace BrowserAutomationMaster.Managers
{
    public static class AnsiManager
    {
        public static void WriteMessage(string message, bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            SetAnsiColors(isSuccess, isWarning, isError);
            // Handle line breaks by padding each line to fill the background
            var lines = message.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Equals("\n")) { continue; }
                var paddedLine = line.PadRight(Console.WindowWidth);

                AnsiConsole.Write(paddedLine);
                if (i < lines.Length){
                    AnsiConsole.WriteLine();
                }
            }
        }
        public static void SetAnsiColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            Color oldFG = AnsiConsole.Foreground;
            var newFG = GetForeground(isSuccess, isWarning, isError);
            if (!oldFG.Equals(newFG)) {
                AnsiConsole.Foreground = newFG;
            }
        }
        public static Color GetAccentColor()
        {
            return ToSpectreColor(GlobalConfig.ThemeType.AccentColor);
        }
        public static Color GetForeground(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            return (isSuccess, isWarning, isError) switch
            {
                (true, false, false) => ToSpectreColor(GlobalConfig.ThemeType.SuccessColor),
                (false, true, false) => ToSpectreColor(GlobalConfig.ThemeType.WarningColor),
                (false, false, true) => ToSpectreColor(GlobalConfig.ThemeType.ErrorColor),
                _ => ToSpectreColor(GlobalConfig.ThemeType.ForegroundColor)
            };
        }
        public static (Color HighlightBackground, Color HighlightForeground) GetHighlights()
        {
            return (
                ToSpectreColor(GlobalConfig.ThemeType.HighlightBackground), 
                ToSpectreColor(GlobalConfig.ThemeType.HighlightForeground)
            );
        }

        public static Style GetStyle(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            var newFG = GetForeground(isSuccess, isWarning, isError);
            return new Style(
                foreground: newFG
            );
        }
        public static string? ReadKey()
        {
            var keyInfo = AnsiConsole.Console.Input.ReadKey(true);
            if (keyInfo == null) { return null; }
            return SanitizeNumericValue(keyInfo.Value.Key.ToString());
        }
        public static string SanitizeNumericValue(string value)
        {
            var result = new StringBuilder();
            for (var i = 0; i < value.Length - 1; i++)
            {
                if (value[i] == 'D' && char.IsDigit(value[i + 1]))
                {
                    result.Append(value[i + 1]);
                    i++;
                    continue;
                }
                result.Append(value[i]);
                i++;
                
            }

            return result.Length > 0 ? result.ToString() : value;
        }
        public static Color ToSpectreColor(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B);
        }
        public static Color ToSpectreColor(byte r, byte g, byte b)
        {
            return new Color(r, g, b);
        }
        public static (int r, int g, int b) ToRGB(string color12bit) // Accepts XXXX/XXXX/XXXX
        {
            var parts = color12bit.Split('/');
            if (parts.Length != 3)
                throw new ArgumentException("Invalid 12-bit color format");

            // Convert from 16-bit (0-65535) to 8-bit (0-255)
            int r = Convert.ToInt32(parts[0], 16) / 257; // 65535 / 255 = 257
            int g = Convert.ToInt32(parts[1], 16) / 257;
            int b = Convert.ToInt32(parts[2], 16) / 257;

            return (r, g, b);
        }
    }
}

