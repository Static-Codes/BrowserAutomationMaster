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

﻿using BrowserAutomationMaster.Managers;
using BrowserAutomationMaster.Managers.AppManager.OS.Linux;
using static BrowserAutomationMaster.Managers.PlatformManager;
using static BrowserAutomationMaster.ProgramFunctions;
using static Tests.Runner;

// Logic from Main application around colored text.
SetPlatform();
await InitializeAsync(["--nohwc"]);

// var data = new Dictionary<int, (object, object)>() {
//     { 1, ( "A", "A" ) },
//     { 2, ( "B", "B" ) },
//     { 3, ( "C", "A" ) },
//     { 4, ( "A", "A" ) },
// };


// var tests = CreateTests(data);
// foreach (var test in tests) {
//     RunTest(test.Key, test.Value);
// }