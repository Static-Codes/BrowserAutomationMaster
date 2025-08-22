# Roadmap

---

This section outlines the planned commands and features for later BAMM releases.
- To return to the previous page, [click here](..)
---

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

## Script Commands
- **`do-loop n`**: Start a loop n times, where n has to be atleast 2
- **`end-loop`**: End of a loop command

  **EXAMPLE:**
  
  ```
  do-loop 5
  click "#ID"
  wait-for-seconds 0.3
  end-loop
  ```
   
## User Experience Enhancements
- Allow users the ability to open a new explorer/finder window to that directory, provided there's at least 100MB of RAM available.

## LSP (Language Server Protocol)
- This will allow you to create bamm scripts with syntax highlighting and other features similar to pylance.

---
