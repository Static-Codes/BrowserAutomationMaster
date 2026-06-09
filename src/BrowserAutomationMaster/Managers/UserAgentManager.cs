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

﻿using System.Text;
using System.Text.Json;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public class UserAgentManager
    {

        public readonly static string userAgentPath = GetUserAgentsPath();
        private static Dictionary<string, List<string>>? userAgentsData;

        private readonly static Random random = new();
        // private static readonly Lock _lock = new();
        // _ required because lock is a reserved keyword

        private static void LoadUserAgents()
        {
            // lock (_lock)
            // {
                if (userAgentsData != null && userAgentsData.Count > 0) {
                    return;
                }

                try
                {
                    var userAgentsObj = new UserAgents();
                    var jsonString = userAgentsObj.LoadJSONString();
                    if (jsonString == null)
                    {
                        WriteAndExit(
                            message:
                                $"BAM Manager (BAMM) was failed to user agent data, please try again." +
                                $"If this error persists, please make a bug report at {ISSUES_LINK}\n\n" +
                                $"Error Log:\njsonString == null or empty",
                            status: 1
                        );
                    }
                    userAgentsData = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString);

                    if (userAgentsData == null || userAgentsData.Count == 0)
                    {
                        WriteAndExit(
                            message:
                                $"BAM Manager (BAMM) was failed to user agent data, please try again." +
                                $"If this error persists, please make a bug report at {ISSUES_LINK}\n\n" +
                                $"Error Log:\nuserAgentsData == null or empty",
                            status: 1
                        );
                    }
                }
                catch (JsonException)
                {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to deserialize embedded user agent data, " +
                            $"please try again, if this error persists, " +
                            $"it is likely a network flaw not an issue with your machine.", 
                        status: 1
                    );
                }
                catch (Exception)
                {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to load embedded user agent data, " +
                            $"please try again, if this error persists, " +
                            $"it is likely a developmental flaw not an issue with your machine.", 
                        status: 1
                    );
                }
                    
            // }
        }

        public static void SetUserAgents() {
            LoadUserAgents();
        }

        public static string? GetUserAgent(string browserName)
        {
            // LoadUserAgents();
            if (userAgentsData == null || userAgentsData.Count == 0)
            {
                WriteAndExit(
                    message: 
                        $"BAM Manager (BAMM) was failed to parse embedded user agent data, " +
                        $"please try again, if this error persists, " +
                        $"it is likely a developmental flaw not an issue with your machine.", 
                    status: 1
                );
                return null; // This will never be reachable, as WriteAndExit does exactly that.
            }

            bool isLoaded = userAgentsData.TryGetValue(
                browserName, out List<string>? userAgentList
            );

            if (isLoaded && userAgentList != null && userAgentList.Count > 0) {
                return userAgentList[random.Next(userAgentList.Count)];
            }

            else
            {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to compile the selected script.  " +
                        $"Supported values for 'browser' command include:\n\n" +
                        $"\"chrome\"\n" +
                        $"\"firefox\"\n\n" +
                        $"Please check for typos and try again.", 
                    status: 1
                );
                return null; // This will never be reachable, as WriteAndExit does exactly that.
            }
        }

        private static string? RetrieveJSON()
        {
            var resourceName = "useragents.json";
            var resourcePattern = "BrowserAutomationMaster.AppData.useragents.json";

            try 
            {
                using Stream stream = EmbeddedResourceManager.GetEmbeddedResource(resourceName, resourcePattern);

                using var reader = new StreamReader(stream, Encoding.UTF8);

                return reader.ReadToEnd();
            }
            catch (Exception ex)
            {
                var message =
                    $"Unable to load useragents.json{NLC}" +
                    $"This file should be placed in:{NLC}{GetUserAgentsPath()}" +
                    $"This file can be downloaded from:{NLC}{USERAGENTS_LINK}{NLC}{NLC}" +
                    $"Error Log:{NLC}{NLC}{ex.Message}";

                WriteAndExit(message, 1);
                return null;
            }
        }

        private static string? ReadJSONContents()
        {
            try
            {
                return File.ReadAllText(userAgentPath);
            }
            catch (Exception ex)
            {
                var message =
                    "Unable to read contents from useragents.json\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" + 
                    $"Error Log:\n{ex.Message}";
                return WriteErrorAndReturnNull(message);
            }
        }

        private static bool WriteJSON()
        {
            var message =
                    "Unable to read contents from useragents.json\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                    "Error Log:\nJSON contents == null.";

            var contents = RetrieveJSON();
            
            if (contents == null) {
                WriteAndExit(message, 1);
            }

            try {
                File.WriteAllText(userAgentPath, contents);
            }
            
            catch (Exception ex) {
                message =
                   "Unable to write useragents.json\n" +
                   $"This file should be placed in:\n{GetUserAgentsPath()}" +
                   $"This file can be downloaded from:\n{USERAGENTS_LINK}\n\n" +
                   $"Error Log:\n\n{ex.Message}";
                WriteAndExit(message, 1);
            }
            return true;
        }

        public class UserAgents 
        {

            public readonly bool fileDownloaded = File.Exists(userAgentPath);

            public string? LoadJSONString()
            {
                try
                {
                    if (!fileDownloaded)
                    {
                        WriteJSON();
                        return RetrieveJSON();
                    }
                    return ReadJSONContents();
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
