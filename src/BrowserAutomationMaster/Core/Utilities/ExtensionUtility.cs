using BrowserAutomationMaster.Core.Python;
using BrowserAutomationMaster.Core.Messaging;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.RequestManager;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Input;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.Core.Utilities.UserInfoUtility;


namespace BrowserAutomationMaster.Core.Utilities;

// This class will be used to parse the commands of
// feature "add-extension" "file://path/to/firefox/extension.xpi"
// feature "add-extension" "https://url/to/firefox/extension.xpi"
// feature "add-extension" "file://path/to/chrome/extension.crx"
// feature "add-extension" "https://url/to/chrome/extension.crx"
public class ExtensionUtility(string rawExtensionPath, string browserName, string[]? args = null)
{
    public string RawExtensionPath { get; init; } = rawExtensionPath;
    public string ExtensionPath { get; init; } = SanitizeExtensionPath(rawExtensionPath);
    public bool IsLocalFile { get; init; } = CheckLocalFileStatus(rawExtensionPath);
    public bool IsURL { get; init; } = CheckURLStatus(rawExtensionPath);
    public bool IsChromeExtension = CheckChromeStatus(rawExtensionPath, browserName);
    public bool IsFirefoxExtension = CheckFirefoxStatus(rawExtensionPath, browserName);
    public bool IsFirefoxDirectDownload = CheckForDirectFirefoxDownload(rawExtensionPath);

    private readonly bool exitOnFail = CheckForExitArgStatus(args);
    
    private static bool CheckChromeStatus(string rawExtensionPath, string browserName)
    {
        return 
            rawExtensionPath.EndsWith(".crx") ||
            rawExtensionPath.StartsWith("https://chromewebstore.google.com/detail/") && 
            browserName.Equals("chrome");
    }

    private static bool CheckFirefoxStatus(string rawExtensionPath, string browserName) 
    {
        return 
            rawExtensionPath.EndsWith(".xpi") || 
            rawExtensionPath.StartsWith("https://addons.mozilla.org/en-US/firefox/addon/") && 
            browserName.Equals("firefox");
    }

    private static bool CheckForDirectFirefoxDownload(string rawExtensionPath) 
    {
        return 
            rawExtensionPath.StartsWith("https://addons.mozilla.org/firefox/downloads/file/") &&
            rawExtensionPath.EndsWith(".xpi");
    }

    private static bool CheckForExitArgStatus(string[]? args) 
    {
        return args != null && args.Any(a => a.Equals("--exit-on-ext-fail"));
    }

    private static bool CheckURLStatus(string rawExtensionPath) 
    {
        return 
            rawExtensionPath.StartsWith("http://") || 
            rawExtensionPath.StartsWith("https://");
    }

    private static bool CheckLocalFileStatus(string rawExtensionPath) => rawExtensionPath.StartsWith("file://");

    public static ExtensionUtility[] CreateExtensionArrayFromPaths(string[] paths, string browserName) 
    {
        var extensionManagers = new ExtensionUtility[paths.Length];
        
        for (int i = 0; i < paths.Length; i++)
        {
            extensionManagers[i] = new ExtensionUtility(paths[i], browserName);
        }

        return extensionManagers; 
    }

    private static string DecodeMatch(byte[] rentedBuffer)
    {
        int manifestStartIndex = 4;
        
        // The number of bytes expected for the whole decoded string, not just the manifest ID;
        int resultSize = 44;
        
        // The overscan is only one byte  
        int overscanAmount = 1;

        // The expectedBye
        int expectedManifestSize = 32;

        int expectedSizeDifference = 12;
        int currentSizeDifference = resultSize - expectedManifestSize;


        // This is the whole decoded object.
        byte[] resultBytes = new byte[resultSize];

        // This will overscan by one byte due to the limitations of Base64.DecodeFromUtf8
        var status = Base64.DecodeFromUtf8(
            rentedBuffer.AsSpan(manifestStartIndex, resultSize), 
            resultBytes,
            out int bytesConsumed, 
            out int bytesWritten
        );



        // Debug values do not add to production releases
        // Console.WriteLine(bytesConsumed);       // Should return 44 (resultSize)
        // Console.WriteLine(bytesWritten);        // Should return 33 (32 intented + 1 overscan)
        // Console.WriteLine(rentedBuffer.Length); // Should return 512

        if (bytesWritten != expectedManifestSize + overscanAmount) {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    "Unable to decode the base64 encoded span containing the manifest ID for the provided chrome extension.",
                    "Error Log:",
                    $"Expected {expectedManifestSize} decoded bytes but received {bytesWritten} bytes."
                ]),
                status: 1
            );
        }
        
        if (status != OperationStatus.Done)
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    "Unable to decode the base64 encoded span containing the manifest ID for the provided chrome extension.",
                    "Error Log:",
                    $"The OperationStatus associated with DecodeMatch returned {status}"
                ]),
                status: 1
            );
        }

        if (expectedSizeDifference != currentSizeDifference) 
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    "Unable to decode the base64 encoded span containing the manifest ID for the provided chrome extension.",
                    "Error Log:",
                    $"Expected {expectedSizeDifference} extra bytes while processing but received {currentSizeDifference} bytes."
                ]),
                status: 1
            );
        }

        try {
            
            // var manifestIDSlice = resultBytes[..bytesWritten].AsSpan().Slice(4, 32);

            // This will allocate roughly 40-50 bytes to the stack.
            var lastValidIndex = bytesConsumed - currentSizeDifference;

            // Debug values do not add to production releases
            // var thing = Encoding.UTF8.GetString(resultBytes)[.. lastValidIndex];
            // File.WriteAllText("/home/nerdy/Desktop/test1234567", thing);

            // This is the decoded manifest ID
            return Encoding.UTF8.GetString(resultBytes)[.. lastValidIndex];
        }
        catch (Exception ex) 
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    "Unable to decode the base64 encoded span containing the manifest ID for the provided chrome extension.",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1
            );
        }
        return null; // This wont be executed, purely to appease Rosyln.
    }

    public async Task<bool> ExtensionExists() 
    {
        if (IsLocalFile) { return File.Exists(ExtensionPath); }

        if (IsURL) 
        {
            if (await SiteIsPingable(ExtensionPath)) { return true; }

            Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
            Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
            return false;
        }

        Warning.Write("Unable to make contact with the website hosting the extension provided.");
        return false;
    }

    public async Task<MemoryStream?> GetExtensionContents() 
    {
        await ValidatePreconditionsAsync();

        if (IsURL) { return await GetHostedExtensionStreamAsync(); }
        
        if (IsLocalFile) { return await GetLocalExtensionStreamAsync(); }
        
        return null;
    }

    private async Task ValidatePreconditionsAsync()
    {
        if (!IsURL && !IsLocalFile) 
        {
            Console.WriteLine("The provided extension does not contain a valid URI protocal.");
            Console.Write(Environment.NewLine);
            Console.WriteLine(ExtensionPath);
            Console.WriteLine(Environment.NewLine);
            
            Console.WriteLine("Valid protocols include:");
            Console.WriteLine("- file://");
            Console.WriteLine("- http://");
            Console.WriteLine("- https://");
            Console.Write(Environment.NewLine);

            WriteAndExit(
                message: $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                status: 1, 
                writePlatformDebugInfo: true
            );
        }

        if (!IsChromeExtension && !IsFirefoxExtension) 
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                    "Error Log:",
                    "An invalid url was provided.",
                    "Please ensure the provided url matches one of the following formats, depending on your selected browser:",
                    Environment.NewLine,
                    "- https://chromewebstore.google.com/detail/<extension-name>/<manifest-id>",
                    "- https://addons.mozilla.org/en-US/firefox/addon/<extension-name>/",
                    "- https://addons.mozilla.org/firefox/downloads/file/<extension-id>/<extension-name>.xpi",
                    "- file://path/to/firefox/extension.xpi",
                    "- file://path/to/chrome/extension.crx",
                ]),
                status: 1, 
                writePlatformDebugInfo: true
            );
        }

        if (!await ExtensionExists()) 
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                    "Error Log:",
                    "The provided extension could not be found.",
                ]),
                status: 1, 
                writePlatformDebugInfo: true
            );
        }
    }

    private async Task<MemoryStream> GetHostedExtensionStreamAsync()
    {
        // Handles both Chrome and Firefox extensions.
        byte[] contents = await GetHostedExtensionContents();
        
        // Note: Removed the "using var" from the original implementation 
        // to prevent returning a disposed stream to the caller.
        return new MemoryStream(contents);
    }

    private async Task<MemoryStream?> GetLocalExtensionStreamAsync()
    {
        try
        {
            LogFileReadStart();
            await Task.Delay(500);

            byte[] finalBuffer = File.ReadAllBytes(ExtensionPath);

            LogFileReadSuccess(finalBuffer.Length);
            await Task.Delay(500);

            await ValidateLocalExtensionBufferAsync(finalBuffer);

            // Validation passed without exiting; safe to wrap and return.
            return new MemoryStream(finalBuffer);
        }
        catch (Exception ex) 
        {
            WriteAndExit(
                message: string.Join(Environment.NewLine, [
                    $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1, 
                writePlatformDebugInfo: true
            );
            return null;
        }
    }

    private async Task ValidateLocalExtensionBufferAsync(byte[] buffer)
    {
        if (IsChromeExtension) { IsCrxContentValid(buffer, exitOnFail); }
        else if (IsFirefoxExtension) { IsXpiContentValid(buffer, exitOnFail); }
    }
    private static void GetFirstRegexMatch(ReadOnlyMemory<char> romContents, string RegexPattern, out byte[] finalBuffer, out int finalLength)
    {
        finalBuffer = [];
        finalLength = 0;

        var valueMatches = Regex.EnumerateMatches(romContents.Span, RegexPattern);

        foreach (var match in valueMatches) 
        {
            var ROM = romContents.Slice(match.Index, match.Length);

            finalBuffer = Encoding.UTF8.GetBytes(ROM.ToArray());
            finalLength = finalBuffer.Length;
            return;
        }
    }

    private async Task<byte[]> GetHostedExtensionContents()
    {
        try 
        {
            if (IsFirefoxExtension && IsFirefoxDirectDownload) {
                return await GetFirefoxDirectDownloadContentsAsync();
            }
            
            if (IsFirefoxExtension && !IsFirefoxDirectDownload) {
                return await GetFirefoxIndirectDownloadContentsAsync();
            }
            
            if (IsChromeExtension) {
                return await GetChromeExtensionContentsAsync();
            }

            return [];
        }
        catch (Exception ex) 
        {
            WriteAndExit
            (
                message: string.Join(Environment.NewLine, [
                    $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {ExtensionPath}",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1, 
                writePlatformDebugInfo: true
            );
            
            return [];
        }
    }

    private async Task<byte[]> GetChromeExtensionContentsAsync()
    {
        try 
        {
            // Resolves redirects and fetches the CRX package metadata via CrxFetch
            var result = await CrxFetch.CrxDownloader.FetchLinkAsync(ExtensionPath);
            
            Console.WriteLine($"Resolved Chrome Extension Package URI: {result.PackageUri}");
            Console.WriteLine();
            
            // If CrxFetch automatically verified the signature during the fetch, we can check it right away.
            if (!result.Info.SignatureVerified)
            {
                WriteAndExit(
                    message: string.Join(Environment.NewLine, [
                        "An exception occured while retrieving the contents of the provided extension.",
                        "Error Log:",
                        "The provided CRX failed signature validation. It is either corrupt or modified."
                    ]),
                    status: 1
                );
            }

            if (!IsCrxContentValid(result.Package, exitOnFail)) { return []; }

            return result.Package;
        }
        catch (Exception ex) 
        {
            WriteAndExit
            (
                message: string.Join(Environment.NewLine, [
                    "An exception occured while retrieving the contents of the provided extension.",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1
            );
            
            return [];
        }
    }
    
    private async Task<byte[]> GetFirefoxDirectDownloadContentsAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        return await NetworkClient.Instance.GetByteArrayAsync(ExtensionPath, cts.Token);
    }

    private async Task<byte[]> GetFirefoxIndirectDownloadContentsAsync()
    {
        try 
        {   
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            var htmlMemory = await NetworkClient.GetReadOnlyMemoryCharsFromURL(url: ExtensionPath, exitOnFail: exitOnFail);

            GetFirstRegexMatch(htmlMemory, XPIExtensionPathRegexPattern, out byte[] finalBuffer, out int bytesWritten);
            
            if (bytesWritten == 0) 
            {
                WriteAndExit(
                    message: string.Join(Environment.NewLine, [
                        "An exception occured while retrieving the contents of the provided extension.",
                        "Error Log:",
                        "The returned buffer is empty."
                    ]),
                    status: 1
                );
            }

            var downloadURL = Encoding.UTF8.GetString(finalBuffer);

            Console.WriteLine("Validating .XPI extension at: {0}", ExtensionPath);
            Console.WriteLine();
            await Task.Delay(500);

            Console.WriteLine("Using download URL: {0}", downloadURL);
            Console.WriteLine();
            await Task.Delay(500);
            
            var finalURL = GetXPIDownloadURL(downloadURL.AsSpan());

            var contents = await NetworkClient.Instance.GetByteArrayAsync(finalURL, cts.Token);

            if (contents.Length == 0) 
            {
                WriteAndExit(
                    message: string.Join(Environment.NewLine, [
                        "An exception occured while retrieving the contents of the provided extension.",
                        "Error Log:",
                        "The response returned an empty stream."
                    ]),
                    status: 1
                );
            }

            if (!IsXpiContentValid(contents)) { return []; }
            
            return contents;
        }
        catch (Exception ex) 
        {
            WriteAndExit
            (
                message: string.Join(Environment.NewLine, [
                    "An exception occured while retrieving the contents of the provided extension.",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1
            );
            
            return [];
        }
    }

    private static async Task<string> GetLatestChromeVersion() 
    {
        var jsonData = await NetworkClient.GetReadOnlyMemoryBytesFromURL(CHROME_VERSION_URL, timeout: 5);
            
        void ReadJsonData(out string version) 
        {
            version = string.Empty;
            var reader = new Utf8JsonReader(jsonData.Span);
                
            try 
            {
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("version"u8)) {
                        reader.Read();

                        version = Encoding.UTF8.GetString(reader.ValueSpan[..3]) + ".0.0.0";
                        // version = Encoding.UTF8.GetString(reader.ValueSpan);
                        break;
                    }
                }
            }
            catch (Exception ex) 
            {
                WriteAndExit(
                    message: string.Join(Environment.NewLine, [
                        "An exception occured while retrieving the latest version of Google Chrome, during .CRX file validation.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
        }

        ReadJsonData(out string latestChromeVersion);
        return latestChromeVersion;
    }

    private static string GetXPIDownloadURL(ReadOnlySpan<char> chars)
    {
        int lastSlashIndex = chars.LastIndexOf('/');
        if (lastSlashIndex == -1 || chars.Count('/') != 7)
        {
            WriteAndExit(
                "",
                status: 1
            );
        }
        
        // Not sure where this overscan comes from but it seems to be a static overscan of 35 bytes starting at chars.Length - 35
        int emptyByteCount = 35;

        ReadOnlySpan<char> urlPrefix = chars[..lastSlashIndex];
        ReadOnlySpan<char> attachmentPart = "/type:attachment";
        ReadOnlySpan<char> urlSuffix = chars[lastSlashIndex..^emptyByteCount];

        return string.Concat(urlPrefix, attachmentPart, urlSuffix);
    }

    private void LogFileReadStart()
    {
        Console.Write(Environment.NewLine);
        Console.Write("Reading contents from: ");
        Warning.Write(RawExtensionPath, noNewLines: true);
        Console.WriteLine(Environment.NewLine);
    }

    private static void LogFileReadSuccess(int bufferLength)
    {
        WriteSuccessMessage("Wrote the contents to a buffer with a size of ", noNewLines: true);
        Warning.Write($"{bufferLength / Math.Pow(1024, 2):0.00} MB ", noNewLines: true);
        Console.WriteLine(Environment.NewLine);
    }


    private static void LogSuccess(string key, ReadOnlyMemory<byte> value)
    {
        int charCount = value.Length * 2;

        // The usage of 'stackalloc char' here is safe because LogSuccess is NOT async
        Span<char> hexBuffer = stackalloc char[charCount];
        ReadOnlySpan<byte> sourceBytes = value.Span; 

        for (int i = 0; i < sourceBytes.Length; i++)
        {
            // Formats each byte into the buffer
            var formattedBuffer = hexBuffer[(i * 2)..];
            sourceBytes[i].TryFormat(formattedBuffer, out _, "X2");
        }

        WriteSuccessMessage($"Located the hex sequence for {key} (", noNewLines: true);

        // Utilizes a direct span write to avoid heap allocation
        Console.Out.Write(hexBuffer);
        WriteSuccessMessage($")", noNewLines: true);
        Console.WriteLine();
    }

    private static string SanitizeExtensionPath(string rawExtensionPath) => rawExtensionPath.Replace("file://", "");

    private static bool IsCrxContentValid(ReadOnlySpan<byte> finalBuffer, bool exitOnFail = false)
    {
        Console.WriteLine("Scanning the provided .CRX file, please wait.");
        Console.WriteLine();

        CrxFetch.CrxInfo? crxInfo = null;

        try {
            crxInfo = CrxFetch.CrxFile.Inspect(finalBuffer);
        }
        catch (Exception ex)
        {
            Warning.Write("An exception occured while attempting to validate the provided chrome extension.");
            if (exitOnFail) { WriteAndExit(ex.Message, status: 1); }
        }
        
        void HandleCrxValidationError(string failureMessage)
        {
            if (exitOnFail) { WriteAndExit(failureMessage, status: 1); }
                
            // Notifying the user instead of exiting if exitOnFail is false.
            Write(failureMessage);

            Console.WriteLine("By default, BAMM does not exit on a failed extension check.");
            Console.WriteLine();
            Console.WriteLine("To change this behavior, please pass --exit-on-ext-fail");
        }

        // Evaluates to an error string if any of the patterns are matched, or null if not.
        string? validationError = crxInfo switch
        {
            { SignatureVerified: false } => 
                "The provided CRX failed signature validation, it is either corrupt or modified.",

            { ExtensionId: null } => 
                "The provided CRX is missing an extension ID, therefore it cannot be validated.",

            { ExtensionId: string id } when id.Length < 32 => 
                $"The provided CRX has an invalid length of {id.Length}, it could not be validated.",

            _ => null 
        };

        if (validationError is not null) { 
            HandleCrxValidationError(validationError); 
            return false;
        }

        return true; 
    }

    private bool IsXpiContentValid(ReadOnlySpan<byte> finalBuffer, bool exitOnFail = false)
    {
        try 
        {
            Console.WriteLine("Scanning the provided .XPI file, please wait.");
            Console.WriteLine();

            Console.WriteLine("Checking for the presence of the documented XPI Magic Numbers..");

            bool isValid = true;

            // Checking for the presence XPI Magic Numbers
            if (finalBuffer.IndexOf(XPIMagicBytes.Span) >= 0) 
            {
                WriteSuccessMessage("Located the documented XPI Magic Numbers.", noNewLines: true);
                Console.WriteLine();
            }
            else 
            {
                string failMsg = "Failed to locate the documented XPI Magic Numbers on the provided .XPI file.";
                
                if (exitOnFail) { WriteAndExit(failMsg, status: 1); } 
                
                Warning.Write(failMsg);
                Console.WriteLine("By default, BAMM does not exit on a failed extension check.");
                Console.WriteLine();
                Console.WriteLine("To change this behavior, please pass --exit-on-ext-fail");
                
                isValid = false;
            }

            for (int i = 0; i < XPIContentChecks.Count; i++) 
            {
                var element = XPIContentChecks.ElementAt(i);
                Console.WriteLine();
                Console.WriteLine($"Scanning for {element.Key}..");
                        
                var found = finalBuffer.IndexOf(element.Value.Span) >= 0;

                if (found) { 
                    LogSuccess(element.Key, element.Value); 
                    continue;
                } 
                
                string hexFailMsg = $"Unable to locate the hex sequence for {element.Key}{Environment.NewLine}";
                
                if (exitOnFail) { WriteAndExit(message: hexFailMsg, status: 1, writePlatformDebugInfo: false); }

                Warning.Write(hexFailMsg);
                isValid = false;
                
            }
            
            return isValid;
        }

        catch (Exception ex) 
        {
            WriteAndExit
            (
                message: string.Join(Environment.NewLine, [
                    $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {ExtensionPath}",
                    "Error Log:",
                    ex.Message
                ]),
                status: 1
            );
            
            return false;
        }
    }

    public async Task<string?> WriteExtensionContents(MemoryStream? contents) 
    {
        string? outputPath = null;
        if (contents == null) 
        {
            WriteAndExit
            (
                message: 
                    string.Join(Environment.NewLine, [
                        "A fatal error occured while writing the content buffer of the provided extension to a file.",
                        "Error Log:",
                        "Contents param returned a null value in WriteExtensionContents"
                    ]),
                status: 1
            );
        }

        // Due to previous checks on contentLength header and memoryStream size, this cast will not throw an exception.
        var totalBufferSize = (int)contents.Length;

        var chunkSize1 = (int)Math.Pow(1024, 2); // 1MB
        var chunkSize8 = chunkSize1 * 8; // 8MB

        // If the totalBufferSize is <= 10MB the file is read at once.
        // 1MB chunks on higher end systems (4CPU)
        // 8MB chunks on lower end systems
        var assignedChunkSize = totalBufferSize <= chunkSize1 * 10 ? totalBufferSize : chunkSize8;

        var memoryInfo = GlobalUserInfo.HardwareInformation.MemoryInfo;
        var coreCount = GlobalUserInfo.HardwareInformation.CpuCoreCount;

        var useLowChunkBuffer = 
            memoryInfo.HasValue && 
            memoryInfo.Value.TotalMemory >= totalBufferSize * 32 && 
            memoryInfo.Value.FreeMemory >= totalBufferSize * 16;

        // Using a 1MB buffer for higher end systems. 
        if (coreCount > 8 && useLowChunkBuffer && assignedChunkSize == chunkSize8){
            assignedChunkSize = chunkSize1;
        }

        var extensionsDirectory = GetExtensionsDirectory(); 
        EnsureDirectoryExists(extensionsDirectory); // Ensures the extensions directory exists

        var formattedContentLength = contents.Length / (double) chunkSize1;

        Console.Write(Environment.NewLine);
        Console.WriteLine("Due to security restrictions imposed by modern browser, BAMM must write the extension to a file before Selenium can access it.");
        Console.Write(Environment.NewLine);
        Console.Write("Extensions used by BAMM are written to: ");
        
        Warning.Write(extensionsDirectory, noNewLines: true);
        Console.WriteLine(Environment.NewLine);
        
        // {extensionsDirectory}{Environment.NewLine}");
        Console.Write($"The current extension's size is ");
        Warning.Write($"{formattedContentLength:0.00} MB", noNewLines: true);
        Console.WriteLine(Environment.NewLine);

        var userChoice = AskForInput("Would you like to continue? [y/n]: ");


        if (ConditionRejected(userChoice)) 
        {
            WriteAndExit
            (
                message: "Operation cancelled by user, BAM Manager (BAMM) will exit now.", 
                status: 1
            ); 
        }

        var fileExt = IsChromeExtension ? ".crx" : ".xpi";
        
        var fileName = string.Empty;

        try 
        {

            Console.Write(Environment.NewLine);
            while (!fileName.EndsWith(fileExt)) 
            {
                Console.Write($"Please ensure the filename you enter ends with ");
                Warning.Write($"'{fileExt}'", noNewLines: true);
                Console.WriteLine(Environment.NewLine);

                fileName = AskForInput("Filename: ");
            }

            outputPath = Path.Combine(extensionsDirectory, fileName);

            
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await contents.CopyToAsync(fileStream, assignedChunkSize, cts.Token);

            Console.Write(Environment.NewLine);
            WriteSuccessMessage("Successfully downloaded the extension to: ", noNewLines: true);
            Warning.Write(outputPath, noNewLines: true);
            Console.WriteLine(Environment.NewLine);
        }

        catch (Exception ex) 
        {
            WriteAndExit
            (
                message: 
                    string.Join(Environment.NewLine, [
                        "A fatal error occured while writing the content buffer of the provided extension.",
                        "Error Log:",
                        ex.Message
                    ]),
                status: 1
            );
        }

        return outputPath;
    }
}