namespace BrowserAutomationMaster.Core.Types.Linux
{
    public static class PackageArtifactFormats
    {
        /// <summary>
        /// The extension BAMM actually publishes for each package type, without a leading dot.
        /// When BAMM does not publish a package for the given PackageType, it maps to <c>null</c>.
        private static readonly Dictionary<PackageType, string?> artifactExtensionMapping = new() {
            { PackageType.Deb, "deb" },
            { PackageType.Rpm, "rpm" },
            { PackageType.PkgTarXz, "pkg.tar.xz" },
            { PackageType.Tbz2, "tbz2" },

            // These PackageType(s) are recognized by WhichDistroSharp, but BAMM has no build target for them. 
            // While they are unused, they still may be referenced in a conditional check somewhere else within the codebase.
            { PackageType.Pkg, null },
            { PackageType.Apk, null },
            { PackageType.Txz, null },
            { PackageType.Tcz, null },
            { PackageType.Ipk, null },
            { PackageType.Xbps, null },
            { PackageType.Eopkg, null },
            { PackageType.Unknown, null },
        };

        /// <summary> The extension BAMM publishes for a package type, or <c>null</c> when it doesn't publish. </summary>
        /// <exception cref="KeyNotFoundException"> Thrown if the packageType cannot be resolved to it's associated file extension. </exception>
        public static string? GetArtifactExtension(PackageType packageType)
        {
            if (!artifactExtensionMapping.TryGetValue(packageType, out string? extension)) {
                throw new KeyNotFoundException(
                    $"The package type '{packageType}' has no entry in {nameof(PackageArtifactFormats)}." +
                    $" Add one, or a distribution using it will be reported as unsupported."
                );
            }

            return extension;
        }

        /// <summary> Returns the extension BAMM uses when publishing for the provided distro. </summary>
        public static string? GetArtifactExtension(WhichDistro distro)
        {
            var packageInfo = PackageManagerInfo.GetInfo(distro);

            return packageInfo == null ? null : GetArtifactExtension(packageInfo.PackageType);
        }

        /// <summary> Returns every member of the PackageType enum. </summary>
        public static IEnumerable<PackageType> GetMappedPackageTypes() => artifactExtensionMapping.Keys;
    }
}
