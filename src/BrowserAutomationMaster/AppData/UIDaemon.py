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