using BrowserAutomationMaster.Resources.NativeFileDialog;

namespace BrowserAutomationMaster.Core.Helpers 
{
    public class FileDialogHelper() 
    {
        private static DialogResult OpenDialog(string? filterList = null, string? defaultPath = null) 
        {
            var dialogResult = Dialog.FileOpen(filterList, defaultPath);
            return dialogResult;
        }

        public static bool TryOpenDialog(out DialogResult? dialogResult, string? filterList = null, string? defaultPath = null) 
        {
            dialogResult = OpenDialog(filterList, defaultPath);
            return dialogResult.IsOk;
        }

        public static void RunTest() 
        {
            var desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var status = TryOpenDialog(
                out DialogResult? dialogResult,
                filterList: "bamc", 
                defaultPath: desktopDir
            );

            if (!status || dialogResult == null) 
            {
                var cancelled = dialogResult?.IsCancelled ?? false;
                var errored = dialogResult?.ErrorMessage != null;
                var message = 
                    cancelled ? "Operation cancelled." : 
                    errored ? dialogResult!.ErrorMessage : 
                    "Unspecified";

                Console.WriteLine
                (
                    string.Join(Environment.NewLine, [
                        "Unable to complete the OpenFileDialog process.",
                        "Error Log:",
                        message
                    ])
                );
                Environment.Exit(1);
            }

            Console.WriteLine($"Selected file path: {dialogResult.Path}");
        }
    }
}