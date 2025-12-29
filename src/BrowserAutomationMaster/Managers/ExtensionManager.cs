using BrowserAutomationMaster.Messaging;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode; 
using static BrowserAutomationMaster.Managers.ConstantManager;

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
        public bool IsChromeExtension = rawExtensionPath.EndsWith(".crx") && browserName.Equals("chrome");
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
                if (await RequestManager.SiteIsPingable(ExtensionPath)) {
                    return true;
                }

                Warning.Write("The provided extension URL provided a non 200 status code, indicating an error.");
                Console.WriteLine("Please try downloading this resource and passing the path to the local file instead.");
                return false;
            }

            Warning.Write("Unable to make contact with the website hosting the extension provided.");
            return false;
        }

        public async Task<MemoryStream?> GetExtensionContents(bool failOnExit = false) 
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
                        return await ValidateXPIContents(dataToValidate, failOnExit);
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

        private async Task<byte[]> GetHostedExtensionContents()
        {
            byte[] contents = [];
            try 
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                contents = await RequestManager.NetworkClient.Instance.GetByteArrayAsync(ExtensionPath, cts.Token);

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
        /// <param name="failOnExit"> A boolean determining if the application should exit if the validation fails </param>
        ///
        /// <returns>A MemoryStream containing the bytes of from the file, assuming an exception doesn't occur, or failOnExit is set to true.</returns>
        /// </summary>
        
        private async Task<MemoryStream> ValidateXPIContents(ReadOnlyMemory<byte> contents, bool failOnExit = false) 
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

                else if (failOnExit) 
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
                    
                    else if (failOnExit) 
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
    }
}