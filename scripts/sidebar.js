const usingOtter = navigator.userAgent.includes("Otter");
// Wait for the DOM to fully load
window.onload = function () {
  setTimeout(function () {}, 2000);
  const html = document.documentElement;
  const body = document.body;

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

  // Error handling for mandatory elements
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

  /* SHOW TOOLTIP ON MENU LINK HOVER */
  for (const link of menuLinks) {
    link.addEventListener("mouseenter", function () {
      // Check if collapsed state is active AND screen size is desktop
      if (
        body.classList.contains(collapsedClass) &&
        window.matchMedia("(min-width: 769px)").matches
      ) {
        const tooltip_element = this.querySelector("span");
        const tooltip = tooltip_element
          ? tooltip_element.textContent
          : "Menu Item";
        this.setAttribute("title", tooltip);
      } else {
        this.removeAttribute("title");
      }
    });

    // Ensure title is removed on mouse leave
    link.addEventListener("mouseleave", function () {
      this.removeAttribute("title");
    });
  }

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
