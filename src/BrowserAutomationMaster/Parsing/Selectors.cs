using System.Text.RegularExpressions;
using BrowserAutomationMaster.Messaging;

namespace BrowserAutomationMaster
{
    public enum SelectorCategory
    {
        Attribute,
        ClassName,
        Id,
        NameAttribute,
        PseudoClass,
        PseudoElement,
        TagName,
        XPath,
        InvalidOrUnknown // Used for click-exp action.
    }

    public class ParsedSelector(SelectorCategory category, string value, string rawInput)
    {
        public SelectorCategory Category = category;
        public string Value = value;
        public string rawInput = rawInput;

        public override string ToString()
        {
            return $"Category: {Category}\nValue: '{Value}'\nOriginal: '{rawInput}'";
        }
    }

    public static partial class SelectorParser
    {
        // Used an LLM to help fix formatting on these regexes, I need to take the time to learn regex properly and not rely on a crutch.
        [GeneratedRegex(@"^(?:#(?<id>[\w-]+)|\.(?<class>[\w-]+)|\[\s*name\s*=\s*(?:\""(?<nameValDQ>[^\""]*)\""|'(?<nameValSQ>[^']*)'|(?<nameValUQ>[^\]\s'\""]+))\s*\]|(?<xpath>(?:\B\/|\.\/|\(\/).*)|(?<tag>[a-zA-Z][\w:-]*))$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
        private static partial Regex CompileMainSelectorRegex();
        readonly static Regex SelectorRegex = CompileMainSelectorRegex();
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
        private static partial Regex CompileCssComponentRegex();
        private static readonly Regex CssComponentRegex = CompileCssComponentRegex();

        // Parses the provided selectorString and returns a ParsedSelector if successful, and exits if not; thus no need for a null check.
        public static ParsedSelector Parse(string selectorString)
        {
            if (string.IsNullOrWhiteSpace(selectorString)) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to validate empty selector, please ensure it's properly formatted then try compiling again.", 1);
            }

            string selectorTrimmed = selectorString.Trim();
            if (string.IsNullOrWhiteSpace(selectorTrimmed)) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to trim empty selector, please ensure it's properly formatted then try compiling again.", 1);
            }

            Match selectorMatch = SelectorRegex.Match(selectorTrimmed);
            if (selectorMatch.Success)
            {
                if (selectorMatch.Groups["id"].Success) {
                    return new ParsedSelector(SelectorCategory.Id, selectorMatch.Groups["id"].Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["class"].Success) {
                    return new ParsedSelector(SelectorCategory.ClassName, selectorMatch.Groups["class"].Value, selectorTrimmed);
                }

                Group DQVal = selectorMatch.Groups["nameValDQ"]; // Double quoted value
                Group SQVal = selectorMatch.Groups["nameValSQ"]; // Single quoted value
                Group UQVal = selectorMatch.Groups["nameValUQ"]; // Unquoted value.

                if (DQVal.Success) {
                    return new ParsedSelector(SelectorCategory.NameAttribute, UQVal.Value, selectorTrimmed);
                }
                else if (SQVal.Success) {
                    return new ParsedSelector(SelectorCategory.NameAttribute, SQVal.Value, selectorTrimmed);
                }
                else if (UQVal.Success) {
                    return new ParsedSelector(SelectorCategory.NameAttribute, UQVal.Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["xpath"].Success) {
                    return new ParsedSelector(SelectorCategory.XPath, selectorMatch.Groups["xpath"].Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["tag"].Success) {
                    return new ParsedSelector(SelectorCategory.TagName, selectorMatch.Groups["tag"].Value, selectorTrimmed);
                }
            }

            else
            {
                Match cssMatch = CssComponentRegex.Match(selectorTrimmed);
                if (cssMatch.Success)
                {
                    if (cssMatch.Groups["cssId"].Success) {
                        return new ParsedSelector(SelectorCategory.Id, cssMatch.Groups["cssId"].Value, selectorTrimmed);
                    }

                    if (cssMatch.Groups["cssClass"].Success) {
                        return new ParsedSelector(SelectorCategory.ClassName, cssMatch.Groups["cssClass"].Value, selectorTrimmed);
                    }

                    if (cssMatch.Groups["cssTagName"].Success) {
                        return new ParsedSelector(SelectorCategory.TagName, cssMatch.Groups["cssTagName"].Value, selectorTrimmed);
                    }

                    if (cssMatch.Groups["attributeName"].Success) {
                        string attrName = cssMatch.Groups["attributeName"].Value;
                        string extractedValue;
                        SelectorCategory categoryForAttribute; // Local declaration is required here to prevent errors from being thrown down the stack.

                        string? valDQ = cssMatch.Groups["attributeDQValue"].Success ? cssMatch.Groups["attributeDQValue"].Value : null;
                        string? valSQ = cssMatch.Groups["attributeSQValue"].Success ? cssMatch.Groups["attributeSQValue"].Value : null;
                        string? valUQ = cssMatch.Groups["attributeUQValue"].Success ? cssMatch.Groups["attributeUQValue"].Value : null;
                        string? actualAttrVal = valDQ ?? valSQ ?? valUQ; // Can return null, thus the null check below. 

                        if (actualAttrVal != null) {
                            extractedValue = actualAttrVal;
                            categoryForAttribute = attrName.Equals("name", StringComparison.OrdinalIgnoreCase) ?
                                                   SelectorCategory.NameAttribute : SelectorCategory.Attribute;
                        }
                        else {
                            extractedValue = attrName;
                            categoryForAttribute = attrName.Equals("name", StringComparison.OrdinalIgnoreCase) ?
                                                  SelectorCategory.NameAttribute : SelectorCategory.Attribute;
                        }
                        return new ParsedSelector(categoryForAttribute, extractedValue, selectorTrimmed);
                    }

                    if (cssMatch.Groups["pseudoClass"].Success) {
                        return new ParsedSelector(SelectorCategory.PseudoClass, cssMatch.Groups["pseudoClass"].Value, selectorTrimmed);
                    }

                    if (cssMatch.Groups["pseudoElement"].Success) {
                        return new ParsedSelector(SelectorCategory.PseudoElement, cssMatch.Groups["pseudoElement"].Value, selectorTrimmed);
                    }
                }
            }



            // If the selector isn't parsed, the user is questioned on whether or not they intended to use a css selector.
            Warning.Write($"BAM Manager (BAMM) was unable to parse selector:\n'{selectorTrimmed}'\n\nIs this a css selector? [y/n]: ");
            string? input = Console.ReadLine();
            if (input == null || input.ToLower().Replace('\n', ' ').Trim() != "y") {
                Errors.WriteErrorAndContinue($"\nBAM Manager (BAMM) was unable to validate selector: '{selectorTrimmed}', please ensure it's properly formatted then try compiling again.");
            }
            Warning.Write($"\nBAM Manager (BAMM) will continue without validating selector:\n'{selectorTrimmed}'\n\nIf you run into any issues, please recompile using a different selector.\n");
            return new ParsedSelector(SelectorCategory.InvalidOrUnknown, selectorTrimmed, selectorTrimmed);
        }

        public static void TestSelectors()
        {
            string[] selectors = [
                "#main-content",
                ".btn-primary",
                "div",
                "*",
                "[href]",
                "[target=_blank]",
                "[data-value=\"some complex 'value' with quotes\"]",
                "[title='A simple title']",
                ":hover",
                ":nth-child(2n+1)",
                ":not(.visible, #main)",
                "::before",
                "my-custom-element",
                "[name='actual_value']"
            ];

            foreach (string selector in selectors) {
                ParsedSelector parsedSelector = SelectorParser.Parse(selector);
                Console.WriteLine(parsedSelector.ToString() + "\n\n");
            }
            Environment.Exit(0);
        }
    }
}