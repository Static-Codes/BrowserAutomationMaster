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

﻿using System;
using System.Diagnostics.CodeAnalysis;
using static BrowserAutomationMaster.Managers.AnsiManager;
using static BrowserAutomationMaster.Messaging.Debug;

namespace BrowserAutomationMaster.Messaging
{
    public class Errors
    {

        public static string GenerateErrorMessage(string fileName, string line, int lineNumber, string issueText)
        {
            return "BAM Manager (BAMM) was unable to continue due to an unexpected error.\n" +
                $"File: {fileName}\n" +
                $"Line Number: {lineNumber}\n" +
                $"Line: {line}\n" +
                $"Error Log: {issueText}";
        }

        [DoesNotReturn]
        public static void ThrowUnsupportedPlatformException() {
            throw new PlatformNotSupportedException(
                "Unsupported OS.\nBAM Manager (BAMM) currently supports:\n" +
                "Windows 10/11\n" +
                "Linux\n" +
                "MacOS 11+\n"
            );
        }

        public static void Write(string message, bool noNewLines = false)
        {
            if (noNewLines)
            {
                WriteMessageNoNewLines(message, isError: true);
                return;
            }
            
            WriteMessage(message, isError: true);
        }

        [DoesNotReturn]
        public static void WriteAndExit(string message, int status, bool writePlatformDebugInfo = true)
        {
            var output = writePlatformDebugInfo switch 
            {
                true => $"{message}\n{GetPlatformInfoForErrorLog()}",
                false => message
            };

            WriteMessage(output, isError: true);
            ReadKey();
            Environment.Exit(status);
        }
        
        public static string? WriteErrorAndReturnNull(string message)
        {
            WriteMessage(message, isError: true);
            ReadKey();
            return null;
        }

        public static bool WriteErrorAndReturnBool(string message, bool returnBool)
        {
            WriteMessage(message, isError: true);
            return returnBool;
        }
        public static string WriteErrorAndReturnEmptyString(string message)
        {
            WriteMessage(message, isError: true);
            ReadKey();
            return string.Empty;
        }

        
    }
}
