using System.Text.Json;

namespace BrowserAutomationMaster
{
    internal static class BrowserFunctions
    {

        public static JsonSerializerOptions options = new()
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
        };

#pragma warning disable // Disable warnings about these 2 functions starting with a lowercase letter
        public static string addHeaderFunction(string headerName, string headerValue) {
            Dictionary<string, string> header = new(){{headerName, headerValue}};

            return @$"driver.request_interceptor = lambda request: setattr(request, 'headers', {{
    **request.headers, " + @$"{{{JsonSerializer.Serialize(header, options)}}}".Replace("\"", "'").Replace("{", " ").Replace("}", " ").Trim() + "})"
    + string.Concat(Enumerable.Repeat('\n', 1));
        }
        
        public static string addHeadersFunction(Dictionary<string, string> headers)
        {

            return @$"driver.request_interceptor = lambda request: setattr(request, 'headers', {{
    **request.headers, " + @$"{{{JsonSerializer.Serialize(headers, options)}}}".Replace("\"", "'").Replace("{", " ").Replace("}", " ").Trim() + "})" 
    + string.Concat(Enumerable.Repeat('\n', 1));
        }

#pragma warning enable

        public static string AddUserAgentFunction(string userAgent) {
            return addHeaderFunction("User-Agent", userAgent);
        }

        public static string browserQuitCode = "stdout.write('Quitting driver...')\ndriver.quit()";

        public static string checkImportFunction = @"def check_import(name: str):
    module_name = name.split('==')[0].split('>=')[0].split('<=')[0].split('!=')[0].split('<')[0].split('>')[0].split('[')[0].strip()
    error_msg = f'Unable to find package: {module_name}, please ensure you its installed via:\npip install {name}'
    if module_name in modules:
        return True
        
    try:
        import_module(module_name)
        return True
    except:
        stderr.write(error_msg)
        return False" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string clickElementFunction = @"def click_element(byType: By, selector: str, actionTimeout: int):
    try:
        WebDriverWait(driver, actionTimeout).until(EC.element_to_be_clickable((byType, selector))).click()
    except NoSuchElementException:
        stderr.write(f'Unable to find element:', selector)
        exit(1)
    except Exception as e:
        stderr.write('An error occured while trying to click element with the selector:', selector, '\n\nError:\n',e)
        exit(1)" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string clickElementExperimentalFunction = $@"def click_element_experimental(selector: str, timeout: int = 10):
    driver.execute_script(f""""""let selector = '{{selector}}';
let element = document.querySelector(selector);
if (element) {{{{
  element.click();
}}}}
setTimeout(() => {{timeout*1000}});
""""""
)
    sleep(timeout)
";

        public static string getScreenBoundsFunction = @"def get_screen_bounds():
    try:
        result = driver.get_window_size()
        if ""width"" not in result.keys() or ""height"" not in result.keys():
            stderr.write(
                'Unable to determine screen boundaries of the current monitor.  '
                'you may see a portion of the browser while it executes.'
            )
            return None
        
        width = result[""width""]
        height = result[""height""]
        return [width, height]
    except:
        stderr.write(
            'Unable to determine screen boundaries of the current monitor.  '
            'You may see a portion of the browser while it executes.'
        )
        return None" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string getTextFunction = $@"def get_text_from_element(byType: By, selector: str, propertyName = 'value'):
    # propertyName is optional and will be overwritten if provided.
    try:
        text = driver.find_element(byType, selector).get_property(propertyName)
        return text
    except NoSuchElementException:
        stderr.write(f'Unable to find element:', selector)
        exit(1)
    except Exception as e:
        stderr.write('An error occured while trying to get text from element with the selector:', selector, '\n\nError:\n',e)
        exit(1)" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string fillTextFunction = @"def fill_text(byType: By, selector: str, value: str):
    try:
        element = driver.find_element(byType, selector)
        element.send_keys(value)
        return True
    except NoSuchElementException:
        stderr.write(f'Unable to find element:', selector)
        exit(1)
    except Exception as e:
        stderr.write(
            'An error occured while trying to fill text on element with the selector:',
            selector,
            '\n\nError:\n',
            e,
        )
        exit(1)" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string fillTextExperimentalFunction = @"def fill_text_exp(byType: By, selector: str, new_value: str, timeout: int = 10) -> bool:
    element: WebElement = None

    try:
        wait = WebDriverWait(driver, timeout)
        element = wait.until(EC.visibility_of_element_located((byType, selector)))
    except TimeoutException:
        stderr.write(f""Timed out while attempting to locate element: \n{selector}"")
        return False
    except Exception as e:
        stderr.write(f""Error finding element:\n{selector}\nError: {e}"")
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
                f""Verification failed: Expected '{expected_value}', got value={current_value}, text={current_text}'""
            )
            return False
        except StaleElementReferenceException:
            stderr.write(f""Unable to update stale element: {el.tag_name}."")
            return False
        except Exception as err:
            stderr.write(
                f""Unable to validate update status for element:\n{selector}\nError:{err}""
            )
            return False

    # ---> Method 1: element.clear() + element.send_keys() <---
    try:
        element.clear()
        element.send_keys(new_value)
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}."")
            return True
        stdout.write(f""Unable to fill text for element: {selector}\nAttempting Method 2.."")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {e}\n\nAttempting Method 2...""
        )
    
    # ---> Method 2: JavaScript arguments[0].textContent <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)
    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 3...""
        )
        return False
    
    try:
        driver.execute_script(""arguments[0].textContent = arguments[1];"", element, new_value)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""input"", { bubbles: true }));',
        #    element,
        #)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""change"", { bubbles: true }));',
        #    element,
        #)
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}"")
            return True
        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 3.."")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 3...""
        )

    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""input"", { bubbles: true }));',
        #    element,
        #)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""change"", { bubbles: true }));',
        #    element,
        #)
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}"")
            return True
        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4.."")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...""
        )
    
    # ---> Method 3: JavaScript arguments[0].value <---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)
    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 4...""
        )
        return False
    
    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""input"", { bubbles: true }));',
        #    element,
        #)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""change"", { bubbles: true }));',
        #    element,
        #)
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}"")
            return True
        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4.."")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...""
        )

    try:
        driver.execute_script(""arguments[0].value = arguments[1];"", element, new_value)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""input"", { bubbles: true }));',
        #    element,
        #)
        #driver.execute_script(
        #    'arguments[0].dispatchEvent(new Event(""change"", { bubbles: true }));',
        #    element,
        #)
        if verify_text_status(element, new_value):
            stdout.write(f""Successfully filled text for element: {selector}"")
            return True
        stderr.write(f""Unable to fill text for element: {selector}\nAttempting Method 4.."")
    except Exception as e:
        stderr.write(
            f""Unable to fill text for element:\n{selector}\nError:\n{e}\n\nAttempting Method 4...""
        )

    # --- Method 4: JavaScript arguments[0].innerText ---
    try:
        # Refetching isn't necessary but its a good idea because an element can become stale.
        element = driver.find_element(byType, selector)
    except Exception as err:
        stderr.write(
            f""Unable to fill text for element: {selector}\nError: {err}\n\nAttempting Method 3...""
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
            stdout.write(f""Successfully filled text for element: {selector}"")
            return True
        stderr.write(f""Unable to fill text for element: {selector}"")
        return False
    except Exception as e:
        stderr.write(f""An error occurred while attempting to fill:\n{selector}\nError:\n{e}"")
        return False" + string.Concat(Enumerable.Repeat("\n", 1));

        public static string installPackagesFunction = @"def install_packages():
    try:
        from pathlib import Path
        import platform
        current_file_directory = Path(__file__).parent.resolve()
        
        if platform.system() == ""Windows"":
            pip_executable = str(current_file_directory / ""venv"" / ""Scripts"" / ""pip.exe"")
        else:
            pip_executable = str(current_file_directory / ""venv"" / ""bin"" / ""pip"")
        requirements_filepath = str(current_file_directory / ""requirements.txt"")
    except:
        stderr.write(f'Unable to determine required values for package installation')
        exit(1)
    raw_package_names = []
    try:
        with open(requirements_filepath, 'r') as file:
            raw_package_names = file.read().splitlines()
    except:
        stderr.write(f'Unable to parse requirements.txt file, please ensure the following file is not actively being used:\n{requirements_filepath}')
        exit(1)
    
    package_names = [name.strip() for name in raw_package_names if name.strip() and not name.strip().startswith('#')]
    missing_packages = any(not check_import(package) for package in package_names)
    if not missing_packages:
        return True

    command = [
        pip_executable,
        ""install"",
        ""-r"",
        requirements_filepath,
    ]


    #command = ['pip', 'install', '-r', requirements_file]
    try:
        process = run(command, cwd=current_file_directory, capture_output=False, text=True, check=False)
        if process.returncode == 0:
            stdout.write('Required packages installed successfully.')
            if process.stderr:
                stderr.write(f'pip response:\n{process.stderr}')
            return True
        else:
            stderr.write(f'Error installing packages.')
            if process.stderr:
                stderr.write('Error:\n', process.stderr)
            return False
    except FileNotFoundError: # This exception occurs if 'pip' itself is not found
        stderr.write('pip command not found, Please make sure Python and pip are installed and in your system PATH.')
        return False
    except Exception as e:
        stderr.write(f'An unexpected error occurred while trying to run pip:\n{e}')
        return False" + string.Concat(Enumerable.Repeat('\n', 1));
        public static string makeRequestFunction(string userAgent)
        {
            string pythonSafeUserAgent = userAgent.Replace("\\", "\\\\").Replace("'", "\\'"); // Handles formatting before issues occur.
            return @"def make_request(url):
    status_code = None
    request_url = None
    final_url = None
" +
@"    try:
        stdout.write(f'Navigating to: {url}')
" +
@$"        {BrowserFunctions.AddUserAgentFunction(pythonSafeUserAgent)}"+
            @"        driver.get(url)
        final_url = driver.current_url
        stdout.write(f'Navigation complete. Final URL: {final_url}')
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
            stdout.write(f'Found status code {status_code} for request URL: {request_url}')
        else:
            stderr.write(f'WARNING: Could not find specific request for {final_url or url} in logs.')
            if driver.last_request and driver.last_request.response:
                stderr.write('Falling back to last request.')
                status_code = driver.last_request.response.status_code
                request_url = driver.last_request.url
            else:
                 stderr.write('No suitable request found.')
    except Exception as e:
        stderr.write(f'\n--- An error occurred ---')
        stderr.write(f'{type(e).__name__}: {e}')
        stderr.write(e)
        stderr.write('-------------------------\n')
    finally:
        if driver:
            if hasattr(driver, 'requests'):
                 del driver.requests
    stdout.write('\n--- Result  ---')
    stdout.write(f'Requested URL: {url}')
    if final_url and final_url != url:
        stdout.write(f'Final URL:     {final_url}')
    if status_code is not None:
        stdout.write(f'Request URL used for status: {request_url}')
        stdout.write(f'Detected Status Code: {status_code}')
        if status_code >= 400:
            stderr.write(f'Status {status_code} indicates an error has occured.')
        else:
            stdout.write(f'Status {status_code} indicates success/redirect.')
    else:
         stderr.write(f'Could not determine status code using selenium-wire.')" + string.Concat(Enumerable.Repeat('\n', 1));
        }

        public static string saveAsHTMLFunction = @"def save_as_html(filename: str):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'
    try:
        stdout.write('Saving page source as html, please wait...')
        html = driver.page_source
        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')
            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {filename}, please try again.')
                return False
        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)
        return True
    except Exception as e:
        stderr.write(f'Unable to save page source, please check the error below:\n\n{e}')
        return False" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string saveAsHTMLExperimentalFunction = @"def save_as_html_experimental(filename: str, timeout: int):
    if not filename.endswith('.html'):
        filename = 'pagesource.html'
    try:
        element_present = EC.presence_of_element_located((By.TAG_NAME, 'html'))
        WebDriverWait(driver, timeout).until(element_present)
    except Exception:
        stderr.write('Timed out waiting for page to load, please try increasing timeout.')
        return False

    try:
        html = driver.execute_script('return document.documentElement.outerHTML')
        if '<html' not in html:
            response = input('HTML tag not found in response, ignore and continue? [y/n]: ')
            if response.lower() != 'y':
                stderr.write(f'Unable to write page response to {filename}, please try again.')
                return False
        with open(filename, 'w', encoding='utf-8') as file:
            file.write(html)
        return True
    except Exception as e:
        stderr.write(f'Unable to write html to: {filename}, please check the error below:\n\n{e}')
        return False" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string selectElementFunction = @"def select_element(byType: By, selector: str, timeout: int):
    try:
        element = WebDriverWait(driver, timeout).until(EC.visibility_of_element_located((byType, selector)))
        return element
    except NoSuchElementException:
        stderr.write(f'Unable to find element:', selector)
        exit(1)
    except Exception as e:
        stderr.write(""An error occured while trying to get text from element with the selector:"", selector, ""\n\nError:\n"", e)
        exit(1)" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string selectOptionByIndexFunction = @"def select_option_by_index(
    byType: By,
    selector: str,
    index: int,
    timeout: int = 10
) -> bool:
    select_tag_element = select_element(byType, selector, timeout)
    if not select_tag_element:
        stderr.write(f""Standard <select> element not found using selector:\n{selector}"")
        return False

    if select_tag_element.tag_name.lower() != 'select':
        stderr.write(f""Element {selector} is not a <select> tag, found a <{select_tag_element.tag_name}> tag."")
        return False

    try:
        select_obj = Select(select_tag_element)
        select_obj.select_by_index(index)
        stdout.write(f""Selected option #{index+1} from {selector}."")
        return True
    except NoSuchElementException:
        stderr.write(f'Unable to find element:', selector)
        return False
    except Exception as e:
        stderr.write(f""Error selecting option #{index+1} (Index: {index}) from <select> tag with selector:\n'{selector}'\nError: {e}"")
        return False" + string.Concat(Enumerable.Repeat('\n', 1));

        public static string takeScreenshotFunction = @"def take_screenshot(filename: str):
    if not filename.endswith('.png'):
        filename = 'screenshot.png'
    try:
        stdout.write('Taking screenshot, please wait...')
        with open(f'{filename}', 'wb') as file:
            file.write(driver.get_screenshot_as_png())
    except Exception as e:
        stderr.write(f'Unable to take screenshot, please check the error below:\n\n{e}')" + string.Concat(Enumerable.Repeat('\n', 1));

    }
}
