using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Managers.Messaging;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.Common.RequestManager;
using static BrowserAutomationMaster.Managers.Common.RegexManager;
using static BrowserAutomationMaster.Managers.Messaging.Errors;
using static BrowserAutomationMaster.Managers.Messaging.Input;
using static BrowserAutomationMaster.Managers.Messaging.Success;


namespace BrowserAutomationMaster.Managers.Utilities
{
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

        private static string BuildDownloadUrl(string manifestID, string versionID) 
        {
            return $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion={versionID}&acceptformat=crx2,crx3&x=id%3D{manifestID}%26uc";
        }
        
        private static bool CheckChromeStatus(string rawExtensionPath, string browserName)
        {
            return 
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
                    message: string.Join(NLC, [
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
                    message: string.Join(NLC, [
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
                    message: string.Join(NLC, [
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
                    message: string.Join(NLC, [
                        "Unable to decode the base64 encoded span containing the manifest ID for the provided chrome extension.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return null; // This wont be executed, purely to appease Rosyln.
        }

        public async Task<MemoryStream?> GetExtensionContents() 
        {
            if (!IsURL && !IsLocalFile) 
            {
                Console.WriteLine($"The provided extension does not contain a valid URI protocal.");
                Console.Write(NLC);
                Console.WriteLine(ExtensionPath);
                Console.WriteLine(NLC);
                
                Console.WriteLine("Valid protocols include:");
                Console.WriteLine("- file://");
                Console.WriteLine("- http://");
                Console.WriteLine("- https://");
                Console.Write(NLC);

                WriteAndExit
                (
                    message: $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                    status: 1, 
                    writePlatformDebugInfo: true
                );
            }

            if (!IsChromeExtension && !IsFirefoxExtension) 
            {
                WriteAndExit
                (
                    
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                        "Error Log:",
                        "An invalid url was provided.",
                        "Please ensure the provided url matches one of the following formats, depending on your selected browser:",
                        NLC,
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
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                        "Error Log:",
                        "The provided extension could not be found.",
                    ]),
                    status: 1, 
                    writePlatformDebugInfo: true
                );
            }

            else if (IsURL) 
            {
                using var memoryStream = new MemoryStream();
                byte[] contents = [];
                
                // Handles both Chrome and Firefox extensions.
                contents = await GetHostedExtensionContents();
                await memoryStream.WriteAsync(contents);
                
                memoryStream.Position = 0;
                
                return memoryStream;
            }

            else if (IsLocalFile) 
            {
                // Retrieving contents from a local Firefox Extension.
                try
                {
                    Console.Write(NLC);
                    Console.Write("Reading contents from: ");
                    Warning.Write(RawExtensionPath, noNewLines: true);
                    Console.WriteLine(NLC);
                    await Task.Delay(500);

                    var finalBuffer = File.ReadAllBytes(ExtensionPath);

                    WriteSuccessMessage("Wrote the contents to a buffer with a size of ", noNewLines: true);
                    Warning.Write($"{finalBuffer.Length / Math.Pow(1024, 2):0.00} MB ", noNewLines: true);
                    Console.WriteLine(NLC);
                    await Task.Delay(500);

                    // According to .NET Documentation on byte[].AsMemory(start, length)
                    // It creates a new memory region over the portion of the target array beginning at a specified position with a specified length.
                    // This is done to prevent garbage data from being rented, single ArrayPool always returns more memory than is required.
                    ReadOnlyMemory<byte> dataToValidate = finalBuffer.AsMemory();

                    return (IsChromeExtension, IsFirefoxExtension) switch {
                        (true, false) => await ValidateCRXContents(dataToValidate),
                        (false, true) => await ValidateXPIContents(dataToValidate, exitOnFail),
                        _ => null
                    };
                }

                catch (Exception ex) 
                {
                    WriteAndExit
                    (
                        
                        message: string.Join(NLC, [
                            $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                            "Error Log:",
                            ex.Message
                        ]),
                        status: 1, 
                        writePlatformDebugInfo: true
                    );
                }
            }

            return null;
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
            byte[] contents = [];
            try 
            {
                # region "Direct Firefox Extension Download"
                if (IsFirefoxExtension && IsFirefoxDirectDownload) 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    contents = await NetworkClient.Instance.GetByteArrayAsync(ExtensionPath, cts.Token);
                }
                # endregion "Direct Firefox Extension Download"

                # region "Indirect Firefox Extension Download"
                else if (IsFirefoxExtension && !IsFirefoxDirectDownload) 
                {

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var htmlMemory = await NetworkClient.GetReadOnlyMemoryCharsFromURL(url: ExtensionPath, exitOnFail: exitOnFail);

                    // // Debugging only do not add to production
                    // var htmlContents = await RequestManager.NetworkClient.GetReadOnlyMemoryBytesFromURL(ExtensionPath);
                    // Console.WriteLine(Encoding.UTF8.GetString(htmlContents.ToArray()));
                    // return [];

                    GetFirstRegexMatch(htmlMemory, XPIExtensionPathRegexPattern, out byte[] finalBuffer, out int bytesWritten);
                    
                    // This url will be 75-150 bytes.
                    var downloadURL = Encoding.UTF8.GetString(finalBuffer);
            
                    try 
                    {   
                        // Ensuring there were bytes written.
                        if (bytesWritten == 0) 
                        {
                            WriteAndExit(
                                message: string.Join(NLC, [
                                    "An exception occured while retrieving the contents of the provided extension.",
                                    "Error Log:",
                                    "The returned buffer is empty."
                                ]),
                                status: 1
                            );
                        }

                        Console.WriteLine("Validating .XPI extension at: {0}", ExtensionPath);
                        Console.Write(NLC);
                        await Task.Delay(500);

                        Console.WriteLine("Using download URL: {0}", downloadURL);
                        Console.Write(NLC);
                        await Task.Delay(500);
                        
                        
                        
                        var finalURL = GetXPIDownloadURL(downloadURL.AsSpan());

                        contents = await NetworkClient.Instance.GetByteArrayAsync(
                            finalURL, 
                            cts.Token
                        );


                        if (contents.Length == 0) 
                        {
                            WriteAndExit(
                                message: string.Join(NLC, [
                                    "An exception occured while retrieving the contents of the provided extension.",
                                    "Error Log:",
                                    "The response returned an empty stream."
                                ]),
                                status: 1
                            );
                        }

                        await ValidateXPIContents(contents);
                    }

                    catch (Exception ex) 
                    {
                        WriteAndExit
                        (
                            message: string.Join(NLC, [
                                "An exception occured while retrieving the contents of the provided extension.",
                                "Error Log:",
                                ex.Message
                            ]),
                            status: 1
                        );
                    }
                }
                # endregion "Indirect Firefox Extension Download"

                # region "Direct Chrome Extension Download"
                else if (IsChromeExtension) 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var htmlMemory = await NetworkClient.GetReadOnlyMemoryCharsFromURL(ExtensionPath, exitOnFail: exitOnFail);
                    
                    // // Debugging only do not add to production
                    // var htmlContents = await RequestManager.NetworkClient.GetReadOnlyMemoryBytesFromURL(ExtensionPath);
                    // Console.WriteLine(Encoding.UTF8.GetString(htmlContents.ToArray()));
                    // return [];

                    GetFirstRegexMatch(htmlMemory, CRXExtensionIDRegexPattern, out byte[] finalBuffer, out int bytesWritten);
                    
                    // Decoded manifest ID (32 bytes).
                    var manifestID = DecodeMatch(finalBuffer);
            
                    try 
                    {   
                        // Ensuring there were bytes written.
                        if (bytesWritten == 0) 
                        {
                            WriteAndExit(
                                message: string.Join(NLC, [
                                    "An exception occured while retrieving the contents of the provided extension.",
                                    "Error Log:",
                                    "The returned buffer is empty."
                                ]),
                                status: 1
                            );
                        }

                        var versionID = await GetLatestChromeVersion();

                        string downloadUrl = BuildDownloadUrl(manifestID, versionID);

                        Console.WriteLine("Validating .CRX extension at: {0}", ExtensionPath);
                        Console.Write(NLC);
                        await Task.Delay(500);
                        
                        Console.WriteLine("Using download URL: {0}", downloadUrl);
                        Console.Write(NLC);
                        await Task.Delay(500);
                        

                        var userAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{versionID} Safari/537.36";

                        // Removing the default User Agent, it will be readded below.
                        NetworkClient.Instance.DefaultRequestHeaders.Remove("User-Agent");
                        NetworkClient.Instance.DefaultRequestHeaders.Add("User-Agent", userAgent);

                        // Retrieving the contents using the generated userAgent above.
                        contents = await NetworkClient.Instance.GetByteArrayAsync(downloadUrl, cts.Token);

                        // Readding the default User Agent, it will be readded below.
                        NetworkClient.Instance.DefaultRequestHeaders.Remove("User-Agent");
                        NetworkClient.Instance.DefaultRequestHeaders.Add("User-Agent", DefaultUserAgent);

                        if (contents.Length == 0) 
                        {
                            WriteAndExit(
                                message: string.Join(NLC, [
                                    "An exception occured while retrieving the contents of the provided extension.",
                                    "Error Log:",
                                    "The response returned an empty stream."
                                ]),
                                status: 1
                            );
                        }

                        await ValidateCRXContents(contents);
                    }

                    catch (Exception ex) 
                    {
                        WriteAndExit
                        (
                            message: string.Join(NLC, [
                                "An exception occured while retrieving the contents of the provided extension.",
                                "Error Log:",
                                ex.Message
                            ]),
                            status: 1
                        );
                    }
                }
                # endregion "Direct Chrome Extension Download"

                if (contents == null) {
                    return [];
                }
            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {ExtensionPath}",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1, 
                    writePlatformDebugInfo: true
                );
            }

            return contents;
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
                        message: string.Join(NLC, [
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

        public async Task<bool> ExtensionExists() 
        {
            if (IsLocalFile) {
                return File.Exists(ExtensionPath);
            }

            if (IsURL) 
            {
                if (await SiteIsPingable(ExtensionPath)) {
                    return true;
                }

                Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
                Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
                return false;
            }

            Warning.Write("Unable to make contact with the website hosting the extension provided.");
            return false;
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

        /// <summary>
        /// <param name="contents"> The ReadOnlyMemory<byte> containing the contents from this.ExtensionPath, created in GetExtensionContents</param>
        ///
        /// <param name="exitOnFail"> A boolean determining if the application should exit if the validation fails </param>
        ///
        /// <returns>A MemoryStream containing the bytes of from the file, assuming an exception doesn't occur, or exitOnFail is set to true.</returns>
        /// </summary>
        
        private async Task<MemoryStream> ValidateCRXContents(ReadOnlyMemory<byte> contents) 
        {
            var memoryStream = new MemoryStream();

            try 
            {
                Console.WriteLine("Scanning the provided .CRX file, please wait.");
                Console.Write(NLC);
                await Task.Delay(500);

                Console.WriteLine("Checking for the presence of the documented CRX Magic Numbers.");
                await Task.Delay(500);

                // Checking for the presence XPI Magic Numbers
                if (contents.Span.IndexOf(CRXMagicBytes.Span) >= 0) 
                {
                    WriteSuccessMessage($"Located the documented CRX Magic Numbers.", noNewLines: true);
                    Console.WriteLine(NLC);
                    await Task.Delay(500);
                }

                else if (exitOnFail) 
                {
                    WriteAndExit
                    (
                        "Failed to locate the documented CRX Magic Numbers on the provided .CRX file.",
                        status: 1
                    );
                } 
                
                else 
                {
                    Warning.Write("Failed to locate the documented CRX Magic Numbers on the provided .CRX file.");
                    Console.WriteLine($"By default, BAMM does not exit on a failed extension check.{NLC}");
                    Console.WriteLine($"To change this behavior, please pass --exit-on-ext-fail");
                    await Task.Delay(500);
                }

                for (int i = 0; i < CRXContentChecks.Count; i++) 
                {
                    var element = CRXContentChecks.ElementAt(i);
                    Console.WriteLine($"Scanning for {element.Key}..");
                    await Task.Delay(500);
                            
                    var found = contents.Span.IndexOf(element.Value.Span) >= 0;
                    var contentLength = element.Value.Span.Length;

                    if (found) 
                    {
                        LogSuccess(element.Key, element.Value);
                        Console.Write(NLC);
                        await Task.Delay(500);
                    } 
                    
                    else if (exitOnFail) 
                    {
                        WriteAndExit
                        (
                            message: $"Unable to locate the hex sequence for {element.Key}{NLC}", 
                            status: 1,
                            writePlatformDebugInfo: false
                        );
                    } 
                    
                    else 
                    {
                        Warning.Write($"Unable to locate the hex sequence for {element.Key}{NLC}");
                    }
                }

                await memoryStream.WriteAsync(contents);

                // Resetting the stream position to prevent incorrect data from being read.
                memoryStream.Position = 0;
                
                return memoryStream;
            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {ExtensionPath}",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return null;
        }


        /// <summary>
        /// <param name="contents"> The ReadOnlyMemory<byte> containing the contents from this.ExtensionPath, created in GetExtensionContents</param>
        ///
        /// <param name="exitOnFail"> A boolean determining if the application should exit if the validation fails </param>
        ///
        /// <returns>A MemoryStream containing the bytes of from the file, assuming an exception doesn't occur, or exitOnFail is set to true.</returns>
        /// </summary>
        
        private async Task<MemoryStream> ValidateXPIContents(ReadOnlyMemory<byte> contents, bool exitOnFail = false) 
        {
            var memoryStream = new MemoryStream();

            try 
            {
                Console.WriteLine("Scanning the provided .XPI file, please wait.");
                Console.Write(NLC);
                await Task.Delay(500);

                Console.WriteLine("Checking for the presence of the documented XPI Magic Numbers..");
                await Task.Delay(500);

                // Checking for the presence XPI Magic Numbers
                if (contents.Span.IndexOf(XPIMagicBytes.Span) >= 0) 
                {
                    WriteSuccessMessage($"Located the documented XPI Magic Numbers.", noNewLines: true);
                    Console.Write(NLC);
                    await Task.Delay(500);
                }

                else if (exitOnFail) 
                {
                    WriteAndExit
                    (
                        "Failed to locate the documented XPI Magic Numbers on the provided .XPI file.",
                        status: 1
                    );
                } 
                
                else 
                {
                    Warning.Write("Failed to locate the documented XPI Magic Numbers on the provided .XPI file.");
                    Console.WriteLine($"By default, BAMM does not exit on a failed extension check.");
                    Console.WriteLine();
                    Console.WriteLine($"To change this behavior, please pass --exit-on-ext-fail");
                    await Task.Delay(500);
                }

                for (int i = 0; i < XPIContentChecks.Count; i++) 
                {
                    var element = XPIContentChecks.ElementAt(i);
                    Console.WriteLine();
                    Console.WriteLine($"Scanning for {element.Key}..");
                    await Task.Delay(500);
                            
                    var found = contents.Span.IndexOf(element.Value.Span) >= 0;
                    var contentLength = element.Value.Span.Length;

                    if (found) 
                    {
                        LogSuccess(element.Key, element.Value);
                        await Task.Delay(500);
                    } 
                    
                    else if (exitOnFail) 
                    {
                        WriteAndExit
                        (
                            message: $"Unable to locate the hex sequence for {element.Key}{NLC}", 
                            status: 1,
                            writePlatformDebugInfo: false
                        );
                    } 
                    
                    else 
                    {
                        Warning.Write($"Unable to locate the hex sequence for {element.Key}{NLC}");
                        await Task.Delay(500);
                    }
                }

                await memoryStream.WriteAsync(contents);

                // Resetting the stream position to prevent incorrect data from being read.
                memoryStream.Position = 0;
                
                return memoryStream;
            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {ExtensionPath}",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return null;
        }

        public async Task<string?> WriteExtensionContents(MemoryStream? contents) 
        {
            string? outputPath = null;
            if (contents == null) 
            {
                WriteAndExit
                (
                    message: 
                        string.Join(NLC, [
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

            var memoryInfo = Runtime.GetMemoryInfo();

            var useLowChunkBuffer = 
                memoryInfo.HasValue && 
                memoryInfo.Value.TotalMemory >= totalBufferSize * 32 && 
                memoryInfo.Value.FreeMemory >= totalBufferSize * 16;

            // Using a 1MB buffer for higher end systems. 
            if (Runtime.GetCoreCount() > 8 && useLowChunkBuffer && assignedChunkSize == chunkSize8){
                assignedChunkSize = chunkSize1;
            }

            var extensionsDirectory = GetExtensionsDirectory(); 
            EnsureDirectoryExists(extensionsDirectory); // Ensures the extensions directory exists

            var formattedContentLength = contents.Length / (double) chunkSize1;

            Console.Write(NLC);
            Console.WriteLine("Due to security restrictions imposed by modern browser, BAMM must write the extension to a file before Selenium can access it.");
            Console.Write(NLC);
            Console.Write("Extensions used by BAMM are written to: ");
            
            Warning.Write(extensionsDirectory, noNewLines: true);
            Console.WriteLine(NLC);
            
            // {extensionsDirectory}{NLC}");
            Console.Write($"The current extension's size is ");
            Warning.Write($"{formattedContentLength:0.00} MB", noNewLines: true);
            Console.WriteLine(NLC);

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

                Console.Write(NLC);
                while (!fileName.EndsWith(fileExt)) 
                {
                    Console.Write($"Please ensure the filename you enter ends with ");
                    Warning.Write($"'{fileExt}'", noNewLines: true);
                    Console.WriteLine(NLC);

                    fileName = AskForInput("Filename: ");
                }

                outputPath = Path.Combine(extensionsDirectory, fileName);

                
                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                await contents.CopyToAsync(fileStream, assignedChunkSize, cts.Token);

                Console.Write(NLC);
                WriteSuccessMessage("Successfully downloaded the extension to: ", noNewLines: true);
                Warning.Write(outputPath, noNewLines: true);
                Console.WriteLine(NLC);
            }

            catch (Exception ex) 
            {
                WriteAndExit
                (
                    message: 
                        string.Join(NLC, [
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


}