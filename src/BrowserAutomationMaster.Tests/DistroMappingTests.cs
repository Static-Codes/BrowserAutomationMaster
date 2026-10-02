using BrowserAutomationMaster.Core.Helpers;
using BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux;
using System.Reflection;
using Xunit;
using static BrowserAutomationMaster.Core.SystemInfo.OS.Unix.Linux.DistroManager;
using static WhichDistroSharp.DistroIsExtensions;
using static WhichDistroSharp.WhichDistroSharp;
using Distro = BrowserAutomationMaster.Core.Types.Linux.Distro;
using DistroFamily = WhichDistroSharp.DistroFamily;
using PackageManager = WhichDistroSharp.PackageManager;
using PackageManagerInfo = WhichDistroSharp.PackageManagerInfo;
using PackageType = WhichDistroSharp.PackageType;
using WhichDistro = WhichDistroSharp.Distro;

namespace BrowserAutomationMaster.Tests
{
    /// <summary> Checks that each distro BAMM claims to support is detected and handled as expected. </summary>
    /// <remarks>
    /// The package managers, commands and formats used to be maintained manually on a per distro basis.
    /// Following a refactor, they are now sourced from WhichDistroSharp.
    /// These tests keep that data pinned to it's state prior to the refactor.
    /// Ultimately, this ensures any discrepancy is caught here instead of on a user's machine.
    /// </remarks>
    public class DistroMappingTests
    {
        /// <summary> Ensures that an upstream distro can resolve to it's claiming entry. </summary>
        /// <param name="detectedDistro"> The distro that was resolved by WhichDistroSharp. </param>
        [Theory]
        [InlineData(WhichDistro.Ubuntu)]
        [InlineData(WhichDistro.Debian)]
        [InlineData(WhichDistro.Raspbian)]
        [InlineData(WhichDistro.Elementary)]
        [InlineData(WhichDistro.Parrot)]
        [InlineData(WhichDistro.Pop)]
        [InlineData(WhichDistro.Zorin)]
        [InlineData(WhichDistro.Kali)]
        [InlineData(WhichDistro.Linuxmint)]
        [InlineData(WhichDistro.Arch)]
        [InlineData(WhichDistro.Altlinux)]
        [InlineData(WhichDistro.Fedora)]
        [InlineData(WhichDistro.Freebsd)]
        [InlineData(WhichDistro.Opensuse)]
        [InlineData(WhichDistro.OpensuseLeap)]
        [InlineData(WhichDistro.OpensuseTumbleweed)]
        [InlineData(WhichDistro.Pclinuxos)]
        public void SupportedDistrosResolveToTheirEntry(WhichDistro detectedDistro)
        {
            Assert.NotNull(Resolve(detectedDistro));
        }

        /// <summary> Ensures that distros that derive from another distro still resolve to their claiming entry. </summary>
        /// <param name="detectedDistro"> The distro that was resolved by WhichDistroSharp. </param>
        /// <param name="expectedName">The result expected when the distro's name is resolved. </param>
        [Theory]
        [InlineData(WhichDistro.Raspbian, "Debian")]
        [InlineData(WhichDistro.OpensuseLeap, "openSUSE")]
        [InlineData(WhichDistro.OpensuseTumbleweed, "openSUSE")]
        public void DerivativeDistrosResolveToTheirBaseEntry(WhichDistro detectedDistro, string expectedName) {
            Assert.Equal(expectedName, Resolve(detectedDistro)?.Name);
        }

        /// <summary> Ensures that a distro not supported by BAMM resolves to null. </summary>
        /// <param name="detectedDistro">The distro that was resolved by WhichDistroSharp. </param>
        [Theory]
        [InlineData(WhichDistro.Manjaro)]
        [InlineData(WhichDistro.Nixos)]
        [InlineData(WhichDistro.Gentoo)]
        [InlineData(WhichDistro.Steamos)]
        [InlineData(WhichDistro.Unknown)]
        public void UnsupportedDistrosResolveToNull(WhichDistro detectedDistro) {
            Assert.Null(Resolve(detectedDistro));
        }

        /// <summary>
        /// Validates that every entry claims atleast one upstream Distro. 
        /// They can claim more than one, however, they MUST claim atleast one.
        /// </summary>
        [Fact]
        public void EveryEntryExceptUnknownClaimsAtLeastOneUpstreamDistro()
        {
            var entries = ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true)
                .Where(d => !d.Is(Distros.Unknown))
                .ToList();

            Assert.NotEmpty(entries);

            foreach (var entry in entries) {
                Assert.True(
                    entry.SupportedDistros.Length > 0,
                    $"The '{entry.Name}' entry claims no upstream distro, please address this immediately."
                );
            }
        }

        /// <summary> This test guards against duplicate entries in DistroManager; This will fail when met with a duplicate entry. </summary>
        [Fact]
        public void NoUpstreamDistroIsClaimedByTwoEntries()
        {
            var claims = new Dictionary<WhichDistro, string>();

            foreach (var entry in ReflectionHelper.GetStaticFieldsOfType<Distro>(typeof(Distros), true))
            {
                foreach (var supported in entry.SupportedDistros)
                {
                    Assert.False(claims.TryGetValue(supported, out var _));
                    claims[supported] = entry.Name;
                }
            }
        }

        /// <summary> Validates that every primary Distro family can be used to resolve a PackageManager object. </summary>
        /// <param name="detectedDistro">The Distro detected by WhichDistroSharp. </param>
        [Theory]
        [InlineData(WhichDistro.Arch)]
        [InlineData(WhichDistro.Altlinux)]
        [InlineData(WhichDistro.Debian)]
        [InlineData(WhichDistro.Elementary)]
        [InlineData(WhichDistro.Fedora)]
        [InlineData(WhichDistro.Freebsd)]
        [InlineData(WhichDistro.Kali)]
        [InlineData(WhichDistro.Linuxmint)]
        [InlineData(WhichDistro.Opensuse)]
        [InlineData(WhichDistro.Parrot)]
        [InlineData(WhichDistro.Pclinuxos)]
        [InlineData(WhichDistro.Pop)]
        [InlineData(WhichDistro.Ubuntu)]
        [InlineData(WhichDistro.Zorin)]
        public void EveryPrimaryUpstreamDistroHasPackageManagerData(WhichDistro detectedDistro) {
            Assert.NotNull(PackageManagerInfo.GetInfo(detectedDistro));
        }

        /// <summary> This test prevents a build from succeeding when PackageManagerInfo.Unknown has broken resolution logic. </summary>
        [Fact]
        public void UnknownPackageInfoHasExpectedValues()
        {
            var unknown = PackageManagerInfo.Unknown;

            Assert.Equal(PackageManager.Unknown, unknown.PackageManager);
            Assert.Equal(PackageType.Unknown, unknown.PackageType);
            Assert.Equal(DistroFamily.Unknown, unknown.Family);
            Assert.Equal("", unknown.PackageManagerCommand);
            Assert.Equal("", unknown.InstallCommand);
            Assert.Equal("", unknown.UninstallCommand);
            Assert.Equal("", unknown.QueryCommand);
            Assert.Equal("", unknown.QueryArguments);
        }

        
        /// <summary> Ensures that the WhichDistroSharp refactor made for the v1.0.0A8 release did not impact PackageType resolution. </summary>
        /// <param name="entryName"></param>
        /// <param name="expectedBase">The expected DistroFamily member. </param>
        /// <param name="expectedPackageManagerCommand">The expected command to invoke the package manager for the current <see cref="DistroFamily"/>. </param>
        /// <param name="expectedInstall">The expected command to invoke an installation operation with the aforementioned package manager. </param>
        /// <param name="expectedUninstall">The expected command to invoke an uninstallation operation with the aforementioned package manager. </param>
        /// <param name="expectedQueryCommand">The expected command to invoke a query operation with the aforementioned package manager. </param>
        /// <param name="expectedQueryArguments">The expected arguments to invoke a query operation with the aforementioned package manager. </param>
        /// <param name="expectedInstallKeyword">The expected keyword used to validate the results of the <see cref="expectedInstall"/> command. </param>
        /// <param name="expectedPackageType">The expected <see cref="PackageType"/> used for releases of BAMM for the target <see cref="entryName"/>. </param>
        [Theory]
        [InlineData("ArchLinux", DistroFamily.Arch, "pacman", "-S", "-Rns", "pacman", "-Qi", null, PackageType.PkgTarXz)]
        [InlineData("AltLinux", DistroFamily.Standalone, "epm", "install -y", "remove --purge -y", "rpm", "-q", null, PackageType.Rpm)]
        [InlineData("Debian", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("ElementaryOS", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("Fedora", DistroFamily.RHEL, "dnf", "install -y", "remove -y", "rpm", "-q", null, PackageType.Rpm)]
        [InlineData("FreeBSD", DistroFamily.BSD, "pkg", "install -y", "delete -y", "pkg", "info", null, PackageType.Pkg)]
        [InlineData("KaliLinux", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("LinuxMint", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("OpenSUSE", DistroFamily.SUSE, "zypper", "install -y", "remove -u", "zypper", "search -i", null, PackageType.Rpm)]
        [InlineData("ParrotOS", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("PCLinuxOS", DistroFamily.Standalone, "apt-get", "install -y", "remove --purge -y", "rpm", "-q", null, PackageType.Rpm)]
        [InlineData("PopOS", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("Ubuntu", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("ZorinOS", DistroFamily.Debian, "apt-get", "install -y", "remove --purge -y", "dpkg-query", "-W -f=${db:Status-Status}", "installed", PackageType.Deb)]
        [InlineData("Unknown", DistroFamily.Unknown, "", "", "", "", "", null, PackageType.Unknown)]
        public void PlatformValuesAreUnchanged(
            string entryName,
            DistroFamily expectedBase,
            string expectedPackageManagerCommand,
            string expectedInstall,
            string expectedUninstall,
            string expectedQueryCommand,
            string expectedQueryArguments,
            string? expectedInstallKeyword,
            PackageType expectedPackageType)
        {
            var entry = GetEntry(entryName);

            Assert.Equal(expectedBase, entry.BaseDistro);
            Assert.Equal(expectedPackageManagerCommand, entry.PackageManagerCommand);
            Assert.Equal(expectedInstall, entry.InstallCommand);
            Assert.Equal(expectedUninstall, entry.UninstallCommand);
            Assert.Equal(expectedQueryCommand, entry.QueryCommand);
            Assert.Equal(expectedQueryArguments, entry.QueryArguments);
            Assert.Equal(expectedInstallKeyword, entry.InstallationKeyword);
            Assert.Equal(expectedPackageType, entry.PackageType);
        }

        
        /// <summary> Ensures that a Distros object only resolves to its original claiming entry. </summary>
        /// <remarks>
        /// A distro's display name is not its identity. 
        /// A renamed or re-skinned distro must still resolve through its claiming entry.
        /// </remarks>
        [Fact]
        public void DistroComparesByReferenceTypeNotByDisplayName()
        {
            Assert.True(Distros.KaliLinux.Is(Distros.KaliLinux));
            Assert.False(Distros.KaliLinux.Is(Distros.Debian));

            Assert.True(Distros.PCLinuxOS.Is(Distros.PCLinuxOS));
            Assert.False(Distros.PCLinuxOS.Is(Distros.Debian));
        }

        /// <summary>
        /// Reads an entry by the name of the static field that holds it, so a theory case names the
        /// member of <c>Distros</c> rather than a display string that could be reworded.
        /// </summary>
        private static Distro GetEntry(string fieldName)
        {
            var field = typeof(Distros).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(field);
            return Assert.IsType<Distro>(field.GetValue(null));
        }

        [SkippableFact]
        public void LiveDetectionFindsTheRunningDistro()
        {
            Skip.IfNot(OperatingSystem.IsLinux(), "Live WhichDistroSharp detection coverage requires a Linux host.");

            Assert.True(Detect().WasFound(), "Detect() did not identify the running distribution.");
            Assert.False(string.IsNullOrWhiteSpace(DetectPlatform().Id), "DetectPlatform() reported no ID.");
        }
    }
}
