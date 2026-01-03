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
var featuresAllowed = false;
var visitAdded = false;

// List of all proxy-related feature commands
const proxyFeatures = [
  "use-http-proxy",
  "use-https-proxy",
  "use-socks4-proxy",
  "use-socks5-proxy",
];

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
  localStorage.clear();
  window.location.reload(true);
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

function duplicateSelectedCommand() {
  var selectedChild = document.querySelector(".list-item");
  if (!selectedChild) {
    createAlert("warning", "Please select an element to duplicate.");
    throw new Error("No element selected");
  }

  var commandText = selectedChild.textContent;

  if (commandText.includes('"browser":')) {
    createAlert(
      "warning",
      "Unable to duplicate the browser command, only one of these commands may be present in any given script."
    );
    return;
  }

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
      createAlert("error", "No commands present in the current script.");
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

function recalculateState() {
  featuresAllowed = false;
  visitAdded = false;

  const scriptCommands = Object.values(commands);
  const browserExists = scriptCommands.some((cmd) =>
    cmd.includes('"browser":')
  );

  if (!browserExists) {
    return;
  }

  visitAdded = scriptCommands.some((cmd) => cmd.includes('"visit":'));

  featuresAllowed = browserExists && !visitAdded;
}

function populateCommandSelect() {
  commandSelect.innerHTML = "";

  recalculateState();

  const browserExists = Object.values(commands).some((cmd) =>
    cmd.includes('"browser":')
  );
  let selectedCommandName = commandSelect.value;
  let hasSetInitialSelection = false;

  commandCollection.forEach((command) => {
    var option = document.createElement("option");
    option.value = command.commandName;
    option.textContent = command.commandName;

    let isDisabled = true;

    if (command.commandName === "Browser") {
      isDisabled = browserExists;
      if (!browserExists && !hasSetInitialSelection) {
        selectedCommandName = command.commandName;
        hasSetInitialSelection = true;
      }
    } else if (command.commandName === "Visit") {
      isDisabled = !featuresAllowed || visitAdded;
      if (!isDisabled && !hasSetInitialSelection) {
        selectedCommandName = command.commandName;
        hasSetInitialSelection = true;
      }
    } else if (command.commandName.includes("Feature:")) {
      isDisabled = !featuresAllowed;
      if (!isDisabled && !hasSetInitialSelection) {
        selectedCommandName = command.commandName;
        hasSetInitialSelection = true;
      }
    } else {
      isDisabled = !visitAdded;
      if (!isDisabled && !hasSetInitialSelection) {
        selectedCommandName = command.commandName;
        hasSetInitialSelection = true;
      }
    }

    option.disabled = isDisabled;
    commandSelect.appendChild(option);
  });

  const finalSelection = commandSelect.querySelector(
    `option[value="${selectedCommandName}"]`
  );

  if (finalSelection && !finalSelection.disabled) {
    commandSelect.value = selectedCommandName;
  } else {
    const firstAvailable = commandSelect.querySelector(
      "option:not([disabled])"
    );
    if (firstAvailable) {
      commandSelect.value = firstAvailable.value;
    }
  }

  const finalSelectedCommand = commandCollection.find(
    (cmd) => cmd.commandName === commandSelect.value
  );
  if (finalSelectedCommand) {
    renderArguments(finalSelectedCommand);
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
  recalculateState();
  populateCommandSelect();
  console.log(getData());
}

function removeSelectedCommand() {
  var selectedChild = document.querySelector(".list-item");
  if (!selectedChild) {
    createAlert("error", "Please select an element to remove.");
    throw new Error("No element selected");
  }

  var index = getIndexOfSelectedCommand();
  var removedCommandText = commands[index];

  if (removedCommandText.includes('"browser":')) {
    createAlert(
      "error",
      "Unable to remove the browser command, this is a requirement for every script."
    );
    return;
  }

  let isJsBlockCommand =
    removedCommandText.includes("start-javascript") ||
    removedCommandText.includes("add-to-js") ||
    removedCommandText.includes("end-javascript");

  if (isJsBlockCommand) {
    let startIndex = index;
    if (removedCommandText.includes("add-to-js")) {
      startIndex = index - 1;
    } else if (removedCommandText.includes("end-javascript")) {
      startIndex = index - 2;
    }

    if (startIndex < 0) {
      startIndex = 0;
    }

    const indicesToRemove = [];
    if (
      commands[startIndex] &&
      commands[startIndex].includes("start-javascript")
    )
      indicesToRemove.push(startIndex);
    if (
      commands[startIndex + 1] &&
      commands[startIndex + 1].includes("add-to-js")
    )
      indicesToRemove.push(startIndex + 1);
    if (
      commands[startIndex + 2] &&
      commands[startIndex + 2].includes("end-javascript")
    )
      indicesToRemove.push(startIndex + 2);

    indicesToRemove
      .sort((a, b) => b - a)
      .forEach((idx) => {
        if (commandList.children[idx]) {
          commandList.children[idx].remove();
        }
      });

    jsMode = false;

    reindexCommands();
    nextIndexAfterDelete = startIndex > 0 ? startIndex - 1 : 0;
    var child = commandList.children[nextIndexAfterDelete];
    if (child) {
      child.classList.add("list-item");
    }
    return;
  }

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

    if (
      Array.isArray(argOptions) &&
      argOptions.length === 0 &&
      !command.isCodeBlock
    ) {
      return;
    }

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
      argsContainer.appendChild(argGroup);
    } else {
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

      argsContainer.appendChild(argGroup);
    }
  });
}

function safeB64Encode(str) {
  var utf8Bytes = new TextEncoder().encode(str);
  let binaryString = "";
  var len = utf8Bytes.byteLength;
  for (let i = 0; i < len; i++) {
    binaryString += String.fromCharCode(utf8Bytes[i]);
  }
  return btoa(binaryString);
}

function setData(data) {
  localStorage.removeItem("commands");
  localStorage.setItem("commands", JSON.stringify(data));
}

function validateArguments(selectedCommandName, commandArgs) {
  const selectedCommand = commandCollection.find(
    (cmd) => cmd.commandName === selectedCommandName
  );

  if (!selectedCommand) {
    createAlert("error", "Command definition not found.");
    return false;
  }

  const argDefinitions = selectedCommand.commandArgs;

  for (const [argKey, argValue] of Object.entries(commandArgs)) {
    if (argDefinitions[argKey] === null) {
      const isQuotedString =
        argValue.startsWith('"') &&
        argValue.endsWith('"') &&
        argValue.length > 1;

      if (selectedCommandName === "Wait-For-Seconds" && argKey === "seconds") {
        const numericValue = parseFloat(argValue);
        if (isNaN(numericValue) || !isFinite(numericValue)) {
          createAlert(
            "error",
            `The value for '${argKey}' in 'Wait-For-Seconds' must be a valid number (i.e, 2 or 0.5) and must not be quoted.`
          );
          return false;
        }
      } else if (
        selectedCommandName === "Add-Headers" &&
        argKey === "headers"
      ) {
        if (!isQuotedString) {
          createAlert(
            "error",
            `The value for 'headers' in 'Add-Headers' must be a single quoted JSON string (i.e., '"{\\"Header\\": \\"Value\\"}"').`
          );
          return false;
        }
        const jsonContent = argValue.slice(1, -1);
        try {
          JSON.parse(jsonContent);
        } catch (e) {
          createAlert(
            "error",
            `The content inside the quotes for 'headers' in 'Add-Headers' must be valid JSON.`
          );
          return false;
        }
      } else if (
        (selectedCommand.placeholder &&
          selectedCommand.placeholder.startsWith('"')) ||
        selectedCommandName === "Add-Header" ||
        selectedCommandName === "Click-At-Position" ||
        selectedCommandName === "Fill-Text" ||
        selectedCommandName === "Fill-Text-Exp" ||
        selectedCommandName === "Open-New-Tab" ||
        selectedCommandName === "Select-Option" ||
        selectedCommandName.startsWith("Feature: use-") || // Includes proxy features
        selectedCommandName.startsWith("Feature: add-") // Add-Extension
      ) {
        if (!isQuotedString) {
          createAlert(
            "error",
            `The value for '${argKey}' in '${selectedCommandName}' must be a quoted string (i.e., '"value"').`
          );
          return false;
        }
      } else if (selectedCommandName === "Add-JS-Code") {
        continue;
      }
    }
  }

  return true;
}

function validateScriptContents() {
  var commandEntries = Object.values(commands);
  if (commandEntries === "undefined" || commandEntries.length === 0) {
    createAlert(
      "error",
      "Please add commands before trying to validate a script's contents."
    );
    throw new Error(
      "Please add commands before trying to validate a script's contents."
    );
  }

  var scriptLines = [];
  try {
    for (const rawEntry of commandEntries) {
      const parsedObj = JSON.parse(rawEntry);

      const key = Object.keys(parsedObj)[0];

      const rawValue = Object.values(parsedObj)[0];

      scriptLines.push(`${key} ${rawValue}`);
    }
  } catch (e) {
    createAlert("error", `Error processing script commands: ${e.message}`);
    console.error("Script Command Processing Error:", e);
    return;
  }

  const rawContents = scriptLines.join("\n");

  let b64Contents;
  try {
    b64Contents = safeB64Encode(rawContents);
  } catch (e) {
    createAlert("error", "Failed to Base64 encode script contents.");
    console.error("Base64 Encoding Error:", e);
    return;
  }

  const finalUrl = `${validateScriptURL}?contents=${b64Contents}`;

  fetch(finalUrl, {
    method: "GET",
    signal: AbortSignal.timeout(5000),
  })
    .then((response) => {
      if (!response.ok) {
        throw new Error(
          `Invalid HTTP status: ${response.status} ${response.statusText}`
        );
      }
      return response.json();
    })
    .then((data) => {
      if (data.success) {
        createAlert("info", "Script validation successful.");
        console.log("Validation details:", data);
      } else {
        createAlert(
          "error",
          `Script validation failed, ${data.error || "No details provided."}`
        );
        console.error("Validation failed response:", data);
      }
    })
    .catch((error) => {
      const errorMessage =
        error.message || "A network or connection error occurred.";
      createAlert(
        "error",
        `Validation request failed.<br/>Error: ${errorMessage}`
      );
      console.error("Validation Fetch Error:", error);
    });
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
    createAlert("error", "Please select a command first.");
    return;
  }

  if (jsMode && selectedCommandName !== "Add-JS-Code") {
    createAlert(
      "error",
      "You are inside a JavaScript block. The next command must be 'Add-JS-Code'."
    );
    return;
  }
  if (!jsMode && selectedCommandName === "Add-JS-Code") {
    createAlert(
      "error",
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

  if (!validateArguments(selectedCommandName, commandData.arguments)) {
    return;
  }

  const selectedCommand = commandCollection.find(
    (cmd) => cmd.commandName === selectedCommandName
  );
  if (!selectedCommand) {
    createAlert("error", "Error: Command definition not found.");
    return;
  }

  // --- Feature Command Validation for Duplicates and Proxies ---
  if (selectedCommandName.startsWith("Feature:")) {
    const selectedFeatureName = selectedCommandName
      .toLowerCase()
      .replace("feature: ", "");

    // Checks for duplicate Feature command
    const isDuplicateFeature = Object.values(commands).some((commandString) => {
      try {
        const commandObject = JSON.parse(commandString);
        return commandObject.feature === selectedFeatureName;
      } catch (e) {
        return false;
      }
    });

    if (isDuplicateFeature) {
      createAlert(
        "error",
        `The feature command '${selectedCommandName}' can only be added once to the script.`
      );
      return;
    }

    // Check for multiple proxy features
    if (proxyFeatures.includes(selectedFeatureName)) {
      let otherProxyExists = false;
      for (const commandString of Object.values(commands)) {
        try {
          const commandObject = JSON.parse(commandString);
          // Check if any proxy features are already in the list
          if (
            commandObject.feature &&
            proxyFeatures.includes(commandObject.feature) &&
            commandObject.feature !== selectedFeatureName
          ) {
            otherProxyExists = true;
            break;
          }
        } catch (e) {}
      }

      if (otherProxyExists) {
        createAlert(
          "error",
          "Only one proxy feature (http, https, socks4, or socks5) is allowed in a single script."
        );
        return;
      }
    }
  }
  // --- END Feature Command Validation ---

  console.log("--- Adding Command ---");

  var commandText;

  if (selectedCommandName === "Start-Javascript") {
    commandText = '{"start-javascript": ""}';
  } else if (selectedCommandName === "End-Javascript") {
    commandText = '{"end-javascript": ""}';
  } else if (selectedCommandName === "Add-JS-Code") {
    let rawCode = commandData.arguments["javascript-code"];
    let b64Code = convertUTF8toBase64(rawCode);

    commandText = `{"add-to-js": "${b64Code}"}`;
  } else if (selectedCommandName.startsWith("Feature:")) {
    const featureName = selectedCommandName
      .toLowerCase()
      .replace("feature: ", "");

    const payload = {
      feature: `"${featureName}"`,
    };

    // Adding all arguments to the payload to be serialized.
    if (
      commandData.arguments &&
      Object.keys(commandData.arguments).length > 0
    ) {
      Object.assign(payload, commandData.arguments);
    }

    // Serializing of the command string.
    commandText = JSON.stringify(payload);
  } else {
    const argKeys = Object.keys(commandData.arguments);

    const formattedCommandName = selectedCommandName
      .toLowerCase()
      .replace(/: /g, "-")
      .replace(/-/g, "-");

    if (argKeys.length === 1 && selectedCommandName !== "Browser") {
      const argValue = commandData.arguments[argKeys[0]];
      const finalObject = {};
      finalObject[formattedCommandName] = argValue;
      commandText = JSON.stringify(finalObject);
    } else {
      commandText = JSON.stringify(commandData.arguments);
    }
  }

  addCommandToCommandList(commandText, true);

  if (selectedCommandName === "Start-Javascript") {
    jsMode = true;
    var nextCommand = commandCollection.find(
      (cmd) => cmd.commandName === "Add-JS-Code"
    );
    commandSelect.value = "Add-JS-Code";
    renderArguments(nextCommand);
  } else if (selectedCommandName === "Add-JS-Code") {
    jsMode = false;
    var nextCommand = commandCollection.find(
      (cmd) => cmd.commandName === "End-Javascript"
    );
    commandSelect.value = "End-Javascript";
    renderArguments(nextCommand);
  }
});

duplicateCommandButton.addEventListener("click", (e) => {
  duplicateSelectedCommand();
});

removeCommandButton.addEventListener("click", (e) => {
  removeSelectedCommand();
});

validateScriptButton.addEventListener("click", (e) => {
  validateScriptContents();
});

loadCurrentScriptCommands();
