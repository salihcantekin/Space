# Multi-Project / Modular Solution Setup (vNext Root Aggregator Change)

> Breaking Change Introduced in This Version
>
> Previous versions of Space generated a single dependency injection extension by scanning ONLY the project that referenced `Space.SourceGenerator`. This prevented discovery of handlers / pipelines / notifications / modules declared in other class libraries unless each library also directly referenced the generator (causing duplicate registration logic & reflection cost risk).
>
> New behavior splits generation into:
> - A single ROOT aggregator project (produces `DependencyInjectionExtensions.g.cs` with `AddSpaceSourceGenerated` logic + runtime reflection to locate other assemblies' generated registration helpers).
> - Any number of SATELLITE libraries (each produces `SpaceAssemblyRegistration_<AssemblyName>.g.cs`) containing ONLY their own handler/pipeline/module/notification registration metadata.
>
> The root project then reflects over loaded assemblies to invoke `RegisterAssemblyServices` and `RegisterAssemblyHandlers` from each satellite.

## 1. Design Goals
- Enable multi-project solutions where handlers live in separate libraries (feature modules) without manual DI wiring.
- Avoid generating multiple root DI aggregators (which previously created collisions & redundant code).
- Provide explicit opt-in/out via MSBuild property instead of implicit behavior that could change with build output type.

## 2. Choosing the Root Aggregator Project
Exactly ONE project must act as the root aggregator.

Rules:
- Set MSBuild property `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>` in the chosen project.
- All other class libraries that contain handlers SHOULD either:
  - Omit the property entirely (default = satellite), or
  - Explicitly set `<SpaceGenerateRootAggregator>false</SpaceGenerateRootAggregator>`.
- If no project sets the property explicitly, a heuristic promotes non-DLL output kinds (Exe, Web) to root automatically.

Example root project `.csproj` fragment:
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>
</PropertyGroup>
```

Satellite library (no root generation):
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <SpaceGenerateRootAggregator>false</SpaceGenerateRootAggregator>
</PropertyGroup>
```

## 3. What Gets Generated Now
| Project Type | Generated File | Purpose |
|--------------|----------------|---------|
| Root | `DependencyInjectionExtensions.g.cs` | Provides `AddSpaceSourceGenerated` & orchestrates registry building + reflection scan for satellites |
| Satellite | `SpaceAssemblyRegistration_<Assembly>.g.cs` | Provides lightweight `RegisterAssemblyServices` + `RegisterAssemblyHandlers` methods for its own symbols |

## 4. Reflection-Based Discovery
At runtime, the root extension locates all static classes named `SpaceAssemblyRegistration_*` and invokes:
- `RegisterAssemblyServices(IServiceCollection, ServiceLifetime)` BEFORE building the provider.
- `RegisterAssemblyHandlers(IServiceProvider, SpaceRegistry, bool isSingleton)` AFTER provider creation.

This keeps per-assembly logic isolated and avoids re-scanning attributes at runtime.

## 5. Conditional Helper Emission
To reduce warnings (`CS8321` unused local functions), the generator now emits registration helper locals (`Reg`, `RegLight`, `RegPipe`, `RegModule`, `RegNotification`, `VT`) ONLY when at least one handler actually needs them. Satellite projects that define only plain handlers will not receive unused helpers.

## 6. Migration Guide (From Previous Version)
| Previous Behavior | New Behavior | Action Required |
|-------------------|-------------|-----------------|
| Every project with generator tried to generate full DI extension | Only designated root does | Add `<SpaceGenerateRootAggregator>true</...>` to root project |
| Handlers in other libraries ignored unless those libraries also referenced generator as root | All handler libraries generate lightweight assembly registration | Add generator reference normally; do NOT set root flag in satellites |
| Potential multiple full roots => collisions | Single orchestrator root | Remove duplicate roots or set property to false |
| All helper functions always emitted | Conditional emission to avoid warnings | No action |

## 7. Example Multi-Project Layout
```
/MySolution
  src/AppHost/AppHost.csproj              <-- Root (<SpaceGenerateRootAggregator>true)
  src/Features/Users/Users.csproj         <-- Satellite (handlers, pipelines)
  src/Features/Billing/Billing.csproj     <-- Satellite (handlers, modules)
  src/Infrastructure/Infra.csproj         <-- Satellite (notifications)
```
All satellites reference `Space.Abstraction` + `Space.DependencyInjection` (which brings the source generator). Only `AppHost` sets the root property.

## 8. Common Errors & Diagnostics
- Warning `SPACE_ROOT_MULTIPLE`: More than one project generated root aggregator. Fix by setting `<SpaceGenerateRootAggregator>false</...>` in non-root projects.
- Missing handlers from satellite: Ensure the satellite references the packages and that the root assembly loads it (direct or transitive reference so it is in the AppDomain).

## 9. Best Practices
- Keep cross-cutting modules in dedicated libraries; root should stay thin.
- Use explicit root property instead of relying on heuristic for clarity in large solutions.
- Avoid circular project references – generator only scans the current compilation.

## 10. FAQ
Q: Do satellites need to call any API manually?  
A: No. Root reflection picks them up automatically.

Q: Can a test project act as root?  
A: Yes—set the property to true. Useful for integration tests spanning multiple handler libraries.

Q: What if two EXE projects both set true?  
A: You will get a `SPACE_ROOT_MULTIPLE` warning in each build. Pick one root for the executed process.

---
This document describes the architectural breaking change enabling first-class multi-project handler discovery.
