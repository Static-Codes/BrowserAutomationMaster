using System.Text.Json;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Compilation
{
    internal class BrowserFunctions
    {

        public static JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
        };


        public static string Indent(int numberOfIndents)
        {
            if (numberOfIndents < 0)
            {
                WriteAndExit(
                    message: "Invalid value provided to Indent(), value must be >= 0.",
                    status: 1
                );
            }
            if (numberOfIndents == 0) { 
                return string.Empty; 
            } // Return an empty string if no indentations are needed.



            string pythonIndent = "    "; // PEP 8 standard (4 spaces = 1 tab)
            return string.Concat(
                Enumerable.Repeat(pythonIndent, numberOfIndents)
            );
        }

        public static string AddCookieFunction(string CookieName, string CookieValue)
        {
            return $"{Indent(2)}driver.add_cookie({{'name' : '{CookieName}', 'value' : '{CookieValue}'}})";
        }

        public static string? AddHeaderFunction(string HeaderName, string HeaderValue) {
            Dictionary<string, string> header = new(){
                { HeaderName, HeaderValue },
            };

            var hasUserAgent = header.Keys.Any(k => k.Equals("User-Agent", StringComparison.OrdinalIgnoreCase));
            
            if (!hasUserAgent)
            {
                header["User-Agent"] = DEFAULT_USER_AGENT;
            }

            try 
            {
                var jsonString = JsonSerializer.Serialize(header, options);
                var sanitizedJSON = jsonString
                    .Replace("\"", "'")
                    .Replace("{", " ")
                    .Replace("}", " ")
                    .Trim();

                var code =
                    "driver.request_interceptor = lambda request: setattr(" + NLC +
                    Indent(3) + "request, " + NLC +
                    Indent(3) + "'headers', " + NLC +
                    Indent(3) + "{" + NLC +
                    Indent(4) + "**request.headers, " + NLC +
                    Indent(4) + sanitizedJSON + "," + NLC +
                    Indent(3) + "}," + NLC +
                    Indent(2) + ")" + Enumerable.Repeat(NLC, 2);

                return code;
            }
            catch
            {
                return null;
            }
        }
        public static string AddHeadersFunction(Dictionary<string, string> headers)
        {
            if (headers == null || headers.Count == 0)
                return "# Unable to add headers using 'add-headers' command";

            var sanitized = @$"{{{JsonSerializer.Serialize(headers, options)}}}"
                .Replace("\"", "'")
                .Replace("{", " ")
                .Replace("}", " ")
                .Trim();
            
            var headerCode =
                "driver.request_interceptor = lambda request: setattr(request, 'headers', {{" +
                Indent(1) + $"**request.headers, **{sanitized}}})" + Enumerable.Repeat(NLC, 2);

            return headerCode; 
                
        }



        public static string? AddUserAgentFunction(string userAgent) {
            return AddHeaderFunction("User-Agent", userAgent);
        }


        public static string browserQuitCode = 
            $"stdout.write('Quitting driver...{eNLC}')" + NLC + 
            "driver.quit()" + Enumerable.Repeat(NLC, 2);



        // Forked from https://pypi.org/project/a-selenium-click-on-coords/ under the MIT License.
        public static string clickAtPositionFunction = @$"def click_at_position(x: int, y: int, script_timeout=10):
    isClicked = False
    try:
        old_timeout = driver.__dict__[""caps""][""timeouts""][""script""]
        driver.set_script_timeout(script_timeout)
        isClicked = driver.execute_script(
            rf""""""var simulateMouseEvent = function(element, eventName, coordX, coordY) {{{{
          element.dispatchEvent(new MouseEvent(eventName, {{{{
            view: window,
            bubbles: true,
            cancelable: true,
            clientX: coordX,
            clientY: coordY,
            button: 0
          }}}}));
        }}}};
        var theElement = document.elementFromPoint({{x}}, {{y}});
        coordX = {{x}},
        coordY = {{y}};
        simulateMouseEvent (theElement, ""mousedown"", coordX, coordY);
        simulateMouseEvent (theElement, ""mouseup"", coordX, coordY);
        simulateMouseEvent (theElement, ""click"", coordX, coordY);
        return theElement;""""""
        )
    finally:
        driver.set_script_timeout(old_timeout)
    return isClicked" + Enumerable.Repeat(NLC, 3);



        public static string clickElementFunction = $@"def click_element(byType: By, selector: str, actionTimeout: int):
    try:
        WebDriverWait(driver, actionTimeout).until(EC.element_to_be_clickable((byType, selector))).click()
    except NoSuchElementException:
        stderr.write(f'Unable to find element: {{selector}}{eNLC}')
        exit(1)
    except Exception as e:
        stderr.write(f'An error occured while trying to click element with the selector: {{selector}}{eNLC}{eNLC}Error:{eNLC}{{str(e)}}{eNLC}')
        exit(1)" + Enumerable.Repeat(NLC, 3);



        public static string clickElementExperimentalFunction = 
            "def click_element_experimental(selector: str, timeout: int = 10):" + NLC +
            Indent(1) + "driver.execute_script(" + NLC +
            Indent(2) + "f'''let selector = '{{selector}}" + NLC +
            Indent(3) + "let element = document.querySelector(selector);" + NLC +
            Indent(3) + "if (element) {{" + NLC +
            Indent(4) + "element.click();" + NLC +
            Indent(3) + "}}" + NLC +
            Indent(3) + "setTimeout(() => {{timeout*1000}});" + NLC +
            Indent(2) + "'''" + NLC +
            Indent(1) + ")" + NLC +
            Indent(1) + "sleep(timeout)" + NLC;




        public static string closeCurrentTabFunction = $@"def close_current_tab():
    current_url = None

    try:
        current_url = driver.current_url

        initial_window_handles = driver.window_handles

        # The index of the current window will be used below.
        current_window_handle = driver.current_window_handle
        current_window_index = initial_window_handles.index(current_window_handle)

        stdout.write(
            f""{eNLC}Closing tab with URL: {{current_url}}{eNLC}""
            f""Current window handle: {{current_window_handle}} (Index: {{current_window_index}}){Enumerable.Repeat(eNLC, 2)}""
        )

        # Close the current tab (current_window_handle)
        if len(initial_window_handles) == 1:
            driver.quit()  # driver.close will cause memory leaks with only 1 tab
            return

        else:
            driver.close()

        updated_window_handles = driver.window_handles

        # Case 1: Any tab except the first tab was closed.
        if current_window_index > 0:
            previous_tab_handle = initial_window_handles[current_window_index - 1]

            # Ensure the previous handle is still alive
            if previous_tab_handle not in updated_window_handles:

                # If the previous handle has gone stale, switching back to the first available.
                driver.switch_to.window(updated_window_handles[0])
                stdout.write(
                    ""Warning: Previous handle not found. ""
                    f""Switched to first available tab: {{updated_window_handles[0]}}, ""
                    f""URL: {{driver.current_url}}{Enumerable.Repeat(eNLC, 2)}""
                )
                return  # Func killed here

            # Switch to previous handle since its still alive
            driver.switch_to.window(previous_tab_handle)
            stdout.write(
                f""Switched back to tab with handle: {{previous_tab_handle}}{eNLC}""
                f""URL: {{driver.current_url}}{Enumerable.Repeat(eNLC, 2)}""
            )

        # Case 2: We closed the first tab (index 0) and others exist
        elif len(updated_window_handles) > 0:
            driver.switch_to.window(updated_window_handles[0])
            stdout.write(
                ""Closed first tab.""
                f""Switched to new first tab: {{updated_window_handles[0]}}, ""
                f""URL: {{driver.current_url}}{eNLC}""
            )


    except Exception as e:
        stderr.write(
            f""Unable to close the current tab.{eNLC}""
            f""Tab URL (before error): {{current_url}}{eNLC}""
            f""Exception Type: {{type(e).__name__}}{eNLC}""  # More readable type name
            f""Error:{eNLC}{{str(e)}}{eNLC}""
        )" + NLC;
        


        public static string getScreenBoundsFunction = $@"def get_screen_bounds():
    try:
        result = driver.get_window_size()

        if ""width"" not in result.keys() or ""height"" not in result.keys():
            stderr.write(
                'Unable to determine screen boundaries of the current monitor.  '
                'you may see a portion of the browser while it executes.{eNLC}'
            )
            return None
        
        width = result[""width""]
        height = result[""height""]
        return [width, height]

    except:
        stderr.write(
            'Unable to determine screen boundaries of the current monitor.  '
            'You may see a portion of the browser while it executes.{eNLC}'
        )
        return None" + NLC;



        public static string getTextFunction = $@"def get_text_from_element(byType: By, selector: str, propertyName = 'value'):
    # propertyName is optional and will be overwritten if provided.
    try:
        text = driver.find_element(byType, selector).get_property(propertyName)
        return text

    except NoSuchElementException:
        stderr.write(f'Unable to find element: {{selector}}')
        stderr.write('{eNLC}')
        exit(1)

    except Exception as e:
        stderr.write('An error occured while trying to get text from element with the selector: {{selector}}{eNLC}{eNLC}Error:{eNLC}{{str(e)}}{eNLC}')
        exit(1)" + NLC;



        public static string fillTextFunction = $@"def fill_text(byType: By, selector: str, value: str):
    try:
        element = driver.find_element(byType, selector)
        element.send_keys(value)
        return True

    except NoSuchElementException:
        stderr.write(f'Unable to find element: ' + selector + '{eNLC}')
        exit(1)

    except Exception as e:
        stderr.write('An error occured while trying to fill text on element with the selector: {{selector}}{eNLC}{eNLC}Error:{eNLC}{{str(e)}}{eNLC}')
        exit(1)" + NLC;



        public static string fillTextExperimentalFunction = $@"def fill_text_exp(byType: By, selector: str, new_value: str, timeout: int = 10) -> bool:
    element: WebElement = None

    try:
        wait = WebDriverWait(driver, timeout)
        element = wait.until(EC.visibility_of_element_located((byType, selector)))

    except TimeoutException:
        stderr.write(f""Timed out while attempting to locate element:{eNLC}{{selector}}{eNLC}"")
        return False

    except Exception as e:
        stderr.write(f""Error finding element:{eNLC}{{selector}}{eNLC}Error: {{e}}{eNLC}"")
        return False

    # Inline function for simplicity
    def verify_text_status(el: WebElement, expected_value: str) -> bool:
        try:
            # For <input> and <textarea> elements, the 'value' attribute is used.
            current_value = el.get_attribute(""value"")

            if current_value == expected_value:
                return True

            # For other elements, both 'innerText' and '.text' are tried.
            current_text = el.text

            if current_text == expected_value:
                return True

            else:
                current_text = el.get_attribute(""innerText"")
                if current_text == expected_value:
                    return True

                else:
                    current_text = el.get_attribute(""textContent"")
                    if current_text == expected_value:
                        return True

            stderr.write(
                f""Verification failed: Expected '{{expected_value}}', got value={{current_value}}, text={{current_text}}'{eNLC}""
            )
            return False

        except StaleElementReferenceException:
            stderr.write(f""Unable to update stale element: {{el.tag_name}}.{eNLC}"")
            return False

        except Exception as err:
            stderr.write(
                f""Unable to validate update status for element:{eNLC}{{selector}}{eNLC}Error:{{err}}{eNLC}""
            )
            return False

    # ---> Method 1: element.clear() + element.send_keys() <---
    try:
        element.clear()
        element.send_keys(new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}.{eNLC}"")
            return True

        stdout.write(f""Unable to fill text for element: {{selector}}{eNLC}Attempting Method 2..{eNLC}"")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element: {{selector}}{eNLC}Error: {{e}}{eNLC}{eNLC}Attempting Method 2...{eNLC}""
        )
    
    # ---> Method 2: JavaScript arguments[0].textContent <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {{selector}}{eNLC}Error: {{err}}{eNLC}{eNLC}Attempting Method 3...{eNLC}""
        )

        return False
    
    try:
        driver.execute_script(""arguments[0].textContent = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}{eNLC}"")
            return True

        stderr.write(f""Unable to fill text for element: {{selector}}{eNLC}Attempting Method 3..{eNLC}"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:{eNLC}{{selector}}{eNLC}Error:{eNLC}{{e}}{eNLC}{eNLC}Attempting Method 3...{eNLC}""
        )

    try:
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}{eNLC}"")
            return True

        stderr.write(f""Unable to fill text for element: {{selector}}{eNLC}Attempting Method 4..{eNLC}"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:{eNLC}{{selector}}{eNLC}Error:{eNLC}{{e}}{eNLC}{eNLC}Attempting Method 4...{eNLC}""
        )
    
    # ---> Method 3: JavaScript arguments[0].value <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {{selector}}{eNLC}Error: {{err}}{eNLC}{eNLC}Attempting Method 4...{eNLC}""
        )
        return False
    
    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}{eNLC}"")
            return True

        stderr.write(f""Unable to fill text for element: {{selector}}{eNLC}Attempting Method 4..{eNLC}"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:{eNLC}{{selector}}{eNLC}Error:{eNLC}{{e}}{eNLC}{eNLC}Attempting Method 4...{eNLC}""
        )

    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}{eNLC}"")
            return True

        stderr.write(f""Unable to fill text for element: {{selector}}{eNLC}Attempting Method 4..{eNLC}"")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:{eNLC}{{selector}}{eNLC}Error:{eNLC}{{e}}{eNLC}{eNLC}Attempting Method 4...{eNLC}""
        )

    # --- Method 4: JavaScript arguments[0].innerText ---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {{selector}}{eNLC}Error: {{err}}{eNLC}Attempting Method 4...{eNLC}""
        )
        return False

    try:
        driver.execute_script(
            ""arguments[0].innerText = arguments[1];"", element, new_value
        )

        driver.execute_script(
            'arguments[0].dispatchEvent(new Event(""input"", {{ bubbles: true }}));',
            element,
        )
        driver.execute_script(
            'arguments[0].dispatchEvent(new Event(""change"", {{ bubbles: true }}));',
            element,
        )

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {{selector}}{eNLC}"")
            return True

        stderr.write(f""Unable to fill text for element: {{selector}}{eNLC}"")
        return False

    except Exception as e:
        stderr.write(f""An error occurred while attempting to fill:{eNLC}{{selector}}{eNLC}Error:{eNLC}{{e}}{eNLC}"")
        return False" + NLC;


        public static string MakeRequestFunction(string userAgent)
        {
            string pythonSafeUserAgent = userAgent.Replace("\\", "\\\\").Replace("'", "\\'"); // Handles formatting before issues occur.
            return @"def make_request(url):
    if (driver is None):
        stderr.write('Unable to initialize selenium instance.')
        exit(1)
    
    status_code = None
    request_url = None
    final_url = None
" +
@"    try:
        stdout.write(f'Navigating to: {url}{eNLC}')

" +
@$"        {AddUserAgentFunction(pythonSafeUserAgent)}"+
            $@"        driver.get(url)
        final_url = driver.current_url
        stdout.write(f'Navigation complete. Final URL: {{final_url}}{eNLC}')
        target_request = None


        for request in reversed(driver.requests or []):
            if request.response and (request.url == final_url or request.url == url):
                if request.url == final_url:
                    target_request = request
                    break
                if not target_request:
                    target_request = request


        if target_request:
            status_code = target_request.response.status_code
            request_url = target_request.url
            stdout.write(f'Found status code {{status_code}} for request URL: {{request_url}}{eNLC}')

        else:
            stderr.write(f'WARNING: Could not find specific request for {{final_url}} or {{url}} in logs.{eNLC}')
            if driver.last_request and driver.last_request.response:
                stderr.write('Falling back to last request.{eNLC}')
                status_code = driver.last_request.response.status_code
                request_url = driver.last_request.url

            else:
                 stderr.write('No suitable request found.{eNLC}')

    except Exception as e:
        stderr.write(f'{eNLC}--- An error occurred ---{eNLC}')
        stderr.write(f'{{type(e).__name__}}: {{e}}{eNLC}')
        stderr.write(str(e) + '{eNLC}')
        stderr.write('-------------------------{eNLC}')

    finally:
        if driver:
            if hasattr(driver, 'requests'):
                 del driver.requests

    stdout.write('{eNLC}--- Result  ---{eNLC}')
    stdout.write(f'Requested URL: {{url}}{eNLC}')

    if final_url and final_url != url:
        stdout.write(f'Final URL: {{final_url}}{eNLC}')

    if status_code is not None:
        stdout.write(f'Request URL used for status: {{request_url}}{eNLC}')
        stdout.write(f'Detected Status Code: {{status_code}}{eNLC}')
        if status_code >= 400:
            stderr.write(f'Status {{status_code}} indicates an error has occured.{eNLC}')

        else:
            stdout.write(f'Status {{status_code}} indicates success/redirect.{eNLC}')

    else:
         stderr.write(f'Could not determine status code using selenium-wire.{eNLC}')" + NLC;
        }


        
        public static string openNewTabFunction = @$"def open_new_tab(url: str, timeout: int):
    try:
        driver.set_page_load_timeout = timeout
        initial_window_handles = driver.window_handles

        original_window_handle = driver.current_window_handle
        desired_window_index = len(initial_window_handles) + 1

        driver.switch_to.new_window(""tab"")
        WebDriverWait(driver, 0.3).until(
            EC.number_of_windows_to_be(desired_window_index)
        )

        new_window = driver.current_window_handle
        driver.get(url)
        return new_window, original_window_handle
    except Exception as e:
        stderr.write(f'Unable to open a new tab.{eNLC}Exception Type: {{type(e)}}{eNLC}Error:{eNLC}{{str(e)}}')" + NLC;



        public static string saveAsHTMLFunction = $@"def save_as_html(filename: str):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'

    try:
        stdout.write('Saving page source as html, please wait...{eNLC}')
        html = driver.page_source

        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')

            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {{filename}}, please try again.{eNLC}')
                return False

        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)

        return True
    except Exception as e:
        stderr.write(f'Unable to save page source, please check the error below:{eNLC}{{e}}{eNLC}')
        return False" + NLC;



        public static string saveAsHTMLExperimentalFunction = $@"def save_as_html_experimental(filename: str, timeout: int):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'

    try:
        element_present = EC.presence_of_element_located((By.TAG_NAME, 'html'))
        WebDriverWait(driver, timeout).until(element_present)

    except Exception:
        stderr.write('Timed out waiting for page to load, please try increasing timeout.{eNLC}')
        return False

    try:
        html = driver.execute_script('return document.documentElement.outerHTML')
        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')

            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {{filename}}, please try again.{eNLC}')
                return False

        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)

        return True

    except Exception as e:
        stderr.write(f'Unable to write html to: {{filename}}, please check the error below:{eNLC}{eNLC}{{e}}{eNLC}')
        return False" + NLC;



        public static string selectElementFunction = $@"def select_element(byType: By, selector: str, timeout: int):
    try:
        element = WebDriverWait(driver, timeout).until(EC.visibility_of_element_located((byType, selector)))
        return element

    except NoSuchElementException:
        stderr.write(f'Unable to find element: {{selector}}{eNLC}')
        exit(1)

    except Exception as e:
        stderr.write(f""An error occured while trying to get text from element with the selector: {{selector}}{eNLC}{eNLC}Error:{eNLC}{{str(e)}}{eNLC}"");
        exit(1)" + NLC;



        public static string selectOptionByIndexFunction = $@"def select_option_by_index(
    byType: By,
    selector: str,
    index: int,
    timeout: int = 10
) -> bool:
    optionNumber = index + 1
    select_tag_element = select_element(byType, selector, timeout)

    if not select_tag_element:
        stderr.write(f""Standard <select> element not found using selector:{eNLC}{{selector}}{eNLC}"")
        return False

    if select_tag_element.tag_name.lower() != 'select':
        stderr.write(f""Element {{selector}} is not a <select> tag, found a <{{select_tag_element.tag_name}}> tag.{eNLC}"")
        return False

    try:
        select_obj = Select(select_tag_element)
        select_obj.select_by_index(index)
        stdout.write(f""Selected option {{optionNumber}} from {{selector}}.{eNLC}"")
        return True

    except NoSuchElementException:
        stderr.write(f'Unable to find element: {{selector}}{eNLC}')
        return False

    except Exception as e:
        stderr.write(f""Error selecting option {{optionNumber}} (Index: {{index}}) from <select> tag with selector:{eNLC}'{{selector}}'{eNLC}Error: {{e}}{eNLC}"")
        return False" + NLC;


        public static string takeScreenshotFunction = $@"def take_screenshot(filename: str):
    if not filename.endswith('.png'):
        filename = 'screenshot.png'

    try:
        stdout.write('Taking screenshot, please wait...{eNLC}')
        with open(f'{{filename}}', 'wb') as file:
            file.write(driver.get_screenshot_as_png())

    except Exception as e:
        stderr.write(f'Unable to take screenshot, please check the error below:{eNLC}{eNLC}{{e}}{eNLC}')" + NLC;
        
    }
}
