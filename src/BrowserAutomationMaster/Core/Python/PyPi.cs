using BrowserAutomationMaster.Core.Common;
using System.Net;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.RequestManager;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;

namespace BrowserAutomationMaster.Core.Python;

/// <summary> A typed representation of a package hosted on https://pypi.org </summary>
/// <param name="PackageName"> The name of the Python Package desired. </param>
/// <param name="PackageVersion"> The latest version of PackageName that supports all versions found in SupportedPythons. </param>
/// <param name="SupportedPython">A string array of Python versions that packageName should support. </param>
public partial class PyPiPackage(string PackageName, string PackageVersion, string[] SupportedPython) 
{
    /// <summary> The name of the Python Package desired. </summary>
    public string PackageName = PackageName;

    /// <summary> The latest version of PackageName that supports all versions found in SupportedPythons. </summary>
    public string PackageVersion = PackageVersion;

    /// <summary> A string array of Python versions that packageName should support. </summary>
    public string[] SupportedPythons = SupportedPython;
}

public static class PyPiPackageExtensions {

    /// <summary> Retrieves a package from the current array of <see cref="PyPiPackage"/>. </summary>
    /// <param name="packages">An array of <see cref="PyPiPackage"/> to query. </param>
    /// <param name="packageName">The desired packageName to retrieve from <see cref="packages"/>.</param>
    /// <returns>The resolved <see cref="PyPiPackage"/> if found, otherwise null.</returns>
    public static PyPiPackage? GetPackage(this PyPiPackage[] packages, string packageName) {
        return packages.FirstOrDefault(package => package.PackageName.Equals(packageName));
    }
}

public partial class PyPi
{
    /// <summary> The base url that is queried when using this class. </summary>
    private readonly static string baseURL = "https://pypi.org/project";
    
    /// <summary> The versions of Python that the packages below should support. </summary>
    private readonly static string[] SupportedPythonVersions = [..
        pythonVersionMapping.Values.TakeLast(pythonVersionMapping.Values.Count - 1)
    ];

    /// <summary>
    /// This PyPiPackage data drives the import logic in <see cref="BrowserFunctions"/> and <see cref="Transpiler"/>.
    /// </summary>
    private static readonly PyPiPackage[] packageData = 
    [
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

    /// <summary> Queries <see cref="packageData" for the provided packageName. /> </summary>
    /// <param name="packageName">The package name to query.</param>
    /// <returns>The supported package version, if any, for the provided packageName.</returns>
    public static string GetSupportedPackageVersion(string packageName)
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
    

    /// <summary> Determines if a specific version of the provided package is deprecated on PyPi. </summary>
    /// <param name="packageName">The name of the python package to validate.</param>
    /// <param name="packageVersion">The version of the specified package to validate.</param>
    /// <returns>A boolean, true if the package is deprecated, otherwise false.</returns>
    public static async Task<bool> IsDeprecated(string packageName, string packageVersion)
    {
        // string url = $"{baseURL}/{packageName}/{packageVersion}";
        string url = Path.Combine(baseURL, packageName, packageVersion);
        

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

            RequestManager requestManager = Create(uriResult);
            HttpResponseMessage? response = await requestManager.GetAsync(followRedirects: true);
            
            if (response == null) { return false; }
            

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
