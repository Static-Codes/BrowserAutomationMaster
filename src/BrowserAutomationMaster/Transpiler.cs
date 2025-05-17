using System.Text;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster
{
    enum BrowserPackage
    {
        aiohttp,
        selenium,
        tls_client
    }

    internal partial class Transpiler
    {
        readonly static string defaultScriptFileName = "untitled-script";  // This will be used in GenerateBackupName(); in the case of failure.
        readonly static string desiredSaveDirectory = "compiled";  // This is the directory all projects are compiled to
        readonly static string projectDirectoryName = DateTime.Now.ToString("MM-dd-yyyy_h-mm-tt");
        readonly static string requirementsFileName = "requirements.txt"; // This is the filename where the package requirements will be written to.
        
        readonly static string pythonIndent = "    "; // PEP 8 standard (4 spaces = 1 tab)

        static BrowserPackage browserPackage = BrowserPackage.selenium; // By default selenium is chosen, however aiohttp and tls-client as also possible options.

        static string pythonScriptFileName = "";  // Modified by SetScriptName();
        static string pythonVersion = "3.10";  
        private static string requestUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0"; // Default value if inhouse function fails.
        
        static string selectedBrowser = "firefox"; // Defaults to firefox.  Brave, Chrome, Firefox

        static bool browserPresent = false; // Not to be confused with noBrowsersFound, this is a flag only for the command 'browser'
        static bool featurePresent = false; // 
        static bool otherPresent = false; // This might not be needed.

        static bool asyncEnabled = false; // Parser ensures both async and bypassCloudflare cannot both be true in a valid file.
        static bool bypassCloudflare = false; // Instructs the parser to use tls-client with a client identifier of safari_ios_16.
        static bool disablePycache = false;  // Disables Visual Studio Code from writing __pycache__ directory.
        static bool noBrowsersFound = false; // Not to be confused with browserPresent, this is a flag that will be set true if no valid browser installations are found.

        static int actionTimeout = 5; // This is the timeout applied to all WebDriverWait calls.
        readonly static Dictionary<string, int> desiredUrls = []; // KeyValuePair<url, lineNumber>
        static List<ApplicationNames> installedBrowsers = []; // Modified by VerifyInstallations();
        static List<ApplicationNames> installedPyVersions = []; // Modified by VerifyInstallations();
        static List<string> configLines = []; // Fix logic and make static Dictionary<int, string> configLines = [];
        static List<string> featureLines = []; // Fix logic and make static Dictionary<int, string> configLines = [];
        readonly static List<string> importStatements = [];
        readonly static List<string> scriptBody = [];
        readonly static List<string> requirements = [];
        private static readonly Regex ActionTimeoutRegex = TimeoutRegex();

        [GeneratedRegex(@"^--set-timeout==(\d+)$", RegexOptions.Compiled)]
        private static partial Regex TimeoutRegex();
        public static void New(string filePath, string[] args)
        {
            CreateProjectDirectory();
            SetScriptName(filePath);
            SetFileLines(filePath);
            GetDesiredUrls();
            Installations installations = InstallationCheck.Run();
            VerifyInstallations(installations);
            AddBrowserImportsAndRequirements();


            // Works but currently not needed (since script generation isn't done)
            HandleCompilation(filePath, args);
            WritePythonFile();
            WriteRequirementsFile();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Compiled -> {pythonScriptFileName}");

        }

        public static void AddBrowserImportsAndRequirements() // Check for proxy and add logic to insert proxy into session/driver variable.
        {
            HandleBrowserCmd();

            // This function will exit if a null value is reached so no worries about a null check here
            string version = PackageManager.New(browserPackage.ToString(), pythonVersion);
            requirements.Add($"{browserPackage}=={version}");

            string braveNotFound = $"""
            BAM Manager (BAMM) was unable to find an installation of Brave.
            
            Please ensure Brave is installed at the following location:
            
            {InstallationCheck.FirefoxPath}
            """;

            string chromeNotFound = $"""
            BAM Manager (BAMM) was unable to find an installation of Chrome.
            
            Please ensure Chrome is installed at the following location.
            
            {InstallationCheck.FirefoxPath}
            """;

            string firefoxNotFound = $"""
            BAM Manager (BAMM) was unable to find an installation of Firefox.
            
            Please ensure Firefox is installed at the following location.
            
            {InstallationCheck.FirefoxPath}
            """;

            string noUrlsFound = "BAM Manager (BAMM) was unable to find any 'visit' commands in the provided file.\n\nPlease ensure the selected file has atleast one 'visit' command.";



            if (desiredUrls.Count == 0) { Errors.WriteErrorAndExit(noUrlsFound, 1); return; }
            switch (browserPackage)
            {
                case BrowserPackage.aiohttp:
                    //importStatements.Add("from aiohttp import ClientSession");

                    //scriptBody.Add("async def main():");
                    //scriptBody.Add($"{Indent(1)}async with ClientSession() as session:");

                    //// Define url variable by adding an element at scriptBody[0] "url = urlValue" (urlValue should be the second value parsed from the "visit" command

                    //// This can stay for now because async won't be available for novice users.
                    //scriptBody.Add($"{Indent(2)}async with session.get(ClientSession(url='{desiredUrls.ElementAt(0)}') as response:");
                    //scriptBody.Add($"{Indent(3)}html = await response.text()");
                    //scriptBody.Add($"{Indent(3)}return html");
                    Errors.WriteErrorAndExit("BAM Manager (BAMM) currently lacks support for the 'async' feature, this message will be modified, when this status changes.", 1);
                    break;

                case BrowserPackage.tls_client:
                    //importStatements.Add("from tls_client import Session");
                    //scriptBody.Add("session = Session(client_identifier='safari_ios_16_0'");
                    //scriptBody.Add($"session.get('{desiredUrls.ElementAt(0)}')");
                    Errors.WriteErrorAndExit("BAM Manager (BAMM) currently lacks support for the 'async' feature, this message will be modified, when this status changes.", 1);
                    break;

                case BrowserPackage.selenium:
                    version = PackageManager.New("selenium-wire", pythonVersion);
                    requirements.Add($"selenium-wire=={version}");
                    requirements.Add($"blinker==1.4"); // This fixes the mess that selenium-wire causes by installing blinker >=1.9
                    importStatements.AddRange([
                        "from selenium.webdriver.common.by import By",
                        "from selenium.webdriver.support.ui import Select, WebDriverWait",
                        "from selenium.webdriver.support import expected_conditions as EC",
                        "from seleniumwire import webdriver",
                        ]
                    );
                    switch (selectedBrowser)
                    {
                        case "brave":
                            if (!installedBrowsers.Contains(ApplicationNames.Brave)) { Errors.WriteErrorAndExit(braveNotFound, 1); }
                            importStatements.AddRange([
                                "from selenium.webdriver.chrome.options import Options",
                                "from selenium.webdriver.chrome.service import Service as ChromeService",
                                "from webdriver_manager.chrome import ChromeDriverManager",
                                "from webdriver_manager.core.os_manager import ChromeType",
                            ]);
                            break;

                        case "chrome":
                            if (!installedBrowsers.Contains(ApplicationNames.Chrome)) { Errors.WriteErrorAndExit(chromeNotFound, 1); }
                            importStatements.AddRange([
                                "from selenium.webdriver.chrome.options import Options",
                                "from selenium.webdriver.chrome.service import Service as ChromeService",
                                "from webdriver_manager.chrome import ChromeDriverManager",
                            ]);
                            break;

                        case "firefox":
                            if (!installedBrowsers.Contains(ApplicationNames.Firefox)) { Errors.WriteErrorAndExit(firefoxNotFound, 1); }
                            importStatements.AddRange([
                                "from selenium.webdriver.firefox.options import Options",
                                "from selenium.webdriver.firefox.service import Service as FirefoxService",
                                "from webdriver_manager.firefox import GeckoDriverManager",
                            ]);
                            break;
                    }
                    break;
            }
        }
        public static void CheckConfigLines()
        {
            int numberOfLines = configLines.Count;
            if (numberOfLines == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Errors.WriteErrorAndExit("BAM Manager (BAMM) encountered a fatal error, the selected file has no lines.\n\nPress any key to exit...", 1);
            }
            if (numberOfLines >= 1 && configLines[0].StartsWith("browser") && configLines[0].Contains(' ') && configLines[0].Split(' ').Length == 2) { browserPresent = true; }
            if (browserPresent) { selectedBrowser = configLines[0].Split(' ')[1].Replace('"', ' ').Trim(); }
            featureLines = [.. configLines.Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line) && line.StartsWith("feature"))];
            featurePresent = featureLines.Count > 0; // Roslyn recommend Any() over Count() > 0
            if (featurePresent ||  featureLines.Any(line => line.Contains(" \"disable-pycache\""))) { disablePycache = true; }
            otherPresent = CheckOtherPresent();
            if (!otherPresent) { Warning.Write("BAM Manager (BAMM) was unable to find any requests logic, if this is intentional, you can safely ignore this warning."); }
            if (disablePycache) { importStatements.AddRange(["import sys", "sys.dont_write_byte_code"]); }
            if (featurePresent || featureLines.Any(line => line.Contains(" \"async\""))) { asyncEnabled = true; }
            if (featurePresent || featureLines.Any(line => line.Contains(" \"bypass-cloudflare\""))) { bypassCloudflare = true; }
            
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
            string projectDirectory = Path.Combine(desiredSaveDirectory, projectDirectoryName);
            if (!Path.Exists(desiredSaveDirectory)) {
                Directory.CreateDirectory(desiredSaveDirectory);
            }
            if (!Path.Exists(projectDirectory)) {
                Directory.CreateDirectory(projectDirectory);
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
                    desiredUrls.Add(args[1].Replace('"', ' ').Trim(), lineNumber);
                }
                lineNumber++;
            }
        }
        public static void HandleBrowserCmd()
        {
            // GetUserAgent will exit in the event an invalid browserName is passed, thus the use of !
            if (browserPresent) { requestUserAgent = UserAgentManager.GetUserAgent(selectedBrowser)!; }
            if (asyncEnabled) { browserPackage = BrowserPackage.aiohttp; }
            if (bypassCloudflare) { browserPackage = BrowserPackage.tls_client; }
        }
        public static void HandleCompilation(string fileName, string[] args) 
        {
            SetTimeout(args);
            string[] browserlessActions = ["save-as-html", "wait-for-seconds"]; 
            int lineNumber = 1;
            bool firstVisitFinished = false; // Prevents duplicate entries of BrowserFunctions.makeRequestFunction();
            bool isCE = false; // This prevents issues caused by click-experimental having unique formatting.
            bool isFT = false; // This prevents issues caused by fill-text if the third argument has spaces in it.
            foreach (string line in configLines)
            {
                string[] splitLine;
                if (line.StartsWith("click-experimental ")) { isCE = true; }
                if (line.StartsWith("fill-text")) { isFT = true; }

                if (isFT) { splitLine = line.Split(" \""); } // This handles fill-text
                else if (!isCE) { splitLine = line.Split(" "); } // This handles all but click-experimental and fill-text
                else { splitLine = line.Split(" '"); } // This handles click-experimental

                int[] validLengths = [2, 3];
                if (!validLengths.Contains(splitLine.Length)) {
                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "Invalid feature command syntax."), 1);
                }

                string firstArg = splitLine.First();
                bool canRunBrowserless = browserlessActions.Any(action => action.StartsWith(firstArg));
                if (!canRunBrowserless) {
                    if (noBrowsersFound)
                    {
                        Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "No valid browser installations found, please install brave, chrome, or firefox."), 1);
                    }
                }
                string sanitizedArg2;
                if (!isCE) { sanitizedArg2 = splitLine[1].Replace('"', ' ').Trim(); }
                else { sanitizedArg2 = splitLine[1].Replace('\'', ' ').Replace('"', ' ').Trim(); }
                string sanitizedArg3 = string.Empty;
                if (splitLine.Length >= 3) { sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim(); } // The parser ensures no invalid lines can be provided to the compiler :)

                switch (firstArg)
                {
                        case "click":
                            string clickSelector = splitLine[1].Replace('"', ' ').Trim();
                            ParsedSelector parsedClickSelector = SelectorParser.Parse(clickSelector);
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'click', please remove this line and recompile."), 1);
                                    break;
                                case BrowserPackage.tls_client:
                                    ;
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'click'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;
                                case BrowserPackage.selenium:
                                    switch (parsedClickSelector.Category)
                                    {
                                        case SelectorCategory.Id:
                                            scriptBody.Add($"click_element(By.ID, {splitLine[1]}, {actionTimeout})");
                                            break;

                                        case SelectorCategory.ClassName:
                                            scriptBody.Add($"click_element(By.CLASS_NAME, {splitLine[1]}, {actionTimeout})");
                                            break;
                                        case SelectorCategory.NameAttribute:
                                            scriptBody.Add($"click_element(By.NAME, {splitLine[1]}, {actionTimeout})");
                                            break;
                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"click_element(By.XPATH, {splitLine[1]}, {actionTimeout})");
                                            break;
                                        case SelectorCategory.InvalidOrUnknown:
                                            Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, $"Unable to parse selector: {splitLine[1]}\nIf this is a CSS Selector, please use:\nclick-experimental '{sanitizedArg2}'"), 1);
                                            break;

                                    }
                                    break;
                            }
                            break;

                        case "click-experimental":
                            isCE = false; // Once since the case its safe to set this flag to false
                            string ceSelector = splitLine[1].Replace('\'', ' ').Trim();
                            ParsedSelector parsedCESelector = SelectorParser.Parse(ceSelector);
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'click', please remove this line and recompile."), 1);
                                    break;
                                case BrowserPackage.tls_client:
                                    ;
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'click'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;
                                case BrowserPackage.selenium:
                                    switch (parsedCESelector.Category)
                                    {
                                        case SelectorCategory.Id or SelectorCategory.ClassName or SelectorCategory.NameAttribute:
                                            scriptBody.Add($"click_element('css', '{sanitizedArg2}', {actionTimeout})");
                                            break;
                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"click_element('xpath', '{sanitizedArg2}', {actionTimeout})");
                                            break;
                                        case SelectorCategory.InvalidOrUnknown:
                                            scriptBody.Add($"click_element('css', '{sanitizedArg2}', {actionTimeout})");
                                            break;
                                        

                                    }
                                    break;
                            }
                            break;

                        case "get-text":
                            string textElementSelector = splitLine[1].Replace('"', ' ').Trim();
                            ParsedSelector parsedTextSelector = SelectorParser.Parse(textElementSelector);
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'get-text', please remove this line and recompile."), 1);
                                    break;
                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'get-text'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;
                                case BrowserPackage.selenium:
                                    switch (parsedTextSelector.Category)
                                    {
                                        case SelectorCategory.Id:
                                            scriptBody.Add($"text = get_text(By.ID, '{sanitizedArg2}')");
                                            break;

                                        case SelectorCategory.ClassName:
                                            scriptBody.Add($"text = get_text(By.CLASS_NAME, '{sanitizedArg2}')");
                                            break;
                                        
                                        case SelectorCategory.NameAttribute:
                                            scriptBody.Add($"text = get_text(By.NAME, '{sanitizedArg2}')");
                                            break;

                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"text = get_text(By.XPATH, '{sanitizedArg2}')");
                                            break;

                                        case SelectorCategory.InvalidOrUnknown:
                                            scriptBody.Add($"text = get_text(By.CSS_SELECTOR, '{sanitizedArg2}')");
                                            break;
                                    }
                                    scriptBody.Add($"if text == None:\n{Indent(1)}print('The element: {sanitizedArg2} did not return any text.')\n");
                                    break;
                            }
                            break;

                        case "fill-text":
                            isFT = false; // Once since the case its safe to set this flag to false
                            sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim(); // Parser will throw an error before this is reached, if an exception is triggered. 
                            string fillElementSelector = splitLine[1].Replace('"', ' ').Trim();
                            ParsedSelector parsedFillSelector = SelectorParser.Parse(fillElementSelector);                        
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'fill-text', please remove this line and recompile."), 1);
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'fill-text'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;

                                case BrowserPackage.selenium:
                                    switch (parsedFillSelector.Category)
                                    {
                                        case SelectorCategory.Id:
                                            scriptBody.Add($"isFilled = fill_text(By.ID, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n");
                                            break;

                                        case SelectorCategory.ClassName:
                                            scriptBody.Add($"isFilled = fill_text(By.CLASS_NAME, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n");
                                            break;

                                        case SelectorCategory.NameAttribute:
                                            scriptBody.Add($"isFilled = fill_text(By.NAME, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n");
                                            break;

                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"isFilled = fill_text(By.XPATH, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n");
                                            break;

                                        case SelectorCategory.InvalidOrUnknown:
                                            scriptBody.Add($"isFilled = fill_text(By.CSS_SELECTOR, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n");
                                            break;
                                    }
                                    scriptBody.Add($"if isFilled:\n{Indent(1)}print('The element: {sanitizedArg2} should be filled, as no error was thrown.')");
                                    scriptBody.Add($"else:\n{Indent(1)}print('Could not fill the element: {sanitizedArg2}')\n");
                                    break;
                            }
                            break;

                        case "save-as-html":
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'save-as-html', please remove this line and recompile."), 1);
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'save-as-html'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;

                                case BrowserPackage.selenium:
                                    scriptBody.Add($"isSaved = save_as_html('{sanitizedArg2}')\n");
                                    scriptBody.Add($"if isSaved:\n{Indent(1)}print('Saved page source to: {sanitizedArg2}')");
                                    scriptBody.Add($"else:\n{Indent(1)}print('Unable to save page source, please ensure the page was fully loaded.')\n");
                                    break;
                            }
                            break;

                        case "save-as-html-experimental":
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'async' feature cannot be used in combination with action 'save-as-html-experimental', please remove this line and recompile."), 1);
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, "The 'bypass-cloudflare' feature cannot be used in combination with action 'save-as-html-experimental'.\n\nPlease remove either this line or the line containing the 'bypass-cloudflare' feature and recompile."), 1);
                                    break;

                                case BrowserPackage.selenium:
                                    scriptBody.Add($"isSaved = save_as_html_experimental('{sanitizedArg2}')\n");
                                    scriptBody.Add($"if isSaved:\n{Indent(1)}print('Saved page source to: {sanitizedArg2}')");
                                    scriptBody.Add($"else:\n{Indent(1)}print('Unable to save page source, please ensure the page was fully loaded.')\n");
                                    break;
                            }
                            break;
                        
                        case "select-element":
                            string selectElementSelector = splitLine[1].Replace('"', ' ').Trim();
                            ParsedSelector parsedSelectSelector = SelectorParser.Parse(selectElementSelector);
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'select-element' commands are currently unsupported while using feature 'async'.");
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'select-element' commands are currently unsupported while using feature 'bypass-cloudflare'.");
                                    break;

                                case BrowserPackage.selenium:
                                    switch (parsedSelectSelector.Category)
                                    {
                                        case SelectorCategory.Id:
                                            scriptBody.Add($"element = select_element(By.ID, '{parsedSelectSelector.Value}', {actionTimeout})\n");
                                            
                                            break;

                                        case SelectorCategory.ClassName:
                                            scriptBody.Add($"element = select_element(By.CLASS_NAME, '{parsedSelectSelector.Value}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.NameAttribute:
                                            scriptBody.Add($"element = select_element(By.NAME, '{parsedSelectSelector.Value}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"element = select_element(By.XPATH, '{parsedSelectSelector.Value}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.InvalidOrUnknown:
                                            scriptBody.Add($"element = select_element(By.CSS_SELECTOR, '{parsedSelectSelector.Value}', {actionTimeout})\n");
                                            break;
                                    }
                                    scriptBody.Add($"if not element:\n{Indent(1)}print('The element: {sanitizedArg2} could not be selected, please try again or use a different selector.')");
                                    break;
                            }
                            break;

                        case "select-option": // Add functionality for non select dropdowns
                            string optionElementSelector = splitLine[1].Replace('"', ' ').Trim();
                            ParsedSelector parsedOptionSelector = SelectorParser.Parse(optionElementSelector);
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'select-option' commands are currently unsupported while using feature 'async'.");
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'select-option' commands are currently unsupported while using feature 'bypass-cloudflare'.");
                                    break;

                                case BrowserPackage.selenium:
                                    switch (parsedOptionSelector.Category)
                                    {
                                        case SelectorCategory.Id:
                                            scriptBody.Add($"isSelected = select_option_by_index(By.ID, '{parsedOptionSelector.Value}', '{sanitizedArg3}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.ClassName:
                                            scriptBody.Add($"isSelected = select_option_by_index(By.CLASS_NAME, '{parsedOptionSelector.Value}', '{sanitizedArg3}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.NameAttribute:
                                            scriptBody.Add($"isSelected = select_option_by_index(By.NAME, '{parsedOptionSelector.Value}', '{sanitizedArg3}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.XPath:
                                            scriptBody.Add($"isSelected = select_option_by_index(By.XPATH, '{parsedOptionSelector.Value}', '{sanitizedArg3}', {actionTimeout})\n");
                                            break;

                                        case SelectorCategory.InvalidOrUnknown:
                                            scriptBody.Add($"isSelected = select_option_by_index(By.CSS_SELECTOR, '{parsedOptionSelector.Value}', '{sanitizedArg3}, {actionTimeout}')\n");
                                            break;

                                    }
                                    scriptBody.Add($"if not isSelected:\n{Indent(1)}print('Could not select the element: {sanitizedArg2}')\n");
                                    break;
                            }
                            break;

                        case "take-screenshot":
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) does not support 'take-screenshot' commands while using feature 'async'.");
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) does not support 'take-screenshot' commands while using feature 'bypass-cloudflare'.");
                                    break;

                                case BrowserPackage.selenium:
                                    scriptBody.Add($"take_screenshot('{sanitizedArg2}')");
                                    break;
                            }
                            break;

                        case "visit":
                            switch (browserPackage)
                            {
                                case BrowserPackage.aiohttp:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'visit' commands are currently unsupported while using feature 'async'.");
                                    break;

                                case BrowserPackage.tls_client:
                                    Errors.WriteErrorAndContinue("BAM Manager (BAMM) warning:\n'visit' commands are currently unsupported while using feature 'bypass-cloudflare'.");
                                    break;

                                case BrowserPackage.selenium:
                                    scriptBody.Add($"url = '{sanitizedArg2}'");
                                    if (!firstVisitFinished)
                                    {
                                        scriptBody.AddRange(
                                        [
                                            "print('Initializing WebDriver...')\n",
                                            "driver = None",
                                            "status_code = None",
                                            "final_url = url",
                                            "request_url = None",
                                            "sw_options = { 'enable_har': True }\n"
                                        ]);
                                        switch (selectedBrowser)
                                        {
                                            case "brave":
                                                scriptBody.Add("driver = webdriver.Chrome(service=ChromeService(ChromeDriverManager(chrome_type=ChromeType.BRAVE).install()))");
                                                break;

                                            case "chrome":
                                                scriptBody.Add("driver = webdriver.Chrome(service=ChromeService(ChromeDriverManager().install()), seleniumwire_options=sw_options)");
                                                break;

                                            case "firefox" or "safari":
                                                scriptBody.Add("driver = webdriver.Firefox(service=FirefoxService(GeckoDriverManager().install()), seleniumwire_options=sw_options)");
                                                break;
                                        }
                                        scriptBody.Add("driver.maximize_window()");
                                        scriptBody.AddRange(
                                            [
                                                "bounds = get_screen_bounds()\n",
                                                "if bounds is not None and len(bounds) == 2:",
                                                $"{Indent(1)}height = bounds[1]",
                                                $"{Indent(1)}width = bounds[0]",
                                                "else:",
                                                $"{Indent(1)}height = 1920",
                                                $"{Indent(1)}width = 1080\n\n",
                                                "driver.set_window_position(width, 0) # Sets the browser off the right of the primary display",
                                                "print('Driver initialized.')\n\n"
                                            ]);
                                        scriptBody.Add("make_request(url)");

                                    }
                                    else
                                    {
                                        scriptBody.Add("make_request(url)");
                                    }
                                    firstVisitFinished = true;
                                    break;
                            }
                            break;

                        case "wait-for-seconds":
                            bool waitTimeValidated = false;
                            string rawTimeArg = sanitizedArg2;
                            if (rawTimeArg.StartsWith('.')) { rawTimeArg = $"0{rawTimeArg}"; } // Handles cases where the input value starts with a decimal
                            if (float.TryParse(rawTimeArg, out float waitTime))
                            {
                                if (!importStatements.Contains("from time import sleep")) { importStatements.Add("from time import sleep"); }
                                scriptBody.Add($"sleep({waitTime})");
                                waitTimeValidated = true;
                            }
                            if (!waitTimeValidated)
                            {
                                Errors.WriteErrorAndExit(Errors.GenerateErrorMessage(fileName, line, lineNumber, $"Invalid argument '{splitLine[1]}'"), 1);
                            }
                            break;
                    }
                lineNumber++;
            }
            importStatements.Add("\n\n"); // Add 2 trailing newlines for readablility
            scriptBody.Insert(0, BrowserFunctions.clickElementFunction);
            scriptBody.Insert(1, BrowserFunctions.clickElementExperimentalFunction);
            scriptBody.Insert(2, BrowserFunctions.getScreenBoundsFunction);
            scriptBody.Insert(3, BrowserFunctions.fillTextFunction);
            scriptBody.Insert(4, BrowserFunctions.getTextFunction);
            scriptBody.Insert(5, BrowserFunctions.makeRequestFunction);
            scriptBody.Insert(6, BrowserFunctions.saveAsHTMLFunction);
            scriptBody.Insert(7, BrowserFunctions.saveAsHTMLExperimentalFunction);
            scriptBody.Insert(8, BrowserFunctions.selectElementFunction);
            scriptBody.Insert(9, BrowserFunctions.selectOptionByIndexFunction);
            scriptBody.Insert(10, BrowserFunctions.takeScreenshotFunction);
            scriptBody.Insert(scriptBody.Count, BrowserFunctions.browserQuitCode);
        } // Finish me
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
            foreach (ApplicationNames app in installations.InstalledApps){
                if (!versionMapping.TryGetValue(app, out string? version)){ continue; }
                foundVersions.Add(version);
                inputMessage += $"{iterationIndex}.     Python {version}\n";
                iterationIndex += 1;
            }
            while (true){
                Console.WriteLine(inputMessage);
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
        }
        public static string Indent(int numberOfIndents) { 
            if (numberOfIndents < 0) { Errors.WriteErrorAndExit("Invalid value provided to Indent(), value must be >= 0.", 1); }
            if (numberOfIndents == 0) { return string.Empty; } // Return an empty string if no indentations are needed.
            return string.Concat(Enumerable.Repeat(pythonIndent, numberOfIndents));
        }
        public static bool IsValidPyVersion(string pyVersion)
        {
            if (string.IsNullOrWhiteSpace(pyVersion)) { return false; }
            string[] parts = pyVersion.Split('.');
            if (parts.Length != 2) { return false; }
            if (!int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor)) { return false; }
            return major == 3 && minor >= 9 && minor <= 13;
        }
        public static void SetFileLines(string filePath)
        {
            string fileNotFoundMessage = $"BAM Manager (BAMM) was unable to find the file:\n\n{filePath}, Please ensure this file exists, then rerun bamm.exe.\n\nPress any key to exit...";
            if (!File.Exists(filePath)) { Errors.WriteErrorAndExit(fileNotFoundMessage, 1); }
            configLines = [.. File.ReadAllLines(filePath).Select(line => line.Trim()).Where(line => !string.IsNullOrWhiteSpace(line))];
            CheckConfigLines();
        }
        public static void SetScriptName(string filePath)
        {
            string failureMessage = $"""
            BAM Manager (BAMM) was unable to access:\n\n{filePath}\n\nPlease ensure this file was not deleted, and is not in use by any other program.\n\nPress any key to exit...
            """;
           
            try
            {
                string fileName = Path.GetFileName(filePath);
                // If null the function i wrote will exit, however the static compiler is unaware of this thus the requirement for !.
                if (fileName == null) { Errors.WriteErrorAndExit(failureMessage, 1); }

                if (!File.Exists(filePath))
                {
                    failureMessage = $"BAM Manager (BAMM) was unable to access:\n\n{fileName}\n\nPlease ensure this file was not deleted, and is not in use by any other program.\\n\\nPress any key to exit...";
                    Console.ForegroundColor = ConsoleColor.Red;
                    Errors.WriteErrorAndExit(failureMessage, 1);
                }

                try
                {
                    pythonScriptFileName = fileName!.Split(".")[0] + ".py"; // I hate c#'s static compiler i already ensured fileName cannot be null, yet i have to yell at the compiler using !
                }
                catch
                {
                    GenerateBackupScriptName();
                }
            }
            catch (Exception) { Errors.WriteErrorAndExit(failureMessage, 1); }
        }
        public static void SetTimeout(string[] args)
        {

            List<string> timeoutArgs = [.. args.Where(arg => arg.StartsWith("--set-timeout=="))];

            if (timeoutArgs.Count > 1)
            {
                Errors.WriteErrorAndExit(
                    $"BAM Manager (BAMM) encountered a fatal error: '--set-timeout' can only be specified once.\n" +
                    $"Found multiple instances:\n\n" +
                    $"1.'{timeoutArgs[0]}'\n\n" +
                    $"2.'{timeoutArgs[1]}'\n\n." +
                    "Please remove duplicates and recompile.",
                    1);
            }
            if (timeoutArgs.Count == 0) { return; }
            else
            {
                string timeoutArg = timeoutArgs[0];
                Match match = ActionTimeoutRegex.Match(timeoutArg);

                if (match.Success)
                {
                    string valueString = match.Groups[1].Value;
                    if (int.TryParse(valueString, out int parsedTimeout))
                    {
                        if (parsedTimeout >= 0)
                        {
                            actionTimeout = parsedTimeout;
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"Timeout set to {actionTimeout} seconds ({actionTimeout * 1000}ms)");
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                        else
                        {
                            Errors.WriteErrorAndExit(
                                $"BAM Manager (BAMM) encountered a fatal error: Invalid value provided for '--set-timeout'.\n" +
                                $"Value must be a non-negative integer.\n\nParsed Timeout: '{parsedTimeout}'\nRaw Argument: '{timeoutArg}'",
                                1);
                        }
                    }
                    else
                    {
                        // This shouldn't be executed due to the strict regex.
                        Errors.WriteErrorAndExit("BAM Manager (BAMM) encountered a a fatal error: Could not parse integer value from '--set-timeout' argument.\n", 1);
                    }
                }
                else
                {
                    // Case for when the argument starts with --set-timeout== but doesn't match the expected format (For example '--set-timeout==X')
                    Errors.WriteErrorAndExit(
                        $"BAM Manager encountered an error: Invalid format for '--set-timeout' argument.\n\n" +
                        $"Expected Format: '--set-timeout==integer'" +
                        $"Received: '{timeoutArg}'",
                        1);
                }
            }
        }
        public static void VerifyInstallations(Installations installations)
        {
            string pythonErrorMessage = """
            BAM Manager (BAMM) was unable to detect any valid versions of Python.

            BAMM was designed for Python 3.10 and 3.11.

            Please install one of these versions using the links below:

            Python 3.10: https://www.python.org/downloads/release/python-31011/
            Python 3.11: https://www.python.org/downloads/release/python-3119/
            """;

            string browserWarningMessage = """
            BAM Manager (BAMM) was unable to detect any supported browser installations.

            BAMM was designed mostly for browser automation, but has a few commands that can be used to automate raw requests (This is only for advanced users).
            
            Please install one of supported browsers below to fully utilize:

            Brave: https://brave.com/download/
            Chrome: https://google.com/chrome/
            Firefox: https://www.mozilla.org/en-US/firefox/
            """;

            string pythonWarningMessage = """
            BAM Manager (BAMM) detected multiple versions of Python.
            
            BAMM was designed for Python 3.10 and 3.11.
            
            While it's possible that scripts compiled with BAMM may run on 3.9, 3.12, 3.13, and 3.14, this should be avoided unless you intend to contribute to development.
            
            You will be prompted shortly to select a version to compile with, Please select either Python 3.10 or 3.11, unless you are explicitly testing other versions.
            """;

            installedBrowsers = [.. installations.InstalledApps.Where(x => InstallationCheck.BrowserApps.Contains(x))];
            installedPyVersions = [.. installations.InstalledApps.Where(x => InstallationCheck.PythonApps.Contains(x))];

            if (installedPyVersions.Count == 0) { Errors.WriteErrorAndExit(pythonErrorMessage, 1); }
            if (installedBrowsers.Count == 0) { 
                noBrowsersFound = true; 
                Warning.Write(browserWarningMessage); 
            }

            List<string> problematicVersions = ["3.9", "3.12", "3.13", "3.14"];
            List<string> bestVersions = ["3.10", "3.11"];
            if (installedPyVersions.Any(x => problematicVersions.Contains(x.ToString()))) { 
                if (installedPyVersions.Any(x => bestVersions.Contains(x.ToString()))){
                    Warning.Write(pythonWarningMessage);
                }
            }
        }
        public static void WriteRequirementsFile()
        {
            string filePath = Path.Combine(desiredSaveDirectory, projectDirectoryName, requirementsFileName);
            using StreamWriter writer = new(filePath, false, Encoding.UTF8);
            foreach (string requirement in requirements) { 
                writer.WriteLine(requirement); 
            }
        }
        public static void WritePythonFile()
        {
            string filePath = Path.Combine(desiredSaveDirectory, projectDirectoryName, pythonScriptFileName);
            using StreamWriter writer = new(filePath, false, Encoding.UTF8);
            foreach (string importStatement in importStatements){
                writer.WriteLine(importStatement);
            }

            if (importStatements.Count > 0 && scriptBody.Count > 0){
                writer.WriteLine();
            }

            foreach (string scriptLine in scriptBody){
                writer.WriteLine(scriptLine);
            }
        }

    }
}
