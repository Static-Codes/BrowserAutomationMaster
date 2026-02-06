using BrowserAutomationMaster.Helpers;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.Python;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static BrowserAutomationMaster.Compilation.BrowserFunctions;
using static BrowserAutomationMaster.Managers.ConfigManager;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.DirectoryManager;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.Managers.Python.BrowserStack.DeviceManager;
using static BrowserAutomationMaster.Managers.Python.RuntimeManager;
using static BrowserAutomationMaster.Managers.Python.WheelManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Compilation
{

    public partial class Transpiler()
    {
        // This will be used in GenerateBackupName(); in the case of failure.
        private readonly static string defaultScriptFileName = "untitled-script";

        private readonly static string desiredSaveDirectory = GetDesiredSaveDirectory();
        private static string projectName = "";
        private readonly static string requirementsFileName = "requirements.txt";
        private static string projectDirectory = "";

        private static string pythonScriptFileName = "";  // Modified by SetScriptName();

        private static string pythonVersion = "3.9"; // Used in VEnvManager.InstallGlobalPackages

        // Default value if inhouse function fails.
        private static string requestUserAgent = DEFAULT_USER_AGENT;

        private readonly static string[] browserlessActions = ["save-as-html", "wait-for-seconds"];

        // Not to be confused with browserPresent, this is a flag that will be set true if no valid browser installations are found.
        private static bool noBrowsersFound = false;

        // This is the timeout applied to all WebDriverWait calls.
        private static int actionTimeout = 10;

        private readonly static Dictionary<string, int> desiredUrls = []; // KeyValuePair<url, lineNumber>

        private static readonly Script script = new();

        private static bool usingBrowserstack = false;

        private static BAMConfig? bamConfig;

        private static readonly HttpStatusCode[] InvalidResponseEnums = 
        [
            HttpStatusCode.Forbidden,
            HttpStatusCode.Locked,
            HttpStatusCode.MovedPermanently,
            HttpStatusCode.Unauthorized
        ];


        public static async Task New(string filePath, string[] args)
        {
            try
            {

                CheckBrowserStackStatus();

                // Checks if this function was executed as a result of the "compile" argument being passed.
                // If so, an empty string is returned, otherwise a null value is passed.
                // If a null value is passed, it signals for BAMM to request the project name.
                var customName = args.Any(arg => arg.Equals("compile", OIC)) ? string.Empty : null;

                // Found it's more reliable to reset the state when a new Transpiler object is created.
                ResetTranspilerState(customName);

                SetBAMConfig(filePath);

                // Sets this.projectDirectory
                CreateProjectDirectory();

                SetScriptName(filePath);

                // Null forgiveness here because SetBAMConfig ensure's the config is not null.
                GetDesiredUrls(bamConfig!.Lines);

                // Sets UserAgentManager.userAgentsData
                UserAgentManager.SetUserAgents();

                AddBrowserImportsAndRequirements(bamConfig);

                await HandleCompilation(filePath, args, bamConfig);

                WritePythonFile();

                WriteRequirementsFile();

                string path = Path.Combine(desiredSaveDirectory, projectName, pythonScriptFileName);
                WriteSuccessMessage($"Compiled -> {bamConfig.Name}");
                WriteSuccessMessage($"Location -> {path}\n");

                HandleAutoCopy();
                //HandleAutoRun here
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    "BAM Manager (BAMM) was unable to continue due to a fatal error.\n\n" +
                    $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                    $"Error Log:\nUnhandled exception: {ex.Message}",
                    status: 1
                );
            }
        }

        private static void AddBrowserImportsAndRequirements(BAMConfig config)
        {
            HandleBrowserCmd(config);

            string noUrlsFound =
                "BAM Manager (BAMM) was unable to find any 'visit' commands in the provided file.\n\n" +
                "Please ensure the selected file has atleast one 'visit' command.";

            if (desiredUrls.Count == 0)
            {
                WriteAndExit(noUrlsFound, 1);
                return;
            }


            // ARMv7 (ARMel + ARMhf) Specific Packages (Precompiled Wheels for each Architecture)
            if (Platforms.IsARMel || Platforms.IsARMhf)
            {
                script.AddRequirementPackages(GetRequirementStrings());
            }

            // This function will exit if a null value is reached so no worries about a null check here
            string sVersion = PyPiPackageManager.Get("selenium", pythonVersion);
            string swVersion = PyPiPackageManager.Get("selenium-wire", pythonVersion);
            string wmVersion = PyPiPackageManager.Get("webdriver_manager", pythonVersion);
            var bsVersion = PyPiPackageManager.Get("browserstack-sdk", pythonVersion);

            var sdkPackage = usingBrowserstack ? $"browserstack-sdk=={bsVersion}" : string.Empty;
            var sdkLocalPackage = usingBrowserstack ? $"browserstack-local >= 1.2.3" : string.Empty;


            string[] packages = [
                "setuptools==80.9.0",
                $"selenium=={sVersion}",
                $"selenium-wire=={swVersion}",
                $"webdriver_manager=={wmVersion}",
                "blinker==1.4", // This fixes the mess that selenium-wire causes by installing blinker >=1.9
                sdkPackage,
                sdkLocalPackage
            ];

            script.Requirements.AddPackages(packages);


            string[] imports = [
                "from selenium.common.exceptions import NoSuchElementException",
                "from selenium.webdriver.common.by import By",
                "from selenium.webdriver.support.ui import Select, WebDriverWait",
                "from selenium.webdriver.support import expected_conditions as EC",
                "from seleniumwire import webdriver"
            ];

            script.Imports.AddStatements(imports);
            string selectedBrowser = config.selectedBrowser;

            string[] statements;

            // Uncomment and remove this comment if Brave support is reintroduced.
            //if (selectedBrowser.Equals("brave", OIC))
            //{
            //    statements = [
            //        "from selenium.webdriver.chrome.options import Options",
            //        "from selenium.webdriver.chrome.service import Service as ChromeService",
            //        "from webdriver_manager.chrome import ChromeDriverManager",
            //        "from webdriver_manager.core.os_manager import ChromeType"
            //    ];
            //    script.Imports.AddStatements(statements);
            //}

            if (selectedBrowser.Equals("chrome", OIC))
            {
                statements = [
                    "from selenium.webdriver.chrome.options import Options",
                    "from selenium.webdriver.chrome.service import Service as ChromeService",
                    "from webdriver_manager.chrome import ChromeDriverManager"
                ];

                script.Imports.AddStatements(statements);
            }

            else if (selectedBrowser.Equals("firefox", OIC))
            {
                statements = [
                    "from selenium.webdriver.firefox.options import Options",
                    "from selenium.webdriver.firefox.service import Service as FirefoxService",
                    "from webdriver_manager.firefox import GeckoDriverManager"
                ];
                script.Imports.AddStatements(statements);
            }

            else
            {
                throw new Exception(
                    "Invalid browser provided to 'browser' command.\n" +
                    "Expected: \"chrome\" or \"firefox\"");
            }
        }
        
        public static void AddImportIfNotPresent(string import, bool addToReqs = false, string? reqText = null)
        {
            bool validStatement = import.StartsWith("from") || import.StartsWith("import");

            if (!validStatement) {
                WriteAndExit(
                    message: $"Invalid import statement: {import}.",
                    status: 1
                );
            }

            if (addToReqs && !string.IsNullOrEmpty(reqText)) {
                WriteAndExit(
                    message: $"Invalid requirement statement: {reqText}.",
                    status: 1
                );
            }

            script.Imports.AddStatement(import);

            if (addToReqs) {
                script.Requirements.AddPackage(reqText!);
            }
        }
        
        private static void AddWatermark()
        {
            // Does not appear to be working
            AddImportIfNotPresent(import: "from time import sleep", addToReqs: false, reqText: null);

            // Remove the import from this text once the function above is fixed.
            var watermarkText =
                $"stdout.write('''Made using BAM Manager (BAMM!){NLC}" +
                $"{BASE_REPO_LINK}{NLC}'''){NLC}" +
                $"sleep(3){NLC}{NLC}";

            script.Body.AddLine(watermarkText, 0);
        }
        
        private static void AddRequiredFunctions(BAMConfig config)
        {
            Dictionary<string, bool> functionsPresent = [];

            // Checks if configLines contains each arg, if so the required function is be added.
            // add-header is added here since its in actionArg, but its not accessed in this function.
            foreach (string actionArg in Parser.actionArgs)
            {
                functionsPresent.Add(
                    actionArg,
                    config.Lines.Any(line => line.StartsWith(actionArg))
                );
            }

            int index = 1; // Accounts for the functions below in the script.Body.
            script.Body.AddLine(MakeRequestFunction(requestUserAgent), 0);

            Action Add(string func) => () => script.Body.AddLine(func, index);
            Action AddRange(string[] lines) => () => script.Body.AddLines(lines);

            Dictionary<string, Action> cmdFuncs = new() {
                {  "click", Add(clickElementFunction) },
                {  "click-at-position", Add(clickAtPositionFunction) },
                {  "click-exp", Add(clickElementExperimentalFunction) },
                {  "close-current-tab", Add(closeCurrentTabFunction) },
                {  "fill-text", Add(fillTextFunction) },
                {  "fill-text-exp", Add(fillTextExperimentalFunction) },
                {  "get-text", Add(getTextFunction) },
                {  "open-new-tab", Add(openNewTabFunction) },
                {  "save-as-html", Add(saveAsHTMLFunction) },
                {  "save-as-html-exp", Add(saveAsHTMLExperimentalFunction) },
                {  "select-option", AddRange([selectElementFunction, selectOptionByIndexFunction]) },
                {  "take-screenshot", Add(takeScreenshotFunction) }
            };

            foreach (var cmdFunc in cmdFuncs)
            {
                // Presence check
                if (functionsPresent.TryGetValue(cmdFunc.Key, out bool isNeeded) && isNeeded)
                {
                    bool wasFound = cmdFuncs.TryGetValue(cmdFunc.Key, out Action? actionToPerform);

                    if (wasFound && actionToPerform != null)
                    {
                        actionToPerform();
                        index++;
                    }
                }
            }

            var lineCount = script.Body.GetLineCount();
            if (lineCount != index) {
                script.Body.AddLine(browserQuitCode, lineCount);
            }

            else {
                Add(browserQuitCode);
            }
        }
        
        public static void CheckBrowserStackStatus()
        {
            // If argument `--bs` is not provided, usingBrowserstack will be false.
            // This functions checks if:
            // 1. The user is running on ChromeOS, since Chromebooks are 99/100 times too underpowered for Selenium execution.
            // 2. The user modified the `use_browserstack` property in config.ini
            // 3. The user is running on Raspberry Pi with less than 2GB of free memory.
            
            var memoryInfo = GetMemoryInfo();

            if (memoryInfo == null) {
                return;
            }
            
            var availableMemory = memoryInfo.Value.FreeMemory;

            if (!usingBrowserstack) {
                usingBrowserstack = Platforms.IsChromeOS || GlobalConfig.UseBrowserstack || Platforms.IsRaspi && availableMemory < 2048;
            }
        }

        private static void CreateProjectDirectory()
        {
            try
            {
                if (!Directory.Exists(desiredSaveDirectory)) {
                    Directory.CreateDirectory(desiredSaveDirectory);
                }
            }
            catch
            {
                WriteAndExit(
                    "BAMM Manager (BAMM) was unable to create the desired project directory, please try again.",
                    status: 1
                );
            }

            projectDirectory = Path.Combine(desiredSaveDirectory, projectName);
            try
            {
                if (!Path.Exists(desiredSaveDirectory)) {
                    Directory.CreateDirectory(desiredSaveDirectory);
                }
            }
            catch (Exception ex)
            { 
                WriteAndExit(
                    message: string.Join(NLC, [
                        "BAMM Manager (BAMM) was unable to create the desired project directory, please try again.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }

            try
            {
                if (!Path.Exists(projectDirectory)) {
                    Directory.CreateDirectory(projectDirectory);
                }
            }
            catch (Exception ex)
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "BAMM Manager (BAMM) was unable to create the desired project directory, please try again.",
                        "Error Log:",
                        ex.Message
                    ]),
                    status: 1
                );
            }
        }

        private static void GenerateBackupScriptName()
        {
            string potentialFileName = $"{defaultScriptFileName}.py";
            int index = 2;
            while (true)
            {
                if (!File.Exists(potentialFileName))
                {
                    pythonScriptFileName = potentialFileName;
                    break;
                }
                potentialFileName = $"{defaultScriptFileName}({index}).py";
                index++;
            }
        }
        
        public static BAMConfig? GetBAMConfig() { 
            return bamConfig; 
        }

        public static bool GetBrowserStackStatus() { 
            return usingBrowserstack; 
        }

        private static void GetDesiredUrls(string[] lines)
        {
            int lineNumber = 1;
            foreach (string line in lines)
            {
                string[] args = line.Split(' ') ?? [];
                if (args.Length == 2 && line.Contains("visit"))
                {
                    string sanitizedArg = args[1].Replace('"', ' ').Trim();
                    desiredUrls.TryAdd(sanitizedArg, lineNumber);
                }
                lineNumber++;
            }
        }

        public static string GetProjectName(string? customName = null)
        {
            var isRunning = customName == null;

            // If a value is passed to the customName param, this loop is skipped and the param value is returned.
            while (isRunning)
            {
                customName = Input.AskForInput("Please enter a name for this project: ");

                if (string.IsNullOrEmpty(customName)) {
                    continue;
                }

                if (ValidDirectoryRegex.IsMatch(customName)) {
                    return customName;
                }

                Write("Invalid name, a project name can contain only alphanumeric characters, dashes, and periods.\nPlease try again.");
                Thread.Sleep(2000);
            }

            // This is the string passed as a parameter, it cannot be null at this point.
            return customName!; 
        }

        private static void HandleAutoCopy()
        {
            if (!GlobalConfig.AutoCopyPath) {
                return;
            }

            if (!Directory.Exists(projectDirectory)) {
                return;
            }

            if (!ClipboardHelper.TrySetText(projectDirectory)) 
            {
                Write(
                    string.Join(NLC, [
                        "Unable to copy project directory to clipboard, please manually copy this path:",
                        projectDirectory
                    ])
                );
            }

            WriteSuccessMessage("Successfully copied project directory to clipboard.");
        }
        
        private static void HandleBrowserCmd(BAMConfig config)
        {
            // GetUserAgent will exit in the event an invalid browserName is passed, thus the use of the nullable operator
            if (config.browserPresent)
            {
                var potentialUA = UserAgentManager.GetUserAgent(config.selectedBrowser);
                if (potentialUA == null)
                {
                    WriteErrorAndReturnNull("Unable to select custom user agent, please try again");
                }

                requestUserAgent = potentialUA!; // null check is done above.
            }
        }
        
        private static async Task HandleCompilation(string fileName, string[] args, BAMConfig config)
        {
            SetCustomUserAgent(args);
            SetTimeout(args);

            // Handles cases where features are requested but unsupported for the given test.
            HandleDisabling(config);

            int lineNumber = 1;

            // Prevents duplicate entries of MakeRequestFunction();
            bool firstVisitFinished = false;

            // Prevents issues caused by set-custom-user-agent having unique formatting (Many spaces).
            bool isCU = false;

            // Prevents issues caused by click-exp having unique formatting.
            bool isCE = false;

            // Prevents issues caused by fill-text and fill-text-exp if the arguments have spaces in them.
            bool isFT = false;

            bool isJSBlock = false; // Prevents issues caused by embedding javascript code into python code.
            bool isJSLine = false;  // Also prevents issued caused by embedding javascript code into python code.
            string jsBlockContent = "";



            foreach (string originalLine in config.Lines)
            {
                // Since iterators can't be overwritten, storing it as a local variable is the best solution.
                string line = originalLine;

                if (string.IsNullOrEmpty(line)) // Skip blank lines.
                {
                    continue;
                }

                // Indicates a comment is present (ignores comments within JS blocks)
                if (line.StartsWith("// ") && !isJSBlock)
                {
                    continue;
                }

                // Handling 'add-headers' before 'visit' is processed would be an issue if it weren't for Parser
                // Parser ensures 'browser' first (or defaults to firefox) then features and finally any other logic.
                Match match = PrecompiledHeaderRegex().Match(line);
                if (match.Success)
                {

                    string requestLine = script.Body.GetMakeRequestLine();

                    if (string.IsNullOrEmpty(requestLine))
                    {
                        WriteAndExit
                        (
                            message:
                                "Unable to locate request logic in partially compiled script, " +
                                "please attempt recompilation.",
                            status: 1
                        );
                    }

                    int index = script.Body.scriptLines.IndexOf(requestLine);

                    if (index == -1)
                    {
                        WriteAndExit
                        (
                            message:
                                "BAM Manager (BAMM) was unable to locate request logic in partially compiled script, " +
                                "please attempt recompilation.",
                            status: 1
                        );
                    }

                    // Value is assumed to be correct,
                    // but will very much cause an issue if the regex is found to not be fully reliable.
                    else
                    {
                        var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(match.Groups["json"].Value);

                        // Nullable value is handled inside AddHeadersFunction
                        var headersString = AddHeadersFunction(headers!);

                        script.Body.AddLine(headersString, index - 1);
                    }

                    continue;
                }


                if (line.StartsWith("click-exp "))
                {
                    isCE = true;
                }

                else if (line.StartsWith("fill-text"))  // Also handles fill-text-exp
                {
                    isFT = true;
                }

                else if (line.StartsWith("set-custom-useragent"))
                {
                    isCU = true;
                }

                else if (line.StartsWith("start-javascript"))
                {
                    isJSBlock = true;
                    continue;
                }

                else if (line.StartsWith("end-javascript"))
                {
                    isJSBlock = false;
                }

                string[] splitLine;

                // This handles fill-text or set-custom-useragent
                if (isFT || isCU)
                {
                    splitLine = line.Split(" \"");
                }

                // This handles all but click-exp, fill-text, and set-custom-user-agent
                else if (!isCE)
                {
                    splitLine = line.Split(" ");
                }

                // This handles click-exp
                else
                {
                    splitLine = line.Split(" '");
                }

                // Prevents the length check below from returning an error for javascript code blocks.
                if (isJSBlock)
                {
                    isJSLine = true;
                }

                int[] validLengths = [2, 3];

                // These are special because they require no parsing.
                // excludes start-javascript + end-javascript theyre handled below.
                string[] specialCommands = ["close-current-tab"];

                bool normalLengthBypass = !validLengths.Contains(splitLine.Length) && !isJSLine;
                bool specialLengthBypass = specialCommands.Any(cmd => line.Replace('"', ' ').Trim().StartsWith(cmd));

                if (specialLengthBypass)
                {
                    continue;
                }

                if (normalLengthBypass)
                {
                    WriteAndExit
                    (
                        message:
                            GenerateErrorMessage(
                                fileName,
                                line,
                                lineNumber,
                                "Invalid command syntax."
                            ),
                        status: 1
                    );
                }

                // Handle case where user attempts to create another jsBlock before closing the previous one.
                if (isJSBlock && line.StartsWith("start-javascript"))
                {
                    WriteAndExit
                    (
                        message:
                            GenerateErrorMessage(
                                fileName,
                                line,
                                lineNumber,
                                "The previous javascript code block was not closed before attempting to create another.  " +
                                "Please close the previous javascript code block and recompile."
                            ),
                        status: 1
                    );
                }

                // Add prevalidated line content to the jsBlock.
                else if (isJSBlock)
                {
                    jsBlockContent += $"{line}\n";
                    continue;
                }

                // Writes the actual JS Block as python code.
                if (line.StartsWith("end-javascript") && !isJSBlock)
                {
                    // Handles cases where Esprima might be more lenient towards invalid code.
                    PreprocessJSCodeBlock(jsBlockContent);

                    if (!JavaScript.IsValidSyntax(jsBlockContent, out string? error))
                    {
                        WriteAndExit
                        (
                            message:
                                GenerateErrorMessage
                                (
                                    fileName,
                                    line,
                                    lineNumber + 1,
                                    $"Invalid javascript code block:\n\nParser Error:\n\n" +
                                    $"{error}"
                                ),
                            status: 1
                        );
                    }

                    script.Body.AddLine($"driver.execute_script('''{jsBlockContent}''')\n");

                    jsBlockContent = string.Empty;
                    isJSLine = false;

                    continue;
                }

                string firstArg = splitLine.First();
                bool canRunBrowserless = browserlessActions.Any(action => action.StartsWith(firstArg));

                if (!canRunBrowserless && noBrowsersFound)
                {
                    WriteAndExit
                    (
                        message: GenerateErrorMessage(fileName, line, lineNumber, "No valid browser installations found, please install chrome or firefox."),
                        status: 1
                    );
                }

                string sanitizedArg2;
                
                if (!isCE) {
                    sanitizedArg2 = splitLine[1].Replace('"', ' ').Trim();
                } else {
                    sanitizedArg2 = splitLine[1].Replace('\'', ' ').Replace('"', ' ').Trim();
                }

                string sanitizedArg3 = string.Empty;

                // The parser ensures no invalid lines can be provided to the compiler :)
                if (splitLine.Length >= 3)
                {
                    sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim();
                }

                switch (firstArg)
                {

                    case "add-cookie":
                        CompilationHandler.AddCookie(script.Body.scriptLines, sanitizedArg2, sanitizedArg3);
                        break;

                    case "add-header":
                        CompilationHandler.AddHeader(script.Body.scriptLines, sanitizedArg2, sanitizedArg3);
                        break;


                    case "click" when CompilationHandler.Click(
                          script.Body.scriptLines,
                          splitLine,
                          actionTimeout
                        ) is false:
                        string issueText = $"Unable to parse selector: {splitLine[1]}\n" +
                                           $"If this is a CSS Selector, please use:\n" +
                                           $"click-exp '{sanitizedArg2}'";
                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, issueText),
                          status: 1
                        );

                        break;


                    case "click-at-position" when CompilationHandler.ClickAtPosition(
                          script.Body.scriptLines,
                          splitLine,
                          sanitizedArg2,
                          sanitizedArg3,
                          actionTimeout
                        ) is (false, var eText):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, issueText: eText),
                          status: 1
                        );
                        break;


                    case "click-exp" when CompilationHandler.ClickExp(
                          script.Body.scriptLines,
                          splitLine,
                          sanitizedArg2,
                          actionTimeout,
                          ref isCE
                        ) is (false, var err):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, err),
                          status: 1
                        );
                        break;


                    case "close-current-tab":
                        CompilationHandler.CloseCurrentTab(script.Body.scriptLines);
                        break;


                    case "get-text" when CompilationHandler.GetText(
                         script.Body.scriptLines,
                         splitLine
                        ) is (false, var e):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, e),
                          status: 1
                        );
                        break;


                    case "fill-text" when CompilationHandler.FillText(
                        script.Body.scriptLines,
                        splitLine,
                        sanitizedArg2,
                        ref isFT
                        ) is (false, var issue):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, issue),
                          status: 1
                        );
                        break;


                    case "fill-text-exp" when CompilationHandler.FillTextExp(
                        script.Body.scriptLines,
                        script.Imports.statementList,
                        splitLine,
                        sanitizedArg2,
                        ref isFT
                        ) is (false, var exceptionText):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, exceptionText),
                          status: 1
                        );
                        break;


                    case "open-new-tab" when CompilationHandler.OpenNewTab(
                        script.Body.scriptLines,
                        sanitizedArg2,
                        sanitizedArg3
                        ) is (false, var errorText):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, errorText),
                          status: 1
                        );
                        break;


                    case "save-as-html":
                        CompilationHandler.SaveAsHTML(script.Body.scriptLines, sanitizedArg2);
                        break;


                    case "save-as-html-exp":
                        CompilationHandler.SaveAsHTMLExp(script.Body.scriptLines, sanitizedArg2);
                        break;


                    case "select-element" when CompilationHandler.SelectElement(
                        script.Body.scriptLines,
                        splitLine,
                        actionTimeout
                        ) is (false, var errMsg):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, errMsg),
                          status: 1
                        );
                        break;


                    case "select-option" when CompilationHandler.SelectOption(
                        script.Body.scriptLines,
                        sanitizedArg2,
                        sanitizedArg3,
                        actionTimeout
                        ) is (false, var errorMsg):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, errorMsg),
                          status: 1
                        );
                        break;


                    case "set-custom-useragent":
                        CompilationHandler.SetCustomUserAgent(splitLine, lineNumber, ref requestUserAgent, ref isCU);
                        break;


                    case "take-screenshot":
                        CompilationHandler.TakeScreenshot(script.Body.scriptLines, sanitizedArg2);
                        break;


                    case "visit" when await CompilationHandler.Visit(
                        script.Body.scriptLines,
                        [.. config.featureLines],
                        sanitizedArg2,
                        config.selectedBrowser,
                        firstVisitFinished,
                        config.disableSSL,
                        config.runHeadless,
                        config.Extensions) is (false, var eMessage):

                        WriteAndExit(
                          message: GenerateErrorMessage(fileName, line, lineNumber, eMessage),
                          status: 1
                        );
                        break;


                    case "wait-for-seconds" when CompilationHandler.WaitForSeconds(
                        script.Body.scriptLines,
                        splitLine,
                        sanitizedArg2) is (false, var errMessage):

                        WriteAndExit(
                            message: GenerateErrorMessage(fileName, line, lineNumber, errMessage),
                            status: 1
                        );
                        break;
                }
                lineNumber++;
            }

            AddRequiredFunctions(config);
            SuppressUnneededWarnings();
            AddWatermark(); // Single comment watermark, completely nonintrusive and easily removable
        }

        public static void HandleDisabling(BAMConfig config)
        {
            var headlessMessage =
                "Headless Mode is not supported while using BrowserStack.\n\n" + 
                "Solution:\n" +
                "   - ChromeOS:" +
                "       - No current resolution.\n\n" + 
                "   - Other OS:" +
                $"      - Stop using BrowserStack, Disable 'using_browserstack' in:\n{GetBrowserStackConfigPath()}";

            // Headless Mode is disabled when BrowserStack is selected.
            // This is due to the number of complexities introduced by supporting 2 additional platforms via BrowserStack
            if (config.runHeadless && GetBrowserStackStatus())
            {
                Warning.Write(headlessMessage);
                config.runHeadless = false;
            }

            if (config.disablePycache)
            {
                string[] statements = ["import sys", "sys.dont_write_byte_code = True"];
                script.Imports.AddStatements(statements);
            }

            // Reminder to add back .Equals("brave"), if Brave support is reintroduced.
            if (config.disableSSL && config.selectedBrowser.Equals("chrome", OIC))
            {
                var statement = "from selenium.webdriver.chrome.options import Options";
                script.Imports.AddStatement(statement);
            }

            else if (config.disableSSL && config.selectedBrowser.Equals("firefox", OIC))
            {
                var statement = "from selenium.webdriver.firefox.options import Options";
                script.Imports.AddStatement(statement);
            }
        
        }
        
        public static void HandlePythonVersionSelection(Installations installations)
        {
            // Since there's 6 python versions supported the max number of found versions is 6.
            var maxVersions = 6;
            var versionArray = new string[maxVersions];

            var versionMapping = new Dictionary<ApplicationNames, string>() {
                { ApplicationNames.Python3_X, "3." },
                { ApplicationNames.Python3_8, "3.8" },
                { ApplicationNames.Python3_9, "3.9" },
                { ApplicationNames.Python3_10, "3.10" },
                { ApplicationNames.Python3_11, "3.11" },
                { ApplicationNames.Python3_12, "3.12" },
                { ApplicationNames.Python3_13, "3.13" },
                { ApplicationNames.Python3_14, "3.14" },
            };

            var errorMessage =
                "Unable to find a valid installation of python.\n" +
                $"If this error persists, please make a bug report at {ISSUES_LINK}";

            int index = 0;
            foreach (ApplicationNames app in installations.AppNames)
            {
                if (index == maxVersions) {
                    break;
                }

                if (!versionMapping.TryGetValue(app, out string? appVersion)) {
                    continue;
                }

                versionArray[index] = appVersion;
                index += 1;
            }

            var foundVersions = versionArray.Where(ver => ver != null && ver.Contains("3."));

            // Checks for valid contents since the array is initialized at the beginning of the function.
            if (!foundVersions.Any()) {
                WriteAndExit(errorMessage, 1);
            }

            if (foundVersions.Count() == 1) {
                pythonVersion = versionArray[0];
                return;
            }

            var response = Input.WriteListFromOptions(versionArray, noun: "version of Python");
            var version = GetVersionNumber(response);

            if (version == "Not Found") {
                return;
            }

            if (IsValidPyVersion(version)) {
                pythonVersion = version;
            }

        }
        
        public static async Task<bool> HandleRunOnCompile()
        {
            if (!GlobalConfig.RunOnCompile) {
                return false;
            }

            if (!Directory.Exists(projectDirectory)) {
                WriteAndExit(
                    "Unable to run the newly compiled project, please ensure this directory still exists.",
                    status: 1
                );
            }

            var path = Path.Combine(projectDirectory, pythonScriptFileName);

            if (!File.Exists(path)) {
                WriteAndExit(
                    "Unable to run the newly compiled project, please ensure this file still exists.\n\n" +
                    $"Path: {path}",
                    status: 1
                );
            }

            var runtimeManager = new RuntimeManager(path);
            await runtimeManager.RunScript(usingBrowserstack);
            return true;
        }
        
        private static bool HasUnclosedQuotes(string line)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool isEscaped = false; // True if the previous character was a backslash
            foreach (char c in line.Trim())
            {
                if (isEscaped)
                {
                    // If this flag is hit the previous character was a backslash, indicating this character is escaped and should be ignored.
                    isEscaped = false;
                    continue;
                }
                if (c == '\\')
                {
                    // If this flag is hit it indicates the current character is a backslash and the next character will be escaped & ignored.
                    isEscaped = true;
                    continue;
                }
                if (c == '\'')
                {
                    // If a single quote is inside a set of double quotes, the single quote is a literal character (most likely an apostrophe)
                    if (!inDoubleQuote)
                    {
                        // A single quote is only a delimiter if it's not inside a set of double quotes.
                        inSingleQuote = !inSingleQuote;
                    }
                }
                else if (c == '"')
                {
                    // If a quote quote is inside a set of single quotes, the double quote is a literal character.
                    if (!inSingleQuote)
                    {
                        // A double quote is only a delimiter if it's not inside a set of single quotes.
                        inDoubleQuote = !inDoubleQuote;
                    }
                }
            }

            // If either flag is true at the end, a quote was left unclosed
            return inSingleQuote || inDoubleQuote;
        }
        
        
        
        private static bool IsValidPyVersion(string pyVersion)
        {
            if (string.IsNullOrWhiteSpace(pyVersion)) { 
                return false; 
            }

            string[] parts = pyVersion.Split('.');
            if (parts.Length != 2) { 
                return false; 
            }

            bool majorFound = int.TryParse(parts[0], out int major);
            bool minorFound = int.TryParse(parts[1], out int minor);

            if (!majorFound || !minorFound) { 
                return false; 
            }
            
            // Since Python 3.15 is in beta, this is an attempt to support it.
            // If this causes fatal crashes, and unexpected behavior, this will be rolled back.
            if (major == 3 && minor >= 15) {
                return WriteErrorAndReturnBool(
                    message: string.Join(string.Empty, [
                        "Python 3.15+ is currently not tested with BAMM, ", 
                        "please be aware you might encounter bugs and other unexpected behavior."
                    ]),
                    returnBool: true
                );
            }

            // This checks Python versions between 3.9 and 3.14, while the above handles those on beta releases.
            bool isValidVersion =
                major == 3 &&
                minor >= 9 &&
                minor <= 14;

            return isValidVersion;
        }

        public static bool IsLocalFile(string link)
        {
            if (string.IsNullOrWhiteSpace(link)) {
                return false;
            }

            if (!link.StartsWith("file://")) {
                return false;
            }

            string filePath = link[7..];

            if (string.IsNullOrWhiteSpace(filePath)) {
                return false;
            }

            return File.Exists(filePath);
        }

        public static bool IsResolvableLink(string link)
        {
            try
            {
                if (IsLocalFile(link)) {
                    return true;
                }

                bool isValidUri = Uri.TryCreate(
                    link, 
                    UriKind.Absolute, 
                    out Uri? uriResult
                );

                if (!isValidUri)
                {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to resolve: '{link}'{NLC}{NLC}" +
                            $"Error log:{NLC}Unable to create Uri object from provided link, returned a false boolean.",
                        status: 1
                    );
                    return false;
                }
                
                if (uriResult == null)
                {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to resolve: '{link}'{Enumerable.Repeat(NLC, 2)}" +
                            $"Error log:{NLC}Unable to create Uri object from provided link, returned a null result.",
                        status: 1
                    );
                    return false;
                }

                RequestManager requestManager = new(uriResult, timeout: 10);

                HttpClient client = requestManager.Client;
                Uri uriToRequest = requestManager.Uri;
                TimeSpan requestTimeout = requestManager.Timeout;

                using var cts = new CancellationTokenSource(requestTimeout); // cts.Token passed to GetASync

                // HttpCompletionOption.ResponseHeadersRead requires only the response headers to be read, no content is loaded.
                Task<HttpResponseMessage> responseTask =
                    client.GetAsync(
                        uriToRequest,
                        HttpCompletionOption.ResponseHeadersRead,
                        cts.Token
                    );

                responseTask.Wait();
                HttpResponseMessage response = responseTask.Result;

                // With these specific status codes
                // The server is responding that the content IS or WAS at this location, however the content is not accessible.
                // A warning is provided, and the issue is assumed to be lack of adequate headers.

                if (InvalidResponseEnums.Contains(response.StatusCode)) {
                    return true;
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Write(
                    message:
                        $"BAM Manager (BAMM) was unable to resolve the url: '{link}'"
                );

                string exceptionMessage = ex.InnerException?.Message ?? "";

                bool isExpectedErrType = ex.GetType() == typeof(PingException);
                bool errPresent = isExpectedErrType && exceptionMessage.StartsWith("No such host is known");

                if (errPresent)
                {
                    Warning.Write(
                        message:
                            $"It is possible the website you are requesting is unable or incorrectly entered.{NLC}{NLC}" +
                            $"Exception:{NLC}{NLC}{ex.InnerException}"
                    );
                }
                
                string response = Input.AskForInput("Would you like to continue compilation? [y/n]: ");
                
                if (Input.ConditionRejected(response)){
                    return false;
                }
            }
            return true;
        }
        
        private static void PreprocessJSCodeBlock(string jsCodeBlock)
        {
            int lineNumber = 0;
            foreach (string line in jsCodeBlock.Split('\n'))
            {
                lineNumber++;
                if (HasUnclosedQuotes(line))
                {
                    WriteAndExit(
                        message:
                            $"BAM Manager (BAMM) encountered a validation error while parsing a javascript code block.\n" +
                            $"Line {lineNumber} contains an unescape quoted, please fix this and recompile.\n\n" +
                            $"Line:\n{line}",
                        status: 1
                    );
                }
            }
        }
        
        private static void ResetTranspilerState(string? customName)
        {
            desiredUrls.Clear();
            script.ResetInstanceState();
            noBrowsersFound = false;
            actionTimeout = 10;
            projectName = GetProjectName(customName);
            requestUserAgent = DEFAULT_USER_AGENT;
        }

        public static void SetBrowserStackStatus(bool status) { usingBrowserstack = status; }
        
        public static void SetCustomUserAgent(string[] args)
        {
            List<string> userAgentArgs = [.. args.Where(arg => arg.StartsWith("--set-custom-useragent=="))];
            if (userAgentArgs.Count > 1)
            {
                WriteAndExit(
                     $"BAM Manager (BAMM) encountered a fatal error: '--set-custom-useragent' can only be specified once.\n" +
                     $"Found multiple instances:\n\n" +
                     $"1.'{userAgentArgs[0]}'\n\n" +
                     $"2.'{userAgentArgs[1]}'\n\n." +
                     "Please remove duplicate arguments and restart.",
                     1);
            }

            if (userAgentArgs.Count == 0)
            {
                return;
            }

            else
            {
                string customUserAgent = userAgentArgs[0];
                Match match = CustomUserAgentRegex.Match(customUserAgent);

                if (match.Success)
                {
                    // Fixes formatting issues caused by passing a string as an argument via cli.
                    string newUserAgent = match.Groups[1].Value.Replace("%20", "");
                    if (Parser.IsValidUserAgentFormat(newUserAgent))
                    {
                        requestUserAgent = newUserAgent;
                        WriteSuccessMessage($"\nOverrode default UserAgent with:");
                        Warning.Write($"{newUserAgent}");
                        return;
                    }
                    WriteAndExit(
                        message:
                            "BAM Manager (BAMM) encountered a fatal error: " +
                            "Could not parse user agent string from the '--set-custom-useragent' argument.\n" +
                            "Valid syntax:\n--set-custom-useragent==" +
                            "\"Mozilla/5.0 (Linux; Android 5.1.1; SAMSUNG SM-G920M Build/LMY47X) AppleWebKit/535.22 (KHTML, like Gecko) Chrome/51.0.1871.243 Mobile Safari/535.7\"",
                        status: 1
                    );

                }
                WriteAndExit(
                    message:
                        $"BAM Manager encountered an error: Invalid format for '--set-custom-useragent' argument.\n\n" +
                        $"Expected Format: '--set-custom-useragent==\"UserAgentString\"",
                    status: 1
                );
            }
        }
        
        private static void SetBAMConfig(string filePath)
        {
            if (bamConfig != null)
            {
                bamConfig = null;
            }

            bamConfig = new BAMConfig(filePath);
            bamConfig.CheckConfigLines();
        }
        
        private static void SetScriptName(string filePath)
        {
            string failureMessage =
                $"BAM Manager (BAMM) was unable to access:{NLC}{NLC}{filePath}{NLC}{NLC}" +
                $"Please ensure this file was not deleted, and is not in use by any other program.{NLC}{NLC}" +
                "Press any key to exit...";

            try
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName == null)
                {
                    WriteAndExit(
                        message: failureMessage,
                        status: 1
                    );
                }

                if (!File.Exists(filePath))
                {
                    failureMessage =
                        $"BAM Manager (BAMM) was unable to access:\n\n{fileName}{NLC}{NLC}" +
                        $"Please ensure this file was not deleted, and is not in use by any other program.{NLC}{NLC}" +
                        "Press any key to exit...";

                    WriteAndExit(
                        message: failureMessage,
                        status: 1
                    );
                }

                try
                {
                    pythonScriptFileName = fileName.Split(".")[0] + ".py";
                }
                catch
                {
                    GenerateBackupScriptName();
                }
            }
            catch (Exception)
            {
                WriteAndExit(
                    message: failureMessage,
                    status: 1
                );
            }
        }
        
        public static void SetTimeout(string[] args)
        {
            List<string> timeoutArgs = [.. args.Where(arg => arg.StartsWith("--set-timeout=="))];

            if (timeoutArgs.Count > 1)
            {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) encountered a fatal error: '--set-timeout' can only be specified once.\n" +
                        $"Found multiple instances:\n\n" +
                        $"1.'{timeoutArgs[0]}'\n\n" +
                        $"2.'{timeoutArgs[1]}'\n\n." +
                        "Please remove duplicate arguments and restart.",
                    status: 1
                );
            }
            if (timeoutArgs.Count == 0) { return; }
            else
            {
                string timeoutArg = timeoutArgs[0];
                Match match = ActionTimeoutRegex.Match(timeoutArg);

                if (!match.Success)
                {
                    // Case for when the argument starts with --set-timeout==
                    // but doesn't match the expected format
                    // (For example '--set-timeout==X')
                    WriteAndExit(
                        message:
                            $"BAM Manager encountered an error: Invalid format for '--set-timeout' argument.\n\n" +
                            $"Expected Format: '--set-timeout==integer'" +
                            $"Received: '{timeoutArg}'",
                        status: 1
                    );
                }

                string valueString = match.Groups[1].Value;
                bool valueParsed = int.TryParse(valueString, out int parsedTimeout);

                if (!valueParsed || parsedTimeout <= 0)
                {
                    WriteAndExit(
                        message:
                            "BAM Manager (BAMM) encountered a a fatal error: " +
                            "Could not parse integer value from '--set-timeout' argument.\n",
                        status: 1
                    );
                }
                actionTimeout = parsedTimeout;
                WriteSuccessMessage(
                    $"Timeout set to {actionTimeout} seconds ({actionTimeout * 1000}ms)"
                );

            }
        }
        
        public static void SuppressUnneededWarnings()
        {
            // This function inserts the required code in reverse order so the output is consistent with whats desired.
            var statements = new Dictionary<string, int>()
            {
                { "filterwarnings('ignore', message='.*pkg_resources is deprecated.*')", 0 },
                { "from warnings import filterwarnings", 0 },
                { "# Disables known warnings that aren't needed.", 0 },
            };

            script.Imports.AddStatements(statements);

        }
        
        public static void WriteRequirementsFile()
        {
            try
            {
                string filePath = Path.Combine(
                    desiredSaveDirectory,
                    projectName,
                    requirementsFileName
                );
                
                using StreamWriter writer = new(
                    path: filePath,
                    append: false,
                    encoding: new UTF8Encoding(false)
                );

                foreach (string requirement in script.Requirements.packageList)
                    writer.WriteLine(requirement);

            }
            catch (Exception e)
            {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable write requirements.txt for '{pythonScriptFileName}'.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nUnhandled exception, if you're reading this, please make a bug report, " +
                        $"clearly there's a huge issue.\n\nInterpreter Response:\n{e.Message}",
                    status: 1
                );
            }
        }
        
        public static void WritePythonFile()
        {
            try
            {

                var importsCount = script.Imports.statementList.Count;
                var bodyLineCount = script.Body.scriptLines.Count;

                // Removing Byte Order Mark (BOM)
                var sanitizedImportStatements = script.Imports.statementList.Select(line => line.TrimStart('\uFEFF'));

                // Removing Byte Order Mark (BOM)
                var sanitizedScriptBody = script.Body.scriptLines.Select(line => line.TrimStart('\uFEFF'));

                string filePath = Path.Combine(
                    desiredSaveDirectory,
                    projectName,
                    pythonScriptFileName
                );

                using StreamWriter writer = new(
                    path: filePath,
                    append: false,
                    encoding: new UTF8Encoding(false)
                );

                foreach (string importStatement in sanitizedImportStatements) {
                    writer.WriteLine(importStatement);
                }

                if (importsCount > 0 && bodyLineCount > 0) {
                    writer.WriteLine();
                }

                foreach (string scriptLine in script.Body.scriptLines) {
                    writer.WriteLine(scriptLine);
                }
            }
            catch (Exception e)
            {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable write '{pythonScriptFileName}' for the desired script.\n\n" +
                        $"If this continues, please make a bug report at {ISSUES_LINK}\n\n" +
                        $"Error log:\nUnhandled exception, if you're reading this, please make a bug report, " +
                        $"clearly there's a huge issue.\n\nInterpreter Response:\n{e.Message}",
                    status: 1
                );
            }
        }

    }
}