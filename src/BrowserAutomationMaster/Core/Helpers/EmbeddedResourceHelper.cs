using BrowserAutomationMaster.Core.Messaging;
using System.IO.Compression;
using System.Reflection;
using static BrowserAutomationMaster.Core.Messaging.Errors;

namespace BrowserAutomationMaster.Core.Helpers;
public class EmbeddedResourceHelper 
{
    private static readonly Assembly assembly = Assembly.GetExecutingAssembly();

    private static readonly TaskStatus[] TaskStates = [
        TaskStatus.Canceled,
        TaskStatus.Faulted,
        TaskStatus.RanToCompletion,
    ];

    public static Stream GetEmbeddedResource(string resourceName, string resourcePattern) 
    {
        Stream? resourceStream = null;

        try
        {
            resourceStream = assembly.GetManifestResourceStream(resourcePattern);

            if (resourceStream == null) {
                WriteAndExit
                (
                    string.Join(Environment.NewLine, [
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
                string.Join(Environment.NewLine, [
                    $"[ERROR]: An exception occured while trying to retrieve the contents of: {resourceName}",
                    $"Error Log:{Environment.NewLine}{ex.StackTrace ?? ex.Message}"
                ]), 
                status: 1
            );
        }

        return resourceStream;
    }

    /// <summary>
    /// Reads a single text entry out of an embedded zip, without extracting the archive.
    /// </summary>
    /// <returns>The entry's contents, or null when the resource or the entry is missing.</returns>
    /// <remarks>
    /// The entry name is matched against the archive's full paths, so a caller passing
    /// "gui/scripts/version.js" also finds the "gui/scripts/version.js" entry of an archive
    /// whose entries are rooted at "gui/".
    /// </remarks>
    public static string? GetEmbeddedZipEntryText(
        string resourceName, string resourcePattern, string entryName
    )
    {
        // GetEmbeddedResource calls WriteAndExit when the resource is missing, which is the
        // established behavior for embedded resources and is deliberately not bypassed here.
        using Stream resourceStream = GetEmbeddedResource(resourceName, resourcePattern);
        using ZipArchive archive = new(resourceStream, ZipArchiveMode.Read);

        string suffix = entryName.TrimStart('/');

        ZipArchiveEntry? match = archive.Entries.FirstOrDefault(
            entry => entry.FullName.Equals(suffix, StringComparison.Ordinal)
                || entry.FullName.EndsWith($"/{suffix}", StringComparison.Ordinal)
        );

        if (match == null)
        {
            Warning.Write(
                string.Join(Environment.NewLine, [
                    $"[WARNING]: The entry '{entryName}' was not found inside '{resourceName}'.",
                    $"Entries present: {string.Join(", ", archive.Entries.Select(entry => entry.FullName))}"
                ])
            );

            return null;
        }

        using Stream entryStream = match.Open();
        using StreamReader reader = new(entryStream);

        return reader.ReadToEnd();
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
            var failedChecksText = string.Join(Environment.NewLine, failedChecks);

            WriteAndExit(
                message: string.Join(Environment.NewLine, [
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

        await ReadFromStreamAndWriteToPath(stream, resourceName, outputPath);
        
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
            var failedChecksText = string.Join(Environment.NewLine, failedChecks);

            WriteAndExit(
                message: string.Join(Environment.NewLine, [
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
        
        await ReadFromStreamAndWriteToPath(stream, resourceName, outputPath);
        
    }

    private static async Task ReadFromStreamAndWriteToPath(Stream stream, string resourceName, string outputPath)
    {
        try 
        {

            if (stream.Length == 0) {
                WriteAndExit(
                    message: string.Join(Environment.NewLine, [
                        $"[ERROR]: Unable to write embedded resource '{resourceName}' to disk, please see below for more information.",
                        "Error Log:",
                        $"The stream object associated with '{resourceName}' has a length of 0."
                    ]),
                    status: 1
                );
            }
            
            
            var bufferArray = new byte[stream.Length];

            var bytesLeftToRead = bufferArray.Length;

            // #if DEBUG
            //     Console.WriteLine("======= PRE LOOP STATE ====== ");
            //     Console.WriteLine($"stream.Length: {stream.Length}");
            //     Console.WriteLine($"stream.Position: {stream.Position}");
            //     Console.WriteLine($"bytesLeftToRead: {bytesLeftToRead}");
            // #endif

            while (stream.Position < stream.Length) {
                
                

                // Using 1MB chunk size or the remaining buffer is less than 1MB in size (1024 bytes).
                var chunkSize = stream.Length - stream.Position > 1024 ? 1024 : (int)(stream.Length - stream.Position);

                // Using chunked reading because the associated performance gains
                stream.ReadExactly(bufferArray, (int)stream.Position, chunkSize);
                
                // Reducing the number of remaining bytes to read.
                bytesLeftToRead -= chunkSize;

                // // Debug only do not remove comments
                // #if DEBUG
                //     Console.WriteLine("======= LOOP STATE ====== ");
                //     Console.WriteLine($"stream.Position: {stream.Position}");
                //     Console.WriteLine($"bytesLeftToRead: {bytesLeftToRead}");
                // #endif
            }

            // Writing the contents to outputPath
            await File.WriteAllBytesAsync(outputPath, bufferArray);
        }

        catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }
}