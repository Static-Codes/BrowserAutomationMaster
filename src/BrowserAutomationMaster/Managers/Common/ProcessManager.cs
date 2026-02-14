using BrowserAutomationMaster.Managers.OS;
using BrowserAutomationMaster.Messaging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Managers.Common.ConstantManager;
using static BrowserAutomationMaster.Managers.Common.PlatformManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Managers.Common
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
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "Unable to determine the number of open instances of BAMM.",
                        $"This is a bug, please make a bug report at {ISSUES_LINK}",
                        "Error Log:",
                        "ProcessManager.GetInstances() returned null on curProc."
                    ]),
                    status: 1
                );
            }

            try
            {
                var instances = Process.GetProcessesByName(curProc.ProcessName);
                if (instances.Length > 1)
                {
                    if (Platforms.IsWindows) {
                        Win.HandleMultipleInstances(instances);  // Execution ends if this line is hit.
                    }

                    WriteAndExit
                    (
                        string.Join(NLC,
                        [
                            "Only one instance of BAMM can be running at once.", 
                            "Please close the current session and open bamm again."
                        ]),
                        status: 1
                    );
                }
            }

            catch (Exception ex)
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to check for multiple instances.",
                        $"Please make a bug report at {ISSUES_LINK}",
                        $"Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
        }

        public static void PreventMemoryLeaks(string? selectedBrowser)
        {
            var errMessage = string.Join(NLC, [
                "An error occured while attempting to terminate an orphan process used by BAM Manager (BAMM).",
                $"If this error persists, please make a bug report at {ISSUES_LINK}",
                NLC,
                "Error Log:",
                NLC,
                "driverName has a size of 0 in PreventMemoryLeaks()"
            ]);

            if (selectedBrowser == null) {
                WriteAndExit(errMessage, 1);
            }

            var dBuilder = new StringBuilder();

            if (selectedBrowser.Equals("chrome", CCIC)) {
                dBuilder.Append("chromedriver");
            }

            else if (selectedBrowser.Equals("firefox", CCIC)) {
                dBuilder.Append("geckodriver");
            }

            if (dBuilder.Length == 0) {
                WriteAndExit(errMessage, 1); 
            }

            if (Platforms.IsWindows) {
                dBuilder.Append(".exe");
            }

            string driverName = dBuilder.ToString();
            foreach (var process in Process.GetProcessesByName(driverName))
            {
                try {
                    process.Kill();
                }
                catch (Exception e)
                {
                    Write(
                        string.Join(NLC, [
                            "Unable to kill:",
                            $"Name: {process.ProcessName}",
                            $"ID: {process.Id}",
                            $"Error Log:",
                            e.Message
                        ])
                    );
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
            {
                WriteAndExit
                (
                    message: string.Join(NLC, [
                        "The process associated with the command:",
                        $"\"{process.StartInfo.FileName} {process.StartInfo.Arguments}\" was not properly spawned.",
                        "Error Log:",
                        "ProcessFactory.Processes does not contain the requested process."
                    ]),
                    status: 1
                );
            }

            // Handle a process is not yet completed.
            while (ProcessIsRunning(process)) {
                await Task.Delay(200);
            }
            
                
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
        /// <param name="whiteOutput">The output for this process should be printed with white text.</param>
        /// <param name="justSpawn">The process object should be created but execution should not start.</param>
        /// <param name="runSync">The process should be run synchronously as opposed to the default of asynchronous.</param>
        /// <param name="preventMemoryLeaks">If an error is thrown, webdriver cleanup operations should be performed.</param>
        /// <param name="browserName">The browser in use, if preventMemoryLeaks is set to true, this needs to be specified.</param>
        /// <param name="timeout">The timeout in seconds after which the process will automatically exit, defaults to 200.</param>
        /// <returns>The newly spawned process (assuming an error doesn't cause the application to exit</returns>
        public static async Task<Process> SpawnProcess(
            ProcessStartInfo psi, 
            string processAction,
            bool raiseEvents = true, 
            bool readSTDInOut = true, 
            bool writeSTDInOut = true, 
            bool whiteOutput = false, 
            bool justSpawn = false, 
            bool runSync = false,
            bool preventMemoryLeaks = false,
            string? browserName = null, 
            int timeout = 200
        )
        {
            var outputLines = new List<string>();
            var errorLines = new List<string>();
            var newProc = new Process() { StartInfo = psi };

            if (justSpawn) {
                return newProc;
            }

            try
            {
                if (raiseEvents)
                {
                    newProc.EnableRaisingEvents = true; // Enabling events to be reported to the handlers below.

                    // Declaring required event handlers -> STDOut, STDErr
                    newProc.OutputDataReceived += (sender, args) =>
                    {
                        if (args.Data == null) {
                            return;
                        }
                            
                        outputLines.Add(args.Data);

                        // "declare -x ..." is returned when the which command is executed.
                        if (writeSTDInOut && !whiteOutput && !args.Data.StartsWith("declare -x")) {
                            WriteSuccessMessage(args.Data + NLC);
                        }

                        else if (writeSTDInOut && whiteOutput) {
                            Console.WriteLine(args.Data + NLC);
                        }
                        
                    };


                    newProc.ErrorDataReceived += (sender, args) =>
                    {
                        if (args.Data == null) {
                            return;
                        }
                        
                        errorLines.Add(args.Data);

                        if (writeSTDInOut) {
                            Write(args.Data + NLC);
                        }
                    };
                }

                // This struct will be populated 25 or so lines below.
                var newProcResponse = new ProcessResponse();

                Processes.Add(newProc, newProcResponse);

                

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
                        Warning.Write(
                            string.Join(NLC, [
                                $"A non fatal error has occured while starting the requested process:",
                                ex.Message
                            ])
                        );
                    }
                }

                if (runSync) {
                    newProc.WaitForExit();
                }

                else
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
                    await newProc.WaitForExitAsync(cts.Token);
                }

                ActiveProcesses.Remove(newProc); // Remove new process from ActiveProcesses upon exit.

                if (!Processes.TryGetValue(newProc, out var _)) 
                {
                    WriteAndExit
                    (
                        message: string.Join(NLC, [
                            "Unable to find the process associated with the command:",
                            $"\"{psi.FileName} {psi.Arguments}\""
                        ]), 
                        status: 1
                    );
                }
                
                // Get the ProcessResponse associated with the newly spawned Process
                var procResponse = Processes[newProc];

                // Updating the local variable from above
                Update(ref procResponse, newProc.ExitCode, outputLines, errorLines);

                // Reassigning the newly updated ProcessResponse to the associated Process from above.
                Processes[newProc] = procResponse;


                // If child process was not successful a stacktrace is generated.
                if (newProc.ExitCode != 0 && raiseEvents && writeSTDInOut)
                {
                    var fullStackTrace = string.Join(NLC, errorLines);

                    var userFriendlyMessage = string.Join(NLC, [
                        $"BAM Manager (BAMM) was unable to {processAction}.",
                        NLC,
                        $"If this persists, please make a bug report at {ISSUES_LINK}"
                    ]);

                    var detailedLog = string.Join(NLC, [
                        "Error Log:",
                        $"\"{psi.FileName} {psi.Arguments}\" failed with exit code {newProc.ExitCode}.",
                        NLC,
                        "Stack Trace:",
                        fullStackTrace,
                        NLC
                    ]);

                    WriteAndExit
                    (
                        string.Join(NLC, [
                            userFriendlyMessage,
                            detailedLog
                        ]),
                        status: 1
                    );
                }
            }

            catch (Exception ex)
            {
                if (preventMemoryLeaks && browserName != null) {
                    ProcessManager.PreventMemoryLeaks(browserName);
                } 

                WriteAndExit(
                    message: string.Join(NLC, [
                        "BAM Manager (BAMM) was unable to spawn the requested process.",
                        $"If this issue persists, please make a bug report at {ISSUES_LINK}",
                        NLC,
                        "Error Log:",
                        "Unable to execute command:",
                        $"{psi.FileName} {psi.Arguments}",
                        NLC,
                        NLC,
                        ex.Message
                    ]),
                    status: 1
               );

            }

            return newProc;
        }
        
        public static bool ProcessIsRunning(Process process) { return !process.HasExited && ActiveProcesses.ContainsKey(process); }


    
    }

}



