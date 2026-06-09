// Copyright (C) 2026 Static Codes
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

﻿using static BrowserAutomationMaster.Parsing.LineValidationHelpers;
using static BrowserAutomationMaster.Parsing.Parser;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Parsing
{
    // Breakup Parser.HandleLineValidation() and Parser.IsValidLine() here

    public static class LineValidationHelpers 
    {

        // Helper to check for integer validity (ignoring quotes)
        public static bool IsInt(string s) => int.TryParse(s.Trim('"', '\'', ' '), out _);

        public static bool ValidateOneArgCommand(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString, bool[]? optionalChecks = null) 
        {   
            var eMessage = $"BAM Manager (BAMM) ran into a BAMC validation error:{NLC}{NLC}" +
                           $"File: \"{fileName}\"{NLC}Invalid syntax on line {lineNumber}{NLC}" +
                           $"Line: {line}{NLC}" +
                           $"Valid Syntax: {firstArg} {selectorString}{NLC}";

            bool extraChecksRequired = optionalChecks != null;
            
            if (lineArgs.Length != 2) {
                return WriteErrorAndReturnBool(eMessage, returnBool: false);
            };

            var trimmedArg1 = lineArgs[1].Trim();

            var firstArgQuoted = 
                trimmedArg1.StartsWith('"') && 
                trimmedArg1.EndsWith('"') ||
                trimmedArg1.StartsWith('\'') && 
                trimmedArg1.EndsWith('\'');

            if (!firstArgQuoted) {
                return WriteErrorAndReturnBool(eMessage, returnBool: false);
            }

            // optionalChecks is guaranteed to not be null here.
            return extraChecksRequired switch
            {
                // If extraChecksRequired and all extraChecks are passing.
                true when optionalChecks!.All(check => check) => true,

                // If extraChecksRequired and not all extraChecks are passing.
                true when !optionalChecks!.All(check => check) => WriteErrorAndReturnBool(
                    eMessage,
                    returnBool: false
                ),
                _ => true,
            };
        } 

        public static bool ValidateTwoArgCommand(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString, bool[]? optionalChecks = null) 
        {
            var eMessage = $"BAM Manager (BAMM) ran into a BAMC validation error:{NLC}{NLC}" +
                         $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}{NLC}" +
                         $"Line: {line}\n" +
                         $"Valid Syntax: {firstArg} {selectorString}\n";


            if (lineArgs.Length != 3) {
                return WriteErrorAndReturnBool(eMessage, returnBool: false);
            }

            var trimmedArg1 = lineArgs[1].Trim();
            var trimmedArg2 = lineArgs[2].Trim();
            
            var firstArgQuoted = 
                trimmedArg1.StartsWith('"') && 
                trimmedArg1.EndsWith('"') ||
                trimmedArg1.StartsWith('\'') && 
                trimmedArg1.EndsWith('\'');

            var secondArgQuoted = 
                trimmedArg2.StartsWith('"') && 
                trimmedArg2.EndsWith('"') ||
                trimmedArg2.StartsWith('\'') && 
                trimmedArg2.EndsWith('\'');

            if (!firstArgQuoted || !secondArgQuoted) {
                return WriteErrorAndReturnBool(eMessage, returnBool: false);
            }

            bool extraChecksRequired = optionalChecks != null;

            // optionalChecks is guaranteed to not be null here.
            return extraChecksRequired switch
            {
                // If extraChecksRequired and all extraChecks are passing.
                true when optionalChecks!.All(check => check) => true,

                // If extraChecksRequired and not all extraChecks are passing.
                true when !optionalChecks!.All(check => check) => WriteErrorAndReturnBool(
                    eMessage,
                    returnBool: false
                ),
                _ => true,
            };
        }
    }

    public static class LineValidation
    {
        public static bool AddCookie(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"cookie-name\" \"cookie-value\"";
            return ValidateTwoArgCommand(
                fileName, line, lineNumber, firstArg, lineArgs, ref selectorString
            );
        }

        public static bool AddHeader(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"header-name\" \"header-value\"";
            return LineValidationHelpers.ValidateTwoArgCommand(
                fileName, line, lineNumber, firstArg, lineArgs, ref selectorString
            );
        }

        public static bool AddHeaders(string fileName, string line, int lineNumber, ref string selectorString)
        {
            selectorString = $"{{\"header-name\": \"header-value\", \"header-name2\": \"header-value2\"}}";

            if (!IsValidHeaderFormat(line))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid header format on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: add-headers {selectorString}\n",
                    returnBool: false
                 );
            }
            return true;
        }

        public static bool BasicCommands(string fileName, string line, int lineNumber, string arg1, string[] lineArgs, ref string selectorString)
        {
            if (arg1.Contains("save-as-html"))
            {
                selectorString = "filename.html";
            }

            else if (arg1.Equals("take-screenshot"))
            {
                selectorString = "filename.png";
            }

            else if (arg1.Equals("select-option"))
            {
                selectorString = "option-selector";
            }
            
            else if (arg1.Equals("visit")) 
            {
                selectorString = "url";
            }

            else if (lineArgs.Length != 2 || !lineArgs[1].Trim().StartsWith('"') || !lineArgs[1].Trim().EndsWith('"'))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\nValid Syntax: {arg1} \"{selectorString}\"\n",
                    returnBool: false
                );
            }

            else if (lineArgs[0].Equals("visit") && !IsValidLinkFormat(lineArgs[1].Replace('"', ' ').Trim()))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\nInvalid url format on line {lineNumber}\n" +
                        $"Line: {line}\n",
                    returnBool: false
                );
            }
            return true;
        }

        public static bool Browser(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "browser \"browser-name\"";
            return ValidateOneArgCommand(fileName, line, lineNumber, firstArg, lineArgs, ref selectorString);
        }

        public static void BuildJSBlock(string fileName, string line, List<string> lines, int index, ref string jsBlockContent, ref int jsBlockStartLine, ref bool jsBlockFinished, ref string jsError)
        {
            // At this point the following are true:
            // There are atleast 3 lines in the file (visit, start-javascript, and end-javascript)
            // Its also possible browser is defined at the top of the file.

            if (line.StartsWith("end-javascript") && JavaScript.IsValidSyntax(jsBlockContent, out jsError))
            {
                jsBlockFinished = true;
            }

            else if (jsError != string.Empty)
            {
                string surroundingLines =
                    $"Line {index - 2} -> {lines[index - 2]}\n" +
                    $"Line {index - 1} -> {lines[index - 1]}\n" +
                    $"Line {index} -> {line} <-- This is the line that's causing the issue.\n";

                WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error on line {index} of '{fileName}'.\n\n" +
                        $"Error log:\n{surroundingLines}\n" +
                        $"Compiler error:\n" +
                        $"In the current block, on {jsError}\n\n" +
                        $"Please correct this and recompile.",
                    returnBool: false
                );
            }

            else if (line.StartsWith("start-javascript"))
            {
                jsBlockStartLine = index + 1;
                WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n\n" +
                        $"Error: Attempted to create a second JavaScript block on line {jsBlockStartLine} " +
                        $"while the previous block has not been closed.\n\n" +
                        $"Please ensure end-javascript is placed at or before line {index}.",
                    returnBool: false
                );
            }

            else
            {
                jsBlockContent += $"{line}\n";
            }
        }

        public static bool ClickAtPosition(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"x-coordinate\" \"y-coordinate\"";

            // Safely extract args for validation. (Defaults to an empty string if length is invalid.)
            var rawX = lineArgs.Length > 1 ? lineArgs[1] : string.Empty;
            var rawY = lineArgs.Length > 2 ? lineArgs[2] : string.Empty;

            return ValidateTwoArgCommand(
                fileName,
                line,
                lineNumber,
                firstArg,
                lineArgs,
                ref selectorString,
                optionalChecks: [IsInt(rawX), IsInt(rawY)]
            );
        }

        public static bool ClickExp(string fileName, string line, int lineNumber, string firstArg, ref string[] lineArgs, ref string selectorString)
        {
            lineArgs = line.Trim().Split(" '");
            selectorString = "'selector'";

            if (lineArgs.Length != 2 || !lineArgs[1].EndsWith('\''))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            return true;
        }

        public static bool Feature(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            
            selectorString = "\"feature-name\"";
            var invalidSyntaxMessage =
                $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                $"File: \"{fileName}\"\n" +
                $"Invalid syntax on line {lineNumber}\n" +
                $"Line: {line}\n" +
                $"Valid Syntax: {firstArg} {selectorString}\n";

            if (!lineArgs[1].Trim().StartsWith('"') || !lineArgs[1].Trim().EndsWith('"'))
            {
                // Console.WriteLine(lineArgs[1].StartsWith('"'));
                // Console.WriteLine(lineArgs[1].Trim().EndsWith('"'));
                return WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);
            }
            // Sanitize args based on length
            switch (lineArgs.Length)
            {
                case 2:
                    lineArgs[1] = lineArgs[1].Replace('"', ' ').Trim();
                    break;

                case 3:
                    lineArgs[1] = lineArgs[1].Replace('"', ' ').Trim();
                    lineArgs[2] = lineArgs[2].Replace('"', ' ').Trim();
                    break;

                default:
                    return WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);
            }


            bool invalidFeature =
                lineArgs.Length != 2 &&
                lineArgs.Length != 3 ||
                !featureArgs.Contains(lineArgs[1].Replace('"', ' ').Trim());

            if (invalidFeature)
            {
                return WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);
            }

            bool failedProxySoftCheck = 
                lineArgs.Length != 3 ||
                lineArgs[2].Count(c => (c == ':')) != 2 ||
                lineArgs[2].Count(c => (c == '@')) != 1;

            if (proxyFeatureArgs.Contains(lineArgs[1]) && failedProxySoftCheck)
            {
                selectorString = $"\"{lineArgs[1]}\"";
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                        $"If no authentication is required: NULL:NULL@IP:PORT\n",
                    returnBool: false
                );
            }

            if (proxyFeatureArgs.Contains(lineArgs[1]) && !IsValidProxyFormat(lineArgs[2]))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAMC Validation Error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                        $"If no authentication is required: NULL:NULL@IP:PORT\n",
                    returnBool: false
                );
            }

            return true;
        }
        
        public static bool FillText(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"selector\" \"Desired value to input\"";

            if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\" \"value\"\n",
                    returnBool: false
                );
            }

            return true;
        }

        public static bool FillTextExp(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"selector\" \"Desired value to input\"";

            if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            return true;
        }

        public static bool OpenNewTab(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"url\" \"sleep-time\"";

            // Invalid # of args
            if (lineArgs.Length != 3)
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            // Removing double quotes.
            for (int i = 0; i < lineArgs.Length; i++)
            {
                lineArgs[i] = lineArgs[i].Replace('"', ' ').Trim();
            }

            // Invalid url format
            if (!IsValidLinkFormat(lineArgs[1]))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Issue: Invalid link format for link: '{lineArgs[1]}'\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            // Invalid timeout
            if (!int.TryParse(lineArgs[2], out int _))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Issue: Invalid timeout argument: '{lineArgs[2]}'\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            return true;
        }
        
        public static bool SelectOption(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            if (lineArgs.Length != 3 ||
               !lineArgs[1].StartsWith('"') ||
               !lineArgs[1].Trim().EndsWith('"') ||
               !int.TryParse(lineArgs[2], out int _))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\" index\n",
                    returnBool: false
                );
            }

            return true;
        }

        public static bool SetCustomUserAgent(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0";
            if (lineArgs.Length != 2 ||
                !lineArgs[1].Trim().EndsWith('"'))
                {
                    return WriteErrorAndReturnBool(
                        message:
                            $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                            $"File: \"{fileName}\"\n" +
                            $"Invalid syntax on line {lineNumber}\n" +
                            $"Line: {line}\n" +
                            $"Valid Syntax: {firstArg} \"{selectorString}\"\n",
                        returnBool: false
                    );
                }

            else if (!IsValidUserAgentFormat(lineArgs[1].Trim()))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid useragent on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\"\n",
                    returnBool: false
                );
            }

            return true;
        }
        
        public static bool WaitForSeconds(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "5";
            if (!IsValidNumberFormat(lineArgs[1].Trim()))
            {
                return WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid url format on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );
            }

            return true;
        }
    }
}
