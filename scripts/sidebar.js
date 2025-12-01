const usingOtter = navigator.userAgent.includes("Otter");

// Will be used in Create, View and Delete Script Buttons
var originalSection = null;
var currentSection = null;

// Waits for the DOM to fully load
window.onload = function () {
  setTimeout(function () {}, 2000);
  const html = document.documentElement;
  const body = document.body;

  currentSection = document.querySelector(".command-combobox-section");

  // const loadScriptBtn = document.querySelector("#loadScript");
  // const deleteScriptBtn = document.querySelector("#deleteScript");
  const collapseBtn = document.querySelector(".sidebar .collapse-btn");
  const toggleMobileMenu = document.querySelector(".toggle-mob-menu");
  const switchInput = document.querySelector(".switch input");
  const switchLabel = document.querySelector(".switch label");
  const switchLabelText = switchLabel.querySelector("span:last-child");

  if (usingOtter) {
    switchLabel.style.display = "flex";
    switchLabelText.style.marginLeft = "1rem";
  }

  const menuLinks = document.querySelectorAll(".sidebar a");

  // Error handling for required elements
  if (
    !switchLabel ||
    !menuLinks.length ||
    !collapseBtn ||
    !toggleMobileMenu ||
    !switchInput
  ) {
    console.error("Critical UI element missing. Check DOM structure.");
    document.body.innerHTML =
      "<h1>Error: Unable to load GUI, please try again.</h1>";
    return;
  }

  if (!switchLabelText) {
    console.error("Switch label text element missing.");
    document.body.innerHTML =
      "<h1>Error: Unable to load GUI, please try again.</h1>";
    return;
  }

  const collapsedClass = "collapsed";
  const lightModeClass = "light-mode";

  /* INITIAL DARK/LIGHT MODE CHECK (using localStorage) */
  const darkModeSetting = localStorage.getItem("dark-mode");

  if (darkModeSetting === "false") {
    html.classList.add(lightModeClass);
    switchInput.checked = false; // Uncheck for light mode
    switchLabelText.textContent = "Light";
  } else {
    // Default or 'true' (Dark Mode)
    switchInput.checked = true; // Check for dark mode
    switchLabelText.textContent = "Dark";
  }

  function getFileNameSelected() {
    var selectElement = document.querySelector("#command-select");
    if (selectElement == null) {
      alert("Combobox element not found, please make a bug report.");
      throw new Error("");
    }

    var selectedOption = selectElement.querySelector(`option[selected]`);
    if (!selectedOption) {
      alert("No selected option found, please make a bug report.");
      throw new Error("");
    }

    var scriptName = Object.keys(localUserScripts).at((script) =>
      script.includes(selectedOption.textContent)
    );
    return scriptName;
  }

  // function swapToViewSection() {
  //   if (
  //     (currentSection && currentSection.id == "create") ||
  //     currentSection.id == "delete"
  //   ) {
  //     originalSection = currentSection;

  //     // <section id="view" class="command-combobox-section">
  //     var viewSectionEl = document.createElement("section");
  //     viewSectionEl.id = "view";
  //     viewSectionEl.classList.add("command-combobox-section");

  //     // <div class="combobox-container">
  //     var containerEl = viewSectionEl.appendChild(
  //       document.createElement("div")
  //     );
  //     containerEl.classList.add("combobox-container");

  //     // <label for="command-select" class="combobox-label">Select File:</label>
  //     var label = containerEl.appendChild(document.createElement("label"));
  //     label.for = "command-select";
  //     label.classList.add("combobox-label");
  //     label.textContent = "Select a File";

  //     // <select id="command-select" class="combobox-input"></select>
  //     var select = containerEl.appendChild(document.createElement("select"));
  //     select.id = "command-select";
  //     select.classList.add("combobox-input");
  //     var scriptIndex = 0;

  //     // Appends each file loaded from localUserScript as a child <option> of parent <select>.
  //     Object.keys(localUserScripts).forEach((key) => {
  //       var substring = null;

  //       if (window.navigator.userAgent.includes("Windows")) {
  //         substring = "\\";
  //       } else {
  //         substring = "/";
  //       }

  //       var index = key.lastIndexOf(substring);
  //       if (index == -1) {
  //         return;
  //         // throw Error("Unable to determine substring, the platform logic needs to be adjusted.");
  //       }

  //       adjustedIndex = index + 1;
  //       var fileName = key.substring(adjustedIndex);
  //       var selectOption = document.createElement("option");
  //       selectOption.textContent = fileName;
  //       selectOption.value = scriptIndex;

  //       if (selectOption == 0) {
  //         selectOption.setAttribute("selected", "");
  //       }
  //       select.appendChild(selectOption);
  //       scriptIndex++;
  //     });

  //     var lastSelectedOption =
  //       select.querySelector("option") || select.options[0];
  //     lastSelectedOption.setAttribute("selected", "");

  //     select.onchange = function (e) {
  //       var newSelectedOption = select.querySelector(
  //         `option[value="${e.target.value}"]`
  //       );
  //       if (lastSelectedOption) {
  //         lastSelectedOption.removeAttribute("selected");
  //       }
  //       if (newSelectedOption) {
  //         newSelectedOption.setAttribute("selected", "");
  //         lastSelectedOption = newSelectedOption;
  //       }
  //     };

  //     // <button class="execute-button" id="execute-command-btn">Load Selected</button>
  //     var button = viewSectionEl.appendChild(document.createElement("button"));
  //     button.classList.add("execute-button");
  //     button.id = "select-file-btn";
  //     button.textContent = "Load Selected";

  //     button.onclick = function () {
  //       var fileName = getFileNameSelected();
  //       if (fileName) {
  //         alert(fileName);
  //       }
  //     };

  //     var parentEl = currentSection.parentNode;

  //     if (parentEl) {
  //       parentEl.replaceChild(viewSectionEl, currentSection);
  //       currentSection = viewSectionEl;
  //       console.log("Successfully swapped to view section.");
  //     } else {
  //       console.error("Failed to swap to view section, parentNode not found.");
  //     }
  //   }
  // }

  // function swapToDeleteSection() {
  //   if (
  //     (currentSection && currentSection.id == "create") ||
  //     currentSection.id == "view"
  //   ) {
  //     originalSection = currentSection;

  //     // <section id="delete" class="command-combobox-section">
  //     var deleteSectionEl = document.createElement("section");
  //     deleteSectionEl.id = "delete";
  //     deleteSectionEl.classList.add("command-combobox-section");

  //     // <div class="combobox-container">
  //     var containerEl = deleteSectionEl.appendChild(
  //       document.createElement("div")
  //     );
  //     containerEl.classList.add("combobox-container");

  //     // <label for="command-select" class="combobox-label">Select File:</label>
  //     var label = containerEl.appendChild(document.createElement("label"));
  //     label.for = "command-select";
  //     label.classList.add("combobox-label");
  //     label.textContent = "Select a File";

  //     // <select id="command-select" class="combobox-input"></select>
  //     var select = containerEl.appendChild(document.createElement("select"));
  //     select.id = "command-select";
  //     select.classList.add("combobox-input");
  //     var scriptIndex = 0;

  //     // Appends each file loaded from localUserScript as a child <option> of parent <select>.
  //     Object.keys(localUserScripts).forEach((key) => {
  //       var substring = null;

  //       if (window.navigator.userAgent.includes("Windows")) {
  //         substring = "\\";
  //       } else {
  //         substring = "/";
  //       }

  //       var index = key.lastIndexOf(substring);
  //       if (index == -1) {
  //         return;
  //         // throw Error("Unable to determine substring, the platform logic needs to be adjusted.");
  //       }

  //       adjustedIndex = index + 1;
  //       var fileName = key.substring(adjustedIndex);
  //       var selectOption = document.createElement("option");
  //       selectOption.textContent = fileName;
  //       selectOption.value = scriptIndex;

  //       if (selectOption == 0) {
  //         selectOption.setAttribute("selected", "");
  //       }
  //       select.appendChild(selectOption);
  //       scriptIndex++;
  //     });

  //     var lastSelectedOption =
  //       select.querySelector("option") || select.options[0];
  //     lastSelectedOption.setAttribute("selected", "");

  //     select.onchange = function (e) {
  //       var newSelectedOption = select.querySelector(
  //         `option[value="${e.target.value}"]`
  //       );
  //       if (lastSelectedOption) {
  //         lastSelectedOption.removeAttribute("selected");
  //       }
  //       if (newSelectedOption) {
  //         newSelectedOption.setAttribute("selected", "");
  //         lastSelectedOption = newSelectedOption;
  //       }
  //     };

  //     // <button class="execute-button" id="execute-command-btn">Delete Selected</button>
  //     var button = deleteSectionEl.appendChild(
  //       document.createElement("button")
  //     );
  //     button.classList.add("execute-button");
  //     button.id = "select-file-btn";
  //     button.textContent = "Delete Selected";

  //     button.onclick = function () {
  //       var fileName = getFileNameSelected();
  //       if (fileName) {
  //         alert(fileName);
  //       }
  //     };

  //     button.onclick = function () {
  //       var selectedOption = select.querySelector(
  //         `option[value="${e.target.value}"]`
  //       );
  //       if (!selectedOption) {
  //         alert("No selected option found, please make a bug report.");
  //         throw new Error("");
  //       }
  //       var scriptName = Object.keys(localUserScripts).at((script) =>
  //         script.includes(selectedOption.textContent)
  //       );

  //       alert(scriptName);
  //     };

  //     var parentEl = currentSection.parentNode;

  //     if (parentEl) {
  //       parentEl.replaceChild(deleteSectionEl, currentSection);
  //       currentSection = deleteSectionEl;
  //       console.log("Successfully swapped to delete section.");
  //     } else {
  //       console.error(
  //         "Failed to swap to delete section, parentNode not found."
  //       );
  //     }
  //   }
  // }

  // loadScriptBtn.addEventListener("click", swapToViewSection);
  // deleteScriptBtn.addEventListener("click", swapToDeleteSection);

  /* TOGGLE HEADER STATE (Collapse/Expand) */
  collapseBtn.addEventListener("click", function () {
    body.classList.toggle(collapsedClass);

    const isExpanded = this.getAttribute("aria-expanded") === "true";
    this.setAttribute("aria-expanded", String(!isExpanded));

    const newLabel = isExpanded ? "expand menu" : "collapse menu";
    this.setAttribute("aria-label", newLabel);
  });

  /* TOGGLE MOBILE MENU */
  toggleMobileMenu.addEventListener("click", function () {
    body.classList.toggle("mob-menu-opened");

    const isExpanded = this.getAttribute("aria-expanded") === "true";
    this.setAttribute("aria-expanded", String(!isExpanded));

    const newLabel = isExpanded ? "close menu" : "open menu";
    this.setAttribute("aria-label", newLabel);
  });

  /* TOGGLE LIGHT/DARK MODE */
  switchInput.addEventListener("input", function () {
    html.classList.toggle(lightModeClass);

    const isLightMode = html.classList.contains(lightModeClass);

    if (isLightMode) {
      switchLabelText.textContent = "Light";
      localStorage.setItem("dark-mode", "false");
    } else {
      switchLabelText.textContent = "Dark";
      localStorage.setItem("dark-mode", "true");
    }
  });
};
