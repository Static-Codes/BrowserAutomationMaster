using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Text;
using static BrowserAutomationMaster.Managers.ConfigManager;

namespace BrowserAutomationMaster.Managers
{
    public static class AnsiManager
    {
        public static (int r, int g, int b) FromXTerm(string Ansi24bit) // Accepts XXXX/XXXX/XXXX
        {
            var parts = Ansi24bit.Split('/');
            if (parts.Length != 3)
                throw new ArgumentException($"Invalid Ansi 24-bit color '{Ansi24bit}', expected format: 'XXXX/XXXX/XXXX'");

            // Convert from 16-bit (0-65535) to 8-bit (0-255)
            int r = Convert.ToInt32(parts[0], 16) / 257; // 65535 / 255 = 257
            int g = Convert.ToInt32(parts[1], 16) / 257;
            int b = Convert.ToInt32(parts[2], 16) / 257;

            return (r, g, b);
        }
        public static Color? FromRGB(string rgbString)
        {
            if (string.IsNullOrEmpty(rgbString)) { 
                return null; 
            }

            rgbString = rgbString.Replace("RGB(", "").Replace(')', ' ').Trim();
            var parts = rgbString.Split(", ");
            
            if (parts.Length != 3){
                return null;
            }

            var bytes = new byte[parts.Length];
            
            for (int i = 0; i < parts.Length; i++)
            {
                if (!byte.TryParse(parts[i], out var byteRes)){
                    return null;
                }
                bytes[i] = byteRes;
            }

            return new Color(bytes[0], bytes[1], bytes[2]);
        }
        public static Color GetAccentColor() {
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
        public static void SetAnsiColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            Color oldFG = AnsiConsole.Foreground;
            var newFG = GetForeground(isSuccess, isWarning, isError);
            if (!oldFG.Equals(newFG))
            {
                AnsiConsole.Foreground = newFG;
            }
        }
        public static System.Drawing.Color ToColor(Color color)
        {
            return System.Drawing.Color.FromArgb(color.R, color.G, color.B);
        }

        public static Color ToSpectreColor(byte r, byte g, byte b) { 
            return new Color(r, g, b); 
        }
        public static Color ToSpectreColor(System.Drawing.Color color) { 
            return new Color(color.R, color.G, color.B); 
        }
        public static Color ToSpectreColor((int r, int g, int b) color) { 
            return new Color((byte) color.r, (byte) color.g, (byte) color.b); 
        }
        public static Color? ToSpectreColor(string colorType, string colorValue)
        {
            return colorType switch
            {
                "Hex" => Color.FromHex(colorValue),
                "RGB" => FromRGB(colorValue) ?? throw new Exception(""),
                "ANSI" => ToSpectreColor(FromXTerm(colorValue)),
                _ => null
            };
        }
        public static void WriteMessage(string message, bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            SetAnsiColors(isSuccess, isWarning, isError);

            var lines = message.Split('\n');

            // Handles line breaks by padding each line to fill the background
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Equals("\n")) { continue; }
                var paddedLine = line.PadRight(Console.WindowWidth);

                AnsiConsole.Write(paddedLine);
                if (i < lines.Length)
                {
                    AnsiConsole.WriteLine();
                }
            }
        }


    }
}

