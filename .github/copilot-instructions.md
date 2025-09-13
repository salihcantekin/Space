# Space Framework

Space is a high-performance, source-generator powered mediator/messaging framework for .NET that eliminates runtime reflection, minimizes boilerplate, and provides an extensible module + pipeline model for cross-cutting concerns (caching, auditing).

**ALWAYS reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.**

## Working Effectively

### Bootstrap and Build the Repository
- **CRITICAL:** Install both .NET 8 and .NET 9 SDKs - the solution requires both:
  - `wget -q https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh`
  - `./dotnet-install.sh --channel 8.0`
  - `./dotnet-install.sh --channel 9.0`
  - `export PATH="$HOME/.dotnet:$PATH"`
- **RESTORE:** `dotnet restore` - takes 12 seconds. NEVER CANCEL. Set timeout to 5+ minutes.
- **BUILD:** `dotnet build -c Release --no-restore` - takes 13 seconds. NEVER CANCEL. Set timeout to 5+ minutes.
- **TEST:** `dotnet test tests/Space.Tests/Space.Tests.csproj -c Release --no-build --logger trx` - takes 1.5 seconds with 22 tests. NEVER CANCEL. Set timeout to 2+ minutes.

### Code Quality
- **FORMAT:** `dotnet format` - fixes whitespace and style issues. The codebase has formatting issues that MUST be fixed.
- **FORMAT CHECK:** `dotnet format --verify-no-changes` - verify code is properly formatted before committing.
- Always run `dotnet format` before making changes to ensure clean diffs.

### Running Applications
- **Test Console:** `dotnet run --project tests/Space.TestConsole/Space.TestConsole.csproj --configuration Release`
  - Interactive application - press ENTER when prompted to start
  - Demonstrates basic framework functionality
- **Benchmarks:** `dotnet run --project tests/Space.Benchmarks/Space.Benchmarks.csproj --configuration Release`
  - Runs performance comparisons against MediatR and Mediator libraries
  - Takes several minutes to complete full benchmark suite

## Validation

### Manual Testing Scenarios
- **ALWAYS test framework functionality after making changes:**
  - Build the solution and run the test console application
  - Create a simple request/response handler and verify it works
  - Test that the source generator produces the expected generated code
  - Run the full test suite to ensure no regressions

### CI Pipeline Validation
- **Validation Build:** Runs on feature branches and PRs - builds and tests only
- **Preview Publish:** Runs on releases marked as pre-release - publishes X.Y.Z-preview packages
- **Stable Publish:** Runs on normal releases - publishes X.Y.Z stable packages
- Always run `dotnet format` before committing or CI will fail with whitespace errors

## Project Structure and Key Locations

### Source Projects (src/)
- **Space.Abstraction:** Core interfaces, attributes, and contracts (.NET Standard 2.0)
- **Space.DependencyInjection:** DI extensions and registration (.NET 8)
- **Space.SourceGenerator:** Roslyn source generator for compile-time registration (.NET Standard 2.0)

### Test Projects (tests/)
- **Space.Tests:** MSTest unit tests (22 tests, runs in 1.5s) (.NET 9)
- **Space.TestConsole:** Interactive demo application (.NET 8)
- **Space.Benchmarks:** BenchmarkDotNet performance tests (.NET 8)

### Key Files to Monitor
- `src/Space.SourceGenerator/` - Contains the source generator that creates DI registration code
- `tests/Space.Tests/` - Comprehensive unit tests covering handlers, pipelines, modules, and notifications
- `docs/` - Complete documentation including project overview, handlers, pipelines, modules, and known issues
- `.github/workflows/` - CI pipelines for validation, preview, and stable publishing

## Common Tasks

### Adding New Features
- **Handlers:** Create classes with `[Handle]` attributes - see `tests/Space.Tests/Handle/` for examples
- **Pipelines:** Create methods with `[Pipeline]` attributes - see `tests/Space.Tests/Pipeline/` for examples
- **Modules:** Implement `ISpaceModuleAttribute` and `SpaceModule` - see `docs/Modules.md`
- **Notifications:** Use `[Notification]` attributes - see `tests/Space.Tests/Notification/` for examples

### Debugging Source Generator Issues
- Enable source generator output by uncommenting in `.csproj` files:
  ```xml
  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
  ```
- Generated files appear in `obj/[Configuration]/[TargetFramework]/Space.SourceGenerator/`

### Working with NuGet Packages
- **Published Packages:**
  - `Space.Abstraction` - Core abstractions
  - `Space.DependencyInjection` - DI extensions with source generator
  - `Space.Modules.InMemoryCache` - Optional caching module
- **Local Testing:** Use `GeneratePackageOnBuild=false` in Debug builds to avoid version conflicts

## Repository Output from Common Commands

### Repository Root Structure
```
ls -la /
.git/
.github/
.gitattributes
.gitignore
docs/
LICENSE.txt
README.md
SpaceSolution.sln
src/
tests/
```

### Solution Projects
```
SpaceSolution.sln contains:
- src/Space.Abstraction/Space.Abstraction.csproj
- src/Space.DependencyInjection/Space.DependencyInjection.csproj  
- src/Space.SourceGenerator/Space.SourceGenerator.csproj
- tests/Space.Tests/Space.Tests.csproj
- tests/Space.TestConsole/Space.TestConsole.csproj
- tests/Space.Benchmarks/Space.Benchmarks.csproj
```

### Documentation Files
```
docs/
├── DeveloperRecommendations.md
├── Handlers.md
├── KnownIssues.md
├── Modules.md
├── Notifications.md
├── Pipelines.md
├── PlannedImprovements.md
├── ProjectDoc.en.md
├── ProjectDoc.txt
└── Versioning.md
```

## Build Timing Expectations
- **dotnet restore:** ~12 seconds - NEVER CANCEL
- **dotnet build:** ~13 seconds - NEVER CANCEL  
- **dotnet test:** ~1.5 seconds - NEVER CANCEL
- **dotnet format:** ~2 seconds
- **dotnet run (console):** ~3 seconds build + interactive
- **dotnet run (benchmarks):** Several minutes for full suite

## Known Issues and Workarounds
- **Formatting:** Codebase has whitespace issues - always run `dotnet format` first
- **Multi-target:** Solution uses both .NET 8 and 9 - install both SDKs
- **Source Generator:** Generated files are in obj/ folders - enable `EmitCompilerGeneratedFiles` for debugging
- **Warnings:** Some CS8321 warnings about unused generated functions are expected
- **Benchmarks:** Permission denied for high priority is expected in sandboxed environments

## Quick Reference Commands
```bash
# Complete setup and validation
wget -q https://dot.net/v1/dotnet-install.sh && chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0 && ./dotnet-install.sh --channel 9.0
export PATH="$HOME/.dotnet:$PATH"
dotnet restore                           # 12s - NEVER CANCEL
dotnet build -c Release --no-restore     # 13s - NEVER CANCEL  
dotnet test tests/Space.Tests/Space.Tests.csproj -c Release --no-build --logger trx  # 1.5s
dotnet format --verify-no-changes        # Check formatting
dotnet format                            # Fix formatting

# Run applications
dotnet run --project tests/Space.TestConsole/Space.TestConsole.csproj -c Release
dotnet run --project tests/Space.Benchmarks/Space.Benchmarks.csproj -c Release
```