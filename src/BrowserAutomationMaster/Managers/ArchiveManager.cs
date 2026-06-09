// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using System.Formats.Tar;
using System.IO.Compression;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.UpdateManager;

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

        public static (bool, string) CreateArchGZIPArchive(string sourceDir, string outputDir, string appVersion) 
        {
            var filePath = string.Empty;
            try 
            {
                var fileName = $"bamm-{appVersion}.pkg.tar.gz";
                filePath = Path.Combine(outputDir, fileName);

                Warning.Write($"Creating {filePath}, please wait..");
                using FileStream fileStream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                
                TarFile.CreateFromDirectory(sourceDir, fileStream, includeBaseDirectory: false);
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
            return (Directory.Exists(sourceDir), filePath);
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