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

﻿using System.Collections;
using System.Reflection;

namespace BrowserAutomationMaster.Helpers
{
    public static class ReflectionHelper
    {
        public static IEnumerable<T> GetStaticFieldsOfType<T>(Type outerType, bool publicOnly = true)
        {
            BindingFlags flags = BindingFlags.Static | (publicOnly ? BindingFlags.Public : BindingFlags.Public | BindingFlags.NonPublic);

            // Gets all fields of the provided class
            // Filters for fields that are assignable to <T>
            // Returns the value of each field
            return outerType.GetFields(flags)
                .Where(f => typeof(T).IsAssignableFrom(f.FieldType))
                .Select(f => (T)f.GetValue(null)!);
        }

        public static void PrintProperties(object? obj, int indent = 0)
        {
            if (obj == null)
            {
                Console.WriteLine("null");
                return;
            }

            string indentString = new(' ', indent * 4);
            Type type = obj.GetType();

            // Handles collections
            if (obj is IEnumerable enumerable and not string)
            {
                int index = 0;
                foreach (var item in enumerable)
                {
                    Console.WriteLine($"{indentString}[{index++}]");
                    PrintProperties(item, indent + 1);
                }
                return;
            }

            // Handles regular objects
            Console.WriteLine($"{indentString}{type.Name}:");
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                object? value = property.GetValue(obj, null);

                bool isCustomObject =
                    value != null &&
                    (
                        property.PropertyType.Namespace == type.Namespace ||
                        property.PropertyType.IsGenericType &&
                        property.PropertyType.GetGenericArguments()[0].Namespace == type.Namespace
                    );

                if (isCustomObject)
                {
                    Console.WriteLine($"{indentString}{property.Name}:");
                    PrintProperties(value, indent + 1);
                }
                else
                {
                    // Handle simple properties
                    if (value is not string && value is IEnumerable)
                    {
                        Console.WriteLine($"{indentString}{property.Name}:");
                        PrintProperties(value, indent + 1);
                    }
                    else
                    {
                        Console.WriteLine($"{indentString}{property.Name}: {value}");
                    }
                }
            }
        }
    }
}