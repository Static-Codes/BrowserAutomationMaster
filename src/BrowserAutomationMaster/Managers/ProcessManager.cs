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
                try
                {
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




    public static class ProcessFactory
    {

        public struct ProcessResponse()
        {
            public int ExitCode = -1;
            public List<string> STDOut = [];
            public List<string> STDErr = [];
        }

        public static void Update(ref ProcessResponse response, int ExitCode, List<string> STDOut, List<string> STDErr)
        {
            response.ExitCode = ExitCode;
            response.STDOut = STDOut;
            response.STDErr = STDErr;
        }

        private readonly static Dictionary<Process, ProcessResponse> ActiveProcesses = [];
        private readonly static Dictionary<Process, ProcessResponse> Processes = [];


        /// <summary> Returns ONLY the processes that are actively running. </summary>
        public static Process[] GetActiveProcesses() { return [.. ActiveProcesses.Keys ]; }

        /// <summary> Returns all processes that have been spawned regardless of active status. </summary>
        public static List<Process> GetAllProcesses() { return [.. Processes.Keys ]; }

        public static async Task<(int ExitCode, List<string> STDOut, List<string> STDErr)> GetProcessResponse(Process process)
        {
            ArgumentNullException.ThrowIfNull(process);

            if (!Processes.ContainsKey(process))
                Errors.WriteAndExit(
                    message:
                        "The process associated with the command: " +
                        $"{process.StartInfo.FileName} {process.StartInfo.Arguments} was not properly spawned.\n\n" +
                        $"Error Log:\n" +
                        "ProcessFactory.Processes does not contain the requested process.",
                    status: 1
                );

            // Handle a process is not yet completed.
            while (ProcessIsRunning(process))
                await Task.Delay(200);
            
                
            Processes.TryGetValue(process, out ProcessResponse response);
            // Weird syntax for deconstructed returns

            return (
                response.ExitCode, 
                response.STDOut,
                response.STDErr
            );

        }

        /// <summary> Returns true if there are spawned processes that are still running. </summary>
        public static bool HasActiveProcesses() { return ActiveProcesses.Count > 0; }

        /// <summary> Returns true if any processes have been spawned across the current app session lifespan. </summary>
        public static bool HasProcesses() { return Processes.Count > 0; }

        /// <summary> Spawns a new Process using the information provided. </summary>
        /// <param name="psi">ProcessStartInfo associated with the desired Process </param>
        /// <param name="processAction">A string describing what the process will do. </param>
        /// <param name="raiseEvents">If the process should redirect I/O, defaults to true. </param>
        /// <param name="writeSTDInOut">If the process should write I/O, defaults to true.</param>
        /// <param name="timeout">The timeout in seconds after which the process will automatically exit, defaults to 200.</param>
        /// <returns>The newly spawned process (assuming an error doesn't cause the application to exit</returns>
        public static async Task<Process> SpawnProcess(ProcessStartInfo psi, string processAction,
            bool raiseEvents = true, bool readSTDInOut = true, bool writeSTDInOut = true, bool whiteOutput = false, bool justSpawn = false, bool runSync = false, int timeout = 200)
        {
            var outputLines = new List<string>();
            var errorLines = new List<string>();
            var newProc = new Process() { StartInfo = psi };

            try
            {
                if (raiseEvents)
                {
                    newProc.EnableRaisingEvents = true; // Enabling events to be reported to the handlers below.

                    // Declaring required event handlers -> STDOut, STDErr
                    newProc.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data == null)
                            return;
                            
                        outputLines.Add(args.Data);

                        if (writeSTDInOut && !whiteOutput)
                            Success.WriteSuccessMessage(args.Data + '\n');

                        else if (writeSTDInOut && whiteOutput)
                            Console.WriteLine(args.Data + '\n');
                        
                    };


                    newProc.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data == null)
                            return;
                        
                        errorLines.Add(args.Data);

                        if (writeSTDInOut)
                            Errors.Write(args.Data + '\n');
                            
                    };
                }

                // This struct will be populated 25 or so lines below.
                var newProcResponse = new ProcessResponse();

                Processes.Add(newProc, newProcResponse);

                if (justSpawn)
                    return newProc;

                newProc.Start();
                ActiveProcesses.Add(newProc, newProcResponse); // Add new process to ActiveProcess upon invoke of Start().


                if (readSTDInOut)
                {
                    try
                    {
                        newProc.BeginOutputReadLine();
                        newProc.BeginErrorReadLine();
                    }
                    catch (InvalidOperationException ex)
                    {
                        Warning.Write($"A non fatal error has occured while starting the requested process:\n{ex.Message}");
                        //Console.WriteLine(ex.Message);
                    }
                }

                if (runSync)
                    newProc.WaitForExit();

                else
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    await newProc.WaitForExitAsync(cts.Token);
                }

                ActiveProcesses.Remove(newProc); // Remove new process from ActiveProcesses upon exit.

                if (!Processes.TryGetValue(newProc, out var _))
                    Errors.WriteAndExit($"Unable to find the process associated with the command:\n{psi.FileName} {psi.Arguments}", 1);

                // Get the ProcessResponse associated with the newly spawned Process
                var procResponse = Processes[newProc];

                // Updating the local variable from above
                Update(ref procResponse, newProc.ExitCode, outputLines, errorLines);

                // Reassigning the newly updated ProcessResponse to the associated Process from above.
                Processes[newProc] = procResponse;


                // If child process was not successful a stacktrace is generated.
                if (newProc.ExitCode != 0 && raiseEvents && writeSTDInOut)
                {
                    var fullStackTrace = string.Join("\n", errorLines);
                    // string[] last5Lines = errorLines.Count >= 5 ? [.. errorLines.TakeLast(5)] : [.. errorLines.TakeLast(errorLines.Count)];

                    var userFriendlyMessage = $"BAM Manager (BAMM) was unable to {processAction}.\n\n" +
                                              $"If this persists, please make a bug report at {ISSUES_LINK}";

                    var detailedLog = "Error log:\n" +
                                      $"Command: {psi.FileName} {psi.Arguments} failed with exit code {newProc.ExitCode}\n\n" +
                                      $"Stack Trace:\n{fullStackTrace}\n\n";

                    Errors.WriteAndExit($"{userFriendlyMessage}\n\n{detailedLog}", 1);
                }
            }

            catch (Exception ex)
            {
               Errors.WriteAndExit(
                   message:
                       "BAM Manager (BAMM) was unable to spawn the requested process.\n" +
                       $"If this issue persists, please make a bug report at {ISSUES_LINK}\n\n" +
                       "Error Log:\n" +
                       $"Unable to execute command:\n{psi.FileName} {psi.Arguments}\n\n{ex.Message}",
                   status: 1
               );

            }

            return newProc;
        }
        
        public static bool ProcessIsRunning(Process process) { return !process.HasExited && ActiveProcesses.ContainsKey(process); }


    
    }

}



