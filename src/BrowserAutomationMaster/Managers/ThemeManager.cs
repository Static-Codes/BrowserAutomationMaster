using System.Drawing;

namespace BrowserAutomationMaster.Managers
{

    public class ThemeManager
    {
        private static readonly Color CherryRed = Color.FromArgb(156, 1, 1);
        private static readonly Color DarkGray = Color.FromArgb(25, 25, 25);
        private static readonly Color DarkGreen = Color.FromArgb(12, 59, 1);
        private static readonly Color EmeraldGreen = Color.FromArgb(40, 196, 40);
        private static readonly Color LightGray = Color.FromArgb(204, 220, 220);
        private static readonly Color LightRed = Color.FromArgb(255, 18, 43);
        private static readonly Color LightYellow = Color.FromArgb(102, 245, 241, 11);
        private static readonly Color NeonYellow = Color.FromArgb(232, 255, 8);

        public static Theme DarkTheme = new(
            BackgroundColor: DarkGray,
            ForegroundColor: Color.White,
            SuccessColor: EmeraldGreen,
            WarningColor: LightYellow,
            ErrorColor: LightRed
        );

        public static Theme LightTheme = new(
            BackgroundColor: LightGray,
            ForegroundColor: Color.Black,
            SuccessColor: DarkGreen,
            WarningColor: Color.DarkGoldenrod,
            ErrorColor: CherryRed
        );
        public static Theme DefaultTheme = DarkTheme;
    }

    public class Theme(Color BackgroundColor, Color ForegroundColor, Color SuccessColor, Color WarningColor, Color ErrorColor)
    {
        public Color BackgroundColor { get; set; } = BackgroundColor;
        public Color ForegroundColor { get; set; } = ForegroundColor;
        public Color SuccessColor { get; set; } = SuccessColor;
        public Color WarningColor { get; set; } = WarningColor;
        public Color ErrorColor { get; set; } = ErrorColor;
    }    
}
