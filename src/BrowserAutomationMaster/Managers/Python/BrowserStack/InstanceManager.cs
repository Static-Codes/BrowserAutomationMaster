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

﻿using BrowserAutomationMaster.Messaging;
using System;
using System.IO;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using static BrowserAutomationMaster.Helpers.EnumHelper;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager.DeviceHelper;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers.Python.BrowserStack
{
    public struct BrowserStackConfig()
    {
        public required string UserName;
        public required string AccessKey;
        public required BrowserStackPlatform[] Platforms;
        public required bool BrowserStackLocal;
        public required string BuildName; // Python script filename (without the extension)
        public required string ProjectName; // projectDirectory (Current timestamp)
        public required string BuildIdentifier;
        public required bool Debug;
        public required string ConsoleLogs;
        public required string Framework;
        public readonly override string ToString()
        {
            return string.Join(NLC, [
                $"UserName: {UserName}",
                $"AccessKey: {AccessKey}",
                $"BrowserStackLocal: {BrowserStackLocal}",
                $"BuildName: {BuildName}",
                $"ProjectName: {ProjectName}",
                // $"BuildIdentifier: {BuildIdentifier}\n" +
                $"Debug: {Debug}",
                $"ConsoleLogs: {ConsoleLogs}",
                $"Framework: {Framework}"
            ]);
        }
    }

    public struct BrowserStackPlatform()
    {
        public required string os;
        public required string osVersion;
        public required string BrowserName;
        public required string BrowserVersion;
        public string? DeviceName = null;
        public string? DeviceOrientation = null;
    }

    public enum DeviceOrientation
    {
        Landscape,
        Portrait
    }

    public class InstanceManager
    {
        public readonly static string browserStackDirectory = GetBrowserStackDirectory();
        public readonly static string browserStackConfig = Path.Combine(browserStackDirectory, "browserstack.yml");

        public static BrowserStackConfig? StackConfig { get; set; }
        private readonly static string tutorialMessage =
            "Please follow the following steps to use BrowserStack:\n\n" +
            "1. Visit https://www.browserstack.com/users/sign_up\n" +
            "2. Sign up using an email you can receive a verification with.\n" +
            "3. Click the verification link inside the email you receive.\n" +
            "4. Go to: https://www.browserstack.com/accounts/profile/\n" +
            "5. Click 'My profile'" +
            "6. Copy and Paste both your username and access key when prompted. (This only has to be done once)";


        public static BrowserStackConfig BuildConfig()
        {
            var userName = Input.AskForInput("BrowserStack Username: ");
            var accessKey = Input.AskForInput("BrowserStack Access Key: ");
            var projectName = Input.AskForInput("Project Name: ");
            var scriptName = Input.AskForInput("Python Script Name: ");

            var rawOSName = Input.WriteListFromOptions(OSNames, noun: "Operating System", pageSize: 4);
            var osName = SanitizeOSName(rawOSName);
            var browserName = GetDesiredBrowser(rawOSName);

            var osVersions = GetVersionsOfOS(osName);
            var rawOSVersion = Input.WriteListFromOptions(osVersions, noun: $"Version of {rawOSName}");
            var osVersion = SanitizeOSVersion(rawOSVersion, rawOSName, osVersions);

            var versions = GetBrowserVersionsSupported(browserName, osName);

            var description = $"version of {rawOSName} that supports {browserName}";
            
            if (versions == null)
            {
                WriteAndExit($"Unable to find a {description}, please try a different combination.", 1);
            }
            
            // Will be used for defining DeviceName and DeviceOrientation if mobile
            // If not mobile, browserVersion must be specified.
            var isMobile = osName switch
            {
                "android" or "ios" => true,
                _ => false,
            };

            var browserVersion = "";
            
            // BrowserStack doesn't allow you to specify the browserVersion on android.
            if (osName != "android")
            {
                browserVersion = GetDesiredBrowerVersion(browserName, osName, osVersion);
            }
            
            string[] devices;
            string? device = null;
            string? deviceOrientation = null;

            if (isMobile)
            {
                devices = osName switch
                {
                    "android" => GetAndroidDeviceNames(osVersion, browserName),
                    "ios" => GetiOSDeviceNames(osVersion, browserName),
                    _ => []
                };

                if (devices.Length == 0)
                {
                    WriteAndExit("Unable to find device supported by BrowserStack that fits your requirements.", status: 1);
                }

                device = Input.WriteListFromOptions(devices, noun: "device");

                var reprs = GetStringReprs(typeof(DeviceOrientation));
                deviceOrientation = Input.WriteListFromOptions(reprs, noun: "orientation");
            }


            // Currently only one platform is supported at a time but plans are to implement multiple if desired.
            var platform = new BrowserStackPlatform[]
            {
                new()
                {
                    os = osName,
                    osVersion = osVersion,
                    BrowserName = browserName,
                    BrowserVersion = browserVersion,
                    DeviceName = device,
                    DeviceOrientation = deviceOrientation
                }
            };

            return new BrowserStackConfig()
            {
                AccessKey = accessKey,
                UserName = userName,
                Platforms = platform,
                Debug = true,
                BrowserStackLocal = false,
                BuildIdentifier = "'#${BUILD_NUMBER}'",
                BuildName = scriptName,
                ProjectName = projectName,
                ConsoleLogs = "disable",
                Framework = "python",
            };
        }


        public static BrowserStackConfig? LoadConfig()
        {
            if (!File.Exists(browserStackConfig))
            {
                WriteConfig(fileNotFound: true);
            }

            try
            {
                var fileText = File.ReadAllText(browserStackConfig);

                var deserializer =
                    new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                return deserializer.Deserialize<BrowserStackConfig>(fileText);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Prompts the user to 
        /// </summary>
        /// <returns></returns>
        public static bool PromptConfigOverride()
        {
            if (!File.Exists(browserStackConfig))
            {
                return false;
            }

            var builder = new StringBuilder();
            Warning.Write("The BrowserStack Config file already exists.\n");
            try
            {
                foreach (var line in File.ReadLines(browserStackConfig))
                {
                    builder.AppendLine(line);
                }
            }

            catch (Exception ex)
            {
                Write($"Unable to read BrowserStack Config.\n\nError Log:\n{ex.Message}");
                return false;
            }

            WriteSuccessMessage("Config Contents:\n");
            Warning.Write(builder.ToString());

            var response = Input.AskForInput("Would you like to overwrite the config above? [y/n]: ");

            if (Input.ConditionRejected(response))
            {
                return false;
            }

            return true;
        }


        /// <summary>
        /// Writes (or overwrites) the BrowserStack Config (browserstack.yml)
        /// </summary>
        /// <param name="fileNotFound">Whether or not to display a message indicating the file was not found.</param>
        public static void WriteConfig(bool fileNotFound)
        {
            try
            {
                if (fileNotFound)
                {
                    Write("Unable to locate the BrowserStack Config.");
                    string response = Input.AskForInput("Do you already have an account on https://browserstack.com [y/n]: ");

                    if (Input.ConditionRejected(response)) {
                        WriteAndExit(tutorialMessage, 1);
                    }

                    Warning.Write("Creating browserstack.yml now.\n\n");
                }

                var config = BuildConfig();

                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var yaml = serializer.Serialize(config);
                if (yaml == null)
                {
                    WriteAndExit("Unable to generate browserstack.yml using the selected information, please try again.", 1);
                }

                EnsureDirectoryExists(browserStackDirectory);
                File.WriteAllText(browserStackConfig, yaml);

            }

            catch (Exception e)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        "Unable to generate browserstack.yml using the selected information, please try again.",
                        "Error Log:",
                        $"{e.StackTrace ?? e.Message}",
                        "in WriteConfig()"
                    ]),
                    status: 1
                );
            }

        }


    }
}
