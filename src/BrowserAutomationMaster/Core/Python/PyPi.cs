using BrowserAutomationMaster.Core.Common;
using System.Net;
using static BrowserAutomationMaster.Core.Common.RequestManager;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;

namespace BrowserAutomationMaster.Core.Python
{
    public partial class PyPiPackage(string PackageName, string PackageVersion, string[] SupportedPython) 
    {
        public string PackageName = PackageName;
        public string PackageVersion = PackageVersion;
        public string[] SupportedPythons = SupportedPython;
    }

    public static class PyPiPackageExtensions 
    {
        public static PyPiPackage? GetPackage(this PyPiPackage[] packages, string packageName) 
        {
            return packages
            .Where(package => package.PackageName.Equals(packageName))
            .FirstOrDefault();
        }
    }
    
    public partial class PyPi
    {
        private readonly static string baseURL = "https://pypi.org/project";
        private readonly static string[] SupportedPythonVersions = [ "3.9", "3.10", "3.11", "3.12", "3.13", "3.14" ];
        private static readonly PyPiPackage[] packageData = 
        [
            new PyPiPackage(
                PackageName: "browserstack-sdk", 
                PackageVersion: "1.31.0", 
                SupportedPython: SupportedPythonVersions
            ),

            new PyPiPackage(
                PackageName: "selenium", 
                PackageVersion: "4.32.0", 
                SupportedPython: SupportedPythonVersions
            ),

            new PyPiPackage(
                PackageName: "selenium-wire",
                PackageVersion: "5.1.0",
                SupportedPython: SupportedPythonVersions
            ),

            new PyPiPackage(
                PackageName: "webdriver_manager",
                PackageVersion: "4.0.2",
                SupportedPython: SupportedPythonVersions

            ),
        ];



        

        public static string GetVersion(string packageName)
        {
            return GetSupportedPackageVersion(packageName);
        }

        private static string GetSupportedPackageVersion(string packageName)
        {
            if (!PrecompiledPackageRegex().IsMatch(packageName))
            {
                WriteAndExit(
                    message: $"Invalid package name '{packageName}', this package name was not matched using PrecompiledPackageRegex()",
                    status: 1
                );
            }

            var package = packageData.GetPackage(packageName);

            if (package == null) 
            {
                WriteAndExit(
                    message: $"Invalid package name '{packageName}', this package name was not matched using PrecompiledPackageRegex()",
                    status: 1
                );
            }

            return package.PackageVersion;
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

    }
}
