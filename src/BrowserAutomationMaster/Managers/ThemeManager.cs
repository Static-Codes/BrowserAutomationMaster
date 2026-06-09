// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

﻿using BrowserAutomationMaster.Messaging;
using System.Drawing;
using static BrowserAutomationMaster.Managers.AppManager.OS.Linux.Functions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers
{

    public class ThemeManager
    {
        private static readonly Color CherryRed = Color.FromArgb(156, 1, 1);
        private static readonly Color DarkGray = Color.FromArgb(25, 25, 25);
        private static readonly Color DarkGreen = Color.FromArgb(12, 59, 1);
        private static readonly Color EmeraldGreen = Color.FromArgb(40, 196, 40);
        private static readonly Color LightBlue = Color.FromArgb(48, 142, 217);
        private static readonly Color LightGray = Color.FromArgb(204, 220, 220);
        private static readonly Color LightRed = Color.FromArgb(255, 18, 43);
        private static readonly Color LightYellow = Color.FromArgb(102, 245, 241, 11);
        private static readonly Color OliveGreen = Color.FromArgb(13, 176, 9);

        public readonly static Theme DarkTheme = new(
            ForegroundColor: Color.Black,
            SuccessColor: EmeraldGreen,
            WarningColor: LightYellow,
            ErrorColor: LightRed,
            HighlightBackground: LightGray,
            HighlightForeground: DarkGreen,
            AccentColor: LightBlue
        );
        public readonly static Theme LightTheme = new(
            ForegroundColor: Color.White,
            SuccessColor: DarkGreen,
            WarningColor: Color.DarkGoldenrod,
            ErrorColor: CherryRed,
            HighlightBackground: DarkGray,
            HighlightForeground: OliveGreen,
            AccentColor: LightBlue
        );
        public static Theme DefaultTheme { get; private set; } = GetDefaultTheme();

        private static Theme GetDefaultTheme()
        {
            try
            {
                if (!Platforms.IsLinux || Platforms.IsChromeOS || Platforms.IsRaspi) {
                    return LightTheme;
                }

                var Ansi24BitColor = GetTerminalBackgroundColor();

                if (Ansi24BitColor == null)
                {
                    return LightTheme;
                }
                
                (int r, int g, int b) = AnsiManager.FromXTerm(Ansi24BitColor);
                var color = Color.FromArgb(r, g, b);

                return GetThemeFromColor(color);
            }
            catch (Exception ex) {
                Errors.Write
                (
                    string.Join(NLC, [
                        "[ERROR]: An exception occured while determining default theme, using LightTheme.",
                        $"[ERROR LOG]: {ex.StackTrace ?? ex.Message}"
                    ])
                );
                return LightTheme; 
            }
        }
        private static Theme GetThemeFromColor(Color terminalBGColor)
        {
            // RGB (TrueColor / 24 bit color)
            // Relative Luminance = 0.2126(R) + 0.7152(G) + 0.0722(B)

            double midpoint = 127.5;

            int R = terminalBGColor.R;
            int G = terminalBGColor.G;
            int B = terminalBGColor.B;

            double luminescence = (0.2126 * R) + (0.7152 * G) + (0.0722 * B);
            bool closerToBlack = luminescence <= midpoint;
            // Debug values
            // Console.WriteLine($"Luminescence: {luminescence}");
            // Console.WriteLine($"Is Dark Theme: {!closerToBlack}");
            // Console.WriteLine($"Is Light Theme: {closerToBlack}");

            return closerToBlack ? LightTheme : DarkTheme;
        }
    }

    public class Theme(Color ForegroundColor, Color SuccessColor, Color WarningColor, Color ErrorColor, Color HighlightBackground, Color HighlightForeground, Color AccentColor)
    {
        public Color ForegroundColor { get; set; } = ForegroundColor;
        public Color SuccessColor { get; set; } = SuccessColor;
        public Color WarningColor { get; set; } = WarningColor;
        public Color ErrorColor { get; set; } = ErrorColor;
        public Color HighlightBackground { get; set; } = HighlightBackground;
        public Color HighlightForeground { get; set; } = HighlightForeground;
        public Color AccentColor { get; set; } = AccentColor;
    }
}
