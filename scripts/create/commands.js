const commandCollection = [
  // --- Actions ---

  {
    commandName: "Browser",
    commandArgs: {
      browser: ['"chrome"', '"firefox"', '"safari"'], // Values are strings, so they are quoted
    },
    commandDescription:
      "Specifies the browser type. This **MUST** be the first valid line of the file. Use quoted values.",
    disabledOnLoad: false,
    placeholder: '"chrome" or "firefox"',
  },

  {
    commandName: "Visit",
    commandArgs: {
      url: null,
    },
    commandDescription:
      "Visits a specified URL. The value must be a valid URL string, including the protocol (e.g., 'https://') and **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"https://example.com"',
  },

  {
    commandName: "Wait-For-Seconds",
    commandArgs: {
      seconds: null,
    },
    commandDescription:
      "Waits for the specified number of seconds before continuing. Supports decimals (e.g., 2 or 0.5).",
    disabledOnLoad: true,
    placeholder: "2 or 0.5 (number value)",
  },

  {
    commandName: "Add-Cookie",
    commandArgs: {
      "cookie-name": null,
      "cookie-value": null,
    },
    commandDescription:
      "Adds a cookie to the current selenium instance. Both name and value must be quoted strings.",
    disabledOnLoad: true,
    placeholder: '"REQUEST-ID", "123456789"',
  },

  {
    commandName: "Add-Header",
    commandArgs: {
      "header-name": null,
      "header-value": null,
    },
    commandDescription:
      "Adds an HTTP Header for the current request. Both name and value must be quoted strings.",
    disabledOnLoad: true,
    placeholder: '"DNT", "1"',
  },

  {
    commandName: "Add-Headers",
    commandArgs: {
      headers: null,
    },
    commandDescription:
      "Adds multiple HTTP Headers for the current request via a JSON Object. The JSON object must be quoted.",
    disabledOnLoad: true,
    placeholder:
      '"{ \\"Header1\\": \\"Value1\\", \\"Header2\\": \\"Value2\\" }"', // Escaped JSON string
  },

  {
    commandName: "Click",
    commandArgs: {
      selector: null,
    },
    commandDescription:
      "Clicks the specified element. Supports ID, NAME, TAG NAME, and XPATH selectors. Selector **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"#submit-button" or "//button"',
  },

  {
    commandName: "Click-At-Position",
    commandArgs: {
      "x-coordinate": null,
      "y-coordinate": null,
    },
    commandDescription:
      "Clicks at a specific point on screen. Coordinates must be quoted strings.",
    disabledOnLoad: true,
    placeholder: '"600", "600"',
  },

  {
    commandName: "Click-Exp",
    commandArgs: {
      "css-selector": null,
    },
    commandDescription:
      "Alternative to `Click`. Supports CSS SELECTOR. Selector **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"div.item > h1"',
  },

  {
    commandName: "Close-Current-Tab",
    commandArgs: {},
    commandDescription: "Closes the current tab. Takes no arguments.",
    disabledOnLoad: true,
    placeholder: "",
  },

  {
    commandName: "Fill-Text",
    commandArgs: {
      selector: null,
      value: null,
    },
    commandDescription:
      "Assigns the specified value to the selected input element. Selector and value **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"#username", "myname"',
  },

  {
    commandName: "Fill-Text-Exp",
    commandArgs: {
      selector: null,
      value: null,
    },
    commandDescription:
      "More advanced version of `Fill-Text`. Selector and value **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"#username", "myname"',
  },

  {
    commandName: "Get-Text",
    commandArgs: {
      selector: null,
    },
    commandDescription:
      "Gets the text for a specified element. Selector **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '".product-price"',
  },

  {
    commandName: "Open-New-Tab",
    commandArgs: {
      url: null,
      "wait-time": null,
    },
    commandDescription:
      "Opens a new tab, pauses, then visits the URL. URL and wait time **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"https://newsite.com", "3"',
  },

  {
    commandName: "Save-As-Html",
    commandArgs: {
      "file-name": null,
    },
    commandDescription:
      "Saves the current page's HTML. File name **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"output.html"',
  },

  {
    commandName: "Save-As-Html-Exp",
    commandArgs: {
      "file-name": null,
    },
    commandDescription:
      "Alternative version of `Save-As-Html`. File name **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"output.html"',
  },

  {
    commandName: "Select-Option",
    commandArgs: {
      selector: null,
      "option-number": null,
    },
    commandDescription:
      "Selects an option from a dropdown menu. Selector and option number **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"#dropdown", "2"',
  },

  {
    commandName: "Select-Element",
    commandArgs: {
      selector: null,
    },
    commandDescription:
      "Selects an element (intended for manual script editing). Selector **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"#element"',
  },

  {
    commandName: "Set-Custom-Useragent",
    commandArgs: {
      "user-agent-string": null,
    },
    commandDescription:
      "Sets a custom user agent. The user agent string **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"Mozilla/5.0 (...)"',
  },

  {
    commandName: "Take-Screenshot",
    commandArgs: {
      "file-name": null,
    },
    commandDescription: "Takes a screenshot. File name **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"screenshot.png"',
  },

  {
    commandName: "Start-Javascript",
    commandArgs: {},
    commandDescription:
      "Instructs the parser to read all following lines as a .js code block. Takes no arguments.",
    disabledOnLoad: true,
    placeholder: "",
  },

  {
    commandName: "End-Javascript",
    commandArgs: {},
    commandDescription:
      "Instructs the parser that the end of a JavaScript code block was reached. Takes no arguments.",
    disabledOnLoad: true,
    placeholder: "",
  },

  // --- Features ---
  // Note: For Feature commands, the BAMC documentation shows the command structure as: feature "feature-name" "arg"
  // Since the UI only captures the command string (Action/Feature Name) and Arguments, we use the kebab-case command name as the key.
  {
    commandName: "Feature: add-extension",
    commandArgs: {
      "extension-path": null,
    },
    commandDescription: "",
    disabledOnLoad: true,
    placeholder: "path/to/extension",
  },

  {
    commandName: "Feature: disable-pycache",
    commandArgs: {
      "disable-pycache": [],
    },
    commandDescription:
      "Feature: Instructs the compiler to disable the writing of the `__pycache__` directory. Takes no arguments.",
    disabledOnLoad: true,
    placeholder: "",
  },

  {
    commandName: "Feature: disable-ssl",
    commandArgs: {
      "disable-ssl": [],
    },
    commandDescription:
      "Feature: Disables SSL certificate authentication for the given session. Takes no arguments.",
    disabledOnLoad: true,
    placeholder: "",
  },

  {
    commandName: "Feature: use-http-proxy",
    commandArgs: {
      "proxy-string": null,
    },
    commandDescription:
      "Feature: Uses the entered HTTP proxy. Format: USER:PASS@IP:PORT. The string **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"USER:PASS@IP:PORT"',
  },

  {
    commandName: "Feature: use-https-proxy",
    commandArgs: {
      "proxy-string": null,
    },
    commandDescription:
      "Feature: Uses the entered HTTPS proxy. Format: USER:PASS@IP:PORT. The string **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"USER:PASS@IP:PORT"',
  },

  {
    commandName: "Feature: use-mobile-user-agent",
    commandArgs: null,
    commandDescription:
      'Feature: Instructs the compiler to return a mobile user agent. This only works when browser is set to "safari"',
    disabledOnLoad: true,
    placeholder: "",
  },

  {
    commandName: "Feature: use-socks4-proxy",
    commandArgs: {
      "proxy-string": null,
    },
    commandDescription:
      "Feature: Uses the entered SOCKS4 proxy. Format: USER:PASS@IP:PORT. The string **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"USER:PASS@IP:PORT"',
  },

  {
    commandName: "Feature: use-socks5-proxy",
    commandArgs: {
      "proxy-string": null,
    },
    commandDescription:
      "Feature: Uses the entered SOCKS5 proxy. Format: USER:PASS@IP:PORT. The string **must be quoted**.",
    disabledOnLoad: true,
    placeholder: '"USER:PASS@IP:PORT"',
  },
];
