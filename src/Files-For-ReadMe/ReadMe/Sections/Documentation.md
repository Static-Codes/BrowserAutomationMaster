## BAMC Documentation

### Examples

- [Chrome Examples](/src/Files-For-ReadMe/Examples/Chrome)
- [Firefox Examples](/src/Files-For-ReadMe/Examples/Firefox)

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
