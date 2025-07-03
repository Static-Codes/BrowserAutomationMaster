from os import getcwd, system
from subprocess import CalledProcessError, run


menuOptions: dict = {
    1: "All Platforms",
    2: "Win-x64",
    3: "Win-ARM64",
    4: "Linux-x64 (Debian PKG)",
    5: "Linux-ARM64 (Debian PKG)",
    6: "OSX-x64",
    7: "OSX-ARM64",
}

print("Welcome to the BAMM Publisher\n")
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
    commands.append("dotnet deb --runtime linux-x64 --configuration Release")
    commands.append("dotnet deb --runtime linux-arm64 --configuration Release")
    commands.append("dotnet publish -c Release -r osx-x64 --self-contained true")
    commands.append("dotnet publish -c Release -r osx-arm64 --self-contained true")
    commands.append("dotnet publish -c Release -r win-x64 --self-contained true")
    commands.append("dotnet publish -c Release -r win-arm64 --self-contained true")

elif choiceIndex == 2:
    commands.append("dotnet publish -c Release -r win-x64 --self-contained true")

elif choiceIndex == 3:
    commands.append("dotnet publish -c Release -r win-arm64 --self-contained true")

elif choiceIndex == 4:
    commands.append("dotnet deb --runtime linux-x64 --configuration Release")

elif choiceIndex == 5:
    commands.append("dotnet deb --runtime linux-arm64 --configuration Release")

elif choiceIndex == 6:
    commands.append("dotnet publish -c Release -r osx-x64 --self-contained true")

elif choiceIndex == 7:
    commands.append("dotnet publish -c Release -r osx-arm64 --self-contained true")


targetDirectory = input("Please enter the path containing your .csproj file:\n")
for cmd in commands:
    print(
        f"\nExecuting: {cmd}\nTarget Directory: {targetDirectory if targetDirectory else getcwd()})"
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
