using System;
using System.Collections;
using System.Reflection;

namespace BrowserAutomationMaster.Helpers
{
    public static class ReflectionHelper
    {
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