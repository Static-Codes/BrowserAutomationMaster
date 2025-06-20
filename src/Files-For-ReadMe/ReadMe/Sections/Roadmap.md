# Roadmap

---

This section outlines the planned commands and features for BAMM.

---

## Features

- **`feature "no-ssl"`**: This feature will disable SSL certificate validation when using Selenium, allowing for more flexible connections.
- **`feature "headless"`**: This feature will run the browser in headless mode, which is great for background tasks and server environments.

## Management

- **`bamm clear compiled`**: This command will clear all project files from the current user's "compiled" directory, helping you manage disk space.
- **`bamm clear userScripts`**: Similarly, this command will clear all scripts from the current user's "userScripts" directory.
- **`bamm uninstall`**:
  - **On Windows**: This will execute `unins000.exe`.
  - **On MacOS**: (Specific uninstallation steps to be determined and implemented.)
  - **On Linux**: This will execute `apt uninstall bamm` for a clean removal.

## Browser Commands

- **`set-custom-useragent "User Agent String"`**: You'll be able to set a custom `requestUserAgent` for all your browser requests, useful for mimicking different devices or browsers.
- **`add-cookie "name" "value"`**: This command will let you add a single cookie to the browser session.
- **`add-cookies {"name": "value", "name2": "value2"}`**: For more complex scenarios, you'll be able to add multiple cookies using a JSON object.
- **`add-header "name" "value"`**: Add a single HTTP header to your requests.
- **`add-headers {"name": "value", "name2": "value2"}`**: Add multiple HTTP headers using a JSON object.
- **`set-element-property "selector" "property" "value"`**: This powerful command will allow you to dynamically change properties of HTML elements.

  **EXAMPLE:**
  Given the HTML: `<div _ngcontent-home-c123="" idpsetfocus="" class="idp-dropdown__selected" id="idp-month__selected" data-selected-value="01">`
  You could use the command: `set-element-property "#idp-month__selected" "data-selected-value" "02"` to change the selected month.

- **`fill-text-exp "selector" "value"`**: This command will fill text into an element using JavaScript.

  **FUNCTION IMPLEMENTATION (solely for reference, will be added to a later update.)**

  ```python
  from selenium.webdriver.common.by import By

  def fill_text(byType: By, selector: str, value: str) -> bool:
      try:
          element = driver.find_element(byType, selector)
          driver.execute_script(f'arguments[0].innerText = "{value}"', element)
          return True
      except Exception as e:
          print(f'An error occurred while trying to fill text on element with the selector: {selector}\n\nError:\n{e}')
          return False
  ```

## User Experience Enhancements

- Adding an option for users to **copy the path of the compiled script directory** to their clipboard.
- Alternatively, users will be able to **open a new explorer/finder window** to that directory, provided there's at least 100MB of RAM available.
- Finally, if the user wishes to simply execute the compiled script directly from BAMM, this feature will be added in a future update.

---
