# Roadmap

---

This section outlines the planned commands and features for later BAMM releases.
- To return to the previous page, [click here](..)
---

## Features

- **`feature "headless"`**: This feature will run the browser in headless mode, which is great for background tasks and server environments.

## Browser Commands

- **`get-validated-text "selector" "desired result"`**: This powerful command will try to get the text of a specific element, if it's found the result is then validated against "desired result", useful for checking the status of a page after input. 
- **`add-cookie "name" "value"`**: This command will let you add a single cookie to the browser session.
- **`add-cookies {"name": "value", "name2": "value2"}`**: For more complex scenarios, you'll be able to add multiple cookies using a JSON object.
- **`set-element-property "selector" "property" "value"`**: This powerful command will allow you to dynamically change properties of HTML elements.

  **EXAMPLE:**
  Given the HTML: `<div _ngcontent-home-c123="" idpsetfocus="" class="idp-dropdown__selected" id="idp-month__selected" data-selected-value="01">`
  You could use the command: `set-element-property "#idp-month__selected" "data-selected-value" "02"` to change the selected month.

## User Experience Enhancements
- Allow multiple visit commands in a single .BAMC script without jeopardizing current functionality.
- Adding an option for users to copy the path of the compiled script directory to their clipboard.
- Alternatively, allow users the ability to open a new explorer/finder window to that directory, provided there's at least 100MB of RAM available.
- Finally, if the user wishes to simply execute the compiled script directly from BAMM, this feature will be added in a future update.

---
