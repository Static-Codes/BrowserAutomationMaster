-----
# Browser Automation Master 🤖

A custom scripting language that compiles into Python 3.9+ code.

Selenium automation in Python can involve a lot of repetitive code and detailed setup. BAM simplifies that process by allowing you to write automation scripts in a more concise, readable, and English-like language. This script is then passed to the compiler, which generates a Python file, effectively reducing the amount of boilerplate you need to manage.

---

### Chrome Example

- NOTE:
  - `#gh-ac` and `#gh-search-btn` are selectors specific to ebay.com, you will have to find your own selectors
    
```
browser "chrome" // Sets the browser you wish to use to chrome

visit "https://www.ebay.com/" // Visits ebay.com

wait-for-seconds 1.5 // Waits for 1 1/2 seconds

fill-text "#gh-ac" "Awesome deals" // Enters text into the searchbox

wait-for-seconds 1  // Waits for 1 second

click "#gh-search-btn" // Clicks the search button

wait-for-seconds 10 // Waits for 10 seconds

save-as-html "ebay-search.html" // Saves the page source as an html file
```

### Firefox Example

- NOTE:
  - `.ytSearchboxComponentInput` and `ytSearchboxComponentSearchButton` are selectors specific to youtube.com, you will have to find your own selectors.
  - If you dont specify the browser you want to use, `chrome` or `firefox`, the compiler will default to firefox.
```
visit "https://www.youtube.com/" // Visits youtube.com

wait-for-seconds 1.5 // Waits for 1/2 seconds

fill-text ".ytSearchboxComponentInput" "This is a test, it works!" // Enters text into the searchbox

wait-for-seconds 1.5 // Waits for 1 1/2 seconds

click ".ytSearchboxComponentSearchButton" // Clicks the search button

wait-for-seconds 5 // Waits for 5 seconds

take-screenshot "youtube-feed.png" // Takes a screenshot of the current browser state.
```
---

---

## Table of Contents 📖

### [Installation](sections/installation.md)

### [BAMC Documentation + Examples (Actions, Arguments, Features, Selectors)](sections/documentation.md)

### [Compile BrowserAutomationMaster from Source](sections/compile.md)

### [Roadmap](sections/roadmap.md)

---

### Supported Browsers 🌐

- **Chrome**
- **Firefox**

### Supported Python Versions 🐍

- **3.9.x**
- **3.10.x**
- **3.11.x**
- **3.12.x**
- **3.13.x**
- **3.14.x**

### Supported Operating Systems 💻

- Linux **(ARM64, x64)**
- MacOS 11.0+ **(ARM64, x64)**
- Windows 10/11 **(ARM64, x64)**

### System Requirements (Minimum Tested) ✨

- 2 Core CPU
- 4GB DDR3 RAM (The application itself uses under 200MB of RAM)
- Any Supported Browser
- Any Supported Python Version
