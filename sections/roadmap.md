# Roadmap

---

This section outlines the planned commands and features for later BAMM releases.
- To return to the previous page, [click here](..)
---

## Codebase Improvements
### 1. Create a function to handle CLI args
```
private static class CLIArgCheck (string CurrentValue, string[] AllowedValues, StringComparison ComparisonMethod) {
  public static string CurrentValue { get; set; } = CurrentValue;
  public static Dictionary<int, string[]> AllowedValues { get; set; } = AllowedValues;
  
  public static StringComparison ComparisonMethod { get; set; } = ComparisonMethod;
};

/// <summary>Handles CLI Commands
/// <param name="ChecksToHandle">A list of CLIArgCheck objects</param>
/// <returns>A dictionary with the index of the argument in the provided command and a bool value on whether or not it matches the expected value.</returns>
/// </summary>
private static Task<Dict<int, bool>> HandleCLIArgs(List<CLIArgCheck> ChecksToHandle){
  // Potential Example
  // @ means is present 
  // ! means is missing
  // # means is required
  // ? means is optional
  // so all keys should be seen as a boolean equation

  // var argDict = new Dictionary<int, string[]> {
    { 0, ["?--nohwc", "?clear"] },
    { 1, ["@clear", "#compiled if argDict[0] == clear"] },
    { 2, ["?--nohwc"] }
  };
  
  var command = "bamm --nohwc clear compiled"
  
}
```
This would allow for less strict command requirements
```
bamm --nohwc clear compiled | Currently invalid.
bamm clear compiled --nohwc | Currently invalid.
bamm clear compiled | Currently valid
```

## Browser Commands
### **Note**: 
These commands are NOT currently in BAM Manager, they will be added in future releases.

### 1. Get Validated Text
This command will try to get the text of a specific element, if it's found the result is then validated against "desired result", useful for checking the status of a page after input. 
```
get-validated-text "selector" "desired result"
```

### 2. Add Cookie
This command will let you add a single cookie to the browser session.
```
add-cookie "name" "value"
```

### 3. Add Cookies
For more complex scenarios, you'll be able to add multiple cookies using a JSON object.
```
add-cookies {"name": "value", "name2": "value2"}
```

### 4. Set Property
This command will allow you to dynamically change properties of HTML elements.
```
set-element-property "selector" "property" "value"
```


  #### Example:
  
  Given the Div:
  ```
  <div id="idp-month__selected" data-selected-value="01">
  ```
  
  You could use the command to change the selected month:
  ```
  set-element-property "#idp-month__selected" "data-selected-value" "02"
  ```

## CLI Commands
### 1. Extend Functionality for `clear` command
Add functionality to delete a project via 
```
bamm clear compiled/PN
bamm --nohwc clear compiled/PN
bamm clear compiled/PN --nohwc
```
Where `PN` is the project name you wish to delete.

## Feature Commands
### 1. Set Python Version
This command will tell the Compiler and Runtime Manager to use the specified version of Python 3.
```
feature "python-version==v"
```
Where `v` is the version (For example: 3.9)

## Script Commands

### 1. Create a Loop

  Start a loop n times, where n has to be atleast 2
  ```
  do-loop n
  ```

  End of a loop command
  ```
  end-loop
  ```

  **EXAMPLE:**
  
  ```
  do-loop 5
  click "#ID"
  wait-for-seconds 0.3
  end-loop
  ```
   
## User Experience Enhancements
- Allow users the ability to open a new explorer/finder window to that directory, provided there's at least 100MB of RAM available.
- Reintroduce 32bit system support (win-x86, osx-x86, linux-x86)
- Update documentation to reflect the new commands [here](changelog-for-latest.md)
- Finish GUI

## LSP (Language Server Protocol)
- This will allow you to create bamm scripts with syntax highlighting and other features similar to pylance.

---
