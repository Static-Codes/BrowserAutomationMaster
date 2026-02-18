using BrowserAutomationMaster.Managers.Parsing;
using System.Net;
using System.Text.Json;
using static BrowserAutomationMaster.Managers.Common.Constants;
using static BrowserAutomationMaster.Managers.Common.DirectoryManager;
using static BrowserAutomationMaster.Managers.GUI.Response;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Managers.Messaging.Errors;
using static System.Text.Encoding;

namespace BrowserAutomationMaster.Managers.GUI 
{
    public static class BackendFunctions
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
                await Server.WriteResponse(response, successMessage);
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

        public static async Task Terminate(HttpListenerResponse response) 
        {
            await Server.WriteResponse(response, UTF8.GetBytes("{ \"terminated\": true }"));
            await Task.Delay(50);
            Server.StopExecution();
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
            bool responseHandled = false;

            try
            {
                var b64Contents = request.QueryString["contents"];

                if (b64Contents == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, missing param \"contents\"");
                    responseHandled = true;
                    return;
                }

                if (!IsB64(b64Contents))
                {
                    await HandleInvalidResponse(response, "Invalid request, this endpoint requires a base64 string for the parameter \"contents\"");
                    responseHandled = true;
                    return;
                }


                var b64contentBytes = System.Convert.FromBase64String(b64Contents);
                var contentString = UTF8.GetString(b64contentBytes);

                if (contentString == null)
                {
                    await HandleInvalidResponse(response, "Invalid request, unable to split content lines, contentString is null.");
                    responseHandled = true;
                    return;
                }

                var contents = contentString.Split('\n');

                if (contents == null || contents.Length == 0)
                {
                    await HandleInvalidResponse(response, "Invalid request, unable to split content lines, contents contains no new line characters.");
                    responseHandled = true;
                    return;
                }

                var finalBuffer = new List<string>();
                for (int i = 0; i < contents.Length; i++)
                {
                    string commandLine = contents[i];

                    if (string.IsNullOrEmpty(commandLine))
                    {
                        await HandleInvalidResponse(response, $"Unable to parse null or empty command on line {i + 1}");
                        responseHandled = true;
                        continue;
                    }

                    if (commandLine == "start-javascript" || commandLine == "end-javascript")
                    {
                        finalBuffer.Add($"{commandLine}{NLC}");
                        continue;
                    }

                    try
                    {
                        int firstSpaceIndex = commandLine.IndexOf(' ');
                        if (firstSpaceIndex <= 0)
                        {
                            throw new FormatException("Missing command value separator. Expected format: 'key value'");
                        }

                        string key = commandLine[..firstSpaceIndex].Trim();
                        string value = commandLine[(firstSpaceIndex + 1)..].Trim();

                        if (key == "add-to-js")
                        {
                            byte[] decodedBytes = System.Convert.FromBase64String(value);
                            string decodedCode = UTF8.GetString(decodedBytes);

                            finalBuffer.Add($"{decodedCode}{NLC}");
                        }
                        else
                        {
                            finalBuffer.Add($"{key} {value}{NLC}");
                        }
                    }
                    catch (JsonException ex)
                    {
                        await HandleInvalidResponse(response, $"JSON Parsing Error on line {i + 1}: {ex.Message}. Content: {commandLine}");
                        responseHandled = true;
                    }
                    catch (Exception ex)
                    {
                        await HandleInvalidResponse(response, $"Error processing command line {i + 1}: {ex.Message}");
                        responseHandled = true;
                    }
                }

                if (responseHandled) { return; }

                byte[]? message = null;

                if (Parser.IsValidFileContents([.. finalBuffer])) {
                    message = UTF8.GetBytes($"{{ \"success\": true }}");
                } else {
                    message = UTF8.GetBytes($"{{\"success\": false, \"error\": \"Please check BAMM's output for more information.\"}}");
                }

                await Server.WriteResponse(response, message);
                response.Close();
                responseHandled = true;
            }
            catch (Exception ex)
            {
                if (!responseHandled) {
                    await HandleInvalidResponse(response, ex.StackTrace ?? ex.Message);
                } else {
                    Console.WriteLine($"Exception caught, but response already sent/closed: {ex.Message}");
                }
            }
        }

        public static async Task Version(HttpListenerResponse response) 
        {
            byte[] responseBytes;
            try
            {
                var responseJson = new Dictionary<string, string>() {
                    { "version", CurrentVersion },
                    { "is_latest", $"{CurrentVersion == LatestVersion}".ToLower()}
                };

                responseBytes = JsonSerializer.SerializeToUtf8Bytes(responseJson);
            }

            catch (Exception ex)
            {
                Write($"Internal server error occurred: {ex.Message}");

                responseBytes = UTF8.GetBytes(
                    string.Concat([
                        $"{{ \"version\": \"{CurrentVersion}\", ",
                        $"\"is_latest\": {(CurrentVersion == LatestVersion).ToString().ToLower()} }}"
                    ])
                );
            }

            await Server.WriteResponse(response, responseBytes);

            
        }

    }

}