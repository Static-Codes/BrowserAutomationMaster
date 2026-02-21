
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Messaging;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Core.Common.Constants;
using static BrowserAutomationMaster.Core.Common.DirectoryManager;
using static BrowserAutomationMaster.Core.Common.PlatformManager;
using static BrowserAutomationMaster.Core.Common.RegexManager;
using static BrowserAutomationMaster.Core.GUI.BackendFunctions;
using static BrowserAutomationMaster.Core.GUI.Response;
using static BrowserAutomationMaster.Core.Python.Runtime;
using static BrowserAutomationMaster.Core.Messaging.Errors;
using static BrowserAutomationMaster.Core.Messaging.Success;
using static BrowserAutomationMaster.ProgramFunctions;
using static System.Text.Encoding;
using Photino.NET;
using BrowserAutomationMaster.Core.Helpers;

namespace BrowserAutomationMaster.Core.GUI
{
    public class Server
    {
        const string DEFAULT_PORT = "8008";
        private readonly static int MINIMUM_GUI_MEMORY_MB = 2048;
        private readonly static string GUI_ZIP_PATH = GetGUIZipPath();
        private static bool isRunning = true;
        private readonly static HttpListener listener = new();

        public static readonly string MAIN_GUI_LINK = GetMainGUIPage(includeProtocol: true);
        public static readonly string MAIN_GUI_PAGE = MAIN_GUI_LINK.Replace("file://", "");
        
        public static bool IsRunning() => isRunning;
        public static void StopExecution() => isRunning = false;

        public static void AddOptionResponseHeaders(HttpListenerResponse response)
        {
            try
            {
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET");
                response.AddHeader("Access-Control-Max-Age", "86400"); // 1 day in seconds.

                response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            catch (Exception e){
                Warning.Write(
                    string.Join("", [
                        "An exception occured while trying to add the required headers to an OPTIONS request.\n\n",
                        $"Error Log:\n{e.Message}"
                    ])
                );
            }
        }

        public static void ExtractGUI()
        {
            try
            {
                ZipFile.ExtractToDirectory(GUI_ZIP_PATH, AppDataDirectory);
                File.Delete(GUI_ZIP_PATH);
                WriteSuccessMessage("Successfully extracted GUI, please wait while the HTTP Server starts..");
            }
            catch (Exception ex)
            {
                Warning.Write("An unhandled exception has occured while attempting to extract BAMM's GUI.");
                WriteAndExit(ex.Message, 1);
            }
        }

        public static (string name, string args) GetProcessNameAndArgs(bool scan = false)
        {
            if (scan && Platforms.IsChromeOS || Platforms.IsUnixLike) { 
                return ("netstat", "-ltu");
            }

            else if (scan && Platforms.IsWindows) {
                return ("cmd.exe", "/c netstat -ano");
            }

            else
                throw new PlatformNotSupportedException("Failed to set all values for members in PlatformInfo.Platforms");
        }

        /// <summary>
        /// </summary>
        /// <param name="groups">GroupCollection from a MatchCollection</param>
        /// <returns>An Enumerable of Group objects</returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static IEnumerable<Group>? GetValues(GroupCollection? groups)
        {
            if (groups == null || groups.Count == 0) {
                return [];
            }

            if (Platforms.IsWindows)
            {
                return groups
                    .Values
                    .Where(val =>
                        !string.IsNullOrEmpty(val.Value) &&
                        !val.Value.StartsWith("TCP", OIC) &&
                        !val.Value.StartsWith("UDP", OIC) &&
                        val.Value.All(c => char.IsNumber(c)) // Fixes issues with the matches including "localhost:"
                    );
            }

            if (Platforms.IsUnixLike || Platforms.IsChromeOS)
            {
                return groups.Values
                    .Where(val => 
                        !string.IsNullOrEmpty(val.Value) &&
                        val.Value.All(c => char.IsNumber(c)) // Fixes issues with the matches including "localhost:"
                    );
            }
            
            throw new PlatformNotSupportedException("Failed to set all values for members in PlatformInfo.Platforms");
        }

        public static async Task HandleEndpointRequests()
        {
            string[] invalidMethods = ["CONNECT", "DELETE", "HEAD", "PATCH", "POST", "PUT", "TRACE"];
            while (isRunning)
            {
                // Waits for a connection that is made.
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.Url == null)
                {
                    Warning.Write("Unable to parse request, please try again.");
                    return;
                }

                if (invalidMethods.Any(method => method.Equals(request.HttpMethod)))
                {
                    await HandleInvalidResponse(response, $"Invalid HTTP Method, {request.HttpMethod} requests are not supported by this very basic GUI.");
                    return;
                }

                if (request.HttpMethod.Equals("OPTIONS"))
                {
                    AddOptionResponseHeaders(response);
                }



                response.AddHeader("Access-Control-Allow-Origin", "*");

                switch (request.Url.AbsolutePath)
                {
                    case "/":
                        Redirect(response);
                        break;

                    case "/export":
                        await Export(request, response);
                        break;

                    case "/load":
                        await Load(response);
                        break;

                    case "/terminate":
                        await WriteResponse(response, UTF8.GetBytes("{ \"terminated\": true }"));
                        await Task.Delay(50);
                        isRunning = false;
                        return; // The use of return ends this functions execution whereas break only exits the current switch statement.

                    // case "/upload" when request.HttpMethod.Equals("GET"):
                    //     await Upload(request, response);
                    //     break;

                    case "/validate":
                        await Validate(request, response);
                        break;
                    
                    case "/version":
                        await Version(response);
                        break;

                    default:
                        Warning.Write($"Invalid route provided: {request.Url.AbsolutePath}");
                        break;


                };
            }
        }

        /// <summary>
        /// Uses netstat on all supported Platforms and OS to return a list of 
        /// </summary>
        /// <returns></returns>
        public static async Task<string[]> ScanForUsedLHPorts()
        {
            (var name, var args) = GetProcessNameAndArgs(scan: true);
            var psi = new ProcessStartInfo()
            {
                FileName = name,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = await ProcessFactory.SpawnProcess(psi, "scanning localhost for open ports", writeSTDInOut: false);
            (int ExitCode, List<string> STDOut, List<string> STDErr) = await ProcessFactory.GetProcessResponse(process);


            if (ExitCode != 0)
            {
                WriteAndExit(
                    message:
                        string.Join(string.Empty, [
                            "Unable to determine open ports on localhost, ",
                            "as a result BAMM's GUI could not be loaded.\n\n",
                            "Error Log:\n",
                            $"{(STDErr.Count != 0 ? string.Join(NLC, STDErr) : "Could not execute command: netstat -ano with cmd.exe")}"
                        ]),
                    status: 1
                );
            }

            if (STDOut.Count == 0)
            {
                WriteAndExit(
                    message:
                        string.Join(string.Empty, [
                            "Unable to determine open ports on localhost, ",
                            "as a result BAMM's GUI could not be loaded.\n\n",
                            $"Error Log:\nCommand: 'netstat -ano with cmd.exe' exited with no output.\n",
                            $"If this issue persist, please make a bug report at {ISSUES_LINK}"
                        ]),
                    status: 1
                );
            }

            var matches = PrecompiledNetStatRegex().Matches(string.Join(NLC, STDOut));
            var foundPorts = new StringBuilder();

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Success && matches[i].Groups.Count > 0)
                {
                    var groups = matches[i].Groups;
                    var values = GetValues(groups);

                    if (values == null) {
                        continue;
                    }

                    foreach (var value in values) {
                        foundPorts.AppendLine(value.Value);
                    }
                }
            }

            return [..
                foundPorts
                .ToString()
                .Split(NLC)
                // Fixes bug where splitting using NLC causes element at index 0 to be empty.
                .Where(val => !string.IsNullOrEmpty(val)) 
                .Distinct()
                .Order()
            ];
        }

        [STAThread] // Required as this will be a separate thread
        public static void StartGUIThread(string port = DEFAULT_PORT) 
        {
            // Starts the backend listener which handles the requests.
            Task.Run(() => StartServer(port));

            (string monitorName, int? xSize, int? ySize) = ScreenHelper.GetScreenSize();

            var usingDefaultSize = xSize == null || ySize == null;

            var window = new PhotinoWindow
            {
                LogVerbosity = -1
            };

            window = usingDefaultSize switch 
            {
                true => 
                    window
                    .SetTitle("BAMM GUI")
                    .SetUseOsDefaultSize(true)
                    .Load(MAIN_GUI_PAGE),
                
                false => 
                    window
                    .SetTitle("BAMM GUI")
                    .SetUseOsDefaultSize(false)
                    .SetSize(xSize!.Value, ySize!.Value)
                    .Load(MAIN_GUI_PAGE),
            };

            window.WaitForClose();    
            Environment.Exit(0);
        }

        public static async Task StartServer(string port = DEFAULT_PORT)
        {
            var url = $"http://127.0.0.1:{port}/";

            if (!HttpListener.IsSupported)
            {
                WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "Unable to run the BAMM GUI on your current operating system.",
                            "",
                            "Please download one the following:",
                            "",
                            "VS Code: https://code.visualstudio.com/",
                            "VS Codium: https://vscodium.com/",
                            "",
                            "Then follow these steps to start coding in BAMC",
                            "1. Open VSCode/VSCodium",
                            "2. Type Ctrl/Cmd + Shift + X",
                            "3. Search for 'BAMC Language Server'",
                            "4. Download the extension by the author 'Static-Codes'",
                            "5. Type Ctrl/Cmd + N",
                            "6. Type Ctrl/Cmd + S",
                            "7. Save the file as <filename>.bamc (Replace <filename> with your desired filename)",
                            $"8. Check out {DOCUMENTATION_LINK} for more information!"
                        ]
                    ), 
                    status: 1
                );
            }

            try
            {
                var usedLHPorts = await ScanForUsedLHPorts();

                if (usedLHPorts.Contains(port))
                {
                    throw new HttpListenerException(1, "Access is denied");
                }

                var memoryInfo = GetMemoryInfo();

                // Coalesce operator will break the current logic implementation, as such this warning can be ignored.
                #pragma warning disable IDE0270
                if (memoryInfo == null)
                {
                    throw new InsufficientMemoryException("Unable to determine available system memory, as such the GUI could not be loaded.");
                }
                #pragma warning restore IDE0270

                if (memoryInfo.Value.TotalMemory < MINIMUM_GUI_MEMORY_MB){
                    throw new InsufficientMemoryException("Your system currently has less than 2GB of total RAM as such the GUI could not be loaded.");
                }


                if (memoryInfo.Value.FreeMemory < MINIMUM_GUI_MEMORY_MB / 2){
                    throw new InsufficientMemoryException("Your system currently has less than 1GB of free RAM as such the GUI could not be loaded.");
                }
                
                // Downloads the GUI files if they are not already present.
                await HandleGUIDownload();

                // Fixes a bug where the 'compiled' directory may not be written to disk at this point.
                EnsureDirectoryExists(GetDesiredSaveDirectory());

                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Started GUI Server on {0}\n", url);
                Console.WriteLine("To access the GUI visit {0}\n", MAIN_GUI_LINK);

                await HandleEndpointRequests();
                listener.Close();
            }

            catch (HttpListenerException ex)
            {
                if (ex.Message.Contains("Access is denied"))
                {
                    WriteAndExit(
                        message:
                            string.Join(string.Empty, [
                                $"Port {port} on localhost is already in use, ",
                                "as a result BAMM's GUI could not be loaded.\n\n",
                                "Error Log:\n",
                                $"Address '127.0.0.1:{port}' is already in use, ",
                                "please use the following argument:\n",
                                "bamm --gui --port==<PORT>\n",
                                "Replace <PORT> with a number between 1 and 65535\n\n",
                                "Example:\n",
                                "bamm --gui --port==42069\n",
                            ]),
                        status: 1
                    );
                }
            }

            catch (InsufficientMemoryException)
            {
                WriteAndExit("Unable to start the handler associated with BAMM's GUI due to insufficient memory.\n\n", 1);
            }

            catch (ObjectDisposedException)
            {
                WriteAndExit(
                    message:
                        string.Join(NLC, [
                            "The GUI server has been terminated, please restart BAMM to use the GUI.",
                            "If you did not terminate the GUI, this is likely a bug.",
                            $"If this issue persists, please make a bug report at {ISSUES_LINK}"
                        ]),
                    status: 1
                );
            }

            catch (Exception ex)
            {
                WriteAndExit(
                    message:
                        string.Join(string.Empty, [
                            "An unknown exception occured during the operation of BAMM's GUI.\n\n",
                                $"Error Log:\n{ex.Message}\n",
                                $"If this issue persists, please make a bug report at {ISSUES_LINK}"
                        ]),
                    status: 1
                );
            }
        }

        public static async Task WriteResponse(HttpListenerResponse response, byte[] data, string contentType = "application/json")
        {
            try
            {
                response.ContentType = contentType;
                response.ContentEncoding = UTF8;
                response.ContentLength64 = data.LongLength;

                await response.OutputStream.WriteAsync(data);
                response.StatusCode = (int)HttpStatusCode.OK;
            }

            catch
            {
                response.StatusCode = (int)HttpStatusCode.Conflict;
            }

            finally
            {
                response.Close();
            }
        }

    }

    public record BasicJsonResponse(bool Success, string? Error = null);
    
    public class DictionaryJsonResponse(BasicJsonResponse response, Dictionary<string, string> items)
    {
        public BasicJsonResponse JsonResponse { get; private set; } = response;
        public Dictionary<string, string> Items { get; set; } = items;
    }
}
