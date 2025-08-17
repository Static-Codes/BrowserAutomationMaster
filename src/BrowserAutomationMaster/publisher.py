from os import getcwd, system
from subprocess import CalledProcessError, run


menuOptions: dict = {
    1: "All Platforms",
    2: "Win-x64",
    3: "Win-ARM64",
    4: "Linux-x64 (.deb)",
    5: "Linux-x64 (.rpm)",
    6: "Linux-ARM64 (.deb)",
    7: "Linux-ARM64 (.rpm)",
    8: "OSX-x64",
    9: "OSX-ARM64",
    10: "Linux-ARM32 (.deb)",
}

print("Welcome to the BAMM Publisher!\n\n")
menuText = ""

for index, optionName in menuOptions.items():
    menuText += f"{index}. {optionName}\n"


choiceIndex: int
choiceText: str
while True:
    raw_choice: str = input(
        f"Please choose an option between 1 and {len(menuOptions)} from the menu below.\n\n{menuText}\n"
    )
    try:
        choiceIndex = int(raw_choice)
        if 0 < choiceIndex <= len(menuOptions):
            break
        print()
    except Exception:
        print("Invalid choice.\n")

commands = []
if choiceIndex == 1:
    commands.append("dotnet deb --runtime linux-x64 --configuration Release -- -p:BuildDebPackage=true")   # Linux x64 (Deb)
    commands.append("dotnet deb --runtime linux-arm64 --configuration Release -- -p:BuildDebPackage=true") # Linux ARM64 (Deb)
    commands.append("dotnet rpm --runtime linux-x64 --configuration Release -- -p:BuildRpmPackage=true")   # Linux x64 (Rpm)
    commands.append("dotnet rpm --runtime linux-arm64 --configuration Release -- -p:BuildRpmPackage=true") # Linux ARM64 (Rpm)
    commands.append("dotnet publish -c Release -r osx-x64 --self-contained true")                       # OSX x64
    commands.append("dotnet publish -c Release -r osx-arm64 --self-contained true")                     # OSX ARM64
    commands.append("dotnet publish -c Release -r win-x64 --self-contained true")                       # Win x64
    commands.append("dotnet publish -c Release -r win-arm64 --self-contained true")                     # Win ARM64

elif choiceIndex == 2:
    commands.append("dotnet publish -c Release -r win-x64 --self-contained true")                       # Win x64

elif choiceIndex == 3:
    commands.append("dotnet publish -c Release -r win-arm64 --self-contained true")                     # Win ARM64

elif choiceIndex == 4:
    commands.append("dotnet deb --runtime linux-x64 --configuration Release -- -p:BuildDebPackage=true")   # Linux x64 (Deb)

elif choiceIndex == 5:
    commands.append("dotnet rpm --runtime linux-x64 --configuration Release -- -p:BuildRpmPackage=true")   # Linux x64 (Rpm)

elif choiceIndex == 6:
    commands.append("dotnet deb --runtime linux-arm64 --configuration Release -- -p:BuildDebPackage=true") # Linux ARM64 (Deb)

elif choiceIndex == 7:
    commands.append("dotnet rpm --runtime linux-arm64 --configuration Release -- -p:BuildRpmPackage=true") # Linux ARM64 (Rpm)

elif choiceIndex == 8:
    commands.append("dotnet publish -c Release -r osx-x64 --self-contained true")                       # OSX x64

elif choiceIndex == 9:
    commands.append("dotnet publish -c Release -r osx-arm64 --self-contained true")                     # OSX ARM64

elif choiceIndex == 10:
    commands.append("dotnet deb --runtime linux-arm --configuration Release -- -p:BuildDebPackage=true")


targetDirectory = getcwd() #input("Please enter the path containing your .csproj file:\n")
for cmd in commands:
    print(
        f"\nExecuting: {cmd}\nTarget Directory: {targetDirectory})"
    )
    try:
        # The 'cwd' parameter is the key here
        process = run(
            cmd,
            shell=True,  # Allows shell features like 'dir' or 'ls'
            check=True,  # Raises CalledProcessError on non-zero exit codes
            text=True,  # Capture output as string
            capture_output=True,  # Capture stdout and stderr
            cwd=targetDirectory,  # THIS IS WHERE WE TELL IT TO 'CD'
        )
        if process.stdout.strip != "":
            print(f"StdOut:\n{process.stdout.strip()}\n")
        if process.stderr.strip != "":
            print(f"StdErr:\n{process.stderr.strip()}\n")
    except CalledProcessError as e:
        print(f"Error executing command: {cmd}")
        print(f"Return Code: {e.returncode}")
        if e.stdout.strip != "":
            print(f"StdOut:\n{e.stdout.strip()}")
        if e.stderr.strip != "":
            print(f"StdErr:\n{e.stderr.strip()}")
        print("Please ensure the .NET 8.X SDK is installed.\n")
        print("Download link:\nhttps://dotnet.microsoft.com/en-us/download/dotnet/8.0")
    except FileNotFoundError:
        print(f"Error: Command '{cmd.split()[0]}' not found.")
        print("Please ensure the .NET 8.X SDK is installed.\n")
        print("Download link:\nhttps://dotnet.microsoft.com/en-us/download/dotnet/8.0")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")
        print("Please ensure the .NET 8.X SDK is installed.\n")
        print("Download link:\nhttps://dotnet.microsoft.com/en-us/download/dotnet/8.0")
