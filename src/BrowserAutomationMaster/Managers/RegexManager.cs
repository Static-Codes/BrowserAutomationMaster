using System.Text;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Managers
{

    public static partial class RegexManager
    {

        /// <summary>
        /// Checks a string against a given regex, extracts the content of all captured groups (excluding Group 0, the full match), 
        /// and returns the concatenated groups from all matches.
        /// </summary>
        /// <param name="regex">The desired precompiled regex function.</param>
        /// <param name="input">The input string to check against the regex.</param>
        /// <param name="outputGroup">Contains a string representation of the concatenated captured group values (excluding Group 0) from all matches.</param>
        /// <param name="numericOnly">If true, only captured groups containing purely numeric characters are included in the output.</param>
        /// <returns>True if at least one match is found for the given regex, false otherwise.</returns>
        public static bool IsMatches(Regex regex, string input, out string outputGroup, bool numericOnly = false)
        {
            outputGroup = string.Empty;

            try
            {
                if (string.IsNullOrEmpty(input))
                    return false;

                var matches = regex.Matches(input);

                if (matches.Count == 0)
                    return false;

                var builder = new StringBuilder();

                foreach (Match match in matches)
                {
                    if (match.Success && match.Groups.Count > 1)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            Group group = match.Groups[i];

                            if (group.Success)
                                builder.AppendLine(group.Value);
                        }
                    }
                }

                outputGroup = builder.ToString().TrimEnd();
                return true;
            }
            catch
            {
                return false;
            }
        }
    

    public static readonly Regex ValidDirectoryRegex = ValidDirRegex();
        [GeneratedRegex(@"^[0-9a-zA-Z_\-.]+$", RegexOptions.Compiled)]
        private static partial Regex ValidDirRegex();

        public static readonly Regex PyVersionRegex = PyVerRegex();
        [GeneratedRegex("^Python\\s(3.[0-9]{1,2})", RegexOptions.Compiled)]
        private static partial Regex PyVerRegex();


        // Used for --set-timeout==5 (or any desired timeout)
        public static readonly Regex ActionTimeoutRegex = TimeoutRegex();
        [GeneratedRegex(@"^--set-timeout==(\d+)$", RegexOptions.Compiled)]
        private static partial Regex TimeoutRegex();


        // Used for --set-custom-useragent=="user-agent-string-here"
        public static readonly Regex CustomUserAgentRegex = CLIUserAgentRegex();
        [GeneratedRegex(@"^--set-custom-useragent==(.+?)$", RegexOptions.Compiled)]
        private static partial Regex CLIUserAgentRegex();

        //public static readonly Regex GUIPortRegex = CLIPortRegex();
        [GeneratedRegex(@"^--port==([0-9]{1,5})$", RegexOptions.Compiled)]
        public static partial Regex GUIPortRegex();


        // Regex to find paths starting with a drive letter, containing path separators, and ending with python.exe
        // Example: "-V:3.12 * C:\Users\UserName\AppData\Local\Programs\Python\Python312\python.exe" -> "C:\Users\UserName\AppData\Local\Programs\Python\Python312\python.exe"
        [GeneratedRegex(@"[a-zA-Z]:\\(?:[^\\/:*?""<>|\r\n]+\\)*python\.exe", RegexOptions.IgnoreCase)]
        public static partial Regex PrecompiledPythonPathRegex();

        // Used for BAMConfig.browserPresent
        public readonly static Regex BrowserRegex = BrowserRegexCompilation();
        [GeneratedRegex(@"^browser\s""(chrome|firefox)""$", RegexOptions.Compiled)]
        private static partial Regex BrowserRegexCompilation();


        // Used In Linux.GetTerminalBackgroundColor
        public static readonly Regex ForegroundMatch = ForegroundColorRegex();
        [GeneratedRegex("rgb:([0-9a-fA-F]+/[0-9a-fA-F]+).*?\n.{51}([0-9a-fA-F]+/[0-9a-fA-F]+)", RegexOptions.Compiled)]
        private static partial Regex ForegroundColorRegex();

        #region Start of ConfigParser Regex

        [GeneratedRegex("^.*=.*(true|false)$")]
        public static partial Regex BoolRegex();

        [GeneratedRegex("^.*=.*(dark|light)$")]
        public static partial Regex ThemeRegex();

        [GeneratedRegex("^.*=.*(\\d+)$")]
        public static partial Regex IntRegex();


        //[GeneratedRegex("^@Override\\s+(?<PropertyName>ForegroundColor|SuccessColor|WarningColor|ErrorColor|HighlightBackground|HighlightForeground|AccentColor)\\s*=\\s*(?:(?<Hex>#[A-Fa-f0-9]+)|(?<RGB>RGB\\((?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d)\\))|(?<XTerm>[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}))$")]
        [GeneratedRegex(
            "^@Override\\s+" +
            "(?<PropertyName>" +
                "ForegroundColor|SuccessColor|WarningColor|ErrorColor|" +
                "HighlightBackground|HighlightForeground|AccentColor" +
            ")\\s*=\\s*" +
            "(?:" +
                "(?<Hex>#[A-Fa-f0-9]+)" +
                "|" +
                "(?<RGB>RGB\\(" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d),\\s*" +
                    "(?:25[0-5]|2[0-4]\\d|1\\d{2}|[1-9]?\\d)" +
                "\\))" +
                "|" +
                "(?<XTerm>[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4}/[0-9A-Fa-f]{1,4})" +
            ")$"
        )]
        public static partial Regex OverrideRegex();

        #endregion

        #region Start of SelectorParser Regex
        // Used an LLM to help fix formatting on these regexes, I need to take the time to learn regex properly and not rely on a crutch. (9/11/25 update I feel comfortable with regex
        [GeneratedRegex(@"^(?:#(?<id>[\w-]+)|\.(?<class>[\w-]+)|\[\s*name\s*=\s*(?:\""(?<nameValDQ>[^\""]*)\""|'(?<nameValSQ>[^']*)'|(?<nameValUQ>[^\]\s'\""]+))\s*\]|(?<xpath>(?:\B\/|\.\/|\(\/).*)|(?<tag>[a-zA-Z][\w:-]*))$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
        private static partial Regex CompileMainSelectorRegex();
        public readonly static Regex SelectorRegex = CompileMainSelectorRegex();
        [GeneratedRegex(
        @"^
        (?:
            # ID Selector: #my-id
            \#(?<cssId>[\w-]+) 
        |
            # Class Selector: .my-class
            \.(?<cssClass>[\w-]+) 
        |
            # Attribute Selector: [attr], [attr=val], [attr~=val], etc.
            \[\s*
                (?<attributeName>[\w-]+) # Attribute name
                \s*
                (?: # Optional operator and value
                    (?<attributeOperator>[*^$|~]?=) # Operator: =, *=, ^=, $=, |=, ~=
                    \s*
                    (?: # Value, quoted or unquoted
                        ""(?<attributeDQValue>(?:\\.|[^\\""])*)"" # Double-quoted value
                    |
                        '(?<attributeSQValue>(?:\\.|[^\\'])*)' # Single-quoted value
                    |
                        (?<attributeUQValue>[^\]\s'""]+) # Unquoted value 
                    )
                )? # End optional operator and value
            \s*\]
        |
            # Pseudo-class Selector: :hover, :nth-child(2n+1)
            :(?<pseudoClass>[\w-]+) 
            (?:\( # Optional arguments
                \s*(?<pseudoClassArgs> (?: [^()""'] | ""(?:\\.|[^\\""])*"" | '(?:\\.|[^\\'])*' | \( (?: [^()""'] | ""(?:\\.|[^\\""])*"" | '(?:\\.|[^\\'])*' )* \) )*? ) \s* 
            \))?
        |
            # Pseudo-element Selector: ::before, ::after
            ::(?<pseudoElement>[\w-]+)
        |
            # Tag Name or Universal Selector: div, span, my-element, *
            (?<cssTagName>(?:[a-zA-Z_][\w-]*|\*))
        )
        $",
            RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace
        )]
        public static partial Regex CompileCssComponentRegex();
        public static readonly Regex CssComponentRegex = CompileCssComponentRegex();

        #endregion End Of SelectorParser Regex

        #region Start of Parser.cs Regex

        const string HeaderFormatPattern = @"^add-headers\s*(?<json>\{\s*(?:""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*""(?:\s*,\s*""(?:[^""\\]|\\.)+"":\s*""(?:[^""\\]|\\.)*"")*)?\s*\})$";
        const string LinkFormatPattern = @"(?i)\b(http|https|file|ftp?://(?:(?:(?:[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?\.)*(?:[a-z\u00a1-\uffff]{2,}|[a-z0-9\u00a1-\uffff](?:[a-z0-9\u00a1-\uffff-]{0,61}[a-z0-9\u00a1-\uffff])?)\.?)|(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|\[(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?:(?::[0-9a-fA-F]{1,4}){1,6})|:(?:(?::[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(?::[0-9a-fA-F]{0,4}){0,4}%[a-zA-Z0-9._~%-]+|::(?:ffff(?::0{1,4}){0,1}:){0,1}(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)|(?:[0-9a-fA-F]{1,4}:){1,4}:(?:(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.){3}(?:25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d))\]))(?::\d{2,5})?(?:[/?#][^\s<>""']*)?\b";

        const string ProxyFormatPattern = @"^([^:]+):([^@]+)@([^:]+):(\d+)$";
        const string NumberFormatPattern = @"^(?:\d+(?:\.\d{1,3})?|\.\d{1,3})$";
        const string UserAgentFormatPattern = "^[^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?(?:[ ][^\\s\\/]+(?:\\/[^\\s]+)?(?:[ ]\\(.*?\\))?)*$";


        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.

        [GeneratedRegex(HeaderFormatPattern)]
        public static partial Regex PrecompiledHeaderRegex(); // Public declaration required for usage in Transpiler.HandleCompilation

        [GeneratedRegex(LinkFormatPattern)]
        public static partial Regex PrecompiledLinkRegex();

        [GeneratedRegex(NumberFormatPattern)]
        public static partial Regex PrecompiledNumberRegex();

        [GeneratedRegex(ProxyFormatPattern)]
        public static partial Regex PrecompiledProxyRegex();

        [GeneratedRegex(UserAgentFormatPattern)]
        public static partial Regex PrecompiledUserAgentRegex();

        #endregion

        // Used in PackageManager
        // Researched from: https://blog.nimblepros.com/blogs/using-generated-regex-attribute/
        // Source generation is used here at build time to create an optimized regex code block, which is then converted into MSIL prior to runtime; reducing overhead and improving efficiency.
        const string packageFormatPattern = @"^([a-zA-Z0-9]|[a-zA-Z0-9][a-zA-Z0-9._-]*[a-zA-Z0-9])$"; // Regex pulled from https://pypi.org/project/twine/
        [GeneratedRegex(packageFormatPattern)]
        public static partial Regex PrecompiledPackageRegex();


        // Used in LocalServerManager.HandleEndpointRequests();
        const string base64FormatPattern = @"^[a-zA-Z0-9\+/]*={0,2}$";
        [GeneratedRegex(base64FormatPattern)]
        public static partial Regex PrecompiledBase64Regex();

        // Used for LocalServerManager.ScanUsedLHPorts() (Windows)
        [GeneratedRegex(@"(?:TCP|UDP)\s{4}(?:localhost|127.0.0.1):([0-9]{1,5})|(?:localhost|127.0.0.1):([0-9]{1,5})")]
        public static partial Regex PrecompiledNetStatRegex();


        //// Used for LocalServerManager.ScanUsedLHPorts() (Unix)
        //[GeneratedRegex(@"(?:TCP|UDP)\s{4}(?:localhost|127.0.0.1):([0-9]{1,5})")]
        //public static partial Regex PrecompiledNetStatRegex();
    }
}
