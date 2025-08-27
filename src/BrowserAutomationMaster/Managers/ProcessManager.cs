using BrowserAutomationMaster.Compilation;
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.PlatformManager;

namespace BrowserAutomationMaster.Managers
{
    public static class ProcessManager
    {
        /// <summary>
        /// Performs a check on all active processes, if more than one instance of BAMM is found, the newest one is closed.
        /// </summary>
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static void CheckForMultipleInstances()
        {
            var curProc = Process.GetCurrentProcess();
            if (curProc == null)
            {
                Errors.WriteAndExit(
                    "Unable to determine the number of open instances of BAMM.\n" +
                    "This is a bug, please make a bug report at {ISSUES_LINK}\n" +
                    "Error log:\n\n" +
                    "ProcessManager.GetInstances() returned null on curProc.",
                    status: 1
                );
            }

            try
            {
                var instances = Process.GetProcessesByName(curProc.ProcessName);
                if (instances.Length > 1)
                {
                    if (IsWindows)
                        Win.HandleMultipleInstances(instances);  // Execution ends if this line is hit.
                    
                    WriteMessage(
                        "Only one instance of BAMM can be running at once, please close the current session and open bamm again.",
                        isError: true
                    );
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Errors.WriteAndExit(
                    message: 
                        "BAM Manager (BAMM) was unable to check for multiple instances, " +
                        $"please make a bug report at {ISSUES_LINK}\n" + 
                        $"Error log:\n\n{ex.Message}",
                    status: 1
                );
            }
        }



        public static void PreventMemoryLeaks(string? selectedBrowser)
        {
            var errMessage =
                    "An error occured while attempting to close left over instance of the webdriver used by BAM Manager (BAMM).\n" +
                    $"If this error persists, please make a bug report at {ISSUES_LINK}\n\n" +
                    "Error Log:\n" +
                    "driverName has a size of 0 in PreventMemoryLeaks()";

            if (selectedBrowser == null)
                Errors.WriteAndExit(errMessage, 1);

            var dBuilder = new StringBuilder();

            if (selectedBrowser.Equals("chrome", CCIC))
                dBuilder.Append("chromedriver");

            else if (selectedBrowser.Equals("firefox", CCIC))
                dBuilder.Append("geckodriver");

            if (dBuilder.Length == 0)
                Errors.WriteAndExit(errMessage, 1);

            if (IsWindows)
                dBuilder.Append(".exe");

            string driverName = dBuilder.ToString();
            foreach (var process in Process.GetProcessesByName(driverName))
            {
                try {
                    process.Kill();
                }
                catch (Exception e)
                {
                    var message =
                        "Unable to kill:\n" +
                        $"Name: {process.ProcessName}\n" +
                        $"ID: {process.Id}\n" +
                        $"Error Log:\n{e.Message}\n\n";

                    Errors.Write(message);
                }
            }
        }
    }


}
