
using BrowserAutomationMaster.Managers.AppManager.OS;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;


namespace BrowserAutomationMaster.Managers
{
    public static class ProcessManager
    {
        [SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "RuntimeManager.IsSupportedWindowsVersion() handles checks.")]
        public static void CheckForMultipleInstances()
        {
            var curProc = Process.GetCurrentProcess();
            if (curProc == null)
            {
                Errors.WriteErrorAndExit(
                    "Unable to determine the number of open instances of BAMM.\n" +
                    "This is a bug, please make a bug report at {ConstantManager.ISSUES_LINK}\n" +
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
                    if (RuntimeManager.IsSupportedWindowsVersion())
                    {
                        Win.HandleMultipleInstances(instances);
                    }
                    else if (RuntimeManager.IsSupportedOSXVersion())
                    {
                    }
                    else if (OperatingSystem.IsLinux())
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndExit(
                    message: 
                        "BAM Manager (BAMM) was unable to check for multiple instances, " +
                        $"please make a bug report at {ConstantManager.ISSUES_LINK}\n" + 
                        $"Error log:\n\n{ex.Message}",
                    status: 1
                );
            }
        }

    }


}
