using System.Reflection;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Managers
{
    public class EmbeddedResourceManager 
    {
        private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

        private static readonly TaskStatus[] TaskStates = [
            TaskStatus.Canceled,
            TaskStatus.Faulted,
            TaskStatus.RanToCompletion,
        ];

        public static Stream GetEmbeddedResource(string resourceName, string resourcePattern) 
        {
            // var resourcePattern = "*MacPackager*.*AppIcon*.icns";
            Stream? resourceStream = null;

            try
            {
                resourceStream = assembly.GetManifestResourceStream(resourcePattern);

                if (resourceStream == null) 
                {
                    WriteAndExit
                    (
                        string.Join(NLC, [
                            $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                            "Error Log:",
                            "resourceStream returned null"
                        ]), 
                        status: 1
                    );
                }
            }
            catch (Exception ex)
            {
                WriteAndExit
                (
                    string.Join(NLC, [
                        $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                        $"Error Log:{NLC}{ex.StackTrace ?? ex.Message}"
                    ]), 
                    status: 1
                );
            }

            return resourceStream;
        }


        public static async Task WriteEmbeddedResourceToDisk(
            string resourceName, string resourcePattern, string outputPath, 
            Dictionary<string, bool[]>? optionalChecks = null, Task? SuccessFunction = null
        ) 
        {
            Stream stream = GetEmbeddedResource(resourceName, resourcePattern);

            // Returns IEnumerable<string>?
            var failedChecks = optionalChecks?
                                .Where(pair => pair.Value.Any(check => !check))
                                .Select(pair => pair.Key);

            // If any checks were provided, and one or more of the checks failed, an error is triggered before the success function can execute.
            if (failedChecks != null && failedChecks.Any()) 
            {
                var failedChecksText = string.Join(NLC, failedChecks);

                WriteAndExit(
                    message: string.Join(NLC, [
                        $"[ERROR]: Unable to write embedded resource '{resourceName}' to disk, due to a failed check, please see below for more information.",
                        $"Error Log:",
                        "The following conditionals returned false:",
                        $"{failedChecksText}" 
                    ]),
                    status: 1
                );
            }

            // Since this is optional, it may not always be passed.
            if (SuccessFunction != null) 
            {
                SuccessFunction.Start();

                // While the SuccessFunction is still actively running, async sleep every second until completion.
                while (TaskStates.All(status => SuccessFunction.Status != status)) 
                {

                    await SuccessFunction.WaitAsync(
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(1)
                        ).Token
                    );
                }

            }
            
            try 
            {
                var bufferArray = new byte[stream.Length];

                var bytesLeftToRead = bufferArray.Length;

                while (stream.Position < stream.Length) {

                    // Using 1MB chunk size or the remaining buffer is less than 1MB in size (1024 bytes).
                    var chunkSize = stream.Length - stream.Position > 1024 ? 1024 : (int)stream.Length;

                    // Using chunked reading because the associated performance gains
                    stream.ReadExactly(bufferArray, (int)stream.Position, chunkSize);
                    
                    // Reducing the number of remaining bytes to read.
                    bytesLeftToRead -= chunkSize;

                }

                // Writing the contents to path/To/AppData/BrowserAutomationMaster/packages.json
                await File.WriteAllBytesAsync(outputPath, bufferArray);
            }

            catch (Exception ex) {
                Console.WriteLine(ex);
            }
            
        }

        public static async Task WriteEmbeddedResourceToDisk(
            Stream stream, string resourceName, string outputPath, 
            Dictionary<string, bool[]>? optionalChecks = null, Task? SuccessFunction = null
        ) 
        {
            // Returns IEnumerable<string>?
            var failedChecks = optionalChecks?
                                .Where(pair => pair.Value.Any(check => !check))
                                .Select(pair => pair.Key);

            // If any checks were provided, and one or more of the checks failed, an error is triggered before the success function can execute.
            if (failedChecks != null && failedChecks.Any()) 
            {
                var failedChecksText = string.Join(NLC, failedChecks);

                WriteAndExit(
                    message: string.Join(NLC, [
                        $"[ERROR]: Unable to write embedded resource '{resourceName}' to disk, due to a failed check, please see below for more information.",
                        $"Error Log:",
                        "The following conditionals returned false:",
                        $"{failedChecksText}" 
                    ]),
                    status: 1
                );
            }

            // Since this is optional, it may not always be passed.
            if (SuccessFunction != null) 
            {
                SuccessFunction.Start();
                while (TaskStates.All(status => SuccessFunction.Status != status)) 
                {
                    await SuccessFunction.WaitAsync(
                        new CancellationTokenSource(
                            TimeSpan.FromSeconds(1)
                        ).Token
                    );
                }

            }
            
            try 
            {
                var bufferArray = new byte[stream.Length];

                var bytesLeftToRead = bufferArray.Length;

                while (stream.Position < stream.Length) {

                    // Using 1MB chunk size or the remaining buffer is less than 1MB in size (1024 bytes).
                    var chunkSize = stream.Length - stream.Position > 1024 ? 1024 : (int)stream.Length;

                    // Using chunked reading because the associated performance gains
                    stream.ReadExactly(bufferArray, (int)stream.Position, chunkSize);
                    
                    // Reducing the number of remaining bytes to read.
                    bytesLeftToRead -= chunkSize;

                }

                // Writing the contents to path/To/AppData/BrowserAutomationMaster/packages.json
                await File.WriteAllBytesAsync(outputPath, bufferArray);
            }

            catch (Exception ex) {
                Console.WriteLine(ex);
            }
            
        }
    }
}