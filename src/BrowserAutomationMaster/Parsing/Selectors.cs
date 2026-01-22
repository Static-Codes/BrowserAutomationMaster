using System;
using System.Text.RegularExpressions;
using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.RegexManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Parsing
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

        /// <summary>
        /// Parses the provided selectorString
        /// </summary>
        /// <param name="selectorString"></param>
        /// <returns>returns a ParsedSelector if successful, exits if not; thus no need for a null check.</returns>
        public static ParsedSelector Parse(string selectorString)
        {
            if (string.IsNullOrWhiteSpace(selectorString)) {
                WriteAndExit(
                    message:
                        $"BAM Manager (BAMM) was unable to validate empty selector, please ensure it's properly formatted then try compiling again.", 
                    status: 1
                );
            }

            string selectorTrimmed = selectorString.Trim();
            if (string.IsNullOrWhiteSpace(selectorTrimmed)) {
                WriteAndExit(
                    message: 
                        $"BAM Manager (BAMM) was unable to trim empty selector, " +
                        $"please ensure it's properly formatted then try compiling again.", 
                    status: 1
                );
            }

            Match selectorMatch = SelectorRegex.Match(selectorTrimmed);
            if (selectorMatch.Success)
            {
                if (selectorMatch.Groups["id"].Success) {
                    return new ParsedSelector(
                        SelectorCategory.Id,
                        selectorMatch.Groups["id"].Value,
                        selectorTrimmed
                    );
                }
                
                else if (selectorMatch.Groups["class"].Success) {
                    return new ParsedSelector(
                        SelectorCategory.ClassName, 
                        selectorMatch.Groups["class"].Value, 
                        selectorTrimmed
                    );
                }
                

                Group DQVal = selectorMatch.Groups["nameValDQ"]; // Double quoted value
                Group SQVal = selectorMatch.Groups["nameValSQ"]; // Single quoted value
                Group UQVal = selectorMatch.Groups["nameValUQ"]; // Unquoted value.

                if (DQVal.Success) {
                    return new ParsedSelector(
                        SelectorCategory.NameAttribute,
                        UQVal.Value,
                        selectorTrimmed
                    );
                }
                
                else if (SQVal.Success) {
                    return new ParsedSelector(
                        SelectorCategory.NameAttribute, 
                        SQVal.Value, 
                        selectorTrimmed
                    );
                }
                
                else if (UQVal.Success) {
                    return new ParsedSelector(
                        SelectorCategory.NameAttribute, 
                        UQVal.Value, 
                        selectorTrimmed
                    );
                }
                
                else if (selectorMatch.Groups["xpath"].Success) {
                    return new ParsedSelector(
                        SelectorCategory.XPath, 
                        selectorMatch.Groups["xpath"].Value, 
                        selectorTrimmed
                    );
                }
                
                else if (selectorMatch.Groups["tag"].Success) {
                    return new ParsedSelector(
                        SelectorCategory.TagName, 
                        selectorMatch.Groups["tag"].Value, 
                        selectorTrimmed
                    );
                }
                
            }

            else
            {
                Match cssMatch = CssComponentRegex.Match(selectorTrimmed);
                if (cssMatch.Success)
                {
                    if (cssMatch.Groups["cssId"].Success) {
                        return new ParsedSelector(
                            SelectorCategory.Id, 
                            cssMatch.Groups["cssId"].Value, 
                            selectorTrimmed);
                    }
                    
                    if (cssMatch.Groups["cssClass"].Success) {
                        return new ParsedSelector(
                            SelectorCategory.ClassName, 
                            cssMatch.Groups["cssClass"].Value, 
                            selectorTrimmed
                        );
                    }
                    
                    if (cssMatch.Groups["cssTagName"].Success) {
                        return new ParsedSelector(
                            SelectorCategory.TagName,
                            cssMatch.Groups["cssTagName"].Value,
                            selectorTrimmed
                        );
                    }
                    

                    if (cssMatch.Groups["attributeName"].Success) 
                    {
                        string attrName = cssMatch.Groups["attributeName"].Value;
                        string extractedValue;

                        // Local declaration is required here to prevent errors from being thrown down the stack.
                        SelectorCategory categoryForAttribute;

                        string? valDQ = 
                            cssMatch.Groups["attributeDQValue"].Success ? 
                            cssMatch.Groups["attributeDQValue"].Value : null;

                        string? valSQ = 
                            cssMatch.Groups["attributeSQValue"].Success ? 
                            cssMatch.Groups["attributeSQValue"].Value : null;

                        string? valUQ = 
                            cssMatch.Groups["attributeUQValue"].Success ? 
                            cssMatch.Groups["attributeUQValue"].Value : null;

                        string? actualAttrVal = valDQ ?? valSQ ?? valUQ; // Can return null, thus the null check below. 

                        // OIC = StringComparison.OrdinalIgnoreCase
                        if (actualAttrVal != null) 
                        {
                            extractedValue = actualAttrVal;

                            categoryForAttribute = 
                                attrName.Equals("name", OIC) ? 
                                SelectorCategory.NameAttribute : 
                                SelectorCategory.Attribute;
                        }

                        else 
                        {
                            extractedValue = attrName;

                            categoryForAttribute = 
                                attrName.Equals("name", OIC) ? 
                                SelectorCategory.NameAttribute : 
                                SelectorCategory.Attribute;
                        }

                        return new ParsedSelector(
                            categoryForAttribute, 
                            extractedValue, 
                            selectorTrimmed
                        );
                    }

                    if (cssMatch.Groups["pseudoClass"].Success) {
                        return new ParsedSelector(
                            SelectorCategory.PseudoClass, 
                            cssMatch.Groups["pseudoClass"].Value, 
                            selectorTrimmed
                        );
                    }
                    

                    if (cssMatch.Groups["pseudoElement"].Success) {
                        return new ParsedSelector(
                            SelectorCategory.PseudoElement,
                            cssMatch.Groups["pseudoElement"].Value,
                            selectorTrimmed
                        );
                    }
                 
                }
            }



            // If the selector isn't parsed the user is questioned on whether or not they intended to use a css selector.
            Warning.Write(
                $"BAM Manager (BAMM) was unable to parse selector:\n'{selectorTrimmed}\n\n"
            );

            string isSelector = Input.AskForInput("Is this a css selector? [y/n]: ");
            
            if (Input.ConditionRejected(isSelector)) {
                Write(
                    message:
                        $"\nBAM Manager (BAMM) was unable to validate selector: '{selectorTrimmed}', " +
                        $"please ensure it's properly formatted then try compiling again."
                );
            }
            
            Warning.Write(
                message: 
                    $"\nBAM Manager (BAMM) will continue without validating selector:" +
                    $"\n'{selectorTrimmed}'\n\n" +
                    $"If you run into any issues, please recompile using a different selector.\n"
            );

            return new ParsedSelector(
                SelectorCategory.InvalidOrUnknown, 
                selectorTrimmed, 
                selectorTrimmed
            );
        }

        // Unused (only for debugging)
        private static void UnitTestSelectors()
        {
            string[] selectors = [
                "//div[@class='ql-editor ql-blank textarea new-input-ui']//p",
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
                Spectre.Console.AnsiConsole.Write(parsedSelector.ToString() + "\n\n");
            }
            Environment.Exit(0);
        }
    }
}