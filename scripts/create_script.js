const commandCollection = [
  {
    commandName: "CreateFile",
    commandArgs: {
      fileName: null,
      fileType: ["js", "css", "html", "md"],
      encoding: ["UTF-8", "ASCII"],
    },
    commandDescription:
      "Creates a new file of a specified type and encoding. Requires a file name.",
  },
  {
    commandName: "UpdatePermissions",
    commandArgs: {
      targetPath: null,
      mode: ["Read-Only", "Read-Write", "Full-Control"],
      recursive: ["Yes", "No"],
    },
    commandDescription:
      "Modifies the access permissions for a file or directory path.",
  },
  {
    commandName: "ArchiveData",
    commandArgs: {
      sourceDir: null,
      outputFormat: ["zip", "tar.gz"],
    },
    commandDescription:
      "Compresses the contents of a source directory into an archive file.",
  },
];

const commandSelect = document.getElementById("command-select");
const argsContainer = document.getElementById("command-arguments-container");
const descriptionElement = document.getElementById("command-description");
const executeButton = document.getElementById("execute-command-btn");

function populateCommandSelect() {
  const defaultOption = document.createElement("option");
  defaultOption.value = "";
  defaultOption.textContent = "--- Select a Command ---";
  defaultOption.disabled = true;
  defaultOption.selected = true;
  commandSelect.appendChild(defaultOption);

  commandCollection.forEach((command) => {
    const option = document.createElement("option");
    option.value = command.commandName;
    option.textContent = command.commandName;
    commandSelect.appendChild(option);
  });
}

function renderArguments(command) {
  argsContainer.innerHTML = "";
  descriptionElement.textContent = command.commandDescription;

  const argKeys = Object.keys(command.commandArgs);

  argKeys.forEach((argName) => {
    const argOptions = command.commandArgs[argName];
    const argGroup = document.createElement("div");
    argGroup.classList.add("arg-group");

    // Create a label for the argument
    const label = document.createElement("label");
    label.classList.add("arg-label");
    label.textContent =
      argName.charAt(0).toUpperCase() + argName.slice(1) + ":";

    argGroup.appendChild(label);

    if (argOptions && argOptions.length > 0) {
      const optionsContainer = document.createElement("div");
      optionsContainer.classList.add("arg-options-container");

      argOptions.forEach((optionValue, index) => {
        const optionWrapper = document.createElement("label");
        optionWrapper.classList.add("arg-option");

        const radioInput = document.createElement("input");
        radioInput.type = "radio";
        radioInput.name = argName;
        radioInput.value = optionValue;
        radioInput.id = `${command.commandName}-${argName}-${optionValue}`;
        if (index === 0) {
          radioInput.checked = true;
        }

        const customRadio = document.createElement("span");
        customRadio.classList.add("radio-custom");

        const optionText = document.createElement("span");
        optionText.textContent = optionValue;

        optionWrapper.appendChild(radioInput);
        optionWrapper.appendChild(customRadio);
        optionWrapper.appendChild(optionText);

        optionsContainer.appendChild(optionWrapper);
      });
      argGroup.appendChild(optionsContainer);
    } else {
      const textInput = document.createElement("input");
      textInput.type = "text";
      textInput.classList.add("arg-text-input");
      textInput.name = argName;
      textInput.placeholder = `Enter value for ${argName}`;
      argGroup.appendChild(textInput);
    }

    argsContainer.appendChild(argGroup);
  });
}

commandSelect.addEventListener("change", (event) => {
  const selectedCommandName = event.target.value;
  const selectedCommand = commandCollection.find(
    (cmd) => cmd.commandName === selectedCommandName
  );

  if (selectedCommand) {
    renderArguments(selectedCommand);
  } else {
    argsContainer.innerHTML = "";
    descriptionElement.textContent = "";
  }
});

executeButton.addEventListener("click", () => {
  const selectedCommandName = commandSelect.value;
  if (!selectedCommandName) {
    alert("Please select a command first.");
    return;
  }

  const commandData = {
    command: selectedCommandName,
    arguments: {},
  };

  const argGroups = argsContainer.querySelectorAll(".arg-group");

  argGroups.forEach((group) => {
    const argLabel = group.querySelector(".arg-label").textContent.slice(0, -1); // Remove the trailing ':'

    const textInput = group.querySelector(".arg-text-input");
    if (textInput) {
      commandData.arguments[argLabel] = textInput.value;
      return;
    }

    const radioInput = group.querySelector(`input[name="${argLabel}"]:checked`);
    if (radioInput) {
      commandData.arguments[argLabel] = radioInput.value;
      return;
    }
  });

  console.log("--- Executing Command ---");
  console.log(commandData);
  alert(
    `Command: ${commandData.command} \nArguments: ${JSON.stringify(
      commandData.arguments,
      null,
      2
    )} \nCheck the browser console for the structured data.`
  );
});

populateCommandSelect();
