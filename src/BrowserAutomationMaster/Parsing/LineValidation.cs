using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Parsing.Parser;

namespace BrowserAutomationMaster.Parsing
{
    // Breakup Parser.HandleLineValidation() and Parser.IsValidLine() here

    public static class LineValidation
    {
        public static bool AddHeader(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"header-name\" \"header-value\"";

            if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].EndsWith('"'))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

            return true;
        }

        public static bool AddHeaders(string fileName, string line, int lineNumber, ref string selectorString)
        {
            selectorString = $"{{\"header-name\": \"header-value\", \"header-name2\": \"header-value2\"}}";

            if (!IsValidHeaderFormat(line))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid header format on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: add-headers {selectorString}\n",
                    returnBool: false
                 );

            return true;
        }

        public static bool BasicCommands(string fileName, string line, int lineNumber, string arg1, string[] lineArgs, ref string selectorString)
        {
            if (arg1.Contains("save-as-html"))
                selectorString = "filename.html";

            else if (arg1.Equals("take-screenshot"))
                selectorString = "filename.png";

            else if (arg1.Equals("select-option"))
                selectorString = "option-selector";

            else if (lineArgs.Length != 2 || !lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\nInvalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\nValid Syntax: {arg1} \"{selectorString}\"\n",
                    returnBool: false
                );

            else if (lineArgs[0].Equals("visit") && !IsValidLinkFormat(lineArgs[1].Replace('"', ' ').Trim()))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\nInvalid url format on line {lineNumber}\n" +
                        $"Line: {line}\n",
                    returnBool: false
                );

            return true;
        }

        public static bool Browser(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs)
        {
            if (lineArgs.Length != 2 ||
                !browserArgs.Contains(lineArgs[1].Replace("\"", "")) ||
                !lineArgs[1].StartsWith('"') ||
                !lineArgs[1].EndsWith('"')
            )

                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {"\"firefox\""}\n",
                    returnBool: false
                );

            return true;
        }

        public static void BuildJSBlock(string fileName, string line, List<string> lines, int index, ref string jsBlockContent, ref int jsBlockStartLine, ref bool jsBlockFinished, ref string jsError)
        {
            // At this point the following are true:
            // There are atleast 3 lines in the file (visit, start-javascript, and end-javascript)
            // Its also possible browser is defined at the top of the file.

            if (line.StartsWith("end-javascript") && JavaScript.IsValidSyntax(jsBlockContent, out jsError))
                jsBlockFinished = true;

            else if (jsError != string.Empty)
            {
                string surroundingLines =
                    $"Line {index - 2} -> {lines[index - 2]}\n" +
                    $"Line {index - 1} -> {lines[index - 1]}\n" +
                    $"Line {index} -> {line} <-- This is the line that's causing the issue.\n";

                Errors.WriteErrorAndReturnBool(
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
                Errors.WriteErrorAndReturnBool(
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
                jsBlockContent += $"{line}\n";
        }

        public static bool ClickAtPosition(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            lineArgs = line.Trim().Split(" ");
            selectorString = "\"x-coordinate\" \"y-coordinate\"";

            if (lineArgs.Length != 3)
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );


            string[] positionArgs = [lineArgs[1], lineArgs[2]];
            foreach (var arg in positionArgs)
            {
                bool notQuoted = !arg.StartsWith('"') || !arg.EndsWith('"');

                bool notParsable = !int.TryParse(
                    arg.Replace('"', ' ').Trim(),
                    out int posArg
                );

                if (notQuoted || notParsable)
                    return Errors.WriteErrorAndReturnBool(
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

        public static bool ClickExp(string fileName, string line, int lineNumber, string firstArg, ref string[] lineArgs, ref string selectorString)
        {
            lineArgs = line.Trim().Split(" '");
            selectorString = "'selector'";

            if (lineArgs.Length != 2 || !lineArgs[1].EndsWith('\''))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

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

            if (!lineArgs[1].StartsWith('"') || !lineArgs[1].EndsWith('"'))
                return Errors.WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);

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
                    return Errors.WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);
            }


            bool invalidFeature =
                lineArgs.Length != 2 &&
                lineArgs.Length != 3 ||
                !featureArgs.Contains(lineArgs[1].Replace('"', ' ').Trim());

            if (invalidFeature)
                return Errors.WriteErrorAndReturnBool(invalidSyntaxMessage, returnBool: false);

            bool failedProxySoftCheck = 
                lineArgs.Length != 3 ||
                lineArgs[2].Count(c => (c == ':')) != 2 ||
                lineArgs[2].Count(c => (c == '@')) != 1;

            if (proxyFeatureArgs.Contains(lineArgs[1]) && failedProxySoftCheck)
            {
                selectorString = $"\"{lineArgs[1]}\"";
                return Errors.WriteErrorAndReturnBool(
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
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAMC Validation Error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString} USER:PASS@IP:PORT\n" +
                        $"If no authentication is required: NULL:NULL@IP:PORT\n",
                    returnBool: false
                );
            
            return true;
        }
        
        public static bool FillText(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"selector\" \"Desired value to input\"";

            if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\" \"value\"\n",
                    returnBool: false
                );

            return true;
        }

        public static bool FillTextExp(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"selector\" \"Desired value to input\"";

            if (lineArgs.Length != 3 || !lineArgs[1].EndsWith('"') || !lineArgs[2].Trim().EndsWith('"'))

                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\" \"value\"\n",
                    returnBool: false
                );

            return true;
        }

        public static bool OpenNewTab(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "\"x-coordinate\" \"y-coordinate\"";

            // Invalid # of args
            if (lineArgs.Length != 3)
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

            // Removing double quotes.
            for (int i = 0; i < lineArgs.Length; i++)
                lineArgs[i] = lineArgs[i].Replace('"', ' ').Trim();

            // Invalid url format
            if (!IsValidLinkFormat(lineArgs[1]))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Issue: Invalid link format for link: '{lineArgs[1]}'\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

            // Invalid timeout
            if (!int.TryParse(lineArgs[2], out int waitTime))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Issue: Invalid timeout argument: '{lineArgs[2]}'\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

            return true;
        }
        
        public static bool SelectOption(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            if (lineArgs.Length != 3 ||
               !lineArgs[1].StartsWith('"') ||
               !lineArgs[1].Trim().EndsWith('"') ||
               !int.TryParse(lineArgs[2], out int _))

                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\" index\n",
                    returnBool: false
                );

            return true;
        }

        public static bool SetCustomUserAgent(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:136.0) Gecko/20100101 Firefox/136.0";
            if (lineArgs.Length != 2 ||
                !lineArgs[1].Trim().EndsWith('"'))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid syntax on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\"\n",
                    returnBool: false
                );

            else if (!IsValidUserAgentFormat(lineArgs[1].Trim()))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid useragent on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} \"{selectorString}\"\n",
                    returnBool: false
                );

            return true;
        }
        
        public static bool WaitForSeconds(string fileName, string line, int lineNumber, string firstArg, string[] lineArgs, ref string selectorString)
        {
            selectorString = "5";
            if (!IsValidNumberFormat(lineArgs[1].Trim()))
                return Errors.WriteErrorAndReturnBool(
                    message:
                        $"BAM Manager (BAMM) ran into a BAMC validation error:\n\n" +
                        $"File: \"{fileName}\"\n" +
                        $"Invalid url format on line {lineNumber}\n" +
                        $"Line: {line}\n" +
                        $"Valid Syntax: {firstArg} {selectorString}\n",
                    returnBool: false
                );

            return true;
        }
    }
}
