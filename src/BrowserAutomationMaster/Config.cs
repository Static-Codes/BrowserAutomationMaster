using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace BrowserAutomationMaster
{
    internal class Config
    {
        private const string ConfigFileName = "config.ini";
        private readonly string configFilePath;
        private readonly Dictionary<string, string> settings = new(StringComparer.OrdinalIgnoreCase); // Checks setting names using case insensitivity since user input is unpredictable.

        public Config()
        {
            configFilePath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
            LoadConfig();
        }

        private bool LoadConfig()
        {
            if (!File.Exists(configFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BAM Manager (BAMM) was unable to read config.ini\n\nError: File not found.\n\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please wait while this file is regenerated.");
            }

            string? currentSection = null;

            try
            {
                string[] lines = File.ReadAllLines(configFilePath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(';') || trimmedLine.StartsWith('#'))
                    {
                        continue;
                    }

                    if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
                    {
                        currentSection = trimmedLine[1..^1].Trim(); // Roslyn recommended this over .Substring(1, trimmedLine.Length - 2)
                        continue;
                    }

                    if (currentSection != null && currentSection.Equals("Settings", StringComparison.OrdinalIgnoreCase))
                    {
                        int equalsIndex = trimmedLine.IndexOf('=');
                        if (equalsIndex > 0)
                        {
                            string key = trimmedLine[..equalsIndex].Trim(); // Rosyln recommended this over .Substring(0, equalsIndex)
                            string value = trimmedLine[(equalsIndex + 1)..].Trim(); // Rosyln recommended this over .Substring(equalsIndex + 1)

                            if (!string.IsNullOrEmpty(key))
                            {
                                settings[key] = value;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"BAM Manager (BAMM) was unable to read config.ini\n\nError: {ex.Message}\n\n");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Please wait while this file is regenerated.");
                // Add function here that returns a bool if the default config file is created.
            }
            return true;
        }

        public string? GetSetting(string key, string? defaultValue = null)
        {
            if (settings.TryGetValue(key, out string? value))
            {
                return value;
            }
            return defaultValue;
        }

        public int GetSettingAsInt(string key, int defaultValue = 0) // Checks setting names using case insensitivity since user input is unpredictable.
        {
            string? stringValue = GetSetting(key);
            if (stringValue != null && int.TryParse(stringValue, out int result))
            {
                return result;
            }
            return defaultValue;
        }

        public bool GetSettingAsBool(string key, bool defaultValue = false) // Checks setting names using case insensitivity since user input is unpredictable.
        {
            string? stringValue = GetSetting(key); // ? Declares stringValue as nullable since user input is subject to spelling mistakes.
            if (stringValue != null)
            {
                return stringValue.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                       stringValue.Equals("1") ||
                       stringValue.Equals("yes", StringComparison.OrdinalIgnoreCase); 
            }
            return defaultValue;
        }
    }
}