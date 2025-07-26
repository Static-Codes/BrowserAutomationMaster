
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Net;
using System.Net.NetworkInformation;
using BrowserAutomationMaster.Checks;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager;
using BrowserAutomationMaster.Parsing;

namespace BrowserAutomationMaster.Compilation
{

    public partial class Transpiler
    {
        // This will be used in GenerateBackupName(); in the case of failure.
        readonly static string defaultScriptFileName = "untitled-script";  
        
        static string desiredSaveDirectory = "";
        static string projectDirectoryName = DateTime.Now.ToString("MM-dd-yyyy_h-mm-tt");
        readonly static string requirementsFileName = "requirements.txt"; 
        static string projectDirectory = "";
        

        static string pythonScriptFileName = "";  // Modified by SetScriptName();
        static string pythonVersion = "3.10";

        // Default value if inhouse function fails.
        private static string requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"; 
        
        private static string selectedBrowser = "firefox"; // Defaults to firefox.  Accepts 'chrome' and 'firefox'

        private readonly static string[] browserlessActions = ["save-as-html", "wait-for-seconds"];

        // Not to be confused with noBrowsersFound, this is a flag only for the command 'browser'
        private static bool browserPresent = false;
        private static bool featurePresent = false;
        private static bool otherPresent = false;


        // Disables Visual Studio Code from writing __pycache__ directory.
        private static bool disablePycache = false;

        // Disables SSL certificate authorization session wide.
        private static bool disableSSL = false;

        // Runs the browser in headless mode if specified.
        private static bool runHeadless = false;

        // Not to be confused with browserPresent, this is a flag that will be set true if no valid browser installations are found.
        static bool noBrowsersFound = false;

        // This is the timeout applied to all WebDriverWait calls.
        static int actionTimeout = 10;

        readonly static Dictionary<string, int> desiredUrls = []; // KeyValuePair<url, lineNumber>
        static List<string> configLines = []; // Fix logic and make static Dictionary<int, string> configLines = [];
        static List<string> featureLines = []; // Fix logic and make static Dictionary<int, string> configLines = [];
        readonly static List<string> importStatements = [
            "from importlib import import_module", 
            "from subprocess import run", 
            "from sys import modules, stderr, stdout\n",
        ];
        readonly static List<string> scriptBody = [];
        readonly static List<string> requirements = [];

        // Used for --set-timeout==5 (or any desired timeout)
        private static readonly Regex ActionTimeoutRegex = TimeoutRegex();
        [GeneratedRegex(@"^--set-timeout==(\d+)$", RegexOptions.Compiled)]
        private static partial Regex TimeoutRegex();

        // Used for --set-custom-useragent=="user-agent-string-here"
        private static readonly Regex CustomUserAgentRegex = CLIUserAgentRegex(); 
        [GeneratedRegex(@"^--set-custom-useragent==(.+?)$", RegexOptions.Compiled)]
        private static partial Regex CLIUserAgentRegex();
        
        
        public static void New(string filePath, string[] args)
        {
            SetDesiredSaveDirectory();
            CreateProjectDirectory(); // Also sets variable projectDirectory
            SetScriptName(filePath);
            SetFileLines(filePath);
            GetDesiredUrls();

            Installations ___ = new(InstalledApps.GetInstalledApps()); // was originally named installations
            AddBrowserImportsAndRequirements();
            //HandlePythonVersionSelection(installations); // This isn't needed currently 

            HandleCompilation(filePath, args);
            WritePythonFile();
            WriteRequirementsFile();

            Success.WriteSuccessMessage($"\nCompiled -> {pythonScriptFileName}");
            Success.WriteSuccessMessage($"Location -> {projectDirectory}\n");
            ResetTranspilerState();
        }
        public static void AddBrowserImportsAndRequirements()
        {
            HandleBrowserCmd();
            requirements.Add("setuptools==80.9.0");

            // This function will exit if a null value is reached so no worries about a null check here
            string version = PackageManager.New("selenium", pythonVersion);
            requirements.Add($"selenium=={version}");

            string noUrlsFound =
                "BAM Manager (BAMM) was unable to find any 'visit' commands in the provided file.\n\n" +
                "Please ensure the selected file has atleast one 'visit' command.";

            if (desiredUrls.Count == 0)
            {
                Errors.WriteErrorAndExit(noUrlsFound, 1);
                return;
            }

            string swVersion = PackageManager.New("selenium-wire", pythonVersion);
            string wmVersion = PackageManager.New("webdriver_manager", pythonVersion);
            requirements.Add($"selenium-wire=={swVersion}");
            requirements.Add($"webdriver_manager=={wmVersion}");

            // This fixes the mess that selenium-wire causes by installing blinker >=1.9
            requirements.Add($"blinker==1.4");

            importStatements.AddRange([
                "from selenium.common.exceptions import NoSuchElementException",
                "from selenium.webdriver.common.by import By",
                "from selenium.webdriver.support.ui import Select, WebDriverWait",
                "from selenium.webdriver.support import expected_conditions as EC",
                "from seleniumwire import webdriver"
                ]
            );

            if (selectedBrowser.Equals("brave", StringComparison.OrdinalIgnoreCase))
            {
                importStatements.AddRange(["from selenium.webdriver.chrome.options import Options",
                                           "from selenium.webdriver.chrome.service import Service as ChromeService",
                                           "from webdriver_manager.chrome import ChromeDriverManager",
                                           "from webdriver_manager.core.os_manager import ChromeType"]);
            }

            else if (selectedBrowser.Equals("chrome", StringComparison.OrdinalIgnoreCase)){
                importStatements.AddRange(["from selenium.webdriver.chrome.options import Options",
                                           "from selenium.webdriver.chrome.service import Service as ChromeService",
                                           "from webdriver_manager.chrome import ChromeDriverManager"]);
            }
            else if (selectedBrowser.Equals("firefox", StringComparison.OrdinalIgnoreCase))
            {
                importStatements.AddRange(["from selenium.webdriver.firefox.options import Options",
                                           "from selenium.webdriver.firefox.service import Service as FirefoxService",
                                           "from webdriver_manager.firefox import GeckoDriverManager"]);
            }
            else { 
                throw new Exception(
                    "Invalid browser provided to 'browser' command.\n" +
                    "Expected: \"chrome\" or \"firefox\""); 
            }
        }
        public static void AddImportIfNotPresent(string import, bool addToReqs = false, string? reqText = null)
        {
            bool validStatement = import.StartsWith("from") || import.StartsWith("import");
            
            if (!validStatement) {
                Errors.WriteErrorAndExit(
                    message: $"Invalid import statement: {import}.",
                    status: 1
                );
            }

            //bool validRequirement = addToReqs && !string.IsNullOrEmpty(reqText);


            if (addToReqs && !string.IsNullOrEmpty(reqText)) {
                Errors.WriteErrorAndExit(
                    message: $"Invalid requirement statement: {reqText}.",
                    status: 1
                );
            }

            if (!importStatements.Contains(import)) {
                importStatements.Add(import);
            }
            if (addToReqs) {
                requirements.Add(reqText!);
            }

        }
        public static void AddWatermark()
        {
            AddImportIfNotPresent(import: "from time import sleep", addToReqs: false, reqText: null);

            scriptBody.Insert(0,
                "stdout.write('''Made using BAM Manager (BAMM!)\n" +
                $"{ConstantManager.BASE_REPO_LINK}\n''')\n" +
                $"sleep(3)\n\n"
            );
        }
        public static void AddRequiredFunctions()
        {
            Dictionary<string, bool> functionsPresent = [];

            // Checks if configLines contains each arg, if so the required function is be added.
            // add-header is added here since its in actionArg, but its not accessed in this function.
            foreach (string actionArg in Parser.actionArgs) {
                functionsPresent.Add(actionArg, configLines.Any(line => line.StartsWith(actionArg))); 
            }
            
            int index = 1; // Accounts for the functions below in the scriptBody.
            scriptBody.Insert(0, BrowserFunctions.makeRequestFunction(requestUserAgent));

            // Starts at line 4 (index 3) to account for imports required by check_imports
            importStatements.Insert(3, BrowserFunctions.checkImportFunction);
            importStatements.Insert(4, BrowserFunctions.installPackagesFunction);
            importStatements.Insert(5, "install_packages()");

            Action Add(string func) => () => scriptBody.Insert(index, func);
            Dictionary<string, Action> functionsAndActions = new() {
                { "click", Add(BrowserFunctions.clickElementFunction)                              },
                { "click-at-position", Add(BrowserFunctions.clickAtPositionFunction)               },
                { "click-exp", () => Add(BrowserFunctions.clickElementExperimentalFunction)        },
                { "close-current-tab", Add(BrowserFunctions.closeCurrentTabFunction)               },
                { "fill-text", Add(BrowserFunctions.fillTextFunction)                              },
                { "fill-text-exp", Add(BrowserFunctions.fillTextExperimentalFunction)              },
                { "get-text", Add(BrowserFunctions.getTextFunction)                                },
                { "open-new-tab", Add(BrowserFunctions.openNewTabFunction)                         },
                { "save-as-html", Add(BrowserFunctions.saveAsHTMLFunction)                         },
                { "save-as-html-exp", Add(BrowserFunctions.saveAsHTMLExperimentalFunction)         },
                { "select-option", Add(BrowserFunctions.selectOptionByIndexFunction)               },
                { "take-screenshot", Add(BrowserFunctions.takeScreenshotFunction)                  }
            };

            foreach (var functionPair in functionsAndActions) {
                // Presence check
                if (functionsPresent.TryGetValue(functionPair.Key, out bool isNeeded) && isNeeded) {

                    bool wasFound =  functionsAndActions.TryGetValue(
                        functionPair.Key, 
                        out Action? actionToPerform
                    );

                    if (wasFound && actionToPerform != null) { 
                        actionToPerform(); 
                        index++; 
                    }
                }
            }
            if (scriptBody.Count != index) { 
                scriptBody.Insert(scriptBody.Count, BrowserFunctions.browserQuitCode); 
            }
            else { 
                Add(BrowserFunctions.browserQuitCode); 
            }
        }
        public static void CheckConfigLines()
        {
            int numberOfLines = configLines.Count;
            if (numberOfLines == 0) {
                Errors.WriteErrorAndExit(
                    message:
                        "BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\n" +
                        "Press any key to exit...", 
                    status: 1
                );
            }

            if (numberOfLines >= 1 && 
                configLines[0].StartsWith("browser") && 
                configLines[0].Contains(' ') && 
                configLines[0].Split(' ').Length == 2) 
                { 
                    browserPresent = true; 
                }

            if (browserPresent) { 
                selectedBrowser = configLines[0].Split(' ')[1].Replace('"', ' ').Trim(); 
            }

            featureLines = [.. 
                configLines
                    .Select(line => line.Trim())
                    .Where(line => 
                        !string.IsNullOrWhiteSpace(line) 
                        && line.StartsWith("feature")
                    )
            ];
            featurePresent = featureLines.Count > 0;

            disablePycache = featurePresent && featureLines.Any(line => line.Contains(" \"disable-pycache\""));
            disableSSL = featurePresent && featureLines.Any(line => line.Contains(" \"disable-ssl\""));
            runHeadless = featurePresent && featureLines.Any(line => line.StartsWith("feature \"run-headless\""));

            otherPresent = CheckOtherPresent();

            if (!otherPresent) { 
                Warning.Write(
                    message:
                        "BAM Manager (BAMM) was unable to find any requests logic, " +
                        "if this is intentional, you can safely ignore this warning."
                ); 
            }

            if (disablePycache) { 
                importStatements.AddRange(["import sys", "sys.dont_write_byte_code"]); 
            }

            if (disableSSL && configLines[0].Contains("\"chrome\"")) { 
                importStatements.Add("from selenium.webdriver.chrome.options import Options"); 
            }

            else if (disableSSL && configLines[0].Contains("\"firefox\"")) { 
                importStatements.Add("from selenium.webdriver.firefox.options import Options"); 
            }


        }
        public static bool CheckOtherPresent()
        {
            
            if (configLines.Count == 0) { return false; }
            foreach (string line in configLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                string trimmedLine = line.Trim();
                string firstArg;
                int spaceCharIndex = trimmedLine.IndexOf(' ');
                if (spaceCharIndex == -1) { firstArg = trimmedLine; }
                else { firstArg = trimmedLine[..spaceCharIndex]; }
                if (Parser.actionArgs.Contains(firstArg)) {  return true; }
            }
            return false;
        }
        public static void CreateProjectDirectory()
        {
            try {
                if (!Directory.Exists(desiredSaveDirectory)) { Directory.CreateDirectory(desiredSaveDirectory); }
            }
            catch { 
                Errors.WriteErrorAndExit(
                    message:
                        "BAMM Manager (BAMM) was unable to create the desired project directory, please try again.", 
                    status: 1
                ); 
            }
            
            projectDirectory = Path.Combine(desiredSaveDirectory, projectDirectoryName);
            try {
                if (!Path.Exists(desiredSaveDirectory)) {
                    Directory.CreateDirectory(desiredSaveDirectory);
                }
            }
            catch { }

            try {
                if (!Path.Exists(projectDirectory)) {
                    Directory.CreateDirectory(projectDirectory);
                }
            }
            catch { 
                Errors.WriteErrorAndExit(
                    message:
                        "BAMM Manager (BAMM) was unable to create the desired project directory, " +
                        "please try again.", 
                    status: 1
                ); 
            }
        }
        public static void GenerateBackupScriptName()
        {
            string potentialFileName = $"{defaultScriptFileName}.py";          
            int index = 2;
            while (true)
            {
                if (!File.Exists(potentialFileName)) {
                    pythonScriptFileName = potentialFileName;
                }
                potentialFileName = $"{defaultScriptFileName}({index}).py";
                index++;
            }
        }
        public static void GetDesiredUrls()
        {
            int lineNumber = 1;
            foreach (string line in configLines)
            {
                string[] args = line.Split(' ') ?? [];
                if (args.Length == 2 && line.Contains("visit")){
                    string sanitizedArg = args[1].Replace('"', ' ').Trim();
                    desiredUrls.TryAdd(sanitizedArg, lineNumber);
                }
                lineNumber++;
            }
        }
        public static void HandleBrowserCmd()
        {
            // GetUserAgent will exit in the event an invalid browserName is passed, thus the use of !
            if (browserPresent) { requestUserAgent = UserAgentManager.GetUserAgent(selectedBrowser)!; }
        }
        public static void HandleCompilation(string fileName, string[] args) 
        {
            SetCustomUserAgent(args);
            SetTimeout(args);
            
            
            int lineNumber = 1;
            bool hasComment = false;

            // Prevents duplicate entries of BrowserFunctions.makeRequestFunction();
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
            foreach (string originalLine in configLines)
            {
                // Since iterators can't be overwritten, storing it as a local variable is current solution.
                string line = originalLine;
                if (string.IsNullOrEmpty(line)) { continue; } // Skip blank lines.

                // Indicates a comment is present (ignores comments within JS blocks)
                if (line.Contains(" // ") && !isJSBlock) { hasComment = true; }

                // Deletes said comment so it's not compiled.
                if (hasComment) { line = Parser.DeleteCommentIfPresent(line); } 
                
                // Handling 'add-headers' before 'visit' is processed would be an issue if it weren't for Parser
                // Parser ensures 'browser' first (or defaults to firefox) then features and finally any other logic.
                Match match = Parser.PrecompiledHeaderRegex().Match(line);
                if (match.Success) {

                    string requestLine = 
                        scriptBody.Where(line => line.Equals("make_request(url)")
                    ).First() ?? string.Empty;

                    if (string.IsNullOrEmpty(requestLine)) { 
                        Errors.WriteErrorAndExit(
                            message:
                                "Unable to locate request logic in partially compiled script, " +
                                "please attempt recompilation.", 
                            status: 1
                        ); 
                    }
                    int index = scriptBody.IndexOf(requestLine);
                    if (index == -1) { Errors.WriteErrorAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to locate request logic in partially compiled script, " +
                            "please attempt recompilation.", 
                        status: 1
                    ); }
                    // Value is assumed to be correct,
                    // but will very much cause an issue if the regex is found to not be fully reliable.
                    else {  
                        scriptBody.Insert(
                            index - 1, 
                            BrowserFunctions.addHeadersFunction(
                                JsonSerializer.Deserialize<Dictionary<string, string>>(match.Groups["json"].Value)!
                            )
                        );
                    } 
                   
                    continue;
                }


                if (line.StartsWith("click-exp ")) { isCE = true; }
                else if (line.StartsWith("fill-text")) { isFT = true; } // Also handles fill-text-exp
                else if (line.StartsWith("set-custom-useragent")) { isCU = true; }
                else if (line.StartsWith("start-javascript")) { isJSBlock = true; continue; }
                else if (line.StartsWith("end-javascript")) { isJSBlock = false; }


                string[] splitLine;
                // This handles fill-text or set-custom-useragent
                if (isFT || isCU) { splitLine = line.Split(" \""); }
                // This handles all but click-exp, fill-text, and set-custom-user-agent
                else if (!isCE) { splitLine = line.Split(" "); }
                // This handles click-exp
                else { splitLine = line.Split(" '"); }
                // Prevents the length check below from returning an error for javascript code blocks.
                if (isJSBlock) { isJSLine = true;} 


                int[] validLengths = [2, 3];
                // These are special because they require no parsing.
                // excludes start-javascript + end-javascript theyre handled below.
                string[] specialCommands = ["close-current-tab"]; 

                bool normalLengthBypass = !validLengths.Contains(splitLine.Length) && !isJSLine;
                bool specialLengthBypass = specialCommands.Any(cmd => line.Replace('"', ' ').Trim().StartsWith(cmd));
                
                if (normalLengthBypass) {
                    if (specialLengthBypass) { continue; }
                    Errors.WriteErrorAndExit(
                        message:
                            Errors.GenerateErrorMessage(
                                fileName, 
                                line, 
                                lineNumber, 
                                "Invalid command syntax."
                            ), 
                        status: 1
                    );
                }

                // Handle case where user attempts to create another jsBlock before closing the previous one.
                if (isJSBlock && line.StartsWith("start-javascript")) {
                    Errors.WriteErrorAndExit(
                        message: 
                            Errors.GenerateErrorMessage(
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
                else if (isJSBlock) { 
                    jsBlockContent += $"{line}\n";
                    continue;
                }

                // Writes the actual JS Block as python code.
                if (line.StartsWith("end-javascript") && !isJSBlock) {
                    // Handles cases where Esprima might be more lenient towards invalid code.
                    PreprocessJSCodeBlock(jsBlockContent);
                    if (!JavaScript.IsValidSyntax(jsBlockContent, out string? error)) {
                        Errors.WriteErrorAndExit(
                            message:
                                Errors.GenerateErrorMessage(
                                    fileName, 
                                    line, 
                                    lineNumber + 1, 
                                    $"Invalid javascript code block:\n\nParser Error:\n\n" +
                                    $"{error}"), 
                            status: 1
                        );
                    }
                    scriptBody.Add($"driver.execute_script('''{jsBlockContent}''')\n");
                    jsBlockContent = string.Empty;
                    isJSLine = false;
                    continue;
                }

                string firstArg = splitLine.First();
                bool canRunBrowserless = browserlessActions.Any(action => action.StartsWith(firstArg));
                if (!canRunBrowserless) {
                    if (noBrowsersFound) {
                        //Errors.WriteErrorAndExit(
                            //Errors.GenerateErrorMessage(
                                //fileName,
                                //line,
                                //lineNumber,
                                //"No valid browser installations found,
                                //please install brave, chrome, or firefox."
                            // ),
                            // status: 1
                        //);

                        Errors.WriteErrorAndExit(
                            message:
                                Errors.GenerateErrorMessage(
                                    fileName, 
                                    line, 
                                    lineNumber, 
                                    "No valid browser installations found, please install chrome or firefox."
                                ), 
                                status: 1
                        );
                    }
                }
                string sanitizedArg2;
                if (!isCE) { sanitizedArg2 = splitLine[1].Replace('"', ' ').Trim(); }
                else { sanitizedArg2 = splitLine[1].Replace('\'', ' ').Replace('"', ' ').Trim(); }
                string sanitizedArg3 = string.Empty;

                // The parser ensures no invalid lines can be provided to the compiler :)
                if (splitLine.Length >= 3) { sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim(); }

                switch (firstArg)
                {
                    case "add-header":
                        CompilationHandler.AddHeader(scriptBody, sanitizedArg2, sanitizedArg3);
                        break;

                    case "click" when CompilationHandler.Click(
                          scriptBody, 
                          splitLine, 
                          actionTimeout
                        ) is false:
                        string issueText = $"Unable to parse selector: {splitLine[1]}\n" +
                                           $"If this is a CSS Selector, please use:\n" +
                                           $"click-exp '{sanitizedArg2}'";
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, issueText),
                          status: 1
                        );
                        break;

                    case "click-at-position" when CompilationHandler.ClickAtPosition(
                          scriptBody,
                          splitLine,
                          sanitizedArg2,
                          sanitizedArg3,
                          actionTimeout
                        ) is (false, var eText):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, issueText: eText),
                          status: 1
                        );
                        break;

                    case "click-exp" when CompilationHandler.ClickExp(
                          scriptBody,
                          splitLine,
                          sanitizedArg2,
                          actionTimeout,
                          ref isCE
                        ) is (false, var err):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, err),
                          status: 1
                        );
                        break;

                    case "close-current-tab":
                        CompilationHandler.CloseCurrentTab(scriptBody);
                        break;

                    case "get-text" when CompilationHandler.GetText(
                         scriptBody, 
                         splitLine
                        ) is (false, var e):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, e),
                          status: 1
                        );
                        break;

                    case "fill-text" when CompilationHandler.FillText(
                        scriptBody,
                        splitLine,
                        sanitizedArg2,
                        ref isFT
                        ) is (false, var issue):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, issue),
                          status: 1
                        );
                        break;

                    case "fill-text-exp" when CompilationHandler.FillTextExp(
                        scriptBody, 
                        importStatements, 
                        splitLine, 
                        sanitizedArg2, 
                        ref isFT
                        ) is (false, var exceptionText):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, exceptionText),
                          status: 1
                        );
                        break;

                    case "open-new-tab" when CompilationHandler.OpenNewTab(
                        scriptBody,
                        sanitizedArg2,
                        sanitizedArg3
                        ) is (false, var errorText):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, errorText),
                          status: 1
                        );
                        break;

                    case "save-as-html":
                        CompilationHandler.SaveAsHTML(scriptBody, sanitizedArg2);
                        break;

                    case "save-as-html-exp":
                        CompilationHandler.SaveAsHTMLExp(scriptBody, sanitizedArg2);
                        break;
                        
                    case "select-element" when CompilationHandler.SelectElement(
                        scriptBody,
                        splitLine,
                        actionTimeout
                        ) is (false, var errMsg):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, errMsg),
                          status: 1
                        );
                        break;

                    case "select-option" when CompilationHandler.SelectOption(
                        scriptBody,
                        sanitizedArg2,
                        sanitizedArg3,
                        actionTimeout
                        ) is (false, var errorMsg):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, errorMsg),
                          status: 1
                        );
                        break;

                    case "set-custom-useragent":
                        CompilationHandler.SetCustomUserAgent(splitLine, lineNumber, ref requestUserAgent, ref isCU);
                        break;

                    case "take-screenshot":
                        CompilationHandler.TakeScreenshot(scriptBody, sanitizedArg2);
                        break;

                    case "visit" when CompilationHandler.Visit(
                        scriptBody,
                        featureLines, 
                        sanitizedArg2,
                        selectedBrowser,
                        firstVisitFinished,
                        disableSSL,
                        runHeadless) is (false, var eMessage):
                        Errors.WriteErrorAndExit(
                          message: Errors.GenerateErrorMessage(fileName, line, lineNumber, eMessage),
                          status: 1
                        );
                        break;

                    case "wait-for-seconds" when CompilationHandler.WaitForSeconds(
                        scriptBody,
                        splitLine,
                        sanitizedArg2) is (false, var errMessage):
                        Errors.WriteErrorAndExit(
                            message: Errors.GenerateErrorMessage(fileName, line, lineNumber, errMessage),
                            status: 1
                        );
                        break;
                }
                lineNumber++;
            }

            AddRequiredFunctions();
            SuppressUnneededWarnings();
            AddWatermark(); // Single comment watermark, completely nonintrusive and easily removable
        }
        public static void HandlePythonVersionSelection(Installations installations)
        {
            List<string> foundVersions = [];
            Dictionary<ApplicationNames, string> versionMapping = new() {
                {ApplicationNames.Python3_9, "3.9" },
                {ApplicationNames.Python3_10, "3.10" },
                {ApplicationNames.Python3_11, "3.11" },
                {ApplicationNames.Python3_12, "3.12" },
                {ApplicationNames.Python3_13, "3.13" },
                {ApplicationNames.Python3_14, "3.14" },
            };
            string inputMessage = """
                Please select the number corresponding to the version of python to compile your BAMC file for:
            """;

            int iterationIndex = 0;
            foreach (ApplicationNames app in installations.AppNames){
                if (!versionMapping.TryGetValue(app, out string? version)){ continue; }
                foundVersions.Add(version);
                inputMessage += $"{iterationIndex}. - Python {version}\n";
                iterationIndex += 1;
            }
            while (true){
                Spectre.Console.AnsiConsole.Write(inputMessage);
                string? inputResponse = Console.ReadLine();
                if (int.TryParse(inputResponse, out int selection) && selection > 0 && selection <= iterationIndex)
                {
                    int elementIndex = selection - 1;
                    string value = foundVersions.ElementAt(elementIndex);
                    if (IsValidPyVersion(value))
                    {
                        pythonVersion = value;
                        break;
                    }
                }
            }
        } // Currently unused.
        public static bool HasUnclosedQuotes(string line)
        {
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool isEscaped = false; // True if the previous character was a backslash
            foreach (char c in line.Trim())
            {
                if (isEscaped) { 
                    // If this flag is hit the previous character was a backslash, indicating this character is escaped and should be ignored.
                    isEscaped = false;
                    continue;
                }
                if (c == '\\') {
                    // If this flag is hit it indicates the current character is a backslash and the next character will be escaped & ignored.
                    isEscaped = true;
                    continue;
                }
                if (c == '\'') {
                    // If a single quote is inside a set of double quotes, the single quote is a literal character (most likely an apostrophe)
                    if (!inDoubleQuote) {
                        // A single quote is only a delimiter if it's not inside a set of double quotes.
                        inSingleQuote = !inSingleQuote;
                    }
                }
                else if (c == '"') {
                    // If a quote quote is inside a set of single quotes, the double quote is a literal character.
                    if (!inSingleQuote) {
                        // A double quote is only a delimiter if it's not inside a set of single quotes.
                        inDoubleQuote = !inDoubleQuote;
                    }
                }
            }

            // If either flag is true at the end, a quote was left unclosed
            return inSingleQuote || inDoubleQuote;
        }
        public static string Indent(int numberOfIndents) { 
            if (numberOfIndents < 0) { 
                Errors.WriteErrorAndExit(
                    message: "Invalid value provided to Indent(), value must be >= 0.", 
                    status: 1
                ); 
            }
            if (numberOfIndents == 0) { return string.Empty; } // Return an empty string if no indentations are needed.



            string pythonIndent = "    "; // PEP 8 standard (4 spaces = 1 tab)
            return string.Concat(
                Enumerable.Repeat(pythonIndent, numberOfIndents)
            );
        }
        public static bool IsValidPyVersion(string pyVersion)
        {
            if (string.IsNullOrWhiteSpace(pyVersion)) { return false; }
            
            string[] parts = pyVersion.Split('.');
            if (parts.Length != 2) { return false; }

            bool majorFound = int.TryParse(parts[0], out int major);
            bool minorFound = int.TryParse(parts[1], out int minor);
            
            if (!majorFound || !minorFound) { return false; }

            bool isValidVersion = 
                major == 3 && 
                minor >= 9 && 
                minor <= 14;

            return isValidVersion;
        }
        public static bool IsResolvableLink(string link)
        {
            try
            {
                bool isValidUri = Uri.TryCreate(link, UriKind.Absolute, out Uri? uriResult);
                if (!isValidUri) {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to resolve: '{link}'\n\n" +
                            $"Error log:\nUnable to create Uri object from provided link, returned a false boolean.",
                        status: 1
                    );
                    return false;
                }
                if (uriResult == null) {
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager (BAMM) was unable to resolve: '{link}'\n\n" +
                            $"Error log:\nUnable to create Uri object from provided link, returned a null result.",
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

                if (response.StatusCode is HttpStatusCode.Forbidden
                                        or HttpStatusCode.Locked
                                        or HttpStatusCode.MovedPermanently
                                        or HttpStatusCode.Unauthorized)
                {
                    return true;
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Errors.WriteErrorAndContinue(
                    message:
                        $"BAM Manager (BAMM) was unable to resolve the url: '{link}'"
                );

                string exceptionMessage = ex.InnerException?.Message ?? "";

                bool isExpectedErrType = ex.GetType() == typeof(PingException);
                bool errPresent = isExpectedErrType && exceptionMessage.StartsWith("No such host is known");
                if (errPresent) {
                    Warning.Write(
                        message:
                            $"It is possible the website you are requesting is unable or incorrectly entered.\n\n" +
                            $"Exception:\n\n{ex.InnerException}"
                    );
                }
                string input = Input.WriteTextAndReturnRawInput("Would you like to continue compilation? [y/n]: ") ?? "n";
                if (!input.Trim().Equals("y", StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }
            return true;
        }
        public static void PreprocessJSCodeBlock(string jsCodeBlock)
        {
            int lineNumber = 0;
            foreach (string line in jsCodeBlock.Split('\n')) {
                lineNumber++;
                if (HasUnclosedQuotes(line)) {
                    Errors.WriteErrorAndExit(
                        message: 
                            $"BAM Manager (BAMM) encountered a validation error while parsing a javascript code block.\n" +
                            $"Line {lineNumber} contains an unescape quoted, please fix this and recompile.\n\n" +
                            $"Line:\n{line}", 
                        status: 1
                    );
                }
            }
        }
        public static void ResetTranspilerState()
        {
            desiredUrls.Clear();
            scriptBody.Clear();
            requirements.Clear();
            configLines.Clear();
            featureLines.Clear();
            browserPresent = false;
            featurePresent = false;
            otherPresent = false;
            disablePycache = false;
            noBrowsersFound = false;
            runHeadless = false;
            actionTimeout = 10;
            projectDirectoryName = DateTime.Now.ToString("MM-dd-yyyy_h-mm-tt");
            importStatements.Clear(); // Since its read only clearing it and reassigning the default values is the ideal solution.
            importStatements.AddRange([
                "from importlib import import_module", 
                "from subprocess import run", 
                "from sys import modules, stderr, stdout",
            ]);
            requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0";
        }
        public static void SetCustomUserAgent(string[] args)
        {
            List<string> userAgentArgs = [.. args.Where(arg => arg.StartsWith("--set-custom-useragent=="))];
            if (userAgentArgs.Count > 1)
            {
                Errors.WriteErrorAndExit(
                     $"BAM Manager (BAMM) encountered a fatal error: '--set-custom-useragent' can only be specified once.\n" +
                     $"Found multiple instances:\n\n" +
                     $"1.'{userAgentArgs[0]}'\n\n" +
                     $"2.'{userAgentArgs[1]}'\n\n." +
                     "Please remove duplicate arguments and restart.",
                     1);
            }
            if (userAgentArgs.Count == 0) { return; }
            else
            {
                string customUserAgent = userAgentArgs[0];
                Match match = CustomUserAgentRegex.Match(customUserAgent);

                if (match.Success) {
                    // Fixes formatting issues caused by passing a string as an argument via cli.
                    string newUserAgent = match.Groups[1].Value.Replace("%20", "");
                    if (Parser.IsValidUserAgentFormat(newUserAgent)) {
                        requestUserAgent = newUserAgent;
                        Success.WriteSuccessMessage($"\nOverrode default UserAgent with:");
                        Warning.Write($"{newUserAgent}");
                        return;
                    }
                    Errors.WriteErrorAndExit(
                        message: 
                            "BAM Manager (BAMM) encountered a fatal error: " +
                            "Could not parse user agent string from the '--set-custom-useragent' argument.\n" +
                            "Valid syntax:\n--set-custom-useragent==" +
                            "\"Mozilla/5.0 (Linux; Android 5.1.1; SAMSUNG SM-G920M Build/LMY47X) AppleWebKit/535.22 (KHTML, like Gecko) Chrome/51.0.1871.243 Mobile Safari/535.7\"", 
                        status: 1
                    );
                    
                }
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager encountered an error: Invalid format for '--set-custom-useragent' argument.\n\n" +
                        $"Expected Format: '--set-custom-useragent==\"UserAgentString\"",
                    status: 1
                );
            }
        }
        public static void SetDesiredSaveDirectory()
        {
            desiredSaveDirectory = DirectoryManager.GetDesiredSaveDirectory();
        }
        public static void SetFileLines(string filePath)
        {
            string fileNotFoundMessage = 
                $"BAM Manager (BAMM) was unable to find the file:\n\n{filePath}, " +
                $"please ensure this file exists, then restart BAMM.\n\n" +
                $"Press any key to exit...";

            if (!File.Exists(filePath)) { 
                Errors.WriteErrorAndExit(message: fileNotFoundMessage, status: 1); 
            }
            configLines = [.. 
                File.ReadAllLines(filePath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
            ];
            CheckConfigLines();
        }
        public static void SetScriptName(string filePath)
        {
            string failureMessage = 
                $"BAM Manager (BAMM) was unable to access:\n\n{filePath}\n\n" + 
                "Please ensure this file was not deleted, and is not in use by any other program.\n\n" +
                "Press any key to exit...";
           
            try
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName == null) { 
                    Errors.WriteErrorAndExit(
                        message: failureMessage, 
                        status: 1
                    ); 
                }

                if (!File.Exists(filePath))
                {
                    failureMessage = 
                        $"BAM Manager (BAMM) was unable to access:\n\n{fileName}\n\n" +
                        $"Please ensure this file was not deleted, and is not in use by any other program.\n\n" +
                        $"Press any key to exit...";

                    Errors.WriteErrorAndExit(
                        message: failureMessage, 
                        status: 1
                    );
                }

                try
                {
                    // I hate c#'s static compiler I already ensured fileName cannot be null
                    // yet I have to yell at the compiler using !
                    pythonScriptFileName = fileName!.Split(".")[0] + ".py"; 
                }
                catch {
                    GenerateBackupScriptName();
                }
            }
            catch (Exception) { 
                Errors.WriteErrorAndExit(
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
                Errors.WriteErrorAndExit(
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
                    Errors.WriteErrorAndExit(
                        message:
                            $"BAM Manager encountered an error: Invalid format for '--set-timeout' argument.\n\n" +
                            $"Expected Format: '--set-timeout==integer'" +
                            $"Received: '{timeoutArg}'",
                        status: 1
                    );
                }
                string valueString = match.Groups[1].Value;
                bool valueParsed = int.TryParse(valueString, out int parsedTimeout);

                if (!valueParsed || parsedTimeout <= 0) {
                    Errors.WriteErrorAndExit(
                        message:
                            "BAM Manager (BAMM) encountered a a fatal error: " +
                            "Could not parse integer value from '--set-timeout' argument.\n",    
                        status: 1
                    );
                }
                actionTimeout = parsedTimeout;
                Success.WriteSuccessMessage(
                    $"Timeout set to {actionTimeout} seconds ({actionTimeout * 1000}ms)"
                );
                
            }
        }
        public static void SuppressUnneededWarnings()
        {
            // This function inserts the required code in reverse order so the output is consistent with whats desired.
            importStatements.Insert(0, "filterwarnings('ignore', message='.*pkg_resources is deprecated.*')");
            importStatements.Insert(0, "from warnings import filterwarnings");
            importStatements.Insert(0, "# Disables known warnings that aren't needed.");
            
        }
        public static void WriteRequirementsFile()
        {
            try {
                string filePath = Path.Combine(
                    desiredSaveDirectory, 
                    projectDirectoryName, 
                    requirementsFileName
                );
                using StreamWriter writer = new(
                    path: filePath, 
                    append: false, 
                    encoding: new UTF8Encoding(false)
                );

                foreach (string requirement in requirements) { 
                    writer.WriteLine(requirement); 
                }
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable write requirements.txt for '{pythonScriptFileName}'.\n\n" +
                        $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error log:\nUnhandled exception, if you're reading this, please make a bug report, " +
                        $"clearly there's a huge issue.\n\nInterpreter Response:\n{e.Message}", 
                    status: 1
                );
            }
        }
        public static void WritePythonFile()
        {
            try {
                // Removing Byte Order Mark (BOM)
                var sanitizedImportStatements = importStatements.Select(line => line.TrimStart('\uFEFF'));
                
                // Removing Byte Order Mark (BOM)
                var sanitizedScriptBody = scriptBody.Select(line => line.TrimStart('\uFEFF'));

                string filePath = Path.Combine(
                    desiredSaveDirectory, 
                    projectDirectoryName, 
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

                if (importStatements.Count > 0 && scriptBody.Count > 0) { 
                    writer.WriteLine(); 
                }

                foreach (string scriptLine in scriptBody) { 
                    writer.WriteLine(scriptLine); 
                }
            }
            catch (Exception e)
            {
                Errors.WriteErrorAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable write '{pythonScriptFileName}' for the desired script.\n\n" +
                        $"If this continues, please make a bug report at {ConstantManager.ISSUES_LINK}\n\n" +
                        $"Error log:\nUnhandled exception, if you're reading this, please make a bug report, " +
                        $"clearly there's a huge issue.\n\nInterpreter Response:\n{e.Message}", 
                    status: 1
                );
            }
        }

    }
}

