using BrowserAutomationMaster.Managers.Python.BrowserStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
