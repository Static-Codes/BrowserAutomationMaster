# Browser Automation Master

A Domain Specific Language (DSL) that compiles into valid python code!

---

### Supported Browsers

- **Chrome**
- **Firefox**

### Supported Python Versions

- **3.9.x**
- **3.10.x**
- **3.11.x**
- **3.12.x**
- **3.13.x**
- **3.14.x**

### Supported Operating Systems

- Linux **(x64)**
- MacOS 11.0+ **(x64)**
- Windows 10 **(ARM64, x64, x86)**
- Windows 11 **(ARM64, x64, x86)**

### Supported Platforms

- **x86**
- **x64**
- **ARM64**

### System Requirements (Minimum)

- 2+ Core CPU
- 4GB DDR3 RAM (The application itself uses under 200MB of RAM)
- Any Supported Browser
- Any Supported Python Version

### Tested On

- AMD Athlon Gold 3150U
- AMD Ryzen 7 2700X
- AMD EPYC 7282
- AMD Ryzen 9 5950X
- Intel I5 4260U (Mac Mini 2014)
- Intel I3 6100
- Intel I5 6500
- Intel I7 7700
- Intel I5 8500

---

## BAMC Documentation

### Actions

The following actions can be used in your BAMC scripts:

- `browser`: This MUST be the first valid line of the file. If not supplied, defaults to a Firefox instance or user agent (depending on the other defined features).
  - **Syntax:**
    - `browser "chrome"`
    - `browser "firefox"`
- `click`: Clicks the specified button element.
  - **Syntax:** `click "selector" \\ Supports ID, NAME, TAG NAME, and XPATH`
- `click-exp`: Alternative to `click`; use this if `click` is causing issues.
  - **Syntax:** `click-exp 'css-selector.item_element' \\ Supports CSS SELECTOR`
- `end-javascript`: Instructs the parser that the end of a javascript code block was reached. An error will be thrown if end-javascript is not found within the file (when a "start-javascript" is present)
  - **Syntax:** `end-javascript`
- `fill-text`: Assigns the specified value to the selected element.
  - **Syntax:** `fill-text "selector" "Value you want to include"`
- `get-text`: Gets the text for a specified element.
  - **Syntax:** `get-text "selector" \\ Supports ID, NAME, TAG NAME, and XPATH`
- `save-as-html`: Saves the current page's HTML to a file with the specified name.
  - **Syntax:** `save-as-html "filename.html"`
- `save-as-html-exp`: Saves the current page's HTML to a file with the specified name but uses different logic; use this if `save-as-html` doesn't fit your needs.
  - **Syntax:** `save-as-html-exp "filename.html"`
- `select-option`: Selects an `<option>` from a `<select>` dropdown menu. Currently only supports `<select><option></option></select>`.
  - **Syntax:** `select-option "selector" 2 // 2 is the option number so the 2nd item in the list would be selected.`
- `select-element`: Selects the element associated with the provided selector (if found).
  - **Syntax:** `select-element "selector" \\ This currently works but, theres no logic to access the selected element, this should only be done if youre manually editing the compiled python script.'`
- `start-javascript`: Instructs the parser to read all following lines as a .js code block, until end-javascript is found; Will throw an error if end-javascript is not found within the file.
  - **Syntax:** `start-javascript`
- `take-screenshot`: Takes a screenshot of the browser after executing the previous line.
  - **Syntax:** `take-screenshot "filename.png" \\ It's recommended to add a "wait-for-seconds" command before executing this.`
- `visit`: Visits a specified URL.
  - **Syntax:** `visit "https://url-to-visit.com/page.html"`
- `wait-for-seconds`: Waits for the specified number of seconds before continuing.
  - **Syntax:** `wait-for-seconds 1 \\ Also supports decimals, so .2 would be 1/5 of a second (200ms)`

### Arguments (CLI)

These arguments can be used when running BAMC from the command line:

- `add`: Adds a `.bamc` file to the `userScripts` directory.
  - **Syntax:** `bamm add 'path/to/filename.bamc'`
- `compile`: Compiles a `.bamc` file that isn't located in the `userScripts` directory.
  - **Syntax:** `bamm compile 'path/to/filename.bamc'`
- `delete`: Adds a local `.bamc` file to the `userScripts` directory.
  - **Syntax:** `bamm delete 'filename.bamc'`
- `help`: Adds a local `.bamc` file to the `userScripts` directory.
  - **Syntax:**
    - `bamm help --all` -> This displays information for all available commands.
    - `bamm help wait-for-seconds` -> This displays information about the selected command
- `--set-timeout`: Sets the default timeout in seconds for all Selenium-based browser actions.
  - **Syntax:** `bamm --set-timeout==5`

### Features

These features can be enabled or configured in your BAMC scripts:

- `async`: This indicates to the compiler you want to create an asynchronous script. This should not be done unless you have experience using async functions in Python.
  - **Syntax:** `feature "async" // Currently not supported, will throw an error if you use.`
- `bypass-cloudflare`: Instructs the browser to use a more advanced approach to bypass Cloudflare.
  - **Syntax:** `feature "bypass-cloudflare" // Currently not supported, will throw an error if you use.`
- `disable-pycache`: Instructs the compiler to disable the writing of the `__pycache__` directory. This directory is written by Visual Studio Code and contains `.pyc` files.
  - **Syntax:** `feature "disable-pycache"`
- `use-http-proxy`: Uses the entered HTTP proxy for the session.
  - **Syntax:** `feature "use-http-proxy" "USER:PASS@IP:PORT"` -> Use `NULL:NULL@IP:PORT" if no user:pass authentication is required.
- `use-https-proxy`: Uses the entered HTTPS proxy for the session.
  - **Syntax:** `feature "use-https-proxy" "USER:PASS@IP:PORT"` -> Use `NULL:NULL@IP:PORT" if no user:pass authentication is required.
- `use-socks4-proxy`: Uses the entered SOCKS4 proxy for the session.
  - **Syntax:** `feature "use-socks4-proxy" "USER:PASS@IP:PORT"` -> Use `NULL:NULL@IP:PORT" if no user:pass authentication is required.
- `use-socks5-proxy`: Uses the entered SOCKS5 proxy for the session.
  - **Syntax:** `feature "use-socks5-proxy" "USER:PASS@IP:PORT"` -> Use `NULL:NULL@IP:PORT" if no user:pass authentication is required.

---

## Supported Selectors

### Basic

- **ID:** `#main-content`
- **Class:** `.btn-primary`
- **Type / Tag:** `div`
- **Custom Element:** `my-custom-element`
- **Name Attribute:** `[name="elementName"]` (Note this is NOT the same as Tag)
- **XPath Selector:** `//form[@class='exampleClassName']//input[@type='text']`

### Advanced

- **Universal:** `*`
- **Attribute (Presence):** `[href]`
- **Attribute (Value):** `[target=_blank]`
- **Attribute (Quoted Value):** `[title='A simple title']`
- **Attribute (Complex Quoted Value):** `[data-value="some complex 'value' with quotes"]`
- **Pseudo-class:** `:hover`
- **Pseudo-class (with arguments):** `:nth-child(2n+1)`
- **Pseudo-class (with complex arguments):** `:not(.visible, #main)`
- **Pseudo-element:** `::before`

### Limitations

- For complex/nested selectors like `:not(div > .item)`, it will capture `div > .item` as a single string but will not parse further.
- This handles attributes with quoted strings quite well, but it doesn't fully support **all** CSS escaping rules Example: `"id="foo.bar""` would require the selector `"#foo\.bar"`.
- This is **not** full CSS/XPATH parsing and will not catch all invalid selectors, however most rules are currently supported.
- The CSS parsing currently doesn't support the following characters: ">", "+", "~", " ", etc.

---

# Compile BrowserAutomationMaster (BAM) from Source

This guide provides instructions on how to compile BrowserAutomationMaster Manager (BAMM) from its source code for various operating systems.

## Prerequisites

Before you begin, ensure you have the following installed on your system:

1.  **.NET 8.x SDK:** The most recent 8.x SDK as of writing this, is 8.0.17. [Download Link](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
2.  **Git (Optional but Recommended):** For cloning the repo, you can also use the source code provided with each release.

## General Compilation Steps

1.  **Download and Extract the Source Code:**

    - Go to the latest release page: `https://github.com/Static-Codes/BrowserAutomationMaster/releases/latest/`
    - Download the `BAMM-{Version}-Source.zip` file.
    - Extract/Unzip the source.

2.  **Navigate to the Source Directory:**

    ```bash
    cd BrowserAutomationMaster
    ```

3.  **Restore .NET Dependencies:**
    This step is usually handled automatically by the build/publish commands, but it's good practice to run it manually if you encounter issues:
    ```bash
    dotnet restore
    ```

## Platform-Specific Compilation Instructions

Choose the instructions relevant to your target operating system. All commands should be run from within the `BrowserAutomationMaster` source directory.

## Linux

### .deb Package Compilation (for Debian/Ubuntu-based systems)

This will create a `.deb` package for easy installation.

1.  **Install `dotnet-deb` tool (if not already installed):**

    ```bash
    dotnet tool install --global dotnet-deb
    # Ensure the .dotnet/tools directory is in your PATH
    # export PATH="$PATH:$HOME/.dotnet/tools" (add to your .bashrc or .zshrc for permanency)
    ```

2.  **Compile and Package:**
    ```bash
    dotnet deb --runtime linux-x64 --configuration Release
    ```
    The resulting `.deb` package will typically be found in the project's root directory or a sub-directory like `bin/Release/`. Check the command output for the exact location.

#### Generic Linux Publish (Self-Contained Application)

If you don't want a `.deb` package or are on a non-Debian-based Linux distribution, you can create a self-contained application.

1.  **Publish for Linux x64:**
    ```bash
    dotnet publish -c Release -r linux-x64 --self-contained true
    ```
    The compiled application will be in `bin/Release/netX.Y/linux-x64/publish/` (where `X.Y` is the .NET version, e.g., `net8.0`).

### macOS

This will create a self-contained application for macOS 11.0+ (Both Intel and Apple Silicon via Rosetta 2).

1.  **Publish for macOS x64:**

    ```bash
    dotnet publish -c Release -r osx-x64 --self-contained true
    ```

    The compiled application will be in `bin/Release/netX.Y/osx-x64/publish/`.

2.  **(Optional) Publish for macOS ARM64 (Apple Silicon):**
    This is for Apple Silicon users **ONLY**:
    ```bash
    dotnet publish -c Release -r osx-arm64 --self-contained true
    ```
    The compiled application will be in `bin/Release/netX.Y/osx-arm64/publish/`.

### Windows

This will create self-contained applications for different Windows architectures.

1.  **Publish for Windows x64 (64-bit):**

    ```bash
    dotnet publish -c Release -r win-x64 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-x64\publish\`.

2.  **Publish for Windows ARM64:**

    ```bash
    dotnet publish -c Release -r win-arm64 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-arm64\publish\`.

3.  **Publish for Windows x86 (32-bit):**

    ```bash
    dotnet publish -c Release -r win-x86 --self-contained true
    ```

    The compiled application will be in `bin\Release\netX.Y\win-x86\publish\`.

4.  **Download [Inno Setup v6.4.3](https://jrsoftware.org/download.php/is.exe?site=1)**
    Ensure "Associate Inno Setup with the .iss file extension" is checked during setup.

5.  **Create an installer**
    Navigate to `BrowserAutomationMaster\src\Installer Files\Windows`
    Right click on the .iss file corresponding to your published build (ARM64, x64, x86)
    Click compile

    The installer will be in `BrowserAutomationMaster\src\Published Builds\{Your architecture}\`

## Installation

- **Linux `.deb` package**
  You can install the package by double clicking the .deb file, or by using:
  ```bash
  sudo dpkg -i <package_name>.deb
  # If there are dependency issues, fix them with:
  sudo apt-get install -f
  ```
- **MacOS**
  The compiled application will be in `bin/Release/netX.Y/osx-arm64/publish/`.
  Place it on your Desktop for easy access!
  use chmod +x Desktop/bamm to make it an executable!
- **Windows**
  Simply double click the installer in `BrowserAutomationMaster\src\Published Builds\{Your architecture}\`

## Opening BAMM

- **MacOS**
  Desktop/bamm

- **Linux and Windows**
  bamm

## Troubleshooting

- **.NET SDK Not Found:** Ensure the .NET SDK is installed correctly and that the `dotnet` command is available in your system's PATH.
- **Permission Issues:** On Linux/macOS, you might need to use `sudo` for global tool installation or for installing `.deb` packages. For compilation itself, `sudo` is generally not required or recommended.
- **Incorrect Runtime Identifier:** Ensure you're using a valid runtime identifier (RID) for your target platform. You can find a list of RIDs [here](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog).

# Roadmap

---

This section outlines the planned commands and features for BAMM.

---

## Features

- **`feature "no-ssl"`**: This feature will disable SSL certificate validation when using Selenium, allowing for more flexible connections.
- **`feature "headless"`**: We'll implement a headless mode, letting you run the browser without a visible user interface, which is great for background tasks and server environments.

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
