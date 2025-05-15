using System.Text.RegularExpressions;

namespace BrowserAutomationMaster
{
    public enum SelectorCategory
    {
        Id,
        ClassName,
        CssSelector,
        TagName,
        XPath,
        NameAttribute,
        InvalidOrUnknown // Used for click-experimental action.
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
        [GeneratedRegex(@"^(?:#(?<id>[\w-]+)|\.(?<class>[\w-]+)|\[\s*name\s*=\s*(?:\""(?<DQVal>[^\""]*)\""|'(?<SQVal>[^']*)'|(?<UQVal>[^\]\s'\""]+))\s*\]|(?<xpath>(?:\B\/|\.\/|\(\/).*)|(?<tag>[a-zA-Z][\w:-]*))$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
        private static partial Regex CompileMainSelectorRegex();
        readonly static Regex SelectorRegex = CompileMainSelectorRegex();

        // Parses the provided selectorString and returns a ParsedSelector if successful, and exits if not; thus no need for a null check.
        public static ParsedSelector Parse(string selectorString)
        {
            if (string.IsNullOrWhiteSpace(selectorString))
            {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to validate empty selector, please ensure it's properly formatted then try compiling again.", 1);
            }
            string selectorTrimmed = selectorString.Trim();
            if (string.IsNullOrWhiteSpace(selectorTrimmed)) {
                Errors.WriteErrorAndExit($"BAM Manager (BAMM) was unable to trim empty selector, please ensure it's properly formatted then try compiling again.", 1);
            }

            Match selectorMatch = SelectorRegex.Match(selectorTrimmed);
            if (selectorMatch.Success)
            {
                if (selectorMatch.Groups["id"].Success)
                {
                    return new ParsedSelector(SelectorCategory.Id, selectorMatch.Groups["id"].Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["class"].Success)
                {
                    return new ParsedSelector(SelectorCategory.ClassName, selectorMatch.Groups["class"].Value, selectorTrimmed);
                }

                Group DQVal = selectorMatch.Groups["nameValDQ"]; // Double quoted value
                Group SQVal = selectorMatch.Groups["nameValSQ"]; // Single quoted value
                Group UQVal = selectorMatch.Groups["nameValUQ"]; // Unquoted value.

                if (DQVal.Success)
                {
                    return new ParsedSelector(SelectorCategory.NameAttribute, UQVal.Value, selectorTrimmed);
                }
                else if (SQVal.Success)
                {
                    return new ParsedSelector(SelectorCategory.NameAttribute, SQVal.Value, selectorTrimmed);
                }
                else if (UQVal.Success)
                {
                    return new ParsedSelector(SelectorCategory.NameAttribute, UQVal.Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["xpath"].Success)
                {
                    return new ParsedSelector(SelectorCategory.XPath, selectorMatch.Groups["xpath"].Value, selectorTrimmed);
                }
                else if (selectorMatch.Groups["tag"].Success)
                {
                    return new ParsedSelector(SelectorCategory.TagName, selectorMatch.Groups["tag"].Value, selectorTrimmed);
                }
            }

            Warning.Write($"BAM Manager (BAMM) was unable to parse selector:\n'{selectorTrimmed}'\n\nIs this a css selector? [y/n]: ");
            string? input = Console.ReadLine();
            if (input == null || input.ToLower().Replace('\n', ' ').Trim() != "y") {
                Errors.WriteErrorAndContinue($"\nBAM Manager (BAMM) was unable to validate selector: '{selectorTrimmed}', please ensure it's properly formatted then try compiling again.");
            }
            Warning.Write($"\nBAM Manager (BAMM) will continue without validating selector:\n'{selectorTrimmed}'\n\nIf you run into any issues, please recompile using a different selector.\n");
            return new ParsedSelector(SelectorCategory.InvalidOrUnknown, selectorTrimmed, selectorTrimmed);
        }
        
    }

}
