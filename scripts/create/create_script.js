var commandTemplate = {
  // command: selectedCommandName,
  arguments: {},
};

var commands = getData();

var commandSelect = document.getElementById("command-select");
var argsContainer = document.getElementById("command-arguments-container");
var descriptionElement = document.getElementById("command-description");
var executeButton = document.getElementById("execute-command-btn");

// var

function checkEntries() {
  for (var i = 0; i < commands.length; i++) {
    if (commands[i].arguments.includes("browser")) {
      alert("works");
    }
  }
}

function getData() {
  try {
    return JSON.parse(localStorage.getItem("commands") ?? commandTemplate);
  } catch {
    return commandTemplate;
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

  commands[Object.keys(commands).length] = JSON.stringify(
    commandData.arguments
  );

  setData(commands);
  console.log(getData());
  checkEntries();
  // alert(
  //   `Command: ${commandData.command} \nArguments: ${JSON.stringify(
  //     commandData.arguments,
  //     null,
  //     2
  //   )} \nCheck the browser console for the structured data.`
  // );
});

populateCommandSelect();
