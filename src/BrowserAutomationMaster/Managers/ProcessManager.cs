
using BrowserAutomationMaster.Messaging;
using Spectre.Console;
using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.System.Console;
using static BrowserAutomationMaster.Managers.AnsiManager;

namespace BrowserAutomationMaster.Managers
{
    public static class ProcessManager
    {
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

            var instances = Process.GetProcessesByName(curProc.ProcessName);
            if (instances.Length > 1)
            {
                if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 10240))
                {
                    // Get the handle associated with instance 1 (index 0)
                    // Hook in said handle
                    // Write to console "There was an attempt to open another instance of BAMM, only one instance can be run at the same time."
                    // From what i've read the fact that StdOut is not shared, means StdErr is the handle required to hook into the process.
                    var instance = instances[0];

                    PInvoke.FreeConsole();
                    if (PInvoke.AttachConsole((uint)instance.Id))
                    {
                        WriteMessage("YUP", isSuccess: true);

                        var safeHandle = PInvoke.GetStdHandle_SafeHandle(STD_HANDLE.STD_ERROR_HANDLE);
                        using var fileStream = new FileStream(safeHandle, FileAccess.ReadWrite);
                        var standardOutput = new StreamWriter(fileStream, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };

                        // Create a custom AnsiConsole instance that writes to the attached console
                        var settings = new AnsiConsoleSettings
                        {
                            Out = new AnsiConsoleOutput(standardOutput)
                        };

                        var console = AnsiConsole.Create(settings);

                        // Use the custom console instance to write the message
                        console.Write(
                            new Text(
                                "There was an attempt to open another instance of BAMM, only one instance can be run at the same time.\n",
                                new Style(
                                    foreground: AnsiManager.ToSpectreColor(ThemeManager.DefaultTheme.ForegroundColor),
                                    background: AnsiManager.ToSpectreColor(ThemeManager.DefaultTheme.BackgroundColor)
                                )
                            )
                        );
                        PInvoke.FreeConsole();
                        if (PInvoke.AttachConsole((uint)instances[1].Id))
                        {
                            WriteMessage("Worked", isSuccess: true);
                        }
                        else
                        {
                            WriteMessage(
                                "Unable to switch window handles, please restart BAMM, " +
                                $"then make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                                $"Error log:\nUnable to attach to the console associated with instance "
                                , isError: true);
                        }
                    }
                    else
                    {
                        AnsiConsole.Write(
                            new Text("There was an attempt to open another instance of BAMM, only one instance can be run at the same time.\n", new Style(foreground: Color.Yellow))
                        );
                    }
                }
            }
        }

    }


}
