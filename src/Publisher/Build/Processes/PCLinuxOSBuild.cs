using System.Text;
using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Managers.UpdateManager;
using static BrowserAutomationMaster.Messaging.Errors;
using static Publisher.Build.BuildInfo;

namespace Publisher.Build.Processes 
{
    public class PCLinuxOSBuild 
    {
        public required string archivePath;

        private static string AddRequirementList() {
            return """
            # PCLinuxOS Specific Requirement List
            Requires:       python3
            Requires:       which
            Requires:       lib64icu-devel
            Requires:       lib64openssl1.1.0
            Requires:       lib64zlib1
            Requires:       lib64krb53
            Requires:       xclip
            """;
        }


        private static byte[] GetBuildSpecContents() 
        {
            return Encoding.UTF8.GetBytes
            (
                $"""
                # Defining the full version for use in version control. 
                %define full_version {AppVersion}

                {GetDefinitions()}

                Name:           {AppName}
                Version:        {BaseVersion}
                Release:        0.{VersionIdentifier}.1pclos{DateTime.Now.Year}
                Summary:        {AppDescription}
                License:        {AppLicenseType}
                Group:          Applications/Editors
                ExclusiveArch:  x86_64
                Source0:        bamm-{BaseVersion}/bamm

                # Disable automatic dependency generation to prevent cross-distro contamination
                AutoReqProv:    no

                {AddRequirementList()}

                %description
                {AppExtendedDescription}
                
                %prep
                {GetSetupBlock()}

                %install
                {GetInstallationBlock()}
                """
            );
        }

        private static string GetDefinitions() {
            return """
            # This disable automatic binary stripping, which has been the cause of many bug reports in the dotnet runtime.
            %define __spec_install_post %{nil}
            %define __os_install_post %{nil}
            """;
        }
        private static string GetInstallationBlock() 
        {
            return """
            # Switch to the build directory, as the progress from prep is lost.
            cd %{name}-%{version}
            
            # Creating the target directories for the virtual buildroot
            mkdir -p %{buildroot}/usr/bin
            mkdir -p %{buildroot}/usr/lib/%{name}
            mkdir -p %{buildroot}/usr/share/applications

            # Copies all app files to the virtual buildroot.
            cp -r * %{buildroot}/usr/lib/%{name}/

            # Creating a sym link.
            ln -s /usr/lib/%{name}/%{name} %{buildroot}/usr/bin/%{name}

            %files
            # Dictating the package files.
            /usr/bin/%{name}
            /usr/lib/%{name}/
            """;
        }

        private static string GetSetupBlock() {
            return string.Join(NLC, [
                "mkdir -p %{_builddir}/%{name}-%{version}",
                "# This resolves to: /home/nerdy/rpmbuild/SOURCES/bamm-1.0.0/bamm",
                "cp %{_sourcedir}/%{name}-%{version}/%{name} %{_builddir}/%{name}-%{version}/%{name}",
                "cd %{_builddir}/%{name}-%{version}"
            ]);
        }

        public static (string rpmRootDir, string specFileName, string archiveName) CreateRequiredDirectories()
        {
            string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string rpmRootDir = Path.Combine(homeDir, "rpmbuild");
            var specFileName = $"{AppName}.spec";
            var archiveName = $"{AppName}-{BaseVersion}.tar.gz";

            string[] subDirs = ["BUILD", "RPMS", "SOURCES", "SPECS", "SRPMS"];

            // Creates each subdirectory within ~/rpmbuild/ (If not already present.)
            foreach (var dir in subDirs) 
            {
                var dirToCreate = Path.Combine(rpmRootDir, dir);

                if (Directory.Exists(dirToCreate)) {
                    continue;
                }
                
                try {
                    Directory.CreateDirectory(dirToCreate);
                }

                catch 
                {
                    WriteAndExit($"A fatal error occured trying to create: '{dirToCreate}'", 1);
                }
            }
            return (rpmRootDir, specFileName, archiveName);
        }


        /// <summary>
        /// <param name="outputPath">outputPath: The path to the PKGBUILD file to be written to the system's disk.</param>
        /// <br />
        /// <returns>Returns a tuple: 
        /// <br />
        /// Item1: A boolean representing the status of the operation
        /// <br />
        /// Item2: The Stream object with the contents of the binaryPath passed to PCLinuxOSBuild
        /// </returns>
        /// </summary>
        public async Task<(bool, string)> WriteBuildSpecFile(string outputPath) 
        {
            var success = false;
            FileStream? tempStream = null;
            
            try 
            {
                var fileContents = GetBuildSpecContents();

                tempStream = new(
                    outputPath, 
                    FileMode.Create, 
                    FileAccess.ReadWrite, 
                    FileShare.Read, 
                    4096, 
                    true
                );

                await tempStream.WriteAsync(fileContents);
                success = true;
            }

            catch (Exception ex) 
            {
                WriteAndExit(
                    message: string.Join(NLC, [
                        "Unable to write build.spec file to disk.",
                        "Error Log:",
                        ex.Message 
                    ]),
                    status: 1
                );
            }

            finally 
            {
                if (tempStream != null) {
                    // Since the stream is no longer needed it can be disposed of safely.
                    await tempStream.DisposeAsync();
                }
            }

            return (success, outputPath);
        }
    }
}