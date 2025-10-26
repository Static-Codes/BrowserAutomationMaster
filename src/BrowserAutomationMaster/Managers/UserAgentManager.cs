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
        private static readonly object _lock = new();
        // private modifier is needed here so lock is not accessed by external code.
        // _ required because lock is a reserved keyword

        private static async Task LoadUserAgents()
        {
            //lock (_lock)
            //{
                if (userAgentsData != null && userAgentsData.Count > 0)
                    return;

                try
                {
                    var userAgentsObj = new UserAgents();
                    var jsonString = await userAgentsObj.LoadJSONString();
                    if (jsonString == null)
                    {
                        WriteAndExit(
                            message:
                                $"BAM Manager (BAMM) was failed to user agent data, please try again." +
                                $"If this error persists, please make a bug report at {ISSUES_LINK}\n\n" +
                                $"Error Log:\njsonString is null or empty",
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
                                $"Error Log:\nuserAgentsData is null or empty",
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
                    
            //}

        }
        public static async Task<string?> GetUserAgent(string browserName)
        {
            await LoadUserAgents();

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

        private static async Task<string?> RetrieveJSON()
        {
            var uri = new Uri(USERAGENTS_LINK);
            try 
            {
                
                var response = await RequestManager.NetworkClient.Instance.GetStringAsync(uri);
                if (response == null)
                    return null;
                return response;
            }
            catch (Exception ex)
            {
                var message =
                    "Unable to load useragents.json\n" +
                    $"This file should be placed in:\n{GetUserAgentsPath()}" +
                    $"This file can be downloaded from:\n{uri}\n\n" +
                    $"Error Log:\n\n{ex.Message}";

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

        private static async Task<bool> WriteJSON()
        {
            var message =
                    "Unable to read contents from useragents.json\n" +
                    $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                    "Error Log:\nJSON contents is null.";

            var contents = await RetrieveJSON();
            if (contents == null)
                WriteAndExit(message, 1);

            try
            {
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

            public async Task<string?> LoadJSONString()
            {
                try
                {
                    if (!fileDownloaded)
                    {
                        await WriteJSON();
                        return await RetrieveJSON();
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
