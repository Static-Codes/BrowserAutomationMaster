namespace BrowserAutomationMaster.Core.Helpers 
{
    // Since System.Globalization.TextInfo.ToTitleCase is more complicated than required
    public static class StringExtensions 
    {
        public static string ToTitle(this string value) => string.Concat(char.ToUpper(value[0]), value[1..]);
        public static string ToTitle(this string[] values) => string.Concat(
            values.Select(val => val.ToTitle())
        );    
    }
}