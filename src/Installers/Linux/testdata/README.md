# Test data for the BAMM for Linux installer

## `package-formats.json`

- A snapshot of the output of `DistroPackageMap.Serialize()` ([DistroPackageMap.cs](../../../BrowserAutomationMaster/Core/Types/Linux/DistroPackageMap.cs)).
- The Publisher writes the real file next to the release artifacts, and `install.sh` downloads it from the release tag to pick which package format to install.
- `test-install.sh` reads this copy so the tests can run without a network connection or a release.

### Why this file is not in `sections/`

- The other test data moved to [known-distribution-gaps.md](../../../../sections/known-distribution-gaps.md), since someone has to read and understand it.
- This one is generated, has to stay machine readable, and the tests have to run without building BAMM first.

### Regenerating

1. From the repository root, run:
   ```bash
   dotnet msbuild src/Installers/Linux/PackageFormats.targets -t:RegeneratePackageFormatMap
   ```
2. This builds the solution and calls the generator, so do not hand edit the file, and do not write a temporary test to regenerate it.
3. To change the `generatedFor` label, add `-p:PackageFormatMapTag=v1.0.0A9`. Nothing reads this value.

- Run this after changing `DistroPackageMap`, `PackageArtifactFormats`, or upgrading WhichDistroSharp.
- Always check the diff before committing. This file records which distributions BAMM publishes a package for, and regenerating it to make a failing test pass changes that silently. `test-install.sh` will then pass against a map that no longer matches the release.
