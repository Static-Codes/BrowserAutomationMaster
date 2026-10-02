// WhichDistroSharp exposes a Distro of it's own, which conflicts with Core.Types.Linux.Distro.
// Core.Types.Linux.Distro is the support entry for a distro, while WhichDistroSharp.Distro is the distro detected at runtime.
// To prevent the ambiguity the upstream enum is aliased to WhichDistro.
global using WhichDistro = WhichDistroSharp.Distro;

global using DistroFamily = WhichDistroSharp.DistroFamily;
global using IPlatform = WhichDistroSharp.IPlatform;
global using IDistroPackageInfo = WhichDistroSharp.IDistroPackageInfo;
global using PackageManager = WhichDistroSharp.PackageManager;
global using PackageManagerInfo = WhichDistroSharp.PackageManagerInfo;
global using PackageType = WhichDistroSharp.PackageType;

// Declared as extension classes rather than in a namespace of it's own.
global using static WhichDistroSharp.DistroIsExtensions;
global using static WhichDistroSharp.PackageTypeExtensions;

// The Is* methods on DistroIsExtensions are not used, a dictionary built from each entry's SupportedDistros resolves in O(1) instead.
