using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.RequestManager.NetworkClient;
using static BrowserAutomationMaster.Messaging.Errors;

namespace Publisher 
{
    public class SourceControl 
    {
        private static string[] FileTypes = new string[2] {
            ".tar.gz",
            ".zip"
        };

        public static string? LatestTag => DetermineLatestReleaseTag().Result;
        
        public static Source LatestRelease = new Source(
            Downloads: [.. FileTypes.Select(fileType => new Download(fileType))]
        );

        public static async Task<string?> DetermineLatestReleaseTag() 
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue(
                "BrowserAutomationMaster"
            ));
            var releases = await client.Repository.Release.GetAll("Static-Codes", "BrowserAutomationMaster");
            var latest = releases.ElementAt(0);

            return latest?.TagName ?? null; 
        }

        public static async Task<bool> DownloadSourceOfLatestRelease(string[] args)
        {
            try 
            {
                var chosenFileType = Input.WriteListFromOptions(FileTypes, "file type for the downloaded source");
                
                // This should not throw an exception because the user is only allowed to choose between two filetypes.
                var chosenDownload = LatestRelease.Downloads.Where(
                    download => download.URL.EndsWith(chosenFileType)
                ).First();

                var sourceBytes = await Instance.GetByteArrayAsync(
                    requestUri: chosenDownload.URL, 
                    cancellationToken: new CancellationTokenSource(
                        TimeSpan.FromSeconds(60)
                    ).Token
                );

                if (sourceBytes == null) 
                {
                    Errors.WriteAndExit(
                        message: "The contents of the latest release could not be resolved, please try again.", 
                        status: 1
                    );
                }

                // This will be modified if it does not resolve from DirectoryManager.
                var AppDataPath = AppDataDirectory;
                
                if (AppDataPath == null) {
                    Errors.Write("DirectoryManager.AppDataDirectory could not be resolved.");
                    AppDataPath = Input.AskForInput("Please enter the directory to save the BAMM codebase.");
                }

                if (!Directory.Exists(AppDataPath)) 
                {
                    Errors.WriteAndExit("DirectoryManager.AppDataPath could not be resolved, please try another directory.", 1);
                }

                var sourceDirectory = Path.Join(AppDataPath, "source");

                // Writing the sourceDirectory if it doesn't already exist.
                EnsureDirectoryExists(sourceDirectory);

                var sourceFileName = Path.GetFileName(chosenDownload.URL) ?? $"BAMM-Source{chosenDownload.FileType}";

                var sourceFilePath = Path.Join(sourceDirectory, sourceFileName);

                var useExistingDownload = args.Any(arg => arg.Equals("--no-download"));

                var filePresent = File.Exists(sourceFilePath);

                if (useExistingDownload && filePresent)
                {
                    Warning.Write($"{NLC}Download skipped, using codebase at: {sourceFilePath}");
                    return true;
                }



                // If the file doesn't already exist, the contents can safely be written to disk.
                if (!filePresent)
                {
                    Warning.Write("Writing the BAMM Codebase to disk, please wait..");
                    File.WriteAllBytes(sourceFilePath, sourceBytes);
                    Success.WriteSuccessMessage("Operation successful!");
                    Warning.Write($"{NLC}Codebase Location: {sourceFilePath}");
                    return true;
                }


                Warning.Write($"A version of the BAMM Codebase exists at: {sourceFilePath}");
                var confirmationStatus = Input.AskForInput("Would you like to overwrite the current archive? [y/n]: ");

                if (Input.ConditionRejected(confirmationStatus)) 
                {
                    Errors.WriteAndExit(
                        "Operation cancelled by user, the BAMM Publisher will exit now.", status: 1
                    );
                }

                Success.WriteSuccessMessage("The archive overwrite operation has been authorized, please wait..");
                Warning.Write("Writing the BAMM Codebase to disk, please wait..");
                File.WriteAllBytes(sourceFilePath, sourceBytes);
                Success.WriteSuccessMessage("Operation successful!");
                Warning.Write($"{NLC}Codebase Location: {sourceFilePath}");
            }

            catch (Exception ex) 
            {
                return Errors.WriteErrorAndReturnBool(
                    message: string.Join(NLC, [
                        "An unknown exception occured while writing the BAMM Codebase to disk.",
                        "Error Log:",
                        NLC,
                        ex.Message ?? ex.StackTrace
                    ]),
                    returnBool: false
                );
            }

            return true;
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