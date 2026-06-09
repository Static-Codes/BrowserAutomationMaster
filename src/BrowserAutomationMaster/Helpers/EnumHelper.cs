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

﻿using BrowserAutomationMaster.Managers.AppManager.OS.Linux;

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
