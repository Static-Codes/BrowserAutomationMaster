using BrowserAutomationMaster.Managers.Python;
using System.Drawing;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.Console;

namespace BrowserAutomationMaster.Managers
{
    public static class AnsiManager
    {
        public static bool HasAnsiSupport()
        {

            if (RuntimeManager.IsSupportedWindowsVersion())
            {
                var handle = PInvoke.GetStdHandle_SafeHandle(
                    STD_HANDLE.STD_OUTPUT_HANDLE
                );
                if (handle.IsInvalid || handle.IsClosed) { return false; }
                PInvoke.GetConsoleMode(handle, out CONSOLE_MODE mode);
                if ((mode & CONSOLE_MODE.ENABLE_VIRTUAL_TERMINAL_PROCESSING) != 0)
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    return true;
                }
            }

            string? term = Environment.GetEnvironmentVariable("TERM");
            if (string.IsNullOrEmpty(term)) { return false; }
            var sc = StringComparison.OrdinalIgnoreCase;
            if (term.Contains("xterm", sc) ||
                term.Contains("xterm-color", sc) ||
                term.Contains("xterm-256color", sc) ||
                term.Contains("screen", sc) ||
                term.Contains("tmux", sc))
            {
                return true;
            }

            return false;
        }
        public static class AnsiColor
        {
            // If no codes are given, CSI m is treated as CSI 0 m (reset / normal). 
            private const string ESC = "\x1b";  // (Escape Char Sequence)           Global prefix for the CSI command below.
            private const string CSI = "[";     // (Control Sequence Introducer)    Beginning of the CSI command.
            private const string FGI = "38";    // (Background Indicator)           Indicates the trailing parameters specify the background color.
            private const string BGI = "48";    // (Foreground Indicator)           Indicates the trailing parameters specify the foreground color. 
            private const string CSP = "2";     // (ColorSpace Parameter)           Indicates the ColorSpace parameter is to be 24 bit color (TrueColor aka RGB).
            private const string SGR = "m";     // (Select Graphic Rendition)       Applies the parameters above (if provided).
            private const string RCI = "0m";    // (Reset CSP Indicator)            Indicates the ColorSpace Parameter should be reset.

            public static string Foreground(Color color)
            {
                // Example: Color.Red -> "\x1b[38;2;255;0;0m"
                return $"{ESC}{CSI}{FGI};{CSP};{color.R};{color.G};{color.B}{SGR}";
            }

            public static string Background(Color color)
            {
                // Example: Color.Blue -> "\x1b[48;2;0;0;255m"
                return $"{ESC}{CSI}{BGI};{CSP};{color.R};{color.G};{color.B}{SGR}";
            }

            public static string Colorize(string text, Color? foregroundColor = null, Color? backgroundColor = null)
            {
                string prefix = "";
                if (backgroundColor.HasValue)
                {
                    prefix += Background(backgroundColor.Value);
                }
                if (foregroundColor.HasValue)
                {
                    prefix += Foreground(foregroundColor.Value);
                }
                if (!string.IsNullOrEmpty(prefix))
                {
                    return $"{prefix}{text}{ESC}{CSI}{RCI}";
                }
                return text;
            }

        }


        public static class ColorConverter
        {
            public static ConsoleColor ToConsoleColor(Color drawingColor)
            {
                ConsoleColor closestConsoleColor = ConsoleColor.Black;
                double minDistance = double.MaxValue;

                foreach (ConsoleColor consoleColor in Enum.GetValues(typeof(ConsoleColor)))
                {
                    Color currentConsoleDrawingColor = GetDrawingColorFromConsoleColor(consoleColor);

                    // Calculate the Euclidean distance between the two colors in RGB space
                    double distance = GetColorDistance(drawingColor, currentConsoleDrawingColor);

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestConsoleColor = consoleColor;
                    }
                }

                return closestConsoleColor;
            }
            private static double GetColorDistance(Color color1, Color color2)
            {
                long rDiff = color1.R - color2.R;
                long gDiff = color1.G - color2.G;
                long bDiff = color1.B - color2.B;

                return Math.Sqrt((rDiff * rDiff) + (gDiff * gDiff) + (bDiff * bDiff));
            }

            private static Color GetDrawingColorFromConsoleColor(ConsoleColor consoleColor)
            {
                return consoleColor switch
                {
                    ConsoleColor.Black => Color.FromArgb(0, 0, 0),
                    ConsoleColor.DarkBlue => Color.FromArgb(0, 0, 128),
                    ConsoleColor.DarkGreen => Color.FromArgb(0, 128, 0),
                    ConsoleColor.DarkCyan => Color.FromArgb(0, 128, 128),
                    ConsoleColor.DarkRed => Color.FromArgb(128, 0, 0),
                    ConsoleColor.DarkMagenta => Color.FromArgb(128, 0, 128),
                    ConsoleColor.DarkYellow => Color.FromArgb(128, 128, 0),
                    ConsoleColor.Gray => Color.FromArgb(192, 192, 192), // Light Gray
                    ConsoleColor.DarkGray => Color.FromArgb(128, 128, 128),
                    ConsoleColor.Blue => Color.FromArgb(0, 0, 255),
                    ConsoleColor.Green => Color.FromArgb(0, 255, 0),
                    ConsoleColor.Cyan => Color.FromArgb(0, 255, 255),
                    ConsoleColor.Red => Color.FromArgb(255, 0, 0),
                    ConsoleColor.Magenta => Color.FromArgb(255, 0, 255),
                    ConsoleColor.Yellow => Color.FromArgb(255, 255, 0),
                    ConsoleColor.White => Color.FromArgb(255, 255, 255),
                    _ => Color.Black, // Fallback, though all ConsoleColor enums are covered
                };
            }
        }
        // Example usage:
        // Color emeraldGreen = Color.FromArgb(40, 196, 40);
        // ConsoleColor closest = AnsiManager.ColorConverter.ToConsoleColor(emeraldGreen);

        // Console.WriteLine($"The closest ConsoleColor to {emeraldGreen} is {closest}");

        // Color purple = Color.FromArgb(100, 50, 200);
        // ConsoleColor purpleClosest = AnsiManager.ColorConverter.ToConsoleColor(purple);
        // Console.WriteLine($"The closest ConsoleColor to {purple} is {purpleClosest}"); 

        // Color cherryRed = Color.FromArgb(156, 1, 1); 
        // ConsoleColor cherryClosest = AnsiManager.ColorConverter.ToConsoleColor(cherryRed);
        // Console.WriteLine($"The closest ConsoleColor to {cherryRed} is {cherryClosest}");
        // Environment.Exit(0 );


    }
}

