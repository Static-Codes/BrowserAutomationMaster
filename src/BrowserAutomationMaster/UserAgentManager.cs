using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    internal class UserAgentManager
    {
        readonly static string UserAgentsFilePath = "UserAgents.json";
        static Dictionary<string, List<string>>? userAgentsData;
        readonly static Random random = new();
        private static readonly object _lock = new(); // private modifier is needed here so lock is not accessed by external code. // _ required because lock is a reserved keyword

        private static void LoadUserAgents()
        {
            if (userAgentsData == null)
            {
                lock (_lock)
                {
                    if (userAgentsData == null)
                    {
                        try
                        {
                            if (!File.Exists(UserAgentsFilePath))
                            {
                                Errors.WriteErrorAndExit($"Error: User agent file not found at {UserAgentsFilePath}", 1);
                            }

                            string jsonString = File.ReadAllText(UserAgentsFilePath);
                            userAgentsData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString);

                            if (userAgentsData == null || userAgentsData.Count == 0)
                            {
                                Errors.WriteErrorAndExit($"Warning: User agent data loaded from {UserAgentsFilePath} is null or empty.", 1);
                            }
                        }
                        catch (JsonException ex)
                        {
                            Errors.WriteErrorAndExit($"Error deserializing JSON from {UserAgentsFilePath}: {ex.Message}", 1);
                        }
                        catch (Exception ex)
                        {
                            Errors.WriteErrorAndExit($"An unexpected error occurred while reading or parsing {UserAgentsFilePath}: {ex.Message}", 1);
                        }
                    }
                }
            }

        }
        public static string? GetUserAgent(string browserName)
        {
            LoadUserAgents();

            if (userAgentsData == null || userAgentsData.Count == 0)
            {
                Errors.WriteErrorAndExit("BAM Manager (BAMM) was unable to load UserAgents.json, please ensure this file exists and is populated.", 1);
                return null; // This will never be reachable, as WriteErrorAndExit does exactly that.
            }

            if (userAgentsData.TryGetValue(browserName, out List<string>? userAgentList) && userAgentList != null && userAgentList.Count > 0)
            {
                return userAgentList[random.Next(userAgentList.Count)];
            }
            else
            {
                Console.Error.WriteLine($"BAM Manager (BAMM) was unable to load any useragents for browser {browserName}.  Please check for typos and try again.");
                return null; // This will never be reachable, as WriteErrorAndExit does exactly that.
            }
        }

    }
}
