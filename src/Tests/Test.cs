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

namespace Tests
{
    public class CommandTest() 
    {
        // so all keys should be seen as a boolean equation
        // Potential Example
        // @ means is present 
        // ! means is missing
        // # means is required
        // ? means is optional
        
        // string argumentName -> The string representing the name of argument in question
        // int[]? validArgLengths -> Length of supported commands or null if the param is a flag. 
        // int[] validLocations [] -> An array of indexes where this command can be placed
        // string[] unsupportedArgs -> An array of argument names that cannot be used with this argumentName        

        // var argDict = new Dictionary<int, string[]> {
        //     { 0, ["?--nohwc", "?clear"] },
        //     { 1, ["@clear", "#compiled if argDict[0] == clear"] },
        //     { 2, ["?--nohwc"] }
        // };
        // bamm | build 12345 --nohwc
        



    }
}