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

﻿using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.InstanceManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{

    public static class BrowserVersionManager
    {
        private static BrowserVersions? browserVersions = null;
        private readonly static JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly static string browserJSONPath = Path.Combine(browserStackDirectory, "browsers.json");

        public static async Task<BrowserVersions?> GetLatestVersionInfo()
        {
            try
            {
                string? response;
                if (File.Exists(browserJSONPath)) {
                    response = File.ReadAllText(browserJSONPath);
                }

                var uri = new Uri("https://raw.githubusercontent.com/browser-update/browser-update/refs/heads/master/data/browsers.json");
                var requestManager = new RequestManager(uri, timeout: 10);
                response = await requestManager.GetStringAsync();

                if (response == null) {
                    return null;
                }

                using JsonDocument doc = JsonDocument.Parse(response);
                JsonElement currentElement = doc.RootElement.GetProperty("current");
                JsonElement desktopElement = currentElement.GetProperty("desktop");

                BrowserVersions? versionInfo = desktopElement.Deserialize<BrowserVersions>(options);
                return versionInfo;
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    "Unable to get the latest browser versions for BrowserStack.\n" +
                    $"If this persists, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error Log:\n{ex.Message}",
                    status: 1
                );
                return null;
            }
        }
        public static BrowserVersions? GetBrowserVersion() { return browserVersions; }
        public static void SetBrowserVersions(BrowserVersions? versions) { browserVersions = versions; }

    }

    public class BrowserVersions
    {
        [JsonPropertyName("c")]
        public required string Chrome { get; set; }

        [JsonPropertyName("e")]
        public required string Edge { get; set; }

        [JsonPropertyName("e_a")]
        public required string EdgeAndroid { get; set; }

        [JsonPropertyName("f")]
        public required string Firefox { get; set; }

        [JsonPropertyName("i")]
        public required string IExplorer { get; set; }

        [JsonPropertyName("ios")]
        public required string SafariIOS { get; set; }

        [JsonPropertyName("s")]
        public required string SafariMac { get; set; }

        [JsonPropertyName("samsung")]
        public required string SamsungBrowser { get; set; }

        [JsonPropertyName("o")]
        public required string Opera { get; set; }

        [JsonPropertyName("o_a")]
        public required string OperaAndroid { get; set; }

        [JsonPropertyName("y")]
        public required string Yandex { get; set; }

        [JsonPropertyName("v")]
        public required string Vivaldi { get; set; }

        [JsonPropertyName("uc")]
        public required string UC { get; set; }

        [JsonPropertyName("a")]
        public required string AndroidBrowser { get; set; }

        [JsonPropertyName("silk")]
        public required string Silk { get; set; }

        [JsonPropertyName("waterfox")]
        public required string Waterfox { get; set; }

        [JsonPropertyName("palemoon")]
        public required string Palemoon { get; set; }
    }

}
