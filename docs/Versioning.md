# Versioning and Release Publishing

This repository uses GitHub Releases to drive NuGet publishing. Packages are NOT published on branch pushes.

- Stable releases (production publish):
  - Create a GitHub Release and do not mark it as pre-release.
  - Tag name can be either vX.Y.Z or X.Y.Z (a single leading v/V is ignored).
  - The stable workflow builds, tests, packs, and publishes packages with version X.Y.Z.

- Preview releases (dev publish):
  - Create a GitHub Release and mark it as pre-release.
  - Tag name can be either vX.Y.Z or X.Y.Z.
  - The preview workflow builds, tests, packs, and publishes packages with version X.Y.Z-preview.

What runs where
- Stable publish: .github/workflows/prod-ci.yml
- Preview publish: .github/workflows/dev-ci.yml (only runs when the release is prerelease=true)
- Validation builds (no publish): .github/workflows/validation-build.yml
  - Triggered on pushes to feature/* or features/*, and on pull requests into dev or master.

Tag parsing details
- The workflows normalize the tag name by:
  - Trimming whitespace.
  - Removing refs/tags/ and tags/ prefixes when present.
  - Stripping a single leading v or V (e.g., v1.2.3 -> 1.2.3).
- Basic SemVer validation is applied: X.Y.Z with optional pre-release/build metadata.

Projects built and published
- Space.Abstraction
- Space.DependencyInjection

The workflows perform these steps
- dotnet restore
- dotnet build -c Release (injecting the parsed version)
- dotnet test (Release)
- dotnet pack (Release) into artifacts with the parsed version
- dotnet nuget push artifacts/*.nupkg to nuget.org with skip-duplicate

Secrets required
- NUGET_API_KEY must be configured in the repository secrets for publishing to NuGet.

Local development notes
- Debug builds use a safe local version such as 0.0.0-local to avoid clashing with published packages (see csproj files).
- Release builds in CI set GeneratePackageOnBuild and inject the correct version from the tag.

Examples
- Stable: tag v1.2.0 (or 1.2.0) -> publish 1.2.0 to NuGet.
- Preview: tag v1.3.0 (or 1.3.0) and mark the release as pre-release -> publish 1.3.0-preview to NuGet.

Steps to publish
1) Create a GitHub tag: vX.Y.Z (recommended) or X.Y.Z.
2) Create a GitHub Release from the tag.
   - Leave pre-release unchecked for stable.
   - Check pre-release for preview.
3) Wait for the corresponding workflow to complete.
