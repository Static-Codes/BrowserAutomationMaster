var commands = getData();

var commandSelect = document.getElementById("command-select");
var argsContainer = document.getElementById("command-arguments-container");
var descriptionElement = document.getElementById("command-description");
var executeButton = document.getElementById("execute-command-btn");

var commandList = document.querySelector("#command-list");

var duplicateCommandButton = document.querySelector(
  ".action-button.duplicate-command-btn"
);
var removeCommandButton = document.querySelector(
  ".action-button.remove-command-btn"
);
var validateScriptButton = document.querySelector(
  ".action-button.validate-script-btn"
);

var nextIndexAfterDelete = 0;
var scriptIsValidated = false;
var jsMode = false;

function addCommandToCommandList(commandText, addToLocalStorage = true) {
  try {
    var childNode = document.createElement("li");

    document.querySelectorAll("#command-list li.list-item").forEach((item) => {
      item.classList.remove("list-item");
    });

    childNode.classList.add("list-item");
    childNode.textContent = commandText;

    commandList.appendChild(childNode);

    if (addToLocalStorage) {
      reindexCommands();
    }

    console.log(`Added element at index ${commandList.children.length - 1}`);
    refocusSelection();
  } catch (e) {
    console.error(e);
  }
}

function clearCurrentScriptState(commandsAdded) {
  setData({});

  if (commandsAdded.length > 0) {
    for (var i = commandsAdded.length - 1; i >= 0; i--) {
      commandsAdded[i].remove();
    }
  }
}

function convertUTF8toBase64(str) {
  const utf8Bytes = new TextEncoder().encode(str);
  const binaryStr = utf8Bytes.reduce((acc, byte) => {
    return acc + String.fromCharCode(byte);
  }, "");
  return btoa(binaryStr);
}

// FIXED: Now grabs content and calls addCommandToCommandList to append to the end.
function duplicateSelectedCommand() {
  var selectedChild = document.querySelector(".list-item");
  if (!selectedChild) {
    alert("Please select an element to duplicate.");
    throw new Error("No element selected");
  }

  var commandText = selectedChild.textContent;

  if (commandText.includes('"browser":')) {
    alert(
      "Unable to duplicate the browser command, only one of these commands may be present in any given script."
    );
    return;
  }

  // Appends to the end then ensures it's selected and re-indexes the object.
  addCommandToCommandList(commandText, true);

  console.log(`Duplicated command and appended to end.`);
}

function getData() {
  try {
    var commandsFromLS = localStorage.getItem("commands");
    var commandsAdded = document.querySelectorAll("#command-list li");

    if (
      commandsFromLS == null ||
      commandsFromLS === "{}" ||
      commandsFromLS === "[]"
    ) {
      if (commandsAdded.length > 0) {
        clearCurrentScriptState(commandsAdded);
      }
      return {};
    }

    let data = JSON.parse(commandsFromLS);
    return Array.isArray(data) ? {} : data;
  } catch (e) {
    console.error(e);
    clearCurrentScriptState([]);
    return {};
  }
}

function getIndexOfSelectedCommand() {
  try {
    if (Object.keys(commands).length === 0) {
      alert("No commands present in the current script.");
      throw new Error("No commands present in the current script.");
    }
    var child = document.querySelector(".list-item");
    if (!child) return -1;
    var parent = child.parentNode;
    return Array.prototype.indexOf.call(parent.children, child);
  } catch (e) {
    return -1;
  }
}

function handleCommandListClick(e) {
  const clickedItem = e.target.closest("li");

  if (clickedItem) {
    commandList.querySelectorAll(".list-item").forEach((item) => {
      item.classList.remove("list-item");
    });
    clickedItem.classList.add("list-item");
  }
}

function loadCurrentScriptCommands() {
  let currentlySelectedItem = null;
  const commandItems = document.querySelectorAll("#command-list li");

  function handleSelection(event) {
    const newSelectedItem = event.currentTarget;

    if (currentlySelectedItem !== null) {
      currentlySelectedItem.classList.remove("list-item");
    }
    newSelectedItem.classList.add("list-item");
    currentlySelectedItem = newSelectedItem;
  }

  if (typeof usingOtter !== "undefined" && usingOtter) {
    for (cmd of commandItems) {
      cmd.addEventListener("click", handleSelection);
    }
  } else {
    commandItems.forEach((item) => {
      item.addEventListener("click", handleSelection);
    });
  }
}

function populateCommandSelect() {
  // 1. Check if the browser command is already in the loaded script
  var browserExists = Object.values(commands).some((cmd) =>
    cmd.includes('"browser":')
  );

  commandCollection.forEach((command) => {
    var option = document.createElement("option");
    option.value = command.commandName;
    option.textContent = command.commandName;

    if (command.commandName === "Browser") {
      // --- HANDLE BROWSER OPTION ---
      if (browserExists) {
        // If script has a browser, disable this option
        option.disabled = true;
        option.selected = false;
        // option.removeAttribute("selected");
      } else {
        // If script is empty, select this option and render inputs
        option.disabled = false;
        option.selected = true;
        renderArguments(command);
      }
    } else {
      // --- HANDLE ALL OTHER OPTIONS ---
      if (browserExists) {
        option.disabled = false;
      } else {
        option.disabled = command.disabledOnLoad;
      }
    }

    commandSelect.appendChild(option);
  });

  if (browserExists && commandCollection.length > 1) {
    // Select the second item (Add-Header)
    var nextCommand = commandCollection[1];
    commandSelect.value = nextCommand.commandName;
    renderArguments(nextCommand);
  }
}

function refocusSelection() {
  commandList.removeEventListener("click", handleCommandListClick);
  commandList.addEventListener("click", handleCommandListClick);
}

function reindexCommands() {
  var newCommands = {};
  var listItems = commandList.children;

  for (var i = 0; i < listItems.length; i++) {
    newCommands[i] = listItems[i].textContent;
  }

  commands = newCommands;
  setData(commands);
  console.log(getData());
}

function removeSelectedCommand() {
  var selectedChild = document.querySelector(".list-item");
  if (!selectedChild) {
    alert("Please select an element to remove.");
    throw new Error("No element selected");
  }

  var index = getIndexOfSelectedCommand();
  var removedCommandText = commands[index];

  if (removedCommandText.includes('"browser":')) {
    alert(
      "Unable to remove the browser command, this is a requirement for every script."
    );
    return;
  }

  // --- JS MODE CLEANUP LOGIC ---
  // Check if the removed command is START, ADD, or END.
  let isJsBlockCommand =
    removedCommandText.includes("start-javascript") ||
    removedCommandText.includes("add-to-js") ||
    removedCommandText.includes("end-javascript");

  if (isJsBlockCommand) {
    // 1. Determine the STARTING index of the entire 3-command block.
    let startIndex = index;
    if (removedCommandText.includes("add-to-js")) {
      // If removing the middle command, the block starts one index before.
      startIndex = index - 1;
    } else if (removedCommandText.includes("end-javascript")) {
      // If removing the end command, the block starts two indices before.
      startIndex = index - 2;
    }

    // Safety check: Ensure the startIndex is valid (not < 0)
    if (startIndex < 0) {
      startIndex = 0;
    }

    // 2. Collect the indices for the expected block (start-js, add-js, end-js)
    // We check for the explicit keys to ensure we're deleting a valid block structure.
    const indicesToRemove = [];

    // Check for the commands at startIndex, startIndex + 1, and startIndex + 2
    if (
      commands[startIndex] &&
      commands[startIndex].includes("start-javascript")
    ) {
      indicesToRemove.push(startIndex);
    }
    if (
      commands[startIndex + 1] &&
      commands[startIndex + 1].includes("add-to-js")
    ) {
      indicesToRemove.push(startIndex + 1);
    }
    if (
      commands[startIndex + 2] &&
      commands[startIndex + 2].includes("end-javascript")
    ) {
      indicesToRemove.push(startIndex + 2);
    }

    // Only proceed if we found at least one part of the block to delete
    if (indicesToRemove.length > 0) {
      // Remove elements from the DOM starting from the highest index (to preserve lower indices during removal)
      indicesToRemove
        .sort((a, b) => b - a)
        .forEach((idx) => {
          // Checks if the DOM element actually exists at that position before attempting removal
          if (commandList.children[idx]) {
            commandList.children[idx].remove();
            console.log(`Deleted JS block command element at index ${idx}`);
          }
        });

      jsMode = false; // Reset the state

      // Re-index all remaining commands after bulk removal
      reindexCommands();

      // Refocuses selection to the command before the removed block
      nextIndexAfterDelete = startIndex > 0 ? startIndex - 1 : 0;
      var child = commandList.children[nextIndexAfterDelete];
      if (child) {
        child.classList.add("list-item");
      }
    }

    // Exits the function here as the removal and re-indexing are complete
    return;
  }
  // --- END JS MODE CLEANUP LOGIC ---

  var cmdCount = Object.keys(commands).length;

  if (cmdCount === 1) {
    nextIndexAfterDelete = -1;
  } else if (index === cmdCount - 1) {
    nextIndexAfterDelete = index - 1;
  } else {
    nextIndexAfterDelete = index;
  }

  try {
    selectedChild.remove();
    console.log(`deleted command element at index ${index}`);

    reindexCommands();

    if (Object.keys(commands).length === 0) {
      console.log("Command list is now empty.");
      return;
    }

    var child = commandList.children[nextIndexAfterDelete];
    if (child) {
      child.classList.add("list-item");
    }
  } catch (e) {
    console.error(e);
  }
}

function renderArguments(command) {
  argsContainer.innerHTML = "";
  descriptionElement.innerHTML = `<p>${command.commandDescription}</p>`;
  descriptionElement.hidden = false;

  var argKeys = Object.keys(command.commandArgs);

  argKeys.forEach((argName) => {
    var argOptions = command.commandArgs[argName];

    var argGroup = document.createElement("div");
    argGroup.classList.add("arg-group");

    var label = document.createElement("label");
    label.classList.add("arg-label");
    label.textContent =
      argName.charAt(0).toUpperCase() + argName.slice(1) + ":";

    argGroup.appendChild(label);

    if (argOptions && argOptions.length > 0) {
      var optionsContainer = document.createElement("div");
      optionsContainer.classList.add("arg-options-container");

      argOptions.forEach((optionValue, index) => {
        var optionWrapper = document.createElement("label");
        optionWrapper.classList.add("arg-option");

        var radioInput = document.createElement("input");
        radioInput.type = "radio";
        radioInput.name = argName;
        radioInput.value = optionValue;
        radioInput.id = `${command.commandName}-${argName}-${optionValue}`;
        if (index === 0) {
          radioInput.checked = true;
        }

        var customRadio = document.createElement("span");
        customRadio.classList.add("radio-custom");

        var optionText = document.createElement("span");
        optionText.textContent = optionValue;

        optionWrapper.appendChild(radioInput);
        optionWrapper.appendChild(customRadio);
        optionWrapper.appendChild(optionText);

        optionsContainer.appendChild(optionWrapper);
      });
      argGroup.appendChild(optionsContainer);
    } else {
      // Using textarea for code blocks ---
      if (command.isCodeBlock) {
        var textAreaInput = document.createElement("textarea");
        textAreaInput.classList.add("arg-text-input", "code-block-input");
        textAreaInput.name = argName;

        if (command.placeholder != null) {
          textAreaInput.placeholder = command.placeholder;
        } else {
          textAreaInput.placeholder = `Enter value for ${argName}`;
        }
        argGroup.appendChild(textAreaInput);
      } else {
        var textInput = document.createElement("input");
        textInput.type = "text";
        textInput.classList.add("arg-text-input");
        textInput.name = argName;

        if (command.placeholder != null) {
          textInput.placeholder = command.placeholder;
        } else {
          textInput.placeholder = `Enter value for ${argName}`;
        }

        argGroup.appendChild(textInput);
      }
    }

    argsContainer.appendChild(argGroup);
  });
}

function setData(data) {
  localStorage.removeItem("commands");
  localStorage.setItem("commands", JSON.stringify(data));
}

function updateCommandComboBoxState() {
  var browserExists = Object.values(commands).some((cmd) =>
    cmd.includes('"browser":')
  );

  document.querySelectorAll("#command-select option").forEach((el) => {
    if (el.value !== "Browser" || !browserExists) {
      el.removeAttribute("disabled");
    }
  });

  var selector = document.querySelector("#command-select option:first-child");
  if (selector) {
    selector.disabled = true;
    selector.removeAttribute("selected");
    selector.selected = false;
  }
}

window.addEventListener("load", (e) => {
  Object.values(commands).forEach((commandText) => {
    addCommandToCommandList(commandText, false);
  });

  reindexCommands();
});

commandSelect.addEventListener("change", (event) => {
  var selectedCommandName = event.target.value;
  var selectedCommand = commandCollection.find(
    (cmd) => cmd.commandName === selectedCommandName
  );

  if (selectedCommand) {
    renderArguments(selectedCommand);
  } else {
    argsContainer.innerHTML = "";
    descriptionElement.textContent = "";
    descriptionElement.hidden = true;
  }
});

executeButton.addEventListener("click", (e) => {
  var selectedCommandName = commandSelect.value;
  if (!selectedCommandName) {
    alert("Please select a command first.");
    return;
  }

  if (jsMode && selectedCommandName !== "Add-JS-Code") {
    alert(
      "You are inside a JavaScript block. The next command must be 'Add-JS-Code'."
    );
    return;
  }
  if (!jsMode && selectedCommandName === "Add-JS-Code") {
    alert(
      "You must start a JavaScript block with 'Start-Javascript' before adding code."
    );
    return;
  }

  var commandData = {
    arguments: {},
  };

  var argGroups = argsContainer.querySelectorAll(".arg-group");

  argGroups.forEach((group) => {
    var argLabel = group.querySelector(".arg-label").textContent.slice(0, -1);
    var key = argLabel.toLowerCase().replace(" ", "-");

    var textInput = group.querySelector(".arg-text-input");
    if (textInput) {
      commandData.arguments[key] = textInput.value;
      return;
    }
  });

  document
    .querySelectorAll('.arg-option input[type="radio"]:checked')
    .forEach((arg) => {
      commandData.arguments[arg.name] = arg.value;
    });

  console.log("--- Executing Command ---");

  var commandText;

  if (selectedCommandName === "Start-Javascript") {
    commandText = '{"start-javascript": ""}';
  } else if (selectedCommandName === "End-Javascript") {
    commandText = '{"end-javascript": ""}';
  } else if (selectedCommandName === "Add-JS-Code") {
    let rawCode = commandData.arguments["javascript-code"];
    // let escapedCode = rawCode
    //   .replace(/\\/g, "\\\\")
    //   .replace(/"/g, '\\"')
    //   .replace(/\n/g, "\\n")
    //   .replace(/\r/g, "\\r");
    let b64Code = convertUTF8toBase64(rawCode);

    // commandText = `{"add-to-js": "${escapedCode}"}`;
    commandText = `{"add-to-js": "${b64Code}"}`;
  } else {
    // For all other commands (with arguments), stringify the arguments
    commandText = JSON.stringify(commandData.arguments);
  }

  addCommandToCommandList(commandText, true);

  let stateTransitionOccurred = false; // Flag to stop general selection logic

  if (selectedCommandName === "Start-Javascript") {
    jsMode = true;
    // Auto-switches to 'Add-JS-Code' and renders
    var nextCommand = commandCollection.find(
      (cmd) => cmd.commandName === "Add-JS-Code"
    );
    commandSelect.value = "Add-JS-Code";
    renderArguments(nextCommand);
    stateTransitionOccurred = true;
  } else if (selectedCommandName === "Add-JS-Code") {
    // Auto-switches to 'End-Javascript' and renders
    jsMode = false;
    var nextCommand = commandCollection.find(
      (cmd) => cmd.commandName === "End-Javascript"
    );
    commandSelect.value = "End-Javascript";
    renderArguments(nextCommand);
    stateTransitionOccurred = true; // Prevents the override below
  }

  updateCommandComboBoxState();
  var browserExists = Object.values(commands).some((cmd) =>
    cmd.includes('"browser":')
  );

  // Only runs general command selection if NO specific state transition just happened and jsMode is false
  if (
    browserExists &&
    commandCollection.length > 1 &&
    !jsMode &&
    !stateTransitionOccurred
  ) {
    var nextCommand = commandCollection[1];
    commandSelect.value = nextCommand.commandName;

    renderArguments(nextCommand);

    console.log(
      `Switched combobox to: ${nextCommand.commandName} and rendered arguments.`
    );
  }
});

duplicateCommandButton.addEventListener("click", (e) => {
  duplicateSelectedCommand();
});

removeCommandButton.addEventListener("click", (e) => {
  removeSelectedCommand();
});

populateCommandSelect();
loadCurrentScriptCommands();
