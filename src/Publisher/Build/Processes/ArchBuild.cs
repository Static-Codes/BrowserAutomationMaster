using BrowserAutomationMaster.Helpers;
using System.Text;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.Build.BuildInfo;

namespace Publisher.Build.Processes
{
    public class ArchBuild
    {
        public required string binaryPath;
        private readonly static byte[] pkgName = Encoding.UTF8.GetBytes($"pkgname='{AppName}'");
        private readonly static byte[] pkgRel = "pkgrel=1"u8.ToArray();
        private readonly static byte[] pkgDesc = Encoding.UTF8.GetBytes($"pkgdesc='{AppDescription}'");
        private readonly static byte[] arch = "arch=('x86_64' 'aarch64' 'armv6h')"u8.ToArray();
        private readonly static byte[] license = Encoding.UTF8.GetBytes($"license=('{AppLicenseType}')");
        private readonly static byte[] source = "source=('src/bamm')"u8.ToArray();
        private readonly static byte[] depends = "depends=('python>3.8' 'which' 'icu' 'openssl' 'zlib' 'krb5' 'xclip')"u8.ToArray();
        private readonly static byte[] makeDepends = "makedepends=('dotnet-sdk') # Dotnet 10 is required for compilation"u8.ToArray();
        
        private async Task<(string, FileStream)> GetSha512SumsAndStream() 
        {
            return await CalculateSHA512HashOfFile(binaryPath);
        }
        
        private readonly static byte[] packageFunction = Encoding.UTF8.GetBytes(
            string.Join(NLC, [
                "package() {",
                $"{HORIZONTAL_TAB}mkdir -p \"${{pkgdir}}/usr/bin\"",
                $"{HORIZONTAL_TAB}cp \"${{srcdir}}/{AppName}\" \"${{pkgdir}}/usr/bin/{AppName}\"",
                $"{HORIZONTAL_TAB}install -Dm755 \"${{srcdir}}/{AppName}\" \"${{pkgdir}}/usr/bin/{AppName}\"",
                "}"
            ])
        );

        /// <summary>
        /// <param name="outputPath">outputPath: The path to the PKGBUILD file to be written to the system's disk.</param>
        /// <br />
        /// <returns>Returns a tuple: 
        /// <br />
        /// Item1: A boolean representing the status of the operation
        /// <br />
        /// Item2: The Stream object with the contents of the binaryPath passed to ArchBuild
        /// </returns>
        /// </summary>
        public async Task<(bool, FileStream?)> WritePKGBUILDFile(string outputPath, string appVersion) 
        {
            var success = false;
            FileStream? binaryStream = null;
            FileStream? tempStream = null;
            
            try 
            {
                // Assigning a value to the already defined binaryStream
                (var sha512Hash, binaryStream) = await GetSha512SumsAndStream();

                var sha512sums = Encoding.UTF8.GetBytes($"sha512sums=({sha512Hash})");

                var pkgVer = Encoding.UTF8.GetBytes($"pkgver='{appVersion}'");

                var NLCBytes = Encoding.UTF8.GetBytes(NLC);
                
                var staticFields = ReflectionHelper.GetStaticFieldsOfType<byte[]>(
                    outerType: typeof(ArchBuild), 
                    publicOnly: false
                );
                
                // Refined calculation logic due to the previous being inconsistent.
                int totalLength = staticFields.Sum(f => f.Length + NLCBytes.Length) 
                    + sha512sums.Length
                    + NLCBytes.Length
                    + pkgVer.Length;

                var fileContents = new byte[totalLength];
                int bytesWritten = 0;

                // Using a Span<byte> is more efficient than directly accessing the byte array.
                Span<byte> buffer = fileContents;

                foreach (var staticField in staticFields) 
                {
                    // Copies the field's content
                    staticField.CopyTo(buffer[bytesWritten..]);
                    bytesWritten += staticField.Length;

                    // Adds a new line char.
                    NLCBytes.CopyTo(buffer[bytesWritten..]);
                    bytesWritten += NLCBytes.Length;
                }

                // Writes the sha512sums string
                sha512sums.CopyTo(buffer[bytesWritten..]);
                bytesWritten += sha512sums.Length;

                // Adds a new line char.
                NLCBytes.CopyTo(buffer[bytesWritten..]);
                bytesWritten += NLCBytes.Length;

                // Writes the pkgVer string
                pkgVer.CopyTo(buffer[bytesWritten..]);
                bytesWritten += pkgVer.Length;

                tempStream = new(
                    outputPath, 
                    FileMode.Create, 
                    FileAccess.ReadWrite, 
                    FileShare.Read, 
                    4096, 
                    true
                );

                await tempStream.WriteAsync(fileContents);
                success = true;
            }

            catch (Exception ex) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to write PKGBUILD file to disk.",
                        "Error Log:",
                        ex.Message ]
                    ),
                    status: 1
                );
            }

            finally 
            {
                if (tempStream != null) {
                    // Since the stream is no longer needed it can be disposed of safely.
                    await tempStream.DisposeAsync();
                }
            }

            return (success, binaryStream);
        }

    }
}