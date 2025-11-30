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

# print(f"Attempting to kill existing process: {procName}")
# try:
#     kill_result = run(kill_command, check=False, stdout=DEVNULL, stderr=DEVNULL)
#     kill_status = kill_result.returncode

#     if kill_status == 0:
#         print("Successfully killed the existing BAMM GUI process.")
#         print("Please wait while the server restarts, this may take up to 30 seconds on low end hardware.")
#     else:
#         print("The BAMM GUI process was not running or failed to kill. Starting now.")
    
# except FileNotFoundError:
#     print(f"ERROR: The necessary command ('taskkill' or 'pkill') was not found on {platName}.")
#     exit(1)
# except Exception as e:
#     print(f"An unexpected error occurred during the kill attempt: {e}")
#     exit(1)

# sleep(1)

# print(f"\nAttempting to start new process: {procName} in the current terminal...")

# if platName in ["Darwin", "Linux"]:
#     system(start_command)
    
# elif platName == "Windows":
#     system(start_command)
    
print("\nSuccessfully executed the start sequence for the BAMM GUI!")
exit(0)