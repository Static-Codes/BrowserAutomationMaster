using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using BrowserAutomationMaster.Core.Types.Linux;

namespace BrowserAutomationMaster.Core.Helpers
{
    public class EnumHelper
    {

        public static bool IsValidMember(Type type, string member)
        {
            if (type == null) { return false; }
            try { return Enum.Parse(type, member) != null; }
            catch { return false; }
        }
        public static string[] GetStringReprs(Type type)
        {
            try { return Enum.GetNames(type); }
            catch { return []; }
        }

        public static string[] GetStringReprs(Enum e)
        {
            try { return Enum.GetNames(e.GetType()); }
            catch { return []; }
        }

        public static object? GetEnumMemberFromStringRepr(Type type, string StringRepr) 
        {
            // Handles case where Distros members are Distro object
            var returnType = type.Name.Equals("Distros") ? typeof(Distro) : type;

            var _ = Enum.TryParse(returnType, StringRepr, out object? parsedEnum);

            return parsedEnum ?? null;
        }

        public static string GetEnumNameAsString(Enum e, Dictionary<string, string> replacements) 
        {
            var enumStrRepr = e.ToString();
            foreach (var r in replacements) {
                enumStrRepr = enumStrRepr.Replace(r.Key, r.Value);
            }
            var lastIndex = enumStrRepr.LastIndexOf('.');

            return lastIndex != -1 ? enumStrRepr[(lastIndex + 1)..] : enumStrRepr;
        }


    }
}
