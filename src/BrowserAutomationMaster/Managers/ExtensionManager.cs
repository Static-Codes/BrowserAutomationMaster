using BrowserAutomationMaster.Messaging;
using System.Buffers;
using System.Buffers.Text;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers 
{
    // This class will be used to parse the commands of
    // bamm extension "file://path/to/firefox/extension.xpi"
    // bamm extension "https://url/to/firefox/extension.xpi"
    // bamm extension "file://path/to/chrome/extension.crx"
    // bamm extension "https://url/to/chrome/extension.crx"
    
    public class ExtensionManager(string rawExtensionPath, string browserName)
    {
        public string RawExtensionPath { get; init; }  = rawExtensionPath;
        public string ExtensionPath { get; init; } = rawExtensionPath.Replace("file://", "");
        public bool IsLocalFile { get; init; } = rawExtensionPath.StartsWith("file://");
        public bool IsURL { get; init; } = rawExtensionPath.StartsWith("http://") || rawExtensionPath.StartsWith("https://");
        public bool IsChromeExtension = rawExtensionPath.StartsWith("https://chromewebstore.google.com/detail/") && browserName.Equals("chrome");
        public bool IsFirefoxExtension = rawExtensionPath.EndsWith(".xpi") && browserName.Equals("firefox");
        public byte[]? Content { get; init; }

        public async Task<bool> ExtensionExists() 
        {
            if (IsLocalFile) 
            {
                return File.Exists(ExtensionPath);
            }

            if (IsURL) 
            {
                if (await RequestManager.SiteIsPingable(ExtensionPath)) 
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

        public async Task<MemoryStream?> GetExtensionContents(bool exitOnFail = false) 
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

                Errors.WriteAndExit
                (
                    message: $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
                    status: 1, 
                    writePlatformDebugInfo: true
                );
            }

            else if (IsURL) 
            {
                using var memoryStream = new MemoryStream();
                byte[] contents = [];
                
                contents = await GetHostedExtensionContents();
                await memoryStream.WriteAsync(contents);
                
                memoryStream.Position = 0;
                
                return memoryStream;
            }
            
            else if (IsLocalFile) 
            {

                byte[]? rentedBuffer = null;
                try
                {
                    // This is currently unmanaged memory, please ensure it's properly disposed.
                    GetRentedBytesFromFile(out int fileLength, out rentedBuffer);

                    // According to .NET Documentation on byte[].AsMemory(start, length)
                    // It creates a new memory region over the portion of the target array beginning at a specified position with a specified length.
                    // This is done to prevent garbage data from being rented, single ArrayPool always returns more memory than is required.
                    ReadOnlyMemory<byte> dataToValidate = rentedBuffer.AsMemory(0, fileLength);


                    if (IsChromeExtension) {
                        return null;
                    }

                    if (IsFirefoxExtension) {
                        return await ValidateXPIContents(dataToValidate, exitOnFail);
                    }
                }
                catch (Exception ex) 
                {
                    Errors.WriteAndExit
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
        
        static string BuildDownloadUrl(string manifestID) => $"https://clients2.google.com/service/update2/crx?response=redirect&prodversion=31.0.1609.0&acceptformat=crx2,crx3&x=id%3D{manifestID}%26uc";
            
        private async Task<byte[]> GetHostedExtensionContents()
        {
            byte[] contents = [];
            try 
            {
                if (IsFirefoxExtension) {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    contents = await RequestManager.NetworkClient.Instance.GetByteArrayAsync(ExtensionPath, cts.Token);
                }

                else if (IsChromeExtension) 
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    var htmlMemory = await RequestManager.NetworkClient.GetReadOnlyMemoryCharsFromURL(ExtensionPath);
                    
                    // // Debugging only do not add to production
                    // var htmlContents = await RequestManager.NetworkClient.GetReadOnlyMemoryBytesFromURL(ExtensionPath);
                    // Console.WriteLine(Encoding.UTF8.GetString(htmlContents.ToArray()));
                    // return [];

                    // byte[] manifestBytes = [];
                    GetEnumeratedMatches(htmlMemory, out byte[] rentedBuffer, out int length);
                    
                    // This is 100 bytes in length.
                    // var b64EncodedString = Encoding.UTF8.GetString(rentedBuffer.AsSpan()[..length]);
                    
                    // Decoded manifest ID (32 char), roughly 40-50 bytes.
                    var manifestID = DecodeMatch(rentedBuffer);
            
                    try 
                    {
                        if (length > 0) 
                        {
                            string downloadUrl = BuildDownloadUrl(manifestID);
                            Console.WriteLine("Download URL: {0}", downloadUrl);
                            
                            // contents = await RequestManager.NetworkClient.Instance.GetByteArrayAsync(downloadUrl, cts.Token);
                        }
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
                Errors.WriteAndExit
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
        
        private async Task<MemoryStream> ValidateXPIContents(ReadOnlyMemory<byte> contents, bool exitOnFail = false) 
        {
            var memoryStream = new MemoryStream();

            try 
            {
                Console.WriteLine("Scanning the provided .XPI file, please wait.");
                Console.Write(NLC);

                Console.WriteLine("Checking for the presence of the documented XPI Magic Numbers");
                Console.Write(NLC);

                // Checking for the presence XPI Magic Numbers
                if (contents.Span.IndexOf(XPIMagicBytes.Span) >= 0) 
                {
                    Console.WriteLine($"Located the documented XPI Magic Numbers.");
                }

                else if (exitOnFail) 
                {
                    Errors.WriteAndExit
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

                for (int i = 0; i < contentChecks.Count; i++) 
                {
                    var element = contentChecks.ElementAt(i);
                    Console.WriteLine($"Scanning for {element.Key}..");
                            
                    var found = contents.Span.IndexOf(element.Value.Span) >= 0;
                    var contentLength = element.Value.Span.Length;

                    if (found) 
                    {
                        LogSuccess(element.Key, element.Value);
                    } 
                    
                    else if (exitOnFail) 
                    {
                        Errors.WriteAndExit
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
                Errors.WriteAndExit
                (
                    message: string.Join(NLC, [
                        $"BAM Manager (BAMM) ran into a fatal error, while attempt to fetch the contents of the extension at: {RawExtensionPath}",
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

            Console.Write($"Located the hex sequence for {key} (");

            // Utilizes a direct span write to avoid heap allocation
            Console.Out.Write(hexBuffer);
            Console.WriteLine($"){NLC}");
        }

        // Helper method to bypass the 'ref struct in async' limitation of C# 12
        // This will be removed when BAMM is ported to .NET 10 (C# 13). (Once its stable on Ubuntu)
        private static void GetEnumeratedMatches(ReadOnlyMemory<char> romContents, out byte[] finalBuffer, out int finalLength)
        {
            finalBuffer = [];
            finalLength = 0;

            var valueMatches = Regex.EnumerateMatches(romContents.Span, CRXExtensionIDRegexPattern);

            foreach (var match in valueMatches) 
            {
                var ROM = romContents.Slice(match.Index, match.Length);
                
                // UTF8 can be up to 3 bytes per char (when accounting wide-glyphs)
                byte[] currentBuffer = ArrayPool<byte>.Shared.Rent(ROM.Length * 3);
                
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
    }
}