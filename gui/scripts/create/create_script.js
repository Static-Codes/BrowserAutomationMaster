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

  if (commands[index].includes('"browser":')) {
    alert(
      "Unable to remove the browser command, this is a requirement for every script."
    );
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

  var jsonString = JSON.stringify(commandData.arguments);

  addCommandToCommandList(jsonString, true);

  updateCommandComboBoxState();
});

duplicateCommandButton.addEventListener("click", (e) => {
  duplicateSelectedCommand();
});

removeCommandButton.addEventListener("click", (e) => {
  removeSelectedCommand();
});

populateCommandSelect();
loadCurrentScriptCommands();
