using BrowserAutomationMaster.Core.Messaging;
using static BrowserAutomationMaster.Core.Common.RequestManager.NetworkClient;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Messaging.Errors;

namespace Publisher 
{
    public class SourceControl 
    {
        private static readonly string[] FileTypes = [
            "Skip Compilation and Start Packaging",
            ".tar.gz",
            ".zip"
        ];

        public static string? LatestTag => DetermineLatestReleaseTag("Static-Codes", "BrowserAutomationMaster").Result;
        
        public readonly static Source LatestRelease = new(
            Downloads: [.. FileTypes.Select(fileType => new Download(fileType))]
        );

        public static string SetArchiveFileType() => Input.WriteListFromOptions(FileTypes, "file type for the source");


        public static async Task<string?> DetermineLatestReleaseTag(string userName, string projectName) 
        {
            var client = new Octokit.GitHubClient(
                new Octokit.ProductHeaderValue(projectName
            ));
            var releases = await client.Repository.Release.GetAll(userName, projectName);
            var latest = releases.ElementAt(0);

            return latest?.TagName ?? null; 
        }

        public static async Task<string?> DownloadSourceOfLatestRelease(string[] args, string archiveFileType)
        {
            string? sourceFilePath = null;
            try 
            {
                // This should not throw an exception because the user is only allowed to choose between two filetypes.
                var chosenDownload = LatestRelease.Downloads.Where(
                    download => download.URL.EndsWith(archiveFileType)
                ).First();

                var sourceDirectory = GetSourceDirectory();

                // Writing the sourceDirectory if it doesn't already exist.
                EnsureDirectoryExists(sourceDirectory);

                var sourceFileName = Path.GetFileName(chosenDownload.URL) ?? $"BAMM-Source{chosenDownload.FileType}";

                sourceFilePath = Path.Join(sourceDirectory, sourceFileName);

                // Skipping download logic
                var useExistingDownload = args.Any(arg => arg.Equals("--no-download"));
                
                var filePresent = File.Exists(sourceFilePath);

                if (useExistingDownload || filePresent)
                {
                    Warning.Write($"{NLC}Download skipped, codebase at: {sourceFilePath}");
                    return sourceFilePath;
                }

                var sourceBytes = await Instance.GetByteArrayAsync(
                    requestUri: chosenDownload.URL, 
                    cancellationToken: new CancellationTokenSource(
                        TimeSpan.FromSeconds(60)
                    ).Token
                );

                if (sourceBytes == null) 
                {
                    WriteAndExit(
                        message: "The contents of the latest release could not be resolved, please try again.", 
                        status: 1
                    );
                }

                // If the file doesn't already exist, the contents can safely be written to disk.
                if (!filePresent)
                {
                    Warning.Write("Writing the BAMM Codebase to disk, please wait..");
                    File.WriteAllBytes(sourceFilePath, sourceBytes);
                    Success.WriteSuccessMessage("Operation successful!");
                    Warning.Write($"{NLC}Codebase Location: {sourceFilePath}");
                    return sourceFilePath;
                }

                Warning.Write($"A version of the BAMM Codebase exists at: {sourceFilePath}");
                var confirmationStatus = Input.AskForInput("Would you like to overwrite the current archive? [y/n]: ");

                if (Input.ConditionRejected(confirmationStatus)) 
                {
                    WriteAndExit(
                        "Operation cancelled by user, the BAMM Publisher will exit now.", status: 1
                    );
                }

                Success.WriteSuccessMessage("The archive overwrite operation has been authorized, please wait..");
                Warning.Write("Writing the BAMM Codebase archive to disk, please wait..");
                File.WriteAllBytes(sourceFilePath, sourceBytes);
                Success.WriteSuccessMessage("Operation successful!");
                Warning.Write($"{NLC}Archive Location: {sourceFilePath}");
            }

            catch (Exception ex) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "An unknown exception occured while writing the BAMM Codebase to disk.",
                        "Error Log:",
                        NLC,
                        ex.Message ?? ex.StackTrace
                    ]),
                    status: 1
                );
            }

            return sourceFilePath;
        }

    }


    // Dynamically builds the URL to the latest release source based on the selected FileType.
    // This automatically updates with UpdateManager.LatestVersion
    public class Download(string FileType) 
    {
        public string FileType = FileType;
        public string URL = BuildURL(SourceControl.LatestTag, FileType);

        private static string BuildURL(string? LatestTag, string FileType) 
        {
            if (LatestTag == null) {
                WriteAndExit("Unable to determine the latest release of BAMM, please try again.", 1);
            }
            return string.Concat(
                BASE_SOURCE_LINK, 
                LatestTag, 
                FileType
            );
        }
    }

    

    public class Source(Download[] Downloads)
    { 
        public Download[] Downloads = Downloads;
    }
}