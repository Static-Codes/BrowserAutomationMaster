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

namespace BrowserAutomationMaster.Managers.AppManager
{
    public class AppInfo
    {
        public required string Name { get; set; } // Added required since an app won't be added if we don't know its common name
        public string? Version { get; set; }
        public string? Publisher { get; set; }
        public required string Path { get; set; }
    }
}