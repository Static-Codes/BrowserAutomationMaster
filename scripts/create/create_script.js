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
var exportScriptButton = document.querySelector(
  ".action-button.export-script-btn"
);

var nextIndexAfterDelete = 0;
var scriptIsValidated = false;

function addCommandToCommandList(commandText, addToLocalStorage = true) {
  try {
    var childNode = document.createElement("li");

    if (document.querySelector(".list-item")) {
      // Changes the state of the previously selected item
      document.querySelector(".list-item").classList.remove("list-item");
    }

    // Makes the newly added item the active selection
    childNode.classList.add("list-item");
    childNode.textContent = commandText;

    commandList.appendChild(childNode);
    if (addToLocalStorage) {
      addCmdToLocalStorage(childNode.textContent);
    }
    console.log(
      `Added element ${Object.keys(commands).length} at index ${
        Object.keys(commands).length - 1
      }`
    );
    refocusSelection();
  } catch (e) {
    console.log(e);
  }
}

function addCmdToLocalStorage(value) {
  commands[Object.keys(commands).length] = value;
}

function getData() {
  try {
    var commandsFromLS = localStorage.getItem("commands");
    if (commandsFromLS == null) {
      localStorage.setItem("commands", JSON.stringify({}));
    }
    return JSON.parse(localStorage.getItem("commands"));
  } catch (e) {
    console.log(e);
    localStorage.setItem("commands", JSON.stringify({}));
    return JSON.parse(localStorage.getItem("commands"));
  }
}

function populateCommandSelect() {
  commandCollection.forEach((command) => {
    var option = document.createElement("option");
    option.value = command.commandName;
    option.disabled = command.disabledOnLoad;
    option.textContent = command.commandName;

    if (command.commandName == "Browser") {
      renderArguments(command);
      option.selected = true;
    }
    commandSelect.appendChild(option);
  });
}

function duplicateSelectedCommand() {
  var index = getIndexOfSelectedCommand();
  if (index == -1) {
    alert("Please select an element to remove.");
    throw new Error("No element selected");
  }
  addCmdToLocalStorage(commands[index]);

  var selectedChild = document.querySelector(".list-item");
  var clonedChild = selectedChild.cloneNode(true);
  addCommandToCommandList(clonedChild.textContent);
}

function getIndexOfSelectedCommand() {
  try {
    if (Object.keys(commands).length == 0) {
      alert("No commands present.");
      throw new Error("No commands present.");
    }
    var child = document.querySelector(".list-item");
    var parent = child.parentNode;
    return Array.prototype.indexOf.call(parent.children, child);
  } catch (e) {
    return -1;
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

  if (usingOtter) {
    for (cmd in commandItems) {
      cmd.addEventListener("click", handleSelection);
    }
  } else {
    commandItems.forEach((item) => {
      item.addEventListener("click", handleSelection);
    });
  }
}

function refocusSelection() {
  commandList.addEventListener("click", (e) => {
    const clickedItem = e.target.closest("li");

    if (clickedItem) {
      commandList.querySelectorAll(".list-item").forEach((item) => {
        item.classList.remove("list-item");
      });

      clickedItem.classList.add("list-item");
    }
  });
}

function removeSelectedCommand() {
  var index = getIndexOfSelectedCommand();
  if (index == -1) {
    alert("Please select an element to remove.");
    throw new Error("No element selected");
  }

  var cmdCount = Object.keys(commands).length;
  var numberOfShownItems = document.querySelectorAll("#command-list li").length;
  if (cmdCount == 1) {
    nextIndexAfterDelete = 0;
  } else if (index === 0 && nextIndexAfterDelete !== 0) {
    nextIndexAfterDelete = 0;
  } else if (index === cmdCount) {
    nextIndexAfterDelete = index - 1;
  } else if (numberOfShownItems !== cmdCount) {
    nextIndexAfterDelete = 0;
  }

  console.log(`next selected index: ${nextIndexAfterDelete}`);
  try {
    delete commands[index + 1];
    console.log(`deleted element at index ${index + 1}`);
    document.querySelector(".list-item").remove();
    console.log(`deleted element .list-item`);
    commandList.children[nextIndexAfterDelete].classList.add("list-item");
    setData(commands);
    refocusSelection();
  } catch (e) {
    console.log(e);
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

    // Create a label for the argument
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

window.addEventListener("load", (e) => {
  Object.values(commands).forEach((el) => addCommandToCommandList(el, false));
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
    // command: selectedCommandName,
    arguments: {},
  };

  var argGroups = argsContainer.querySelectorAll(".arg-group");

  // Includes any textbox inputs
  argGroups.forEach((group) => {
    var argLabel = group.querySelector(".arg-label").textContent.slice(0, -1); // Remove the trailing ':'

    var textInput = group.querySelector(".arg-text-input");
    if (textInput) {
      commandData.arguments[argLabel.toLowerCase().replace(" ", "-")] =
        textInput.value;
      return;
    }
  });

  // Includes any radiobox inputs
  document
    .querySelectorAll('.arg-option input[type="radio"]:checked')
    .forEach((arg) => {
      commandData.arguments[arg.name] = arg.value;
    });

  console.log("--- Executing Command ---");
  console.log(JSON.stringify(commandData));

  var jsonString = JSON.stringify(commandData.arguments);
  commands[Object.keys(commands).length] = jsonString;

  setData(commands);
  console.log(getData());
  updateCommandComboBoxState();
  addCommandToCommandList(jsonString);
});

duplicateCommandButton.addEventListener("click", (e) => {
  duplicateSelectedCommand();
});
removeCommandButton.addEventListener("click", (e) => {
  removeSelectedCommand();
});

populateCommandSelect();
loadCurrentScriptCommands();
