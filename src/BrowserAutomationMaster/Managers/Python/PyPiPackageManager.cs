using System.Net;
using System.Text.Json;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.EmbeddedResourceManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Managers.RequestManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers.Python
{
    public partial class PyPiPackageManager
    {
        
        public readonly CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));
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
                "Error Log:";

            try
            {
                await WriteEmbeddedResourceToDisk(
                    resourceName: "packages.json",
                    resourcePattern: "BrowserAutomationMaster.AppData.packages.json",
                    outputPath: packagePath
                );

                WriteSuccessMessage("Successfully downloaded required Python package data!");
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        baseMessage,
                        ex.Message
                    ]), 
                    status: 1
                );
            }
        }
        private static async Task SetPackageData()
        {
            var baseMessage =
                $"Unable to get package data from:{NLC}{packagePath}{NLC}" +
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
                WriteAndExit(errMessage, 1);
            }

        }
        public static string? GetSupportedPackageVersion(string packageName, string pythonVersion)
        {
            if (!PrecompiledPackageRegex().IsMatch(packageName))
            {
                WriteAndExit(
                    message: $"Invalid package name '{packageName}', this package name was not matched using PrecompiledPackageRegex()",
                    status: 1
                );
                return null;
            }

            bool[] invalidStates = [
                !packageData.TryGetValue(packageName, out Dictionary<string, List<string>>? packageVersionMappings), 
                packageVersionMappings == null
            ];

            // Checking if any of the invalidStates are true.
            if (invalidStates.Any(state => state))
            {
                WriteAndExit(
                    message: $"No version of '{packageName}' is supported by Python {pythonVersion}, please check for typos and try again.",
                    status: 1
                );
            }

            // Null forgiveness is used here because: 
            // The null check done in invalidStates is not seen by the compile due to the manner in which it was handled.
            List<string> supportedPackageVersions = [.. packageVersionMappings!
                .Where(pair => pair.Value != null && pair.Value.Contains(pythonVersion))
                .Select(pair => pair.Key)];

            if (supportedPackageVersions.Count == 0)
            {
                WriteAndExit(
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
                WriteAndExit(
                    message: "Invalid packageName provided, please check your spelling and try again.",
                    status: 1
                );
            }

            if (!selectedPackageData.TryGetValue(packageVersion, out List<string>? supportedPyVersions) || supportedPyVersions.Count == 0)
            {
                WriteAndExit(
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
                if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult) || uriResult == null) { 
                    return false; 
                }

                RequestManager requestManager = Create(uriResult);
                HttpResponseMessage? response = await requestManager.GetAsync(followRedirects: true);
                
                if (response == null) {
                    return false; 
                }
              

                response.EnsureSuccessStatusCode();

                HttpStatusCode statusCode = response.StatusCode;
                if (statusCode != HttpStatusCode.OK) {  
                    Write(unvalidatedMessage); 
                    return false; 
                }

                HttpContent content = response.Content;
                if (content == null) { 
                    Write(unvalidatedMessage); 
                    return false; 
                }
                
                string responseBody = await content.ReadAsStringAsync(); // Catch Aggregate Exception
                
                if (string.IsNullOrEmpty(responseBody)) { 
                    Write(unvalidatedMessage); 
                    return false; 
                }
                
                if (responseBody.Contains("This release has been yanked<br>")) { 
                    Write(deprecatedMessage); 
                    return true; 
                }

                if (responseBody.Contains("<span>Latest version</span>") || responseBody.Contains("<span>Newer version available (")) {
                    WriteSuccessMessage(validMessage);
                    return true;
                }

            }
            catch { } // Reminder to add AggregateException if encountered.

            Write(unvalidatedMessage);
            return false;

        }

        public static async Task Initalize()
        {
            EnsureDirectoryExists(AppDataDirectory);

            if (!File.Exists(packagePath)) 
            {
                var resourceName = "packages.json";
                var resourcePattern = "BrowserAutomationMaster.AppData.packages.json";

                // Declaration includes "using" for manual memory management to the Garbage Collector.
                using Stream stream = EmbeddedResourceManager.GetEmbeddedResource(resourceName, resourcePattern);

                if (stream.Length > int.MaxValue) {
                    await DownloadPackageJSON();
                } else {
                    await EmbeddedResourceManager.WriteEmbeddedResourceToDisk(
                        stream, 
                        resourceName, 
                        outputPath: packagePath
                    );
                }
            }

            await SetPackageData();
        }

    }
}
