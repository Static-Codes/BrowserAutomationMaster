using BrowserAutomationMaster.Managers.AppManager.OS.Linux;

namespace BrowserAutomationMaster.Helpers
{
    public class EnumHelper
    {

        public static bool IsValidMember(Type type, string member)
        {
            if (type == null) return false;
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
            if (type == null) { 
                return null;
            }

            // Handles case where Distros members are Distro object
            var returnType = type.Name.Equals("Distros") ? typeof(Distro) : type;

            try { 
                return Enum.Parse(returnType, StringRepr) != null; 
            }
            
            catch { 
                return null; 
            }
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
