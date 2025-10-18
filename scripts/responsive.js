var parent = document.querySelector("head");

var tabletChild = document.createElement("link");
tabletChild.href = "styles/responsive/tablet.css";
tabletChild.rel = "stylesheet";
tabletChild.id = "tablet-style";

var widescreenChild = document.createElement("link");
widescreenChild.href = "styles/responsive/widescreen.css";
widescreenChild.rel = "stylesheet";
widescreenChild.id = "widescreen-style";

function appendStyle(styleElement) {
  if (!document.getElementById(styleElement.id)) {
    parent.appendChild(styleElement);
  }
}

function removeStyle(id) {
  var styleElement = document.getElementById(id);
  if (styleElement) {
    styleElement.remove();
  }
}

function setResponsiveness() {
  var width = window.innerWidth;
  var height = window.innerHeight;

  if (width >= 1600 && width < 1920 && height >= 1050) {
    removeStyle("tablet-style");
    appendStyle(widescreenChild);
  } else if (width >= 1024 && width <= 1600 && height >= 768 && height < 1050) {
    removeStyle("widescreen-style");
    appendStyle(tabletChild);
  } else {
    removeStyle("tablet-style");
    removeStyle("widescreen-style");
  }
}

setResponsiveness();

window.addEventListener("resize", setResponsiveness);
