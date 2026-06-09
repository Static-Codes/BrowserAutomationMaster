# Copyright (C) 2026 Static Codes
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License, or
# (at your option) any later version.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program. If not, see <https://www.gnu.org/licenses/>.

from getpass import getuser
from platform import system as pSystem
from os import environ, execlp, system
from time import sleep
from subprocess import DEVNULL, run
import sys
import warnings

warnings.filterwarnings("ignore")

procName = "bamm"
platName = pSystem()
userName = getuser()
interpreter = sys.executable # The path to the python interpreter running this script
daemonScriptPath = None
kill_command = None
start_command = None

if platName == "Windows":
    daemonScriptPath = f"C:/Users/{userName}/AppData/Roaming/BrowserautomationMaster/guiDaemon.py"
    kill_command = ["taskkill", "/F", "/im", f"{procName}.exe"]
    start_command = f"start /B {procName}.exe --gui"

elif platName in ["Darwin", "Linux"]:
    daemonScriptPath = f"/home/{userName}/.config/BrowserAutomationMaster/guiDaemon.py"
    kill_command = ["pkill", procName]
    start_command = f"{procName} --gui"

else:
    print("Unsupported OS.")
    exit(1)


args = [daemonScriptPath, "--gui"]
execlp(interpreter, interpreter, *args)
    
print("\nSuccessfully executed the start sequence for the BAMM GUI!")
exit(0)