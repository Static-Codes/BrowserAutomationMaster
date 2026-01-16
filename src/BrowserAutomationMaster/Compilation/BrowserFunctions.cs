using System.Text.Json;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Compilation.Transpiler; // Imported for Indent();
using static BrowserAutomationMaster.Managers.ConstantManager;

namespace BrowserAutomationMaster.Compilation
{
    internal class BrowserFunctions
    {

        public static JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
        };


        public static string? AddCookieFunction(string CookieName, string CookieValue)
        {
            try 
            {
                return $"driver.add_cookie({{'name' : '{CookieName}', 'value' : '{CookieValue}'}})";
            }
            catch (Exception ex)
            {
                Warning.Write($"Unable to add cookie object: {{'name' : '{CookieName}', 'value' : '{CookieValue}'}}{NLC}{ex.Message}");
                return null;
            }
        }

        public static string? AddHeaderFunction(string HeaderName, string HeaderValue) {
            Dictionary<string, string> header = new(){
                { HeaderName, HeaderValue }
            };

            try 
            {
                var jsonString = JsonSerializer.Serialize(header, options);
                var sanitizedJSON = jsonString
                    .Replace("\"", "'")
                    .Replace("{", " ")
                    .Replace("}", " ")
                    .Trim();

                var code =
                    "driver.request_interceptor = lambda request: setattr(" + "\n" +
                    Indent(3) + "request, " + "\n" +
                    Indent(3) + "'headers', " + "\n" +
                    Indent(3) + "{" + "\n" +
                    Indent(4) + "**request.headers, " + "\n" +
                    Indent(4) + "'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0'," + "\n" +
                    Indent(3) + "}," + "\n" +
                    Indent(2) + ")" + "\n\n";

                //$"\ndriver.request_interceptor = lambda request: setattr(request, 'headers', {{\n{Indent(1)}" +
                //$"**request.headers, {sanitizedJSON}}})\n\n";

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
                Indent(1) + $"**request.headers, {sanitized}}})" + "\n\n";

            return headerCode; 
                
        }



        public static string? AddUserAgentFunction(string userAgent) {
            return AddHeaderFunction("User-Agent", userAgent);
        }


        public static string browserQuitCode = 
            "stdout.write('Quitting driver...')" + "\n" +
            "driver.quit()" + "\n\n";



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
    return isClicked" + "\n\n\n";



        public static string clickElementFunction = @"def click_element(byType: By, selector: str, actionTimeout: int):
    try:
        WebDriverWait(driver, actionTimeout).until(EC.element_to_be_clickable((byType, selector))).click()
    except NoSuchElementException:
        stderr.write(f'Unable to find element: ' + selector + '\n')
        exit(1)
    except Exception as e:
        stderr.write('An error occured while trying to click element with the selector: ' + selector + '\n\nError:\n' + str(e) + '\n')
        exit(1)" + "\n\n\n";



        public static string clickElementExperimentalFunction = 
            "def click_element_experimental(selector: str, timeout: int = 10):" + "\n" +
            Indent(1) + "driver.execute_script(" + "\n" +
            Indent(2) + "f'''let selector = '{{selector}}" + "\n" +
            Indent(3) + "let element = document.querySelector(selector);" + "\n" +
            Indent(3) + "if (element) {{" + "\n" +
            Indent(4) + "element.click();" + "\n" +
            Indent(3) + "}}" + "\n" +
            Indent(3) + "setTimeout(() => {{timeout*1000}});" + "\n" +
            Indent(2) + "'''" + "\n" +
            Indent(1) + ")" + "\n" +
            Indent(1) + "sleep(timeout)" + '\n';




        public static string closeCurrentTabFunction = $@"def close_current_tab():
    current_url = None

    try:
        current_url = driver.current_url

        initial_window_handles = driver.window_handles

        # The index of the current window will be used below.
        current_window_handle = driver.current_window_handle
        current_window_index = initial_window_handles.index(current_window_handle)

        stdout.write(
            f""\nClosing tab with URL: {{current_url}}\n""
            f""Current window handle: {{current_window_handle}} (Index: {{current_window_index}})\n\n""
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
                    f""URL: {{driver.current_url}}\n\n""
                )
                return  # Func killed here

            # Switch to previous handle since its still alive
            driver.switch_to.window(previous_tab_handle)
            stdout.write(
                f""Switched back to tab with handle: {{previous_tab_handle}}\n""
                f""URL: {{driver.current_url}}\n\n""
            )

        # Case 2: We closed the first tab (index 0) and others exist
        elif len(updated_window_handles) > 0:
            driver.switch_to.window(updated_window_handles[0])
            stdout.write(
                ""Closed first tab.""
                f""Switched to new first tab: {{updated_window_handles[0]}}, ""
                f""URL: {{driver.current_url}}\n""
            )


    except Exception as e:
        stderr.write(
            f""Unable to close the current tab.\n""
            f""Tab URL (before error): {{current_url}}\n""
            f""Exception Type: {{type(e).__name__}}\n""  # More readable type name
            f""Error:\n{{str(e)}}\n""
        )" + '\n';
        


        public static string getScreenBoundsFunction = @"def get_screen_bounds():
    try:
        result = driver.get_window_size()

        if ""width"" not in result.keys() or ""height"" not in result.keys():
            stderr.write(
                'Unable to determine screen boundaries of the current monitor.  '
                'you may see a portion of the browser while it executes.\n'
            )
            return None
        
        width = result[""width""]
        height = result[""height""]
        return [width, height]

    except:
        stderr.write(
            'Unable to determine screen boundaries of the current monitor.  '
            'You may see a portion of the browser while it executes.\n'
        )
        return None" + '\n';



        public static string getTextFunction = $@"def get_text_from_element(byType: By, selector: str, propertyName = 'value'):
    # propertyName is optional and will be overwritten if provided.
    try:
        text = driver.find_element(byType, selector).get_property(propertyName)
        return text

    except NoSuchElementException:
        stderr.write(f'Unable to find element: ' + selector + '\n')
        exit(1)

    except Exception as e:
        stderr.write('An error occured while trying to get text from element with the selector: ' + selector + '\n\nError:\n' + str(e) + '\n')
        exit(1)" + '\n';



        public static string fillTextFunction = @"def fill_text(byType: By, selector: str, value: str):
    try:
        element = driver.find_element(byType, selector)
        element.send_keys(value)
        return True

    except NoSuchElementException:
        stderr.write(f'Unable to find element: ' + selector + '\n')
        exit(1)

    except Exception as e:
        stderr.write('An error occured while trying to fill text on element with the selector: ' + selector + '\n\nError:\n' + str(e) + '\n')
        exit(1)" + '\n';



        public static string fillTextExperimentalFunction = @"def fill_text_exp(byType: By, selector: str, new_value: str, timeout: int = 10) -> bool:
    element: WebElement = None

    try:
        wait = WebDriverWait(driver, timeout)
        element = wait.until(EC.visibility_of_element_located((byType, selector)))

    except TimeoutException:
        stderr.write(f""Timed out while attempting to locate element: \n{selector}\n"")
        return False

    except Exception as e:
        stderr.write(f""Error finding element:\n{selector}\nError: {e}\n"")
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
                f""Verification failed: Expected '{expected_value}', got value={current_value}, text={current_text}'\n""
            )
            return False

        except StaleElementReferenceException:
            stderr.write(f""Unable to update stale element: {el.tag_name}.\n"")
            return False

        except Exception as err:
            stderr.write(
                f""Unable to validate update status for element:\n{selector}\nError:{err}\n""
            )
            return False

    # ---> Method 1: element.clear() + element.send_keys() <---
    try:
        element.clear()
        element.send_keys(new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}.\n"")
            return True

        stdout.write(f""Unable to fill text for element: {selector}\nAttempting Method 2..\n"")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {e}\n\nAttempting Method 2...\n""
        )
    
    # ---> Method 2: JavaScript arguments[0].textContent <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 3...\n""
        )

        return False
    
    try:
        driver.execute_script(""arguments[0].textContent = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}\n"")
            return True

        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 3..\n"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 3...\n""
        )

    try:
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}\n"")
            return True

        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4..\n"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...\n""
        )
    
    # ---> Method 3: JavaScript arguments[0].value <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 4...\n""
        )
        return False
    
    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}\n"")
            return True

        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4..\n"")

    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...\n""
        )

    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}\n"")
            return True

        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4..\n"")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...\n""
        )

    # --- Method 4: JavaScript arguments[0].innerText ---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)

    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 3...\n""
        )
        return False

    try:
        driver.execute_script(
            ""arguments[0].innerText = arguments[1];"", element, new_value
        )

        driver.execute_script(
            'arguments[0].dispatchEvent(new Event(""input"", { bubbles: true }));',
            element,
        )
        driver.execute_script(
            'arguments[0].dispatchEvent(new Event(""change"", { bubbles: true }));',
            element,
        )

        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}\n"")
            return True

        stderr.write(f""Unable to fill text for element: {selector}\n"")
        return False

    except Exception as e:
        stderr.write(f""An error occurred while attempting to fill:\n{selector}\nError:\n{e}\n"")
        return False" + '\n';


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
        stdout.write(f'Navigating to: {url}\n')

" +
@$"        {AddUserAgentFunction(pythonSafeUserAgent)}"+
            @"        driver.get(url)
        final_url = driver.current_url
        stdout.write(f'Navigation complete. Final URL: {final_url}\n')
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
            stdout.write(f'Found status code {status_code} for request URL: {request_url}\n')

        else:
            stderr.write(f'WARNING: Could not find specific request for {final_url or url} in logs.\n')
            if driver.last_request and driver.last_request.response:
                stderr.write('Falling back to last request.\n')
                status_code = driver.last_request.response.status_code
                request_url = driver.last_request.url

            else:
                 stderr.write('No suitable request found.\n')

    except Exception as e:
        stderr.write(f'\n--- An error occurred ---\n')
        stderr.write(f'{type(e).__name__}: {e}\n')
        stderr.write(str(e) + '\n')
        stderr.write('-------------------------\n')

    finally:
        if driver:
            if hasattr(driver, 'requests'):
                 del driver.requests

    stdout.write('\n--- Result  ---\n')
    stdout.write(f'Requested URL: {url}\n')

    if final_url and final_url != url:
        stdout.write(f'Final URL:     {final_url}\n')

    if status_code is not None:
        stdout.write(f'Request URL used for status: {request_url}\n')
        stdout.write(f'Detected Status Code: {status_code}\n')
        if status_code >= 400:
            stderr.write(f'Status {status_code} indicates an error has occured.\n')

        else:
            stdout.write(f'Status {status_code} indicates success/redirect.\n')

    else:
         stderr.write(f'Could not determine status code using selenium-wire.\n')" + '\n';
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
        stderr.write(f'Unable to open a new tab.\nException Type: {{type(e)}}\nError:\n{{str(e)}}')" + '\n';



        public static string saveAsHTMLFunction = @"def save_as_html(filename: str):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'

    try:
        stdout.write('Saving page source as html, please wait...\n')
        html = driver.page_source

        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')

            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {filename}, please try again.\n')
                return False

        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)

        return True
    except Exception as e:
        stderr.write(f'Unable to save page source, please check the error below:\n\n{e}\n')
        return False" + '\n';



        public static string saveAsHTMLExperimentalFunction = @"def save_as_html_experimental(filename: str, timeout: int):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'

    try:
        element_present = EC.presence_of_element_located((By.TAG_NAME, 'html'))
        WebDriverWait(driver, timeout).until(element_present)

    except Exception:
        stderr.write('Timed out waiting for page to load, please try increasing timeout.\n')
        return False

    try:
        html = driver.execute_script('return document.documentElement.outerHTML')
        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')

            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {filename}, please try again.\n')
                return False

        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)

        return True

    except Exception as e:
        stderr.write(f'Unable to write html to: {filename}, please check the error below:\n\n{e}\n')
        return False" + '\n';



        public static string selectElementFunction = @"def select_element(byType: By, selector: str, timeout: int):
    try:
        element = WebDriverWait(driver, timeout).until(EC.visibility_of_element_located((byType, selector)))
        return element

    except NoSuchElementException:
        stderr.write(f'Unable to find element: ' + selector +  '\n')
        exit(1)

    except Exception as e:
        stderr.write(""An error occured while trying to get text from element with the selector: "" + selector + ""\n\nError:\n"" + str(e) + ""\n"");
        exit(1)" + '\n';



        public static string selectOptionByIndexFunction = @"def select_option_by_index(
    byType: By,
    selector: str,
    index: int,
    timeout: int = 10
) -> bool:
    optionNumber = index + 1
    select_tag_element = select_element(byType, selector, timeout)

    if not select_tag_element:
        stderr.write(f""Standard <select> element not found using selector:\n{selector}\n"")
        return False

    if select_tag_element.tag_name.lower() != 'select':
        stderr.write(f""Element {selector} is not a <select> tag, found a <{select_tag_element.tag_name}> tag.\n"")
        return False

    try:
        select_obj = Select(select_tag_element)
        select_obj.select_by_index(index)
        stdout.write(f""Selected option {optionNumber} from {selector}.\n"")
        return True

    except NoSuchElementException:
        stderr.write(f'Unable to find element: {selector}\n')
        return False

    except Exception as e:
        stderr.write(f""Error selecting option {optionNumber} (Index: {index}) from <select> tag with selector:\n'{selector}'\nError: {e}\n"")
        return False" + '\n';


        public static string takeScreenshotFunction = @"def take_screenshot(filename: str):
    if not filename.endswith('.png'):
        filename = 'screenshot.png'

    try:
        stdout.write('Taking screenshot, please wait...\n')
        with open(f'{filename}', 'wb') as file:
            file.write(driver.get_screenshot_as_png())

    except Exception as e:
        stderr.write(f'Unable to take screenshot, please check the error below:\n\n{e}\n')" + '\n';
        
    }
}
