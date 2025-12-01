using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.EndpointFunctions;
using static BrowserAutomationMaster.Managers.EndpointHelpers;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.RuntimeManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static System.Text.Encoding;

namespace BrowserAutomationMaster.Managers
{
    public class LocalServerManager
    {
        private readonly static HttpListener listener = new();
        const string DEFAULT_PORT = "8008";
        private readonly static int MINIMUM_GUI_MEMORY_MB = 2048;
        private readonly static string GUI_ZIP_PATH = GetGUIZipPath();

        private static bool isRunning = true;
        public static bool IsRunning() { return isRunning; }

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

        public static async Task<bool> DownloadGUI()
        {
            var msg = "Unable to download the GUI, any attempt to use the `--gui` flag will throw an error.";
            try
            {
                var response = await RequestManager.NetworkClient.Instance.GetAsync(GUI_ZIP_LINK);
                
                if (!response.IsSuccessStatusCode)
                    return WriteErrorAndReturnBool(msg, false);

                var content = await response.Content.ReadAsByteArrayAsync();

                if (content == null)
                    return WriteErrorAndReturnBool(msg, false);

                await File.WriteAllBytesAsync(GUI_ZIP_PATH, content);

                return File.Exists(GUI_ZIP_PATH);
            }

            catch (Exception ex)
            {
                var error = string.Join("", [msg, "Error Log:\n\n", ex.Message]);
                return WriteErrorAndReturnBool(error, false);
            }
        }

        public static bool ExtractGUI()
        {
            try
            {
                ZipFile.ExtractToDirectory(GUI_ZIP_PATH, AppDataDirectory);
                File.Delete(GUI_ZIP_PATH);
                return true;
            }
            catch (Exception ex)
            {
                Warning.Write("An unhandled exception has occured while attempting to extract BAMM's GUI.");
                WriteAndExit(ex.Message, 1);
            }
            return false;
        }

        public static (string name, string args) GetProcessNameAndArgs(bool scan = false)
        {
            if (scan && Platforms.IsChromeOS || Platforms.IsUnixLike) 
                return ("netstat", "-ltu");
            
            else if (scan && Platforms.IsWindows)
                return ("cmd.exe", "/c netstat -ano");
            
            else
                throw new PlatformNotSupportedException("Invalid OS.");
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
                    AddOptionResponseHeaders(response);




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
                        await WriteResponse(response, UTF8.GetBytes(string.Join("", [
                            $"{{\"version\": \"{UpdateManager.CurrentVersion}\", ", 
                            $"\"is_latest\": {(UpdateManager.CurrentVersion == UpdateManager.LatestVersion).ToString().ToLower()}}}"
                        ])));
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

        public static async Task StartServer(string port = DEFAULT_PORT)
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

                var memoryInfo = GetMemoryInfo();

                #pragma warning disable IDE0270 // Coalesce operator will break the current logic implementation, as such this warning can be ignored.
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

                listener.Prefixes.Add(url);
                listener.Start();
                Console.WriteLine("Started GUI Server on {0}\n", url);
                Console.WriteLine("To access the GUI visit {0}\n", GetMainGUIPage(includeProtocol: true));

                // ADD THIS FEATURE
                // Would you like to open the GUI in your default browser?
                // If yes, then open for the user

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


    public static class EndpointFunctions
    {

        public static async Task Export(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");
                response.StatusCode = (int)HttpStatusCode.NoContent; // 204 No Content
                response.Close();
                return;
            }

            try
            {
                var b64Contents = request.QueryString["contents"];
                var fileName = request.QueryString["filename"];

                if (b64Contents == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, missing param \"contents\"");
                    return;
                }

                if (!IsB64(b64Contents))
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a base64 string for the parameter \"contents\"");
                    return;
                }

                if (fileName == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a filename ending in '.bamc' for the parameter \"name\"");
                    return;
                }

                if (!fileName.EndsWith(".bamc", OIC))
                {
                    await HandleInvalidResponse(response, "Invalid request, specified file is not a .BAMC file.");
                    return;
                }

                var contentsBytes = System.Convert.FromBase64String(b64Contents);
                var contents = UTF8.GetString(contentsBytes).Split('\n');

                // Case insensitivity cpvers all supported platforms
                if (Parser.GetBAMCFiles().Any(file => file.EndsWith(fileName, CCIC)))
                {
                    await HandleInvalidResponse(response, "Invalid request, specified file already exists, please choose a different name.");
                    return;
                }

                var scriptPath = Path.Combine(Parser.userScriptsDirectory, fileName);

                if (File.Exists(scriptPath))
                {
                    await HandleInvalidResponse(
                        response,
                        $"Invalid request, the specific script already exists at: {scriptPath}, please choose another name."
                    );
                    return;
                }

                var file = File.Create(scriptPath);

                ArgumentNullException.ThrowIfNull(file);

                for (int i = 0; i < contents.Length; i++)
                {
                    string commandLine = contents[i];

                    if (string.IsNullOrEmpty(commandLine))
                    {
                        await HandleInvalidResponse(response, $"Unable to parse null or empty command on line {i + 1}");
                        continue;
                    }

                    if (commandLine == "start-javascript" || commandLine == "end-javascript")
                    {
                        var simpleBuffer = UTF8.GetBytes($"{commandLine}{NLC}");
                        file.Write(simpleBuffer);
                        continue;
                    }

                    try
                    {
                        var contentDict = JsonSerializer.Deserialize<Dictionary<string, string>>(commandLine);

                        ArgumentNullException.ThrowIfNull(contentDict);

                        var contentPair = contentDict.First();
                        string key = contentPair.Key;
                        string value = contentPair.Value;

                        if (key == "add-to-js")
                        {
                            byte[] decodedBytes = System.Convert.FromBase64String(value);
                            string decodedCode = UTF8.GetString(decodedBytes);

                            var codeBuffer = UTF8.GetBytes($"{decodedCode}{NLC}");
                            file.Write(codeBuffer);
                        }
                        else
                        {
                            var standardBuffer = UTF8.GetBytes($"{key} {value}{NLC}");
                            file.Write(standardBuffer);
                        }
                    }
                    catch (JsonException ex)
                    {
                        await HandleInvalidResponse(response, $"JSON Parsing Error on line {i + 1}: {ex.Message}. Content: {commandLine}");
                    }
                    catch (Exception ex)
                    {
                        await HandleInvalidResponse(response, $"Error processing command line {i + 1}: {ex.Message}");
                    }
                }
                file.Close();
                var successMessage = UTF8.GetBytes($"{{ \"success\": true, \"message\": \"Exported {fileName} successfully to {scriptPath}!\"}}");
                await LocalServerManager.WriteResponse(response, successMessage);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.StackTrace ?? ex.Message);
            }
        }

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

        public static void Redirect(HttpListenerResponse response)
        {
            string absolutePath = GetMainGUIPage(includeProtocol: false);

            string instructionalHTML = $@"
    <html>
    <head>
        <title>Local GUI Access Required</title>
        <style>
            body {{ background-color: #1e1e1e; color: #f4f4f4; font-family: sans-serif; padding: 20px; text-align: center; }}
            .container {{ max-width: 600px; margin: 50px auto; border: 1px solid #333; padding: 30px; border-radius: 8px; background-color: #252526; }}
            .path-box {{ background-color: #000; padding: 15px; border-radius: 4px; overflow-wrap: break-word; font-family: monospace; text-align: left; margin: 20px 0; }}
            .instructions {{ text-align: left; margin-top: 20px; }}
            .path-link {{ color: #007acc; text-decoration: none; word-break: break-all; }}
            .copy-btn {{ background-color: #007acc; color: white; border: none; padding: 8px 15px; border-radius: 4px; cursor: pointer; margin-left: 10px; }}
        </style>
    </head>
    <body>
        <div class='container'>
            <h1>GUI Access Instructions</h1>
            <p>The GUI file cannot be served directly due to browser security restrictions on local files.</p>
            <p>To launch the GUI, please manually open the following file:</p>
            <div class='path-box' id='filePathBox'>
                {absolutePath}
                <button class='copy-btn' onclick=""navigator.clipboard.writeText('{absolutePath.Replace(@"\", @"\\")}')"">Copy Path</button>
            </div>
            
            <div class='instructions'>
                <h2>Steps to follow:</h2>
                <ol>
                    <li>Copy the full path above (using the button).</li>
                    <li>Open a new browser window.</li>
                    <li>Paste the path into the address bar and press Enter.</li>
                </ol>
                <p>
                    For convenience, you may also try clicking this direct link, but browsers may block it: 
                    <a class='path-link' href='file://{absolutePath.Replace("\\", "/")}'>file://{absolutePath.Replace("\\", "/")}</a>
                </p>
            </div>
            <script>
                function fallbackCopyTextToClipboard(text) {{
                    var textArea = document.createElement(""textarea"");
                    textArea.value = text;
                    textArea.style.position = ""fixed"";
                    textArea.style.left = ""-999999px"";
                    document.body.appendChild(textArea);
                    textArea.focus();
                    textArea.select();
                    try {{
                        document.execCommand('copy');
                        alert('Path copied to clipboard!');
                    }} catch (err) {{
                        console.error('Failed to copy text: ', err);
                    }}
                    document.body.removeChild(textArea);
                }}

                document.querySelector('.copy-btn').onclick = function() {{
                    const text = '{absolutePath.Replace(@"\", @"\\")}';
                    if (navigator.clipboard && navigator.clipboard.writeText) {{
                        navigator.clipboard.writeText(text).then(function() {{
                            alert('Path copied to clipboard!');
                        }}, function(err) {{
                            console.error('Async: Could not copy text: ', err);
                            fallbackCopyTextToClipboard(text);
                        }});
                    }} else {{
                        fallbackCopyTextToClipboard(text);
                    }}
                }};
            </script>
        </div>
    </body>
    </html>
    ";

            byte[] buffer = UTF8.GetBytes(instructionalHTML);

            response.StatusCode = 200;
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = buffer.Length;

            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        // This is fully functional but was removed for time sake.
        [Obsolete("Removed support, since this project needs to have a final alpha release soon.")]
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
                var b64Contents = request.QueryString["contents"];

                if (b64Contents == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, missing param \"contents\"");
                    return;
                }

                if (!IsB64(b64Contents))
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a base64 string for the parameter \"contents\"");
                    return;
                }


                var b64contentBytes = System.Convert.FromBase64String(b64Contents);
                var contentString = UTF8.GetString(b64contentBytes);
                
                if (contentString == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, unable to split content lines, contentString is null.");
                    return;
                }

                var contents = contentString.Split('\n');

                if (contents == null || contents.Length == 0){
                    await HandleInvalidResponse(response, "Invalid request, unable to split content lines, contents contains no new line characters.");
                    return;
                }

                var finalBuffer = new List<string>();
                for (int i = 0; i < contents.Length; i++)
                {
                    string commandLine = contents[i];

                    if (string.IsNullOrEmpty(commandLine))
                    {
                        await HandleInvalidResponse(response, $"Unable to parse null or empty command on line {i + 1}");
                        continue;
                    }

                    if (commandLine == "start-javascript" || commandLine == "end-javascript")
                    {
                        // var jsBlockBuffer = UTF8.GetBytes($"{commandLine}{NLC}");
                        finalBuffer.Add($"{commandLine}{NLC}");
                        continue;
                    }

                    try
                    {
                        var contentDict = JsonSerializer.Deserialize<Dictionary<string, string>>(commandLine);

                        ArgumentNullException.ThrowIfNull(contentDict);

                        var contentPair = contentDict.First();
                        string key = contentPair.Key;
                        string value = contentPair.Value;

                        if (key == "add-to-js")
                        {
                            byte[] decodedBytes = System.Convert.FromBase64String(value);
                            string decodedCode = UTF8.GetString(decodedBytes);

                            // var codeBuffer = UTF8.GetBytes($"{decodedCode}{NLC}");
                            finalBuffer.Add($"{decodedCode}{NLC}");
                        }
                        else
                        {
                            // var standardBuffer = UTF8.GetBytes($"{key} {value}{NLC}");
                            finalBuffer.Add($"{key} {value}{NLC}");
                        }
                    }
                    catch (JsonException ex)
                    {
                        await HandleInvalidResponse(response, $"JSON Parsing Error on line {i + 1}: {ex.Message}. Content: {commandLine}");
                    }
                    catch (Exception ex)
                    {
                        await HandleInvalidResponse(response, $"Error processing command line {i + 1}: {ex.Message}");
                    }
                }

                // Another subpar solution, but I will be starting finals shortly so this will have to do.
                // This isnt inherently a critical performance flaw, but it does use added memory to convert a List<string> to a string[]. 
                Parser.IsValidFileContents([.. finalBuffer ]);

                var successMessage = UTF8.GetBytes($"{{ \"success\": true");
                await LocalServerManager.WriteResponse(response, successMessage);
            }
            catch (Exception ex)
            {
                await HandleInvalidResponse(response, ex.StackTrace ?? ex.Message);
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

        public static string EscapeMultiLineBlock(string block)
        {
            return block
                // 1. Escapes backslashes first, otherwise subsequent escapes will double them
                .Replace("\\", "\\\\") 
                // 2. Escapes double quotes within the code
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");
        }

        public static async Task HandleInvalidResponse(HttpListenerResponse response, string error)
        {
            var invalidResp = JsonSerializer.Serialize(Error(error));
            var respBytes = UTF8.GetBytes(invalidResp);
            await LocalServerManager.WriteResponse(response, respBytes);
        }

        public static async Task HandleValidResponse(HttpListenerResponse response, Dictionary<string, string> items)
        {
            try
            {
                var validRespObj = JsonSerializer.Serialize(Success(items));
                var validRespBytes = UTF8.GetBytes(validRespObj);
                await LocalServerManager.WriteResponse(response, validRespBytes);
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
