using BrowserAutomationMaster.Messaging;
using System.Drawing;
using System.Text;

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

                Spectre.Console.AnsiConsole.Write(paddedLine);
                //Spectre.Console.AnsiConsole.Write(line);
                if (i < lines.Length){
                    Spectre.Console.AnsiConsole.WriteLine();
                }
            }
        }
        public static void SetAnsiColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            Spectre.Console.Color oldBG = Spectre.Console.AnsiConsole.Background;
            Spectre.Console.Color oldFG = Spectre.Console.AnsiConsole.Foreground;

            var (newBG, newFG) = GetColors(isSuccess, isWarning, isError);
            
            if (!oldBG.Equals(newBG)) {
                Spectre.Console.AnsiConsole.Background = ToSpectreColor(newBG);
            }

            if (!oldFG.Equals(newFG)) {
                Spectre.Console.AnsiConsole.Foreground = ToSpectreColor(newFG);
            }
        }
        private static (Color newFG, Color newBG) GetColors(bool isSuccess = false, bool isWarning = false, bool isError = false)
        {
            Color newBackgroundColor;
            Color newForegroundColor;
            if (ConfigManager.GlobalConfig != null)
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
                (true, false, false) => ConfigManager.GlobalConfig!.ThemeType.SuccessColor,
                (false, true, false) => ConfigManager.GlobalConfig!.ThemeType.WarningColor,
                (false, false, true) => ConfigManager.GlobalConfig!.ThemeType.ErrorColor,
                _ => ConfigManager.GlobalConfig!.ThemeType.ForegroundColor
            };
            return (newBackgroundColor, newForegroundColor);

        }

        public static Spectre.Console.Color ToSpectreColor(Color color)
        {
            return new Spectre.Console.Color(color.R, color.G, color.B);
        }
        public static string? ReadKey()
        {
            var keyInfo = Spectre.Console.AnsiConsole.Console.Input.ReadKey(true);
            if (keyInfo == null) { return null; }
            return SanitizeNumericValue(keyInfo.Value.Key.ToString());
        }
        public static string ReadLine()
        {
            var builder = new StringBuilder();
            while (true)
            {
                var keyInfo = Spectre.Console.AnsiConsole.Console.Input.ReadKey(false);
                if (keyInfo == null) { 
                    return string.Empty; 
                }
                if (keyInfo.Value.Key.Equals(ConsoleKey.Enter)) {
                    break;
                }
                var keyString = SanitizeNumericValue(keyInfo.Value.Key.ToString());
                builder.AppendLine(keyString);
            }
            return builder.ToString();
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
    }
}

