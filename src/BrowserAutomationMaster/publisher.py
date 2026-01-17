from os import getcwd
from subprocess import CalledProcessError, run
import platform as platform_package

class Platform:
    def __init__(self, name, architecture, package_type=None):
        self.name = name
        self.architecture = architecture.lower() if architecture else None
        self.package_type = package_type

    def get_commands(self):
        # Windows + MacOS
        if self.name in ["Win", "OSX"]:
            return [
                f"dotnet publish -c Release -r {self.name.lower()}-"
                f"{self.architecture} --self-contained true"
            ]

        # Linux
        elif self.name == "Linux":
            rid_map = {
                "x64": "linux-x64",
                "arm": "linux-arm",
                "arm64": "linux-arm64",
            }

            rid = rid_map.get(self.architecture)

            if not rid:
                return []

            return [
                f"dotnet {self.package_type.lower()} --runtime {rid} --configuration Release "
                f"-- " ## Passed the param below to MSBuild.
                f"-p:Build{self.package_type.title()}Package=true"
            ]

        return []


def main():
    error = (
        "Please ensure the .NET 10.X SDK is installed.\n"
        "Download link:\n"
        "https://dotnet.microsoft.com/en-us/download/dotnet/10.0"
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
            arch_display = option[1].upper()
            package_info = f" ({option[2]})" if len(option) == 3 else ""
            menu_text += (
                f"{index+1}. {option[0]}-{arch_display}"
                f"{package_info}\n"
            )

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
    operating_system = platform_package.platform().lower()

    for cmd in commands:
        new_cmd = cmd
        if "dotnet rpm" in cmd or "dotnet deb" in cmd:
            if "windows" in operating_system:
                new_cmd = f"set DOTNET_ROLL_FORWARD=Major && {cmd}"
            elif "darwin" in operating_system or "linux" in operating_system:
                new_cmd = f"export DOTNET_ROLL_FORWARD=Major && {cmd}"
            else:
                print("Unsupported operating system: {0}", operating_system)
                exit(1)

        print(f"\nExecuting: {new_cmd}\nTarget Directory: {target_directory}\n")
        try:
            process = run(
                new_cmd,
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
            print(f"Error executing command: {new_cmd}")
            
            # if (e.returncode == 150):
            #     print("Please install the dotnet 9 runtime, to execute dotnet deb or dotnet rpm.")
            #     return

            if e.stdout.strip():
                print(f"StdOut:\n{e.stdout.strip()}\n")

            if e.stderr.strip():
                print(f"StdErr:\n{e.stderr.strip()}\n")

            if "linux" in cmd:
                error += (
                    "\nIf you are compiling for a Debian based Linux, please ensure dotnet-deb is installed by running.\n"
                    "dotnet tool install --global dotnet-deb --version 0.1.232\n"
                    "\nIf you are compiling for a Fedora based Linux, please ensure dotnet-rpm is installed by running.\n"
                    "dotnet tool install --global dotnet-rpm --version 0.1.232"
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
