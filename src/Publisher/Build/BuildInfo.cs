using System.Security.Cryptography;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace Publisher.Build 
{
    public class BuildInfo 
    {
        public readonly static string AppName = "bamm";
        public readonly static string AppVersion = CurrentVersion[1..];  // Removes the leading "v" in the version tag.
        public readonly static string AppDescription = "A English-like scripting language for Selenium Automation that compiles into Python 3.9+ code.";
        public readonly static string AppExtendedDescription = "BAM Manager (BAMM) is a Dynamic Scripting Language (DSL) that simplifies the process of writing automation tests in Selenium using Python 3.9+";
        public readonly static string AppLicenseType = "MIT";

        
        // <summary>
        // <param name="filePath"> The path to the file in which the hash will be calculated from.</param>
        // <returns>
        // A tuple: </br>
        // Item1: The SHA512 hash of the specified file. </br>
        // Item2: The associated stream object, ensure it is disposed. </br>
        // </returns>
        // </summary>
        public static async Task<(string, FileStream)> CalculateSHA512HashOfFile(string filePath) 
        {
            var stream = new FileStream(filePath, FileMode.Open);

            byte[] result = new byte[stream.Length];
            
            CancellationToken cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(30)
            ).Token;

            try 
            {
                using SHA512 sha512 = SHA512.Create();
                result = await sha512.ComputeHashAsync(stream, cts); 
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to calculate SHA512 sum of the provided binary.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return (Convert.ToHexString(result).ToLowerInvariant(), stream);
        }

        public static async Task<(string, FileStream)> CalculateSHA512HashOfFile(FileStream fileStream) 
        {   
            // When passed, this stream's position at the end of the stream, a reset is needed.
            fileStream.Position = 0;

            uint SHA512_SIZE_IN_BYTES = 64;
            byte[] hashBytes = new byte[SHA512_SIZE_IN_BYTES];
            
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            try 
            {
                using SHA512 sha512 = SHA512.Create();
                hashBytes = await sha512.ComputeHashAsync(fileStream, cts.Token); 
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to calculate SHA512 sum of the provided binary.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            // Resetting the position again because it seems to be causing issues.
            fileStream.Position = 0;
            return (Convert.ToHexString(hashBytes).ToLowerInvariant(), fileStream);
        }
        
    }
}