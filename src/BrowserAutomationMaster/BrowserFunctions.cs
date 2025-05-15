using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserAutomationMaster
{
    internal static class BrowserFunctions
    {
        public static string browserQuitCode = "print('Quitting driver...')\ndriver.quit()";

        public static string clickElementFunction = @"def click_element(byType: By, selector: str, actionTimeout: int):
    try:
        WebDriverWait(driver, actionTimeout).until(EC.element_to_be_clickable((byType, selector))).click()
    except Exception as e:
        print('An error occured while trying to click element with the selector:', selector, '\n\nError:\n',e)" + string.Concat(Enumerable.Repeat('\n', 3));

        public static string clickElementExperimentalFunction = $@"def click_element_experimental(selectorType: str, selector: str):
    byType = By.CSS_SELECTOR if selectorType == 'css' else By.XPATH
    try:
        WebDriverWait(driver, 10).until(EC.element_to_be_clickable((byType, selector))).click()
    except Exception as e:
        print('An error occured while trying to click element with the selector:', selector, '\n\nError:\n',e)" + string.Concat(Enumerable.Repeat('\n', 3));

        public static string getTextFromElementFunction = $@"def get_text_from_element(byType: By, selector: str, propertyName = 'value'):
    # propertyName is optional and will be overwritten if provided.
    try:
        text = driver.find_element(byType, selector).get_property(propertyName)
        return text
    except Exception as e:
        print('An error occured while trying to get text from element with the selector:', selector, '\n\nError:\n',e)
        return None" + string.Concat(Enumerable.Repeat('\n', 3));

        public static string fillTextFunction = @"def fill_text(byType: By, selector: str, value: str):
    try:
        element = driver.find_element(byType, selector)
        driver.execute_script(f'arguments[0].innerText = ""{value}""', element)
        return True
    except Exception as e:
        print('An error occured while trying to fill text on element with the selector:', selector, '\n\nError:\n',e)
        return False" + string.Concat(Enumerable.Repeat('\n', 3));

        public static string makeRequestFunction = @"def make_request(url):
    status_code = None
    request_url = None
    final_url = None
    try:
        print(f'Navigating to: {url}')
        driver.get(url)
        final_url = driver.current_url
        print(f'Navigation complete. Final URL: {final_url}')
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
            print(f'Found status code {status_code} for request URL: {request_url}')
        else:
            print(f'WARNING: Could not find specific request for {final_url or url} in logs.')
            if driver.last_request and driver.last_request.response:
                print('Falling back to last request.')
                status_code = driver.last_request.response.status_code
                request_url = driver.last_request.url
            else:
                 print('No suitable request found.')
    except Exception as e:
        print(f'\n--- An error occurred ---')
        print(f'{type(e).__name__}: {e}')
        import traceback
        traceback.print_exc()
        print('-------------------------\n')
    finally:
        if driver:
            if hasattr(driver, 'requests'):
                 del driver.requests
        else:
            print('Driver was not initialized.')
    print('\n--- Result (using selenium-wire) ---')
    print(f'Requested URL: {url}')
    if final_url and final_url != url:
        print(f'Final URL:     {final_url}')
    if status_code is not None:
        print(f'Request URL used for status: {request_url}')
        print(f'Detected Status Code: {status_code}')
        if status_code >= 400:
            print('Status indicates an error.')
        else:
            print('Status indicates success (or redirect).')
    else:
         print(f'Could not determine status code using selenium-wire.')
" + string.Concat(Enumerable.Repeat('\n', 3));

        public static string takeScreenshotFunction = @"def take_screenshot(filename: str):
    if not filename.endswith('.png'):
        filename = 'screenshot.png'
    try:
        print('Taking screenshot, please wait...')
        with open(f'{filename}', 'wb') as file:
            file.write(driver.get_screenshot_as_png())
    except Exception as e:
        print(f'Unable to take screenshot, please check the error below:\n\n{e}')" + string.Concat(Enumerable.Repeat('\n', 3));

    }
}
