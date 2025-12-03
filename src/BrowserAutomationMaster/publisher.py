from os import getcwd
from subprocess import CalledProcessError, run

class Platform:
    def __init__(self, name, architecture, package_type=None):
        self.name = name
        self.architecture = architecture
        self.package_type = package_type

    def get_commands(self):
        rid_map = {
            "Win": "win",
            "OSX": "osx",
        }
        rid_prefix = rid_map.get(self.name)
        
        if self.name in ["Win", "OSX"]:
            rid = f"{rid_prefix}-{self.architecture.lower()}"
            
            return [
                f"dotnet publish -c Release -r {rid} "
                f"--self-contained true /p:PublishSingleFile=true"
            ]

        elif self.name == "Linux":
            rid_map = {
                "x64": "linux-x64",
                "ARM": "linux-arm",
                "ARM64": "linux-arm64",
            }

            rid = rid_map.get(self.architecture)

            if not rid or not self.package_type:
                return []

            return [
                f"dotnet publish --runtime {rid} "
                f"--configuration Release -p:Build{self.package_type.capitalize()}Package=true"
            ]

        return []

def main():

    error = (
        "Please ensure the .NET 8.X SDK is installed.\n"
        "Download link:\n"
        "https://dotnet.microsoft.com/en-us/download/dotnet/8.0"
    )
    
    platform_options = [
        ("All Platforms", None),
        ("Win", "x64"),
        ("Win", "ARM64"),
        ("Linux", "x64", "deb"),
        ("Linux", "x64", "rpm"),
        ("Linux", "ARM", "deb"),
        ("Linux", "ARM", "rpm"),
        ("Linux", "ARM64", "deb"),
        ("Linux", "ARM64", "rpm"),
        ("OSX", "x64"),
        ("OSX", "ARM64")
    ]

    platforms = []
    for option in platform_options:

        if len(option) == 3:
            platforms.append(Platform(option[0], option[1], option[2]))

        elif len(option) == 2:
            platforms.append(Platform(option[0], option[1]))

    print("Welcome to the BAMM Publisher!\n")
    menu_text = ""

    for index, option in enumerate(platform_options):
        if option[0] == "All Platforms":
            menu_text += f"{index+1}. All Platforms\n"

        else:
            package_info = f" ({option[2]})" if len(option) == 3 else ""
            menu_text += (
                f"{index+1}. {option[0]}-{option[1]}"
                f"{package_info}\n"
            )

    choice_index: int
    while True:
        raw_choice = input(
            f"Please choose an option from 1 to {len(platform_options)} "
            f"from the menu below.\n\n{menu_text}\n"
        )

        try:
            choice_index = int(raw_choice)
            if 0 < choice_index <= len(platform_options):
                break
            print()

        except ValueError:
            print("Invalid choice. Please enter a number.\n")

    commands = []

    if choice_index == 1:
        for p in platforms[1:]:
            commands.extend(p.get_commands())

    else:
        selected_platform = platforms[choice_index - 1]
        commands = selected_platform.get_commands()

    target_directory = getcwd()
    nologo_flag = " /nologo" 

    for cmd in commands:
        full_cmd = cmd + nologo_flag 
        print(f"\nExecuting: {full_cmd}\nTarget Directory: {target_directory}")

        try:
            process = run(
                full_cmd, 
                shell=True,
                check=True,
                text=True,
                capture_output=True,
                cwd=target_directory,
            )

            if process.stdout.strip():
                print(f"StdOut:\n{process.stdout.strip()}\n")

            if process.stderr.strip():
                print(f"StdErr:\n{process.stderr.strip()}\n")

        except CalledProcessError as e:
            print(f"Error executing command: {cmd}")
            print(f"Return Code: {e.returncode}\n")

            if e.stdout.strip():
                print(f"StdOut:\n{e.stdout.strip()}\n")

            if e.stderr.strip():
                print(f"StdErr:\n{e.stderr.strip()}\n")

            if "BuildDebPackage" in cmd:
                error += (
                    "\nIf you are compiling for a **Debian** based Linux, please ensure the necessary tool/build target "
                    "is installed, such as `dotnet tool install --global dotnet-deb --version 0.1.232`."
                )
            elif "BuildRpmPackage" in cmd:
                error += (
                    "\nIf you are compiling for a **Fedora/RHEL** based Linux, please ensure the necessary tool/build target "
                    "is installed, such as `dotnet tool install --global dotnet-rpm --version 0.1.232`."
                )
            
            print(error) 

        except FileNotFoundError:
            print(f"Error: Command '{cmd.split()[0]}' not found.")
            print(error)

        except Exception as e:
            print(f"An unexpected error occurred:\n{e}\n")
            print(error)

if __name__ == "__main__":
    main()