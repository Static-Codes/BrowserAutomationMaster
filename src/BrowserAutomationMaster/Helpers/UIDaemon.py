from platform import system as pSystem
from os import system
from time import sleep
from subprocess import check_call, DEVNULL
import warnings

warnings.filterwarnings("ignore")

procName = "bamm"
platName = pSystem()
killCmd = None
startCmd = None

if platName == "Windows":
    killCmd = f"taskkill /F /im {procName}.exe"
    startCmd = f"{procName}.exe --gui"

elif platName in ["Darwin", "Linux"]:
    killCmd = f"pkill {procName}"
    startCmd = f"{procName} --gui"

else:
    print("Unsupported OS.")
    exit(1)

try:
    killStatus = check_call(killCmd, stdout=DEVNULL, stderr=DEVNULL, stdin=DEVNULL)

    if killStatus == 128:
        print("The HTTP Server associated with BAMM's GUI is currently inactive, starting now.\n")

    elif killStatus not in [0, 128]:
        print(
            "Unable to restart the HTTP Server associated with BAMM's GUI, "
            f"please try again.\n\nProcess exited with status code: {killStatus}\n"
        )
        exit(killStatus)
    
    else:
        print(
            "Successfully killed the HTTP Server associated with BAMM's GUI.\n"
            "Please wait while the server restarts, this may take up to 30 seconds on low end hardware.\n"
        )
    
    sleep(1)
    system(startCmd)
    startStatus = check_call(killCmd, stdout=DEVNULL, stderr=DEVNULL, stdin=DEVNULL)
    sleep(1)
    
    if startStatus != 0:
       print(
            "Unable to restart the HTTP Server associated with BAMM's GUI, "
            f"please try again.\n\nProcess exited with status code: {startStatus}\n"
        )
    else:
        print("Successfully restarted the HTTP Server associated with BAMM's GUI!\n")
    
    exit(startStatus)

except Exception as e:
    print(
        "An unhandled exception occured while attempt to restart the HTTP server associated with BAMM's GUI.\n\n"
        f"Exception:\n{e}"
    )
    exit(1)