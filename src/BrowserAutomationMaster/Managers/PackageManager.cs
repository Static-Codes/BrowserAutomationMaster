using BrowserAutomationMaster.Messaging;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.RequestManager;

namespace BrowserAutomationMaster.Managers
{
    public partial class PackageManager
    {
        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.
        const string packageFormatPattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9._-]*[a-zA-Z0-9])$"; // Regex pulled from https://pypi.org/project/twine/
        [GeneratedRegex(packageFormatPattern)]
        private static partial Regex PrecompiledPackageRegex();
        readonly private static string packagePath = GetPackagesPath();
        readonly private static string baseURL = "https://pypi.org/project";
        private static Dictionary<string, Dictionary<string, List<string>>> packageData = [];
        

        public static string Get(string packageName, string pythonVersion)
        {
            // C# requires notice that the value is for certain not nullable, thus the !
            string packageVersion = GetSupportedPackageVersion(packageName!, pythonVersion) ?? "Not Found";
            return packageVersion; // "Not Found" should never be returned its purely to appease the compiler.
        }

        private static async Task DownloadPackageJSON()
        {
            var baseMessage =
                "Unable to download the required data to install the necessary Python Packages, please try again.\n" +
                $"If this issue persists, please make a bug report at {ISSUES_LINK}\n" + 
                "Error Log:\n";

            var nullMessage = baseMessage + "response was null.";

            try
            {
                var response = await NetworkClient.Instance.GetStringAsync(PACKAGES_LINK);
                if (response == null)
                    Errors.WriteAndExit(nullMessage, 1);
                File.WriteAllText(packagePath, response);
                Success.WriteSuccessMessage("Successfully downloaded required Python package data!\n");
            }
            catch (Exception ex)
            {
                var exMessage = $"{baseMessage}{ex.Message}\n";
                Errors.WriteAndExit(exMessage, 1);
            }
        }


        private static async Task SetPackageData()
        {
            var baseMessage =
                $"Unable to get package data from:\n{packagePath}" +
                $"If this error persists, please make a bug report at {ISSUES_LINK}\n" +
                "Error Log:\n";
            
            try
            {
                var packageJson = await File.ReadAllTextAsync(packagePath);
                packageData = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, List<string>>>>(packageJson) ?? [];
            }

            catch (Exception ex)
            {
                var errMessage = $"{baseMessage}{ex.Message}";
                Errors.WriteAndExit(errMessage, 1);
            }

        }
        public static string? GetSupportedPackageVersion(string packageName, string pythonVersion)
        {
            if (!PrecompiledPackageRegex().IsMatch(packageName))
            {
                Errors.WriteAndExit(
                    message: $"Invalid package name '{packageName}', this package name was not matched using PrecompiledPackageRegex()",
                    status: 1
                );
                return null;
            }
            if (!packageData.TryGetValue(packageName, out Dictionary<string, List<string>>? packageVersionMappings) || packageVersionMappings == null)
            {
                Errors.WriteAndExit(
                    message: $"No version of '{packageName}' is supported by Python {pythonVersion}, please check for typos and try again.",
                    status: 1
                );
            }

            List<string> supportedPackageVersions = [.. packageVersionMappings
                .Where(pair => pair.Value != null && pair.Value.Contains(pythonVersion))
                .Select(pair => pair.Key)];

            if (supportedPackageVersions.Count == 0)
            {
                Errors.WriteAndExit(
                    message: $"No versions of package '{packageName}' found that support Python {pythonVersion}.",
                    status: 1
                );
                return null;
            }
            return supportedPackageVersions.First();
        }

        public static List<string> GetSupportedPyVersions(string packageName, string packageVersion)
        {

            if (!packageData.TryGetValue(packageName, out Dictionary<string, List<string>>? selectedPackageData))
            {
                Errors.WriteAndExit(
                    message: "Invalid packageName provided, please check your spelling and try again.",
                    status: 1
                );
            }

            if (!selectedPackageData.TryGetValue(packageVersion, out List<string>? supportedPyVersions) || supportedPyVersions.Count == 0)
            {
                Errors.WriteAndExit(
                    message: $"Unable to find python versions for package {packageName}=={packageVersion}, please check for typos and try again.",
                    status: 1
                );
            }
            return supportedPyVersions;
        }

        public static async Task<bool> IsDeprecated(string packageName, string packageVersion)
        {

            string url = $"{baseURL}/{packageName}/{packageVersion}";
            

            string unvalidatedMessage = $"""
                BAM Manager (BAMM) was unable to determine the validate {packageVersion}=={packageName}.\n
                This doesn't mean you will run into any issues, BAMM is simply unable to ensure so. 
            """;

            string deprecatedMessage = $"""
                BAM Manager (BAMM) found a deprecated package:\n\n{packageName}=={packageVersion}\n
                Please contact the developer to push a fix.
            """;

            string validMessage = $"BAM Manager (BAMM) validated package: {packageName}=={packageVersion}\n";

            try
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) || uriResult == null) { return false; }
                RequestManager requestManager = RequestManager.Create(uriResult);
                HttpResponseMessage? response = await requestManager.GetAsync(followRedirects: true);
                
                if (response == null)
                    return false; 
              

                response.EnsureSuccessStatusCode();

                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK) {  
                    Errors.Write(unvalidatedMessage); 
                    return false; 
                }

                HttpContent content = response.Content;
                if (content == null) { 
                    Errors.Write(unvalidatedMessage); 
                    return false; 
                }
                
                string responseBody = await content.ReadAsStringAsync(); // Catch Aggregate Exception
                
                if (string.IsNullOrEmpty(responseBody)) { 
                    Errors.Write(unvalidatedMessage); 
                    return false; 
                }
                
                if (responseBody.Contains("This release has been yanked<br>")) { 
                    Errors.Write(deprecatedMessage); 
                    return true; 
                }

                if (responseBody.Contains("<span>Latest version</span>") || responseBody.Contains("<span>Newer version available (")) {
                    Success.WriteSuccessMessage(validMessage);
                    return true;
                }

            }
            catch { } // Reminder to add AggregateException if encountered.

            Errors.Write(unvalidatedMessage);
            return false;

        }

        public static async Task Initalize()
        {
            if (!File.Exists(packagePath))
                await DownloadPackageJSON();

            await SetPackageData();
        }

    }
}
