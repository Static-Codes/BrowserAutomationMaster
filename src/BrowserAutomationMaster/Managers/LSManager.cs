using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.EndpointFunctions;
using static BrowserAutomationMaster.Managers.EndpointHelpers;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static System.Text.Encoding;

namespace BrowserAutomationMaster.Managers
{
    public class LSManager
    {
        private readonly static HttpListener listener = new();
        const string DEFAULT_PORT = "80";

        private static bool isRunning = true;
        public static bool IsRunning() { return isRunning; }
        
        public static void Terminate()
        {
            isRunning = false;
        }



        public static (string name, string args) GetProcessNameAndArgs()
        {
            if (Platforms.IsChromeOS || Platforms.IsUnixLike) return ("netstat", "-ltu");
            else if (Platforms.IsWindows) return ("cmd.exe", "/c netstat -ano");
            else throw new PlatformNotSupportedException("Invalid OS.");
        }

        /// <summary>
        /// </summary>
        /// <param name="groups">GroupCollection from a MatchCollection</param>
        /// <returns>An Enumerable of Group objects</returns>
        /// <exception cref="PlatformNotSupportedException"></exception>
        public static IEnumerable<Group>? GetValues(GroupCollection? groups)
        {
            if (groups == null || groups.Count == 0) 
                return [];

            if (Platforms.IsWindows)
                return groups
                    .Values
                    .Where(val =>
                        !string.IsNullOrEmpty(val.Value) &&
                        !val.Value.StartsWith("TCP", OIC) &&
                        !val.Value.StartsWith("UDP", OIC) &&
                        val.Value.All(c => char.IsNumber(c)) // Fixes issues with the matches including "localhost:"
                    );

            if (Platforms.IsUnixLike || Platforms.IsChromeOS)
                return groups.Values
                    .Where(val => 
                        !string.IsNullOrEmpty(val.Value) &&
                        val.Value.All(c => char.IsNumber(c)) // Fixes issues with the matches including "localhost:"
                    );

            throw new PlatformNotSupportedException("Invalid OS.");
        }

        public static async Task HandleEndpointRequests()
        {
            string[] invalidMethods = ["CONNECT", "DELETE", "HEAD", "OPTIONS", "PATCH", "POST", "PUT", "TRACE"];
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

                switch (request.Url.AbsolutePath)
                {
                    case "/":
                        var mainRouteBytes = UTF8.GetBytes("Load GUI");
                        await WriteResponse(response, mainRouteBytes);
                        break;

                    case "/app":
                        // LOAD THE GUI HERE
                        break;

                    case "/create":
                        throw new NotImplementedException("Implement me");

                    case "/export":
                        // This executes UserScriptManager.AddScript(), pass it to the newly created Export() method in EndpointFunctions
                        //UserScriptManager _ = new(path, "add");
                        throw new NotImplementedException("Implement me");

                    case "/load":
                        await Load(response);
                        break;

                    case "/terminate":
                        isRunning = false;
                        await WriteResponse(response, UTF8.GetBytes("{ \"terminated\": true }"));
                        return; // The use of return ends this functions execution whereas break only exits the current switch statement.

                    case "/upload" when request.HttpMethod.Equals("GET"):
                        await Upload(request, response);
                        break;

                    case "/validate":
                        await Validate(request, response);
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
            (var name, var args) = GetProcessNameAndArgs();
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
            (var ExitCode, var STDOut, var STDErr) = await ProcessFactory.GetProcessResponse(process);


            if (ExitCode != 0)
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

            if (STDOut.Count == 0)
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

            var matches = PrecompiledNetStatRegex().Matches(string.Join(NLC, STDOut));
            var foundPorts = new StringBuilder();

            for (int i = 0; i < matches.Count; i++)
            {
                if (matches[i].Success && matches[i].Groups.Count > 0)
                {
                    var groups = matches[i].Groups;
                    var values = GetValues(groups);

                    if (values == null)
                        continue;

                    foreach (var value in values)
                        foundPorts.AppendLine(value.Value);
                }
            }

            return [..
                foundPorts
                .ToString()
                .Split(NLC)
                .Where(val => !string.IsNullOrEmpty(val)) // Fixes bug where splitting using NLC causes element at index 0 to be empty.
                .Distinct()
                .Order()
            ];
        }

        public static async Task Start(string port = DEFAULT_PORT)
        {

            var url = $"http://127.0.0.1:{port}/";

            if (!HttpListener.IsSupported)
                WriteAndExit(
                    message:
                        string.Join(
                            string.Empty, 
                            [
                                "Unable to run the BAMM GUI on your current operating system, ",
                                "please open a text editor of your choice and create a new .bamc file."
                            ]
                        ), 
                    status: 1
                );

            try
            {

                var usedLHPorts = await ScanForUsedLHPorts();

                if (usedLHPorts.Contains(port))
                    throw new HttpListenerException(1, "Access is denied");

                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Started GUI on {0}\n", url);

                await HandleEndpointRequests();
                listener.Close();
            }

            catch (HttpListenerException ex)
            {
                if (ex.Message.Contains("Access is denied"))
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


    public static class EndpointFunctions
    {
        public static async Task Load(HttpListenerResponse response)
        {
            try
            {
                var data = new Dictionary<string, string>();

                foreach (var file in Parser.GetBAMCFiles())
                {
                    var fileBytes = UTF8.GetBytes(File.ReadAllText(file));
                    var b64Contents = System.Convert.ToBase64String(fileBytes);
                    data.Add(file, b64Contents);
                }

                await HandleValidResponse(response, data);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.Message);
            }
        }

        public static async Task Upload(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var b64Path = request.QueryString["path"];

                if (b64Path == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, missing param \"path\"");
                    return;
                }

                if (!IsB64(b64Path))
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a base64 string for the parameter \"path\"");
                    return;
                }


                var pathBytes = System.Convert.FromBase64String(b64Path);
                var path = UTF8.GetString(pathBytes);

                if (!File.Exists(path))
                {
                    await HandleInvalidResponse(response, "Invalid request, specified file doesn't exist.");
                    return;
                }

                if (!path.EndsWith(".bamc", OIC))
                {
                    await HandleInvalidResponse(response, "Invalid request, specified file is not a .BAMC file.");
                    return;
                }

                if (!Parser.IsValidFile(path))
                {
                    await HandleInvalidResponse(
                        response, 
                        "The .BAMC file you submitted contains invalid syntax, please check your terminal for more information."
                    );
                    return;
                }

                var lines = File.ReadAllLines(path);
                var items = new Dictionary<string, string>();

                for (int i = 0; i < lines.Length; i++)
                    items.Add(i.ToString(), lines[i]);

                await HandleValidResponse(response, items);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.Message);
            }
        }

        public static async Task Validate(HttpListenerRequest request, HttpListenerResponse response)
        {
            try
            {
                var b64Path = request.QueryString["path"];

                if (b64Path == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, missing param \"path\"");
                    return;
                }

                if (!IsB64(b64Path))
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a base64 string for the parameter \"path\"");
                    return;
                }


                var pathBytes = System.Convert.FromBase64String(b64Path);
                var filePath = UTF8.GetString(pathBytes);

                if (!File.Exists(filePath) || !filePath.EndsWith(".bamc", OIC))
                {
                    await HandleInvalidResponse(response, "Invalid request, specified file doesn't exist.");
                    return;
                }

                if (!Parser.IsValidFile(filePath))
                {
                    await HandleInvalidResponse(response, "The .BAMC file you submitted contains invalid syntax, please check your terminal for more information.");
                    return;
                }

                await HandleValidResponse(response, []);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.Message);
            }
        }

    }

    public static class EndpointHelpers
    {
        private static readonly Dictionary<string, string> Items = [];

        public static readonly DictionaryJsonResponse validResponse = new(
            response: new BasicJsonResponse(Success: true),
            items: Items
        );

        public static DictionaryJsonResponse Success(Dictionary<string, string> data)
        {
            return new DictionaryJsonResponse(
                response: new BasicJsonResponse(Success: true),
                items: data
            );
        }

        public static DictionaryJsonResponse Error(string error)
        {
            return new DictionaryJsonResponse(
                response: new BasicJsonResponse(Success: false) { Error = error },
                items: Items
            );
        }

        public static async Task HandleInvalidResponse(HttpListenerResponse response, string error)
        {
            var invalidResp = JsonSerializer.Serialize(Error(error));
            var respBytes = UTF8.GetBytes(invalidResp);
            await LSManager.WriteResponse(response, respBytes);
        }

        public static async Task HandleValidResponse(HttpListenerResponse response, Dictionary<string, string> items)
        {
            try
            {
                var validRespObj = JsonSerializer.Serialize(Success(items));
                var validRespBytes = UTF8.GetBytes(validRespObj);
                await LSManager.WriteResponse(response, validRespBytes);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.Message);
            }
        }
        
        public static bool IsB64(string b64string)
        {
            if (string.IsNullOrEmpty(b64string))
                return false;

            return 
                PrecompiledBase64Regex().IsMatch(b64string) &&
                b64string.Length % 4 == 0;
        }

    }
}
