# Browser Automation Master

A Domain Specific Language that compiles into valid python code!

### Supported Browsers

- Brave
- Chrome
- Firefox

### Supported Python Versions

- 3.9.x
- 3.10.x
- 3.11.x
- 3.12.x
- 3.13.x
- 3.14.x

### Supported Operating Systems

- Windows 10
- Windows 11

### Supported Platforms

- x86
- x64
- ARM64

### System Requirements

- 2+ Core CPU
- 4GB DDR4 RAM
- Any Supported Browser
- Any Supported Python Version

### Tested On

- AMD Athlon Gold 3150U
- AMD Ryzen 7 2700X
- AMD EPYC 7282
- AMD Ryzen 9 5950X
- Intel I3 6100
- Intel I5 6500
- Intel I7 7700
- Intel I5 8500

## BAMC Documentation

### Actions

The following actions can be used in your BAMC scripts:

- `click`: Clicks the specified button element.
- `click-exp`: Alternative to `click`; use this if `click` is causing issues.
- `get-text`: Gets the text for a specified element.
- `fill-text`: Assigns the specified value to the selected element.
- `save-as-html`: Saves the current page's HTML to a file with the specified name.
- `save-as-html-exp`: Saves the current page's HTML to a file with the specified name but uses different logic; use this if `save-as-html` doesn't fit your needs.
- `select-option`: Selects an `<option>` from a `<select>` dropdown menu. Currently only supports `<select><option></option></select>`.
- `select-element`: Selects the element associated with the provided selector (if found).
- `take-screenshot`: Takes a screenshot of the browser after executing the previous line.
- `visit`: Visits a specified URL.
- `wait-for-seconds`: Waits for the specified number of seconds before continuing.

### Arguments (CLI)

These arguments can be used when running BAMC from the command line:

- `add`: Adds a local `.bamc` file to the `userScripts` directory.
  - **Syntax:** `bamm.exe add 'filename'`
- `--set-timeout`: Sets the default timeout for all Selenium-based browser actions.

### Features

These features can be enabled or configured in your BAMC scripts:

- `async`: This indicates to the compiler you want to create an asynchronous script. This should not be done unless you have experience using async functions in Python.
- `browser`: This MUST be the first valid line of the file. If not supplied, defaults to a Firefox instance or user agent (depending on the other defined features).
- `bypass-cloudflare`: Instructs the browser to use a more advanced approach to bypass Cloudflare.
- `disable-pycache`: Instructs the compiler to disable the writing of the `__pycache__` directory. This directory is written by Visual Studio Code and contains `.pyc` files.
- `use-http-proxy`: Uses the entered HTTP proxy for the session.
- `use-https-proxy`: Uses the entered HTTPS proxy for the session.
- `use-socks4-proxy`: Uses the entered SOCKS4 proxy for the session.
- `use-socks5-proxy`: Uses the entered SOCKS5 proxy for the session.
