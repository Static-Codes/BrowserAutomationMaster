using System.Net.NetworkInformation;
using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Messaging;
using BrowserAutomationMaster.Parsing;
using static BrowserAutomationMaster.Compilation.BrowserFunctions;
using static BrowserAutomationMaster.Compilation.Transpiler;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static BrowserAutomationMaster.Messaging.Success;

namespace BrowserAutomationMaster.Compilation
{
    public static class CompilationHandler
    {

        public static void AddCookie(List<string> scriptBody, string cookieName, string cookieValue)
        {
            scriptBody.Add(
                AddCookieFunction(cookieName, cookieValue)
            );
        }

        public static void AddHeader(List<string> scriptBody, string sanitizedArg2, string sanitizedArg3)
        {
            var headerString = AddHeaderFunction(sanitizedArg2, sanitizedArg3);
            if (headerString == null)
            {
                Warning.Write($"Unable to add header '{sanitizedArg2}' with value '{sanitizedArg3}'");
                return;
            }
            scriptBody.Add(headerString);
        }

        public static bool Click(List<string> scriptBody, string[] splitLine, int actionTimeout)
        {
            string clickSelector = splitLine[1].Replace('"', ' ').Trim();
            ParsedSelector parsedClickSelector = SelectorParser.Parse(clickSelector);

            if (parsedClickSelector.Category is SelectorCategory.Id)
            {
                scriptBody.Add($"click_element(By.ID, '{parsedClickSelector.Value}', {actionTimeout})");
            }
            else if (parsedClickSelector.Category is SelectorCategory.ClassName)
            {
                scriptBody.Add($"click_element(By.CLASS_NAME, '{parsedClickSelector.Value}', {actionTimeout})");
            }
            else if (parsedClickSelector.Category is SelectorCategory.NameAttribute)
            {
                scriptBody.Add($"click_element(By.NAME, '{parsedClickSelector.Value}', {actionTimeout})");
            }
            else if (parsedClickSelector.Category is SelectorCategory.TagName)
            {
                scriptBody.Add($"click_element(By.TAG_NAME, '{parsedClickSelector.Value}', {actionTimeout})");
            }
            else if (parsedClickSelector.Category is SelectorCategory.XPath)
            {
                scriptBody.Add($"click_element(By.XPATH, '{parsedClickSelector.Value}', {actionTimeout})");
            }
            else if (parsedClickSelector.Category is SelectorCategory.InvalidOrUnknown)
            {
                return false;
            }
            return true;
        }
        public static (bool, string) ClickAtPosition(List<string> scriptBody, string[] splitLine, string sanitizedArg2, string sanitizedArg3, int actionTimeout)
        {
            if (!int.TryParse(sanitizedArg2, out int xPos))
            {
                return (false, $"Invalid argument {splitLine[1]}");
            }
            if (!int.TryParse(sanitizedArg3, out int yPos))
            {
                return (false, $"Invalid argument {splitLine[2]}");
            }
            scriptBody.Add($"click_at_position({xPos}, {yPos}, {actionTimeout})");
            return (true, string.Empty);
        }
        public static (bool, string) ClickExp(List<string> scriptBody, string[] splitLine, string sanitizedArg2, int actionTimeout, ref bool isCE)
        {
            isCE = true;
            try
            {
                string ceSelector = splitLine[1].Replace('\'', ' ').Trim();
                ParsedSelector parsedCESelector = SelectorParser.Parse(ceSelector);
                switch (parsedCESelector.Category)
                {
                    case SelectorCategory.Attribute:
                    case SelectorCategory.ClassName:
                    case SelectorCategory.Id:
                    case SelectorCategory.NameAttribute:
                    case SelectorCategory.PseudoClass:
                    case SelectorCategory.PseudoElement:
                    case SelectorCategory.TagName:
                        scriptBody.Add($"click_element_experimental(\"{parsedCESelector.rawInput}\", {actionTimeout})");
                        break;
                    case SelectorCategory.XPath:
                        scriptBody.Add($"click_element_experimental('{parsedCESelector.rawInput}', {actionTimeout})");
                        break;
                    case SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add($"click_element(\"{sanitizedArg2}\", {actionTimeout})");
                        break;
                }
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static void CloseCurrentTab(List<string> scriptBody) { scriptBody.Add("close_current_tab()"); }
        public static (bool, string) GetText(List<string> scriptBody, string[] splitLine)
        {
            try
            {
                string textElementSelector = splitLine[1].Replace('"', ' ').Trim();
                ParsedSelector parsedTextSelector = SelectorParser.Parse(textElementSelector);
                switch (parsedTextSelector.Category)
                {
                    case SelectorCategory.Id:
                        scriptBody.Add($"text = get_text(By.ID, '{parsedTextSelector.Value}')");
                        break;

                    case SelectorCategory.ClassName:
                        scriptBody.Add($"text = get_text(By.CLASS_NAME, '{parsedTextSelector.Value}')");
                        break;

                    case SelectorCategory.NameAttribute:
                        scriptBody.Add($"text = get_text(By.NAME, '{parsedTextSelector.Value}')");
                        break;

                    case SelectorCategory.TagName:
                        scriptBody.Add($"text = get_text(By.TAG_NAME, '{parsedTextSelector.Value}')");
                        break;

                    case SelectorCategory.XPath:
                        scriptBody.Add($"text = get_text(By.XPATH, '{parsedTextSelector.Value}')");
                        break;

                    case SelectorCategory.Attribute:
                    case SelectorCategory.PseudoClass:
                    case SelectorCategory.PseudoElement:
                    case SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add($"text = get_text(By.CSS_SELECTOR, '{parsedTextSelector.Value}')");
                        break;
                }
                scriptBody.Add(
                    $"if text == None:\n{Indent(1)}" +
                    $"stderr.write('The element: {parsedTextSelector.Value} did not return any text.')\n"
                );
                return (true, string.Empty);

            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static (bool, string) FillText(List<string> scriptBody, string[] splitLine, string sanitizedArg2, ref bool isFT)
        {
            try
            {
                isFT = false; // Once since the case its safe to set this flag to false
                string sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim(); // Parser will throw an error before this is reached, if an exception is triggered. 
                string fillElementSelector = splitLine[1].Replace('"', ' ').Trim();
                ParsedSelector parsedFillSelector = SelectorParser.Parse(fillElementSelector);
                switch (parsedFillSelector.Category)
                {
                    case SelectorCategory.Id:
                        scriptBody.Add(
                            $"isFilled = fill_text(By.ID, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n"
                        );
                        break;

                    case SelectorCategory.ClassName:
                        scriptBody.Add(
                            $"isFilled = fill_text(By.CLASS_NAME, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n"
                        );
                        break;

                    case SelectorCategory.NameAttribute:
                        scriptBody.Add(
                            $"isFilled = fill_text(By.NAME, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n"
                        );
                        break;

                    case SelectorCategory.TagName:
                        scriptBody.Add(
                            $"isFilled = fill_text(By.TAG_NAME, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n"
                        );
                        break;

                    case SelectorCategory.XPath: // Special case to handle xpath's (keep the escaped double quotes)
                        scriptBody.Add(
                            $"isFilled = fill_text(By.XPATH, \"{parsedFillSelector.Value}\", '{sanitizedArg3}')\n"
                        );
                        break;

                    case SelectorCategory.Attribute or
                    SelectorCategory.PseudoClass or
                    SelectorCategory.PseudoElement or
                    SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add(
                            $"isFilled = fill_text(By.CSS_SELECTOR, '{parsedFillSelector.Value}', '{sanitizedArg3}')\n"
                        );
                        break;
                }
                scriptBody.Add($"if isFilled:\n" +
                               $"{Indent(1)}" +
                               $"print(\"The element: {sanitizedArg2} should be filled, as no error was thrown.\")");

                scriptBody.Add($"else:\n" +
                               $"{Indent(1)}stderr.write(\"Could not fill the element: {sanitizedArg2}\")\n" +
                               $"{Indent(1)}exit(1)\n");

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }

        }
        public static (bool, string) FillTextExp(List<string> scriptBody, List<string> importStatements, string[] splitLine, string sanitizedArg2, ref bool isFT)
        {
            try
            {
                isFT = false; // Once inside the case its safe to set this flag to false
                
                string sanitizedArg3 = splitLine[2].Replace('"', ' ').Trim(); // Parser will throw an error before this is reached, if an exception is triggered. 
                string fillElementExpSelector = splitLine[1].Replace('"', ' ').Trim();
                
                ParsedSelector parsedFillExpSelector = SelectorParser.Parse(fillElementExpSelector);
                importStatements.AddRange(["from selenium.webdriver.remote.webelement import WebElement",
                                                   "from selenium.common.exceptions import StaleElementReferenceException, TimeoutException"]);

                switch (parsedFillExpSelector.Category)
                {
                    case SelectorCategory.Id:
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.ID, '{parsedFillExpSelector.Value}', '{sanitizedArg3}'){NLC}"
                        );
                        break;

                    case SelectorCategory.ClassName:
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.CLASS_NAME, '{parsedFillExpSelector.Value}', '{sanitizedArg3}'){NLC}"
                        );
                        break;

                    case SelectorCategory.NameAttribute:
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.NAME, '{parsedFillExpSelector.Value}', '{sanitizedArg3}'){NLC}"
                        );
                        break;

                    case SelectorCategory.TagName:
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.TAG_NAME, '{parsedFillExpSelector.Value}', '{sanitizedArg3}'){NLC}"
                        );
                        break;

                    case SelectorCategory.XPath: // Special case to handle xpath's (keep the escaped double quotes)
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.XPATH, \"{parsedFillExpSelector.Value}\", '{sanitizedArg3}'){NLC}"
                        );
                        break;

                    case SelectorCategory.Attribute:
                    case SelectorCategory.PseudoClass:
                    case SelectorCategory.PseudoElement:
                    case SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add(
                            $"isFilled = fill_text_exp(By.CSS_SELECTOR, '{parsedFillExpSelector.Value}', '{sanitizedArg3}'){NLC}"
                        );
                        break;
                }
                scriptBody.Add(
                    $"if isFilled:{NLC}" +
                    $"{Indent(1)}" +
                    $"print(\"The element: {sanitizedArg2} should be filled, as no error was thrown.\")"
                );
                scriptBody.Add(
                    $"else:{NLC}" +
                    $"{Indent(1)}stderr.write(\"Could not fill the element: {sanitizedArg2}\"){NLC}" +
                    $"{Indent(1)}exit(1){NLC}"
                );
                return (true, string.Empty);
            }

            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static (bool, string) OpenNewTab(List<string> scriptBody, string sanitizedArg2, string sanitizedArg3)
        {
            try
            {
                using Ping pinger = new();
                if (sanitizedArg2.EndsWith('/')) { 
                    sanitizedArg2 = sanitizedArg2[..^1]; 
                }

                if (!IsResolvableLink(sanitizedArg2)) {
                    WriteAndExit(
                        message:
                            "BAM Manager (BAMM) was unable to compile the requested script:\n\nError log:\n" +
                            $"{sanitizedArg2} was unresolvable, please check for typos.\n\n" +
                            $"If this error persists please make a bug report at {ISSUES_LINK}",
                        status: 1
                    );
                }

                scriptBody.Add($"open_new_tab('{sanitizedArg2}', {sanitizedArg3})");
                return (true, string.Empty);
            }
            catch (Exception e)
            {
                Write(
                    message:
                        $"BAM Manager (BAMM) was unable to resolve the url: '{sanitizedArg2}'\n" +
                        $"Error log:\n\n{e.Message}"
                );
                return (false, e.Message);
            }
        }
        public static void SaveAsHTML(List<string> scriptBody, string sanitizedArg2)
        {
            scriptBody.AddRange([
                $"isSaved = save_as_html('{sanitizedArg2}')\n",
                "if isSaved:\n",
                $"{Indent(1)}print('Saved page source to: {sanitizedArg2}')\n",
                "else:",
                $"\n{Indent(1)}print('Unable to save page source, please ensure the page was fully loaded.')\n"
            ]);
        }
        public static void SaveAsHTMLExp(List<string> scriptBody, string sanitizedArg2)
        {
            scriptBody.AddRange([
                $"isSaved = save_as_html('{sanitizedArg2}')\n",
                "if isSaved:\n",
                $"{Indent(1)}print('Saved page source to: {sanitizedArg2}')\n",
                "else:",
                $"\n{Indent(1)}print('Unable to save page source, please ensure the page was fully loaded.')\n"
            ]);
        }
        public static (bool, string) SelectElement(List<string> scriptBody, string[] splitLine, int actionTimeout)
        {
            try
            {
                string selectElementSelector = splitLine[1].Replace('"', ' ').Trim();
                ParsedSelector parsedSelectSelector = SelectorParser.Parse(selectElementSelector);
                switch (parsedSelectSelector.Category)
                {
                    case SelectorCategory.Id:
                        scriptBody.Add(
                            $"element = select_element(By.ID, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.ClassName:
                        scriptBody.Add(
                            $"element = select_element(By.CLASS_NAME, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.NameAttribute:
                        scriptBody.Add(
                            $"element = select_element(By.NAME, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.TagName:
                        scriptBody.Add(
                            $"element = select_element(By.TAG_NAME, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.XPath:
                        scriptBody.Add(
                            $"element = select_element(By.XPATH, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.Attribute or
                         SelectorCategory.PseudoClass or
                         SelectorCategory.PseudoElement or
                         SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add(
                            $"element = select_element(By.CSS_SELECTOR, '{parsedSelectSelector.Value}', {actionTimeout})\n"
                        );
                        break;
                }
                scriptBody.Add($"if not element:\n{Indent(1)}" +
                               $"stderr.write('The element: {parsedSelectSelector.Value} could not be selected, " +
                               $"please try again or use a different selector.')" +
                               $"\n{Indent(1)}exit(1)\n");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static (bool, string) SelectOption(List<string> scriptBody, string sanitizedArg2, string sanitizedArg3, int actionTimeout)
        {
            try
            {
                ParsedSelector parsedOptionSelector = SelectorParser.Parse(sanitizedArg2);

                switch (parsedOptionSelector.Category)
                {
                    case SelectorCategory.Id:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.ID, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.ClassName:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.CLASS_NAME, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.NameAttribute:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.NAME, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.TagName:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.TAG_NAME, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.XPath:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.XPATH, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                    case SelectorCategory.Attribute or
                    SelectorCategory.PseudoClass or
                    SelectorCategory.PseudoElement or
                    SelectorCategory.InvalidOrUnknown:
                        scriptBody.Add(
                            $"isSelected = select_option_by_index(By.CSS_SELECTOR, '{parsedOptionSelector.Value}', {sanitizedArg3}, {actionTimeout})\n"
                        );
                        break;

                }
                scriptBody.Add($"if not isSelected:\n" +
                               $"{Indent(1)}stderr.write('Could not select the element: {sanitizedArg2}')" +
                               $"\n{Indent(1)}exit(1)\n");
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
        public static void SetCustomUserAgent(string[] splitLine, int lineNumber, ref string requestUserAgent, ref bool isCU)
        {
            // Parser already ensures this line is valid so a second null check is not required; assuming set-custom-useragent is not modified without testing.
            string customUserAgent = splitLine[1].Replace('"', ' ').Trim();
            requestUserAgent = customUserAgent;
            WriteSuccessMessage($"\nSuccessfully set custom user agent on line {lineNumber}.");
            isCU = false;
        }
        public static void TakeScreenshot(List<string> scriptBody, string sanitizedArg2)
        {
            scriptBody.Add($"take_screenshot('{sanitizedArg2}')");
        }
        public static async Task<(bool, string)> Visit(List<string> scriptBody, 
            List<string> featureLines, string sanitizedArg2, string selectedBrowser, 
            bool firstVisitFinished, bool disableSSL, bool runHeadless, ExtensionManager[] Extensions
        )
        {
            if (!IsResolvableLink(sanitizedArg2))
                WriteAndExit(
                    message:
                        "BAM Manager (BAMM) was unable to compile the requested script:\n\nError log:\n" +
                        $"{sanitizedArg2} was unresolvable, please check for typos.\n\n" +
                        $"If this error persists please make a bug report at {ISSUES_LINK}",
                    status: 1);

            scriptBody.Add($"url = '{sanitizedArg2}'");
            if (firstVisitFinished)
            {
                scriptBody.Add("make_request(url)");
                return (true, string.Empty);
            }
            scriptBody.AddRange([
                "print('Initializing WebDriver...')\n",
                "driver = None",
                "status_code = None",
                "final_url = url",
                "request_url = None"
            ]);

            string proxyLine = featureLines.Where(
                x => x.Contains("use-") && x.Contains("-proxy")
            ).FirstOrDefault("");

            if (!string.IsNullOrEmpty(proxyLine))
            {
                string[] splitProxyLine = [];
                // Handles cases of malformed lines, although this shouldn't happen
                try
                {
                    splitProxyLine = proxyLine.Trim().Split(" ");
                    if (splitProxyLine.Length != 3)
                    {
                        scriptBody.Add("sw_options = { 'enable_har': True }\n");
                        scriptBody.Add("options = Options()");
                        return (true, string.Empty);
                    }
                }
                catch
                {
                    scriptBody.Add("sw_options = { 'enable_har': True }\n");
                    scriptBody.Add("options = Options()");
                    return (true, string.Empty);
                }

                string prefix = "use-";
                string suffix = "-proxy";

                int startIndexActual = proxyLine.IndexOf(prefix) + prefix.Length;
                int endIndexActual = proxyLine.IndexOf(suffix);

                if (startIndexActual >= prefix.Length && endIndexActual > startIndexActual)
                {
                    int length = endIndexActual - startIndexActual;
                    string proxyType = proxyLine.Substring(startIndexActual, length);

                    scriptBody.Add(
                        $"sw_options = {{\n  'enable_har': True,\n   'proxy':{{\n    '"
                        + proxyType
                        + "': '"
                        + proxyType +
                        $"://{splitProxyLine[2].Replace("\"", " ").Trim()}'\n   }}\n}}"
                    );
                    scriptBody.Add("options = Options()");
                }

                else
                {
                    Warning.Write
                    (
                        message:
                            "Unable to add proxy to script, if you reading this, " +
                            "there is a huge bug in the use-proxyType-proxy feature.\n" +
                            $"Please make a bug report at {ISSUES_LINK}."
                    );
                }
            }
            else
            {
                scriptBody.Add("sw_options = { 'enable_har': True }\n");
                scriptBody.Add("options = Options()");
            }
            

            # region Adding extensions after driver initialization.

            // Bidirectional switch for Chrome due to security changes with manifest V3 in Chrome 137+
            var bidiSwitch = selectedBrowser == "chrome" ? $"options.enable_bidi = True{NLC}" : NLC;

            // Adding the bidiOptions object as a param during browser initialization.
            var bidiOptionsParam = bidiSwitch != NLC ? ", options = options" : "";

            // Disables the new standard behavior on Chrome 137+ (https://github.com/SeleniumHQ/selenium/issues/15788#issuecomment-2931704434)
            var experimentalChromeFlag =
                bidiOptionsParam != "" ?
                $"options.add_argument('--disable-features=DisableLoadExtensionCommandLineSwitch'){NLC}" :
                NLC;

            // Downloading a copy of the extensions provided
            string[] extensionInstallCommands = new string[Extensions.Length];

            for (int i = 0; i < extensionInstallCommands.Length; i++) 
            {
                var contents = await Extensions[i].GetExtensionContents();
                var downloadPath = await Extensions[i].WriteExtensionContents(contents);
                
                if (downloadPath == null) {
                    Errors.Write("Failed to download extension from: ", noNewLines: true);
                    Warning.Write(Extensions[i].ExtensionPath, noNewLines: true);
                    Console.WriteLine(NLC);
                    continue;
                }

                if (selectedBrowser == "chrome") {
                    extensionInstallCommands[i] = $"options.add_extension('{downloadPath}')";
                } else {
                    extensionInstallCommands[i] = $"driver.install_addon('{downloadPath}', temporary=True)";
                }
            } 

            var extensionsInstallString = extensionInstallCommands.Length > 0 ? string.Join(NLC, extensionInstallCommands) : NLC;


            # endregion Adding extensions after driver initialization.


            switch (selectedBrowser)
            {
                //case "brave":
                //    scriptBody.Add("driver = webdriver.Chrome(service=ChromeService(ChromeDriverManager(chrome_type=ChromeType.BRAVE).install()))");
                //    break;

                case "chrome":
                    if (disableSSL)
                    {
                        scriptBody.AddRange([
                            "options = Options()",
                            "options.add_argument('--ignore-certificate-errors')",
                            experimentalChromeFlag,
                            bidiSwitch,
                            "try:",
                            $"{Indent(1)}",
                            $"driver = webdriver.Chrome(service=ChromeService(ChromeDriverManager().install()), options=options, seleniumwire_options=sw_options{bidiOptionsParam})",
                            extensionsInstallString,
                            "except Exception as e:",
                            $"{Indent(1)}if 'cannot find Chrome binary' in str(e):",
                            $"{Indent(2)}stderr.write('Please install chrome and try compiling again.')",
                            $"{Indent(2)}exit(1)\n"
                        ]);
                        break;
                    }
                    scriptBody.AddRange
                    ([
                        "try:",
                        $"{Indent(1)}{experimentalChromeFlag}",
                        $"{Indent(1)}{bidiSwitch}",
                        $"{Indent(1)}driver = webdriver.Chrome(service=ChromeService(ChromeDriverManager().install()), seleniumwire_options=sw_options{bidiOptionsParam})",
                        $"{Indent(1)}{extensionsInstallString}",
                        "except Exception as e:",
                        $"{Indent(1)}if 'cannot find Chrome binary' in str(e):",
                        $"{Indent(2)}stderr.write('Please install chrome and try compiling again.')",
                        $"{Indent(2)}exit(1)\n"
                    ]);
                    break;

                case "firefox" or "safari":
                    if (disableSSL)
                    { 
                        scriptBody.AddRange([
                            "options = Options()",
                            "options.accept_insecure_certs = True",
                            "try:",
                            $"{Indent(1)}driver = webdriver.Firefox(",
                            $"{Indent(2)}service=FirefoxService(GeckoDriverManager().install())",
                            $"{Indent(2)}seleniumwire_options=sw_options,",
                            $"{Indent(1)})",
                            $"{Indent(1)}{extensionsInstallString}",
                            $"{Indent(1)}if 'cannot find Firefox binary' in str(e):\n",
                            $"{Indent(2)}stderr.write('Please install firefox and try running again.')",
                            $"{Indent(2)}exit(1)",
                        ]);
                    }
                    else
                    { // Uses SSL
                        scriptBody.AddRange([
                            "try:",
                            $"{Indent(1)}driver = webdriver.Firefox(service=FirefoxService(GeckoDriverManager().install()), seleniumwire_options=sw_options)",
                            $"{Indent(1)}{extensionsInstallString}",
                            "except Exception as e:",
                            $"{Indent(1)}if 'cannot find Firefox binary' in str(e):",
                            $"{Indent(2)}stderr.write('Please install firefox and try running again.')",
                            $"{Indent(2)}exit(1)\n",
                        ]);
                    }
                    break;
            }
            
            scriptBody.AddRange([
                "# Silently pass through since a maximized window isnt necessary",
                "try:",
                $"{Indent(1)}driver.maximize_window()",
                "except:",
                $"{Indent(1)}pass\n"
            ]);

            if (runHeadless)
            {
                // Runs browser in headless mode
                scriptBody.AddRange([
                    "driver.set_window_position(-5000, 0) # Sets the browser off the left of the primary display",
                    "print('Driver initialized.')\n\n"
                ]);
            }

            scriptBody.Add("make_request(url)");


            return (true, string.Empty);
        }
        public static (bool, string) WaitForSeconds(List<string> scriptBody, string[] splitLine, string sanitizedArg2)
        {
            bool waitTimeValidated = false;

            string rawTimeArg = sanitizedArg2;

            // Handles cases where the input value starts with a decimal
            if (rawTimeArg.StartsWith('.')) { rawTimeArg = $"0{rawTimeArg}"; }

            if (float.TryParse(rawTimeArg, out float waitTime))
            {
                scriptBody.AddRange([
                    @$"stdout.write('Pausing execution for: {waitTime} seconds.\n')",
                    $"sleep({waitTime})"
                ]);
                waitTimeValidated = true;
            }
            if (!waitTimeValidated)
            {
                return (false, $"Invalid argument '{splitLine[1]}'");
            }
            return (true, string.Empty);
        }
    }
}
