using System.Formats.Tar;
using System.IO.Compression;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Managers 
{
    public enum ArchiveType 
    {
        TAR_GZ,
        ZIP
    }

    public class ArchiveManager(string archiveType, string filePath) 
    {
        public string archiveType = archiveType;
        public string filePath = filePath;
        
        /// <summary>
        /// Extracts the file provided to the ArchiveManager object, unless "--no-extraction" is passed as an argument.
        /// <param name="args"> The arguments passed to the application </param>
        /// </summary>
        public bool UnarchiveFile(string[] args, string codebaseSourceDir) 
        {
            bool noExtractionRequested = args.Contains("--no-extraction");
            bool alreadyExtracted = Directory.Exists(codebaseSourceDir);

            if (noExtractionRequested || alreadyExtracted) {
                return true; 
            }

            return archiveType.Equals(".tar.gz") 
                ? UnarchiveGZIP(codebaseSourceDir) 
                : UnarchiveZip(codebaseSourceDir);
        }

        private bool UnarchiveGZIP(string codebaseSourceDir) 
        {
            try 
            {
                Warning.Write("Decompressing the BAMM Codebase .tar.gz archive., please wait..");
                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
                using GZipStream gzipStream = new(fileStream, CompressionMode.Decompress, leaveOpen: true);
                var exportDirectory = DirectoryManager.GetSourceDirectory();
                TarFile.ExtractToDirectory(gzipStream, exportDirectory, overwriteFiles: false);
                Success.WriteSuccessMessage($"Decompressed to directory: {exportDirectory}");
            }
            
            catch (Exception ex) 
            {
                Errors.WriteAndExit
                (
                    string.Join(NLC, [
                        "An unknown exception occured while decompressing the BAMM Codebase .tar.gz archive.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return Directory.Exists(codebaseSourceDir);
        }

        private bool UnarchiveZip(string codebaseSourceDir) 
        {
            try 
            {
                Warning.Write("Decompressing the BAMM Codebase .zip archive., please wait..");
                using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);

                var exportDirectory = DirectoryManager.GetSourceDirectory();
                ZipFile.ExtractToDirectory(fileStream, exportDirectory, overwriteFiles: false);
                Success.WriteSuccessMessage($"Decompressed to: {exportDirectory}");
            }
            
            catch (Exception ex) 
            {
                Errors.WriteAndExit
                (
                    string.Join(NLC, [
                        "An unknown exception occured while decompressing the BAMM Codebase .zip archive.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
            return Directory.Exists(codebaseSourceDir);
        }
    } 
}