using System.Drawing;
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