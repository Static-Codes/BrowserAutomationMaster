using System.Drawing;

namespace BrowserAutomationMaster.Managers
{

    public class ThemeManager
    {
        private static Color CherryRed = Color.FromArgb(156, 1, 1);
        private static Color DarkGray = Color.FromArgb(25, 25, 25);
        private static Color DarkGreen = Color.FromArgb(12, 59, 1);
        private static Color EmeraldGreen = Color.FromArgb(40, 196, 40);
        private static Color LightGray = Color.FromArgb(204, 220, 220);
        private static Color LightRed = Color.FromArgb(255, 18, 43);
        private static Color LightYellow = Color.FromArgb(102, 245, 241, 11);
        private static Color NeonYellow = Color.FromArgb(232, 255, 8);

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
    }

    public class Theme(Color BackgroundColor, Color ForegroundColor, Color SuccessColor, Color WarningColor, Color ErrorColor)
    {
        Color BackgroundColor { get; set; } = BackgroundColor;
        Color ForegroundColor { get; set; } = ForegroundColor;
        Color SuccessColor { get; set; } = SuccessColor;
        Color WarningColor { get; set; } = WarningColor;
        Color ErrorColor { get; set; } = ErrorColor;
    }
}
