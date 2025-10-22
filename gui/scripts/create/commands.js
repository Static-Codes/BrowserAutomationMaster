const commandCollection = [
  {
    commandName: "Browser",
    commandArgs: {
      browser: ["chrome", "firefox"],
    },
    commandDescription:
      "Specifies which browser will be used for the current script." +
      "<br>" +
      "This must be specified as the first line!",
    disabledOnLoad: false,
  },

  {
    commandName: "Add Header",
    commandArgs: {
      header: null,
      // fileType: ["js", "css", "html", "md"],
      // encoding: ["UTF-8", "ASCII"],
    },
    commandDescription:
      "Adds a new header to the browser instance for the given session." +
      "<br>" +
      "Example:" +
      "<br>" +
      '{ "headerName": "headerValue" }',
    placeholder: '{ "headerName": "headerValue" }',
    disabledOnLoad: true,
  },

  {
    commandName: "browser",
    commandArgs: {
      targetPath: null,
      mode: ["Read-Only", "Read-Write", "Full-Control"],
      recursive: ["Yes", "No"],
    },
    commandDescription:
      "Modifies the access permissions for a file or directory path.",
    disabledOnLoad: true,
  },

  {
    commandName: "ArchiveData",
    commandArgs: {
      sourceDir: null,
      outputFormat: ["zip", "tar.gz"],
    },
    commandDescription:
      "Compresses the contents of a source directory into an archive file.",
    disabledOnLoad: true,
  },
];
