using BrowserAutomationMaster.Messaging;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Managers.RequestManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;


namespace BrowserAutomationMaster.Managers 
{
    // This class will be used to parse the commands of
    // bamm extension "file://path/to/firefox/extension.xpi"
    // bamm extension "https://url/to/firefox/extension.xpi"
    // bamm extension "file://path/to/chrome/extension.crx"
    // bamm extension "https://url/to/chrome/extension.crx"
    public class ExtensionManager(string rawExtensionPath, string browserName, string[]? args = null)
    {
        public string RawExtensionPath { get; init; }  = rawExtensionPath;
        public string ExtensionPath { get; init; } = SanitizeExtensionPath(rawExtensionPath);
        public bool IsLocalFile { get; init; } = CheckLocalFileStatus(rawExtensionPath);
        public bool IsURL { get; init; } = CheckURLStatus(rawExtensionPath);
        public bool IsChromeExtension = CheckChromeStatus(rawExtensionPath, browserName);
        public bool IsFirefoxExtension = CheckFirefoxStatus(rawExtensionPath, browserName);
        public bool IsFirefoxDirectDownload = CheckForDirectFirefoxDownload(rawExtensionPath);
        public byte[]? Content { get; init; }
        private readonly bool exitOnFail = CheckForExitArgStatus(args);

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
            return args != null && args.Contains("--exit-on-ext-fail");
        }

        private static bool CheckURLStatus(string rawExtensionPath) 
        {
            return 
                rawExtensionPath.StartsWith("http://") || 
                rawExtensionPath.StartsWith("https://");
        }

        private static bool CheckLocalFileStatus(string rawExtensionPath) => rawExtensionPath.StartsWith("file://");

        private static string SanitizeExtensionPath(string rawExtensionPath) => rawExtensionPath.Replace("file://", "");

        public async Task<bool> ExtensionExists() 
        {
            if (IsLocalFile) 
            {
                return File.Exists(ExtensionPath);
            }

            if (IsURL) 
            {
                if (await SiteIsPingable(ExtensionPath)) 
                {
                    return true;
                }

                Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
                Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
                return false;
            }

            Warning.Write("Unable to make contact with the website hosting the extension provided.");
            return false;
        }

        public async Task<MemoryStream?> GetExtensionContents() 
        {
            if (!IsURL && !IsLocalFile) 
            {
                Console.WriteLine($"The provided extension does not contain a valid URI protocal.");
                Console.Write(NLC);
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
                        "- https://addons.mozilla.org/firefox/downloads/file/<extension-id>/<extension-name>.xpi"
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
                // Chrome 137+ doesn't support adding via local file.
                if (IsChromeExtension) 
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                            "Error Log:",
                            "Local .CRX Files are not supported as of Chrome 137, please use the chrome web store link for the given extension."
                        ]),
                        status: 1, 
                        writePlatformDebugInfo: true
                    );
                }

                byte[]? rentedBuffer = null;
                try
                {
                    // This is currently unmanaged memory, please ensure it's properly disposed.
                    GetRentedBytesFromFile(out int fileLength, out rentedBuffer);

                    // According to .NET Documentation on byte[].AsMemory(start, length)
                    // It creates a new memory region over the portion of the target array beginning at a specified position with a specified length.
                    // This is done to prevent garbage data from being rented, single ArrayPool always returns more memory than is required.
                    ReadOnlyMemory<byte> dataToValidate = rentedBuffer.AsMemory(0, fileLength);

                    if (IsFirefoxExtension) {
                        return await ValidateXPIContents(dataToValidate, exitOnFail);
                    }
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

                finally 
                {
                    // Returning the buffer to the pool which allows it to be overwritten.
                    if (rentedBuffer != null) 
                    {
                        ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }
                }
            }

            return null;
        }

        // Normally File.ReadAllBytes() would suffice however, due to the existence of larger extensions, the efficiency gains caused by using Referne
        private void GetRentedBytesFromFile(out int fileLength, out byte[] buffer) 
        {
            var fileInfo = new FileInfo(ExtensionPath);
            int length = (int)fileInfo.Length;
            fileLength = length;

            buffer = ArrayPool<byte>.Shared.Rent(length);

            try 
            {
                using var fs = fileInfo.OpenRead();
                fs.ReadExactly(buffer, 0, length);
                // THIS IS CURRENTLY UNMANAGED MEMORY, PLEASE ENSURE THE CALLER DISPOSES OF IT PROPERLY!!!!!
            } 
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        
        static string BuildDownloadUrl(string manifestID, string versionID) => $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion={versionID}&acceptformat=crx2,crx3&x=id%3D{manifestID}%26uc";
            
        private async Task<byte[]> GetHostedExtensionContents()
        {
            byte[] contents = [];
            try 
            {
                if (IsFirefoxExtension && IsFirefoxDirectDownload) 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    contents = await NetworkClient.Instance.GetByteArrayAsync(ExtensionPath, cts.Token);
                }

                else if (IsFirefoxExtension && !IsFirefoxDirectDownload) 
                {

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var htmlMemory = await NetworkClient.GetReadOnlyMemoryCharsFromURL(url: ExtensionPath, exitOnFail: exitOnFail);

                    // // Debugging only do not add to production
                    // var htmlContents = await RequestManager.NetworkClient.GetReadOnlyMemoryBytesFromURL(ExtensionPath);
                    // Console.WriteLine(Encoding.UTF8.GetString(htmlContents.ToArray()));
                    // return [];

                    // The default extraAllocationFactor of 1 is used here, since no extra bytes are overscanned.
                    GetEnumeratedMatches(htmlMemory, XPIExtensionPathRegexPattern, out byte[] rentedBuffer, out int bytesWritten);


                    // This is 100 bytes in length.
                    // var b64EncodedString = Encoding.UTF8.GetString(rentedBuffer.AsSpan()[..bytesWritten]);
                    
                    // This url will be 75-150 bytes.
                    var downloadURL = Encoding.UTF8.GetString(rentedBuffer.AsSpan());
            
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
                        Console.WriteLine("Using download URL: {0}", downloadURL);
                        Console.Write(NLC);
                        

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

                    finally 
                    {
                        // Returning the rented buffer to the pool to prevent a memory bug.
                        if (rentedBuffer.Length > 0)
                        {
                            ArrayPool<byte>.Shared.Return(rentedBuffer);
                        }
                    }
                }

                else if (IsChromeExtension) 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var htmlMemory = await NetworkClient.GetReadOnlyMemoryCharsFromURL(ExtensionPath, exitOnFail: exitOnFail);
                    
                    // // Debugging only do not add to production
                    // var htmlContents = await RequestManager.NetworkClient.GetReadOnlyMemoryBytesFromURL(ExtensionPath);
                    // Console.WriteLine(Encoding.UTF8.GetString(htmlContents.ToArray()));
                    // return [];

                    // A custom extraAllocationFactor of 3 is used here, since an overscan is present.
                    GetEnumeratedMatches(htmlMemory, CRXExtensionIDRegexPattern, out byte[] rentedBuffer, out int bytesWritten, extraAllocationFactor: 3);
                    
                    
                    // Decoded manifest ID (32 char), roughly 40-50 bytes.
                    var manifestID = DecodeMatch(rentedBuffer);
            
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
                        Console.WriteLine("Using download URL: {0}", downloadUrl);
                        Console.Write(NLC);

                        var userAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{versionID} Safari/537.36";

                        // Removing the default User Agent, it will be readded below.
                        NetworkClient.Instance.DefaultRequestHeaders.Remove("User-Agent");
                        NetworkClient.Instance.DefaultRequestHeaders.Add("User-Agent", userAgent);

                        // Retrieving the contents.
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

                    finally 
                    {
                        // Returning the rented buffer to the pool to prevent a memory bug.
                        if (rentedBuffer.Length > 0)
                        {
                            ArrayPool<byte>.Shared.Return(rentedBuffer);
                        }
                    }
                
                }
                
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

                Console.WriteLine("Checking for the presence of the documented CRX Magic Numbers.");

                // Checking for the presence XPI Magic Numbers
                if (contents.Span.IndexOf(CRXMagicBytes.Span) >= 0) 
                {
                    WriteSuccessMessage($"Located the documented CRX Magic Numbers.", noNewLines: true);
                    Console.WriteLine(NLC);
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
                }

                for (int i = 0; i < CRXContentChecks.Count; i++) 
                {
                    var element = CRXContentChecks.ElementAt(i);
                    Console.WriteLine($"Scanning for {element.Key}..");
                            
                    var found = contents.Span.IndexOf(element.Value.Span) >= 0;
                    var contentLength = element.Value.Span.Length;

                    if (found) 
                    {
                        LogSuccess(element.Key, element.Value);
                        Console.Write(NLC);
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

                Console.WriteLine("Checking for the presence of the documented XPI Magic Numbers..");

                // Checking for the presence XPI Magic Numbers
                if (contents.Span.IndexOf(XPIMagicBytes.Span) >= 0) 
                {
                    WriteSuccessMessage($"Located the documented XPI Magic Numbers.", noNewLines: true);
                    Console.WriteLine(NLC);
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
                    Console.WriteLine($"By default, BAMM does not exit on a failed extension check.{NLC}");
                    Console.WriteLine($"To change this behavior, please pass --exit-on-ext-fail");
                }

                for (int i = 0; i < XPIContentChecks.Count; i++) 
                {
                    var element = XPIContentChecks.ElementAt(i);
                    Console.WriteLine($"{NLC}Scanning for {element.Key}..");
                            
                    var found = contents.Span.IndexOf(element.Value.Span) >= 0;
                    var contentLength = element.Value.Span.Length;

                    if (found) 
                    {
                        LogSuccess(element.Key, element.Value);
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

        // Helper method to bypass the 'ref struct in async' limitation of C# 12
        // This will be removed when BAMM is ported to .NET 10 (C# 13). (Once its stable on Ubuntu)
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
        
        // Helper method to bypass the 'ref struct in async' limitation of C# 12
        // This will be removed when BAMM is ported to .NET 10 (C# 13). (Once its stable on Ubuntu)
        private void LogSuccess(string key, ReadOnlyMemory<byte> value)
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

        // Helper method to bypass the 'ref struct in async' limitation of C# 12
        // This will be removed when BAMM is ported to .NET 10 (C# 13). (Once its stable on Ubuntu)
        private static void GetEnumeratedMatches(
            ReadOnlyMemory<char> romContents, 
            string RegexPattern, 
            out byte[] finalBuffer, 
            out int finalLength,
            int extraAllocationFactor = 1
        )
        {
            finalBuffer = [];
            finalLength = 0;

            var valueMatches = Regex.EnumerateMatches(romContents.Span, RegexPattern);

            foreach (var match in valueMatches) 
            {
                var ROM = romContents.Slice(match.Index, match.Length);
                
                // UTF8 can be up to 3 bytes per char (when accounting wide-glyphs)
                byte[] currentBuffer = ArrayPool<byte>.Shared.Rent(ROM.Length * extraAllocationFactor);
                
                try 
                {
                    if (Utf8.TryWrite(currentBuffer, $"{ROM.Span}", out int written)) 
                    {
                        finalBuffer = currentBuffer;
                        finalLength = written;
                        return; // Caller now has ownership of the references above.
                    }
                } 
                catch (Exception ex) 
                {
                    Console.WriteLine(ex.Message);
                }

                // If this is condition is executed, the current attempt to rent was unsuccessful.
                // As such the currentBuffer must be returned to prevent a NullReferenceException.
                ArrayPool<byte>.Shared.Return(currentBuffer);
            }
        }

        // Helper method to bypass the 'ref struct in async' limitation of C# 12
        // This will be removed when BAMM is ported to .NET 10 (C# 13). (Once its stable on Ubuntu)
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
    }
}