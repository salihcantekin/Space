# Space Module Contribution Guide

This document describes how to propose, build, and submit a new Space module.

The goal is to keep modules consistent, easy to review, and straightforward to adopt. Please read this end to end before opening a proposal.

## Contribution flow (high level)

1) Propose a module
- Open a GitHub Discussion in the Space repository under the Ideas category.
- Title format: "Proposal: <ModuleName> module"
- Include: problem the module solves, rough design, expected options, minimal sample code.

2) Repository creation
- A maintainer will create a dedicated repository for the module under the maintainer’s account.
- Repository naming convention: `Space.Modules.<ModuleName>` (PascalCase; no spaces).
- You will fork that repository and submit PRs there. Do not create modules inside the main Space repo.

3) Development and PRs
- Work in topic branches off `main`.
- Submit focused PRs with a clear description, rationale, and tests.
- The maintainer will review, iterate via comments, and merge when ready.

4) Publishing
- The maintainer owns publishing. Contributors must not publish to NuGet under the official package id.
- Releases will be prepared and tagged by the maintainer after review.

## Repository expectations

Recommended structure:
```
Space.Modules.<ModuleName>/
  src/
    Space.Modules.<ModuleName>/                 # main module project (runtime)
  tests/
    Space.Modules.<ModuleName>.Tests/           # unit tests (MSTest)
  samples/
    SampleApp/                                  # optional runnable sample
  docs/
    README.md                                   # usage and design notes for this module
  .editorconfig
  .gitattributes
  .gitignore
  LICENSE
  Directory.Build.props (optional)
  global.json (optional)
```

Targets:
- Runtime project: .NET 8 (aligns with Space.DependencyInjection target). Keep APIs usable from apps on .NET 8+.
- Optional abstractions-only project: .NET Standard 2.0 (only if needed for shared contracts).

Namespaces & package id:
- Root namespace: `Space.Modules.<ModuleName>`
- NuGet package id: `Space.Modules.<ModuleName>`

CI (GitHub Actions):
- Build (Release), run unit tests, verify formatting (`dotnet format --verify-no-changes`).
- Optional: publish preview package on tag ending with `-preview` (maintainer will enable).

## Module design guidelines

- Attribute-first: define a module attribute that implements/extends Space’s module attribute model to activate behavior, e.g. `[MyModule(...)]`.
- Options: if your module has options, provide an options class and an options builder/extension to configure profiles ("Default" and custom profiles). Keep options POCO.
- Provider: if the module delegates to a provider (e.g., audit, cache backend), define a provider interface (e.g., `IMyModuleProvider`) and allow swapping providers.
- Profiles: support profiles consistently (`Profile = "Default"` and custom). Respect profile resolution in the generated code.
- Performance: follow Space’s micro-optimizations—no per-call reflection, prefer ValueTask, avoid unnecessary allocations, and fast-path conditionals.
- Source generator friendly: attributes should be discoverable by the Space source generator (public/internal visibility as appropriate). Keep attributes simple and immutable.

### Attribute example (sketch)
```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class MyModuleAttribute : Attribute, ISpaceModuleAttribute
{
    public string Profile { get; set; } = "Default";
    public int Level { get; set; } = 0;
}
```

### Provider example (sketch)
```csharp
public interface IMyModuleProvider : IModuleProvider
{
    ValueTask Before<TRequest, TResponse>(TRequest request)
        where TRequest : notnull where TResponse : notnull;
    ValueTask After<TRequest, TResponse>(TResponse response)
        where TRequest : notnull where TResponse : notnull;
}
```

### DI extensions (sketch)
```csharp
public static class MyModuleServiceCollectionExtensions
{
    public static IServiceCollection AddSpaceMyModule(this IServiceCollection services, Action<MyModuleOptions> configure = null)
    {
        var options = new MyModuleOptions();
        configure?.Invoke(options);
        // register providers, defaults, profiles
        return services;
    }
}
```

## Coding standards

- C#: follow .NET coding conventions, run `dotnet format` before commits.
- Async: return `ValueTask` where appropriate; prefer `IsCompletedSuccessfully` fast paths.
- No runtime reflection in hot paths; rely on Space’s generated registrations.
- Public API: keep minimal and consistent; XML docs for public types/members.
- Nullability: enable nullable context.

## Testing

- Unit tests: MSTest, xUnit, nUnit. Cover positive/negative paths.
- Integration: optional sample app in `samples/` to demonstrate usage.
- Benchmarks: optional BenchmarkDotNet project to validate overhead.

## Versioning & compatibility

- SemVer (MAJOR.MINOR.PATCH).
- Keep compatible with the latest Space major version. If a breaking Space change happens, align the module’s next major.
- Document breaking changes in a CHANGELOG.

## Documentation required in the module repo

- `docs/README.md`: motivation, installation, basic usage, configuration, examples.
- `CHANGELOG.md`: notable changes per release.
- `CONTRIBUTING.md`: module-specific contribution notes (PR process can link back here).
- `LICENSE`: MIT unless otherwise discussed.

## Pull request (PR) checklist

- [ ] Module attribute(s) and options implemented
- [ ] Provider interface(s) (if applicable) with default provider (if applicable)
- [ ] DI registration extension(s)
- [ ] Unit tests passing (`dotnet test`)
- [ ] Formatting validated (`dotnet format --verify-no-changes`)
- [ ] Samples/docs updated
- [ ] No runtime reflection in hot paths; benchmarks (optional)

## Review & merge process

- Maintainer reviews PRs, may request changes.
- Technical discussions happen in the PR or linked Discussion.
- After approval, maintainer merges to `main`.

## How to start

1. Open a Discussion (Ideas) with your proposed module name and description.
2. Wait for the maintainer to create `Space.Modules.<ModuleName>` repository.
3. Fork it and start contributing via PRs.

Questions? Open a Discussion in the main Space repo.
