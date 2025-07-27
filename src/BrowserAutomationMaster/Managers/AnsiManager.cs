using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                //Spectre.Console.AnsiConsole.Write(line);
                if (i < lines.Length){
                    AnsiConsole.WriteLine();
                }
            }
        }
        public static void SetAnsiColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            Color oldBG = AnsiConsole.Background;
            Color oldFG = AnsiConsole.Foreground;

            var (newBG, newFG) = GetColors(isSuccess, isWarning, isError);
            
            if (!oldBG.Equals(newBG)) {
                AnsiConsole.Background = ToSpectreColor(newBG);
            }

            if (!oldFG.Equals(newFG)) {
                AnsiConsole.Foreground = ToSpectreColor(newFG);
            }
        }
        public static (System.Drawing.Color newFG, System.Drawing.Color newBG) GetColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            System.Drawing.Color newBackgroundColor;
            System.Drawing.Color newForegroundColor;
            if (ConfigManager.GlobalConfig != null && ConfigManager.GlobalConfig.ThemeType != null)
            {

                newBackgroundColor = ConfigManager.GlobalConfig!.ThemeType.BackgroundColor;

                newForegroundColor = (isSuccess, isWarning, isError) switch
                {
                    (true, false, false) => ConfigManager.GlobalConfig!.ThemeType.SuccessColor,
                    (false, true, false) => ConfigManager.GlobalConfig!.ThemeType.WarningColor,
                    (false, false, true) => ConfigManager.GlobalConfig!.ThemeType.ErrorColor,
                    _ => ConfigManager.GlobalConfig!.ThemeType.ForegroundColor
                };
                return (newBackgroundColor, newForegroundColor);
            }

            newBackgroundColor = ThemeManager.DefaultTheme.BackgroundColor;

            newForegroundColor = (isSuccess, isWarning, isError) switch
            {
                (true, false, false) => ThemeManager.DefaultTheme.SuccessColor,
                (false, true, false) => ThemeManager.DefaultTheme.WarningColor,
                (false, false, true) => ThemeManager.DefaultTheme.ErrorColor,
                _ => ThemeManager.DefaultTheme.ForegroundColor
            };
            return (newBackgroundColor, newForegroundColor);

        }

        public static Color ToSpectreColor(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B);
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

        public static Style GetStyle(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            var (newBG, newFG) = GetColors(isSuccess, isWarning, isError);
            return new Style(
                foreground: ToSpectreColor(newFG),
                background: ToSpectreColor(newBG)
            );
        }
    }
}

