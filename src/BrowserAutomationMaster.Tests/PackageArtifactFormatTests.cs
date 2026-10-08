using BrowserAutomationMaster.Core.Types.Linux;
using WhichDistroSharp;
using Xunit;
using PackageType = WhichDistroSharp.PackageType;

namespace BrowserAutomationMaster.Tests
{
    /// <summary>
    /// Covers PackageArtifactFormats, the table that maps a package format to the extension BAMM publishes for it.
    /// </summary>
    /// <remarks>
    /// A mapping that's missing an entry doesn't resolve to an artifact. The user is instructed that the distribution is unsupported.
    /// </remarks>
    public class PackageArtifactFormatTests
    {
        /// <summary>
        /// An update to WhichDistroSharp can add a PackageType. 
        /// This guard clause prevents builds from succeeding if this happens to be the case.
        /// That should fail here instead of on a user's machine.
        /// </summary>
        [Fact]
        public void EveryPackageTypeHasAnEntry()
        {
            var unmapped = Enum.GetValues<PackageType>()
                .Where(type => !PackageArtifactFormats.GetMappedPackageTypes().Contains(type))
                .ToList();

            Assert.True(
                unmapped.Count == 0,
                $"These package types have no artifact mapping: {string.Join(", ", unmapped)}. " +
                "Add each to PackageArtifactFormats, or a distribution using it will be reported as unsupported."
            );
        }

        /// <summary>
        /// This guard clause ensures that only <see cref="PackageType"/> values used by the publisher are resolved.
        /// </summary>
        [Fact]
        public void OnlyPublishedFormatsResolveToAnExtension()
        {
            var published = Enum.GetValues<PackageType>()
                .Where(type => PackageArtifactFormats.GetArtifactExtension(type) != null)
                .ToList();

            Assert.Equal(
                new[] { PackageType.Deb, PackageType.Rpm, PackageType.PkgTarXz, PackageType.Tbz2 }.OrderBy(t => t),
                published.OrderBy(t => t)
            );
        }

        [Theory]
        [InlineData(PackageType.Apk)]
        [InlineData(PackageType.Pkg)]
        [InlineData(PackageType.Ipk)]
        [InlineData(PackageType.Xbps)]
        [InlineData(PackageType.Eopkg)]
        [InlineData(PackageType.Txz)]
        [InlineData(PackageType.Tcz)]
        [InlineData(PackageType.Unknown)]
        public void UnpublishedPackageTypesResolveToNoArtifact(PackageType type)
        {
            Assert.Null(PackageArtifactFormats.GetArtifactExtension(type));
        }

        // The third column is the Publisher target that justifies the entry, so this answers "does BAMM really ship this". 
        // Note to future-self: 
        // If FreeBSD looks wrong in the table, ignore it, the BAMM Publisher has no BSD target, that's handled via standalone binary releases.
        [Theory]
        [InlineData(PackageType.Deb, "deb", "Debian Package (.deb)")]
        [InlineData(PackageType.Rpm, "rpm", "Fedora Package (.rpm)")]
        [InlineData(PackageType.PkgTarXz, "pkg.tar.xz", "Arch Package (.pkg.tar.xz)")]
        [InlineData(PackageType.Tbz2, "tbz2", "Gentoo Package (.tbz2)")]
        public void EachPublishedFormatHasAPublisherBuildTarget(
            PackageType type, string expected, string publisherTarget)
        {
            Assert.Equal(expected, PackageArtifactFormats.GetArtifactExtension(type));
            Assert.NotEmpty(publisherTarget);
        }

        // Pkg is the null that looks wrong. Pinned so it does not get "fixed" without a BSD target.
        [Fact]
        public void PkgHasNoArtifactBecauseThePublisherHasNoBsdTarget()
        {
            Assert.Equal(PackageType.Pkg, PackageManagerInfo.GetInfo(WhichDistroSharp.Distro.Freebsd)?.PackageType);
            Assert.Null(PackageArtifactFormats.GetArtifactExtension(WhichDistroSharp.Distro.Freebsd));
            Assert.Null(PackageArtifactFormats.GetArtifactExtension(WhichDistroSharp.Distro.Dragonfly));
            Assert.Null(PackageArtifactFormats.GetArtifactExtension(WhichDistroSharp.Distro.Solaris));
        }

        /// <summary>
        /// Ensures the following: 
        ///     1. PCLinuxOS reports Rpm with an apt package manager. 
        ///     2. Deriving the extension from either of those, returns a .deb instead of the published format.
        /// </summary>
        
        [Fact]
        public void PclinuxosPublishesAnRpmDespiteUsingApt()
        {
            var packageInfo = PackageManagerInfo.GetInfo(WhichDistroSharp.Distro.Pclinuxos);

            Assert.NotNull(packageInfo);
            Assert.Equal(PackageManager.Apt, packageInfo.PackageManager);
            Assert.Equal(PackageType.Rpm, packageInfo.PackageType);

            // The published format, which is what the installer downloads.
            Assert.Equal("rpm", PackageArtifactFormats.GetArtifactExtension(WhichDistroSharp.Distro.Pclinuxos));
        }

        /// <summary>
        /// Guard clause for package extension resolution for non-rpm and non-deb Distros.
        /// </summary>
        /// <param name="distro">The distro to test. </param>
        /// <param name="expected">The package extension to test. </param>
        [Theory]
        [InlineData(WhichDistroSharp.Distro.Arch, "pkg.tar.xz")]
        [InlineData(WhichDistroSharp.Distro.Gentoo, "tbz2")]
        public void DistrosOutsideDebAndRpmResolveToTheirOwnFormat(WhichDistroSharp.Distro distro, string expected)
        {
            Assert.Equal(expected, PackageArtifactFormats.GetArtifactExtension(distro));
        }

        [Fact]
        public void TheFormatlessDistroResolvesToNoArtifact()
        {
            Assert.Null(PackageArtifactFormats.GetArtifactExtension(WhichDistroSharp.Distro.Unknown));
        }
    }
}
