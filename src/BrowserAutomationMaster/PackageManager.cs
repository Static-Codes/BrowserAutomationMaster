using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    public class PackageJson
    {
        readonly public static string jsonString = """
        {

            "selenium": {
                "4.32.0": [ 3.10, 3.11, 3.12 ]
            },
            "webdriver-manager": {
                "4.0.2": [ 3.9, 3.10, 3.11 ]
            }
        }
        """;
    }

    public partial class PackageManager
    {
        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.
        const string packageFormatPattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9._-]*[a-zA-Z0-9])$"; // Regex pulled from https://pypi.org/project/twine/
        [GeneratedRegex(packageFormatPattern)]
        private static partial Regex PrecompiledPackageRegex();

        private static Dictionary<string, Dictionary<string, List<double>>> packageData = [];

        public static void New(string packageName, double pythonVersion)
        {
            if (packageName == null || !PrecompiledPackageRegex().IsMatch(packageName)) {
                Errors.WriteErrorAndExit("Invalid packageName provided to PackageManager(), please check your spelling and try again.", 1);
            }
            string jsonString;
            try { jsonString = File.ReadAllText("packages.json"); }
            catch { jsonString = PackageJson.jsonString; }
            if (string.IsNullOrEmpty(jsonString)) {
                Errors.WriteErrorAndExit("Unable to parse packages.json, please ensure this file exists in the same directory as BAMM.exe", 1);
            }
            try { packageData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<double>>>>(jsonString)!; }
            catch (JsonException jsonEx){ Errors.WriteErrorAndExit($"Critical Error: Failed to parse package data from 'packages.json'. JSON is malformed: {jsonEx.Message}", 1); }
            string version = GetSupportedPackageVersion(packageName!, pythonVersion) ?? "Not Found"; // C# requires notice that the value is for certain not nullable, thus the !
            Console.WriteLine(version);
        }

        readonly static string baseURL = "https://pypi.org/project";

        public static bool IsDeprecated(string packageName, string packageVersion)
        {
            HttpClient client = new();
            string attemptedURL = $"{baseURL}/{packageName}/{packageVersion}";
            

            string unvalidatedMessage = $"""
                BAM Manager (BAMM) was unable to determine the validate {packageVersion}=={packageName}.\n
                This doesn't mean you will run into any issues, BAMM is simply unable to ensure so. 
            """;

            string deprecatedMessage = $"""
                BAM Manager (BAMM) found a deprecated package:\n\n{packageName}=={packageVersion}\n
                Please contact the developer to push a fix.
            """;

            string validMessage = "BAM Manager (BAMM) validated package: {packageName}=={packageVersion}\n";

            try
            {
                HttpRequestMessage request = new(HttpMethod.Get, attemptedURL);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0");
                HttpResponseMessage response = client.SendAsync(request).Result; // Catch Aggregate Exception

                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK) {  
                    Errors.WriteErrorAndContinue(unvalidatedMessage); 
                    return false; 
                }

                HttpContent content = response.Content;
                if (content == null) { 
                    Errors.WriteErrorAndContinue(unvalidatedMessage); 
                    return false; 
                }
                
                string responseBody = content.ReadAsStringAsync().Result; // Catch Aggregate Exception
                
                if (string.IsNullOrEmpty(responseBody)) { 
                    Errors.WriteErrorAndContinue(unvalidatedMessage); 
                    return false; 
                }
                
                if (responseBody.Contains("This release has been yanked<br>")) { 
                    Errors.WriteErrorAndContinue(deprecatedMessage); 
                    return true; 
                }

                if (responseBody.Contains("<span>Latest version</span>") || responseBody.Contains("<span>Newer version available ("))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(validMessage);
                    return true;
                }

            }
            catch { } // Reminder to add AggregateException if encountered.

            Errors.WriteErrorAndContinue(unvalidatedMessage);
            return false;

        }

        public static List<double> GetSupportedPyVersions(string packageName, string packageVersion)
        {

            if (!packageData.TryGetValue(packageName, out Dictionary<string, List<double>>? selectedPackageData))
            {
                Errors.WriteErrorAndExit("Invalid packageName provided, please check your spelling and try again.", 1);
                return []; // C# compiler is dumb, the function i wrote above will exit once done, but it doesnt know that since its static!
            }
            if (selectedPackageData == null || !selectedPackageData.TryGetValue(packageVersion, out List<double>? supportedPyVersions) || supportedPyVersions.Count == 0)
            {
                Errors.WriteErrorAndExit($"Unable to find python versions for package {packageName}=={packageVersion}, please check for typos and try again.", 1);
                return [];
            }
            return supportedPyVersions;


        }

        public static string? GetSupportedPackageVersion(string packageName, double pythonVersion)
        {
            if (!packageData.TryGetValue(packageName, out Dictionary<string, List<double>>? packageVersionMappings) || packageVersionMappings == null)
            {
                Errors.WriteErrorAndExit($"No version of '{packageName}' is supported by Python {pythonVersion}, please check for typos and try again.", 1);
                return null;
            }

            List<string> supportedPackageVersions = [.. packageVersionMappings
                .Where(pair => pair.Value != null && pair.Value.Contains(pythonVersion))
                .Select(pair => pair.Key)];

            if (supportedPackageVersions.Count == 0)
            {
                Console.WriteLine($"No versions of package '{packageName}' found that support Python {pythonVersion}.");
                return null;
            }
            return supportedPackageVersions.First();
        }


    }
}
