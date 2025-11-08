# Known Issues / Breaking Changes

- Breaking (Packaging shift): The Space.SourceGenerator analyzer now ships with `Space.Abstraction`. `Space.DependencyInjection` only contains DI extensions and runtime implementations.
  - Migration: Add `Space.Abstraction` to EVERY project that uses Space attributes. For host/root projects that use DI, also reference `Space.DependencyInjection`. Remove any direct `Space.SourceGenerator` references.
  - Root aggregator project MUST reference BOTH packages. Satellite libraries typically only need `Space.Abstraction`.
  - Benefit: Compile-time generation works even without DI; DI is optional for libraries.

- Breaking (Multi-Project Root Aggregator): A new MSBuild property `<SpaceGenerateRootAggregator>` controls which project generates the root dependency injection aggregator (`DependencyInjectionExtensions.g.cs`). Other projects now generate lightweight `SpaceAssemblyRegistration_<Assembly>.g.cs` files. You MUST set this property to `true` in exactly one root project when using multiple handler class libraries. See `MultiProjectSetup.md`.
  - Migration: Add `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>` to your host / composition root `.csproj`. Set `false` (or omit) in feature libraries.
  - Benefit: Enables discovery & registration of handlers/pipelines/modules/notifications across multiple assemblies without duplicate roots or manual DI wiring.

- Breaking: `ISpace.Send<TRequest, TResponse>` now requires `TRequest : IRequest<TResponse>`.
  - Migration:
    - If your request already implements `IRequest<TRes>`, nothing to change.
    - If not, either add `IRequest<TRes>` to your request type, or use `Send<TRes>(IRequest<TRes> request)` or `Send<TRes>(object request)` overloads.
  - Benefit: Better compile-time safety and discoverability.

- Circular dependency in `ISpace` and `SpaceRegistry`: The first handler may observe a null `ISpace` (lazy) instance; it is populated on subsequent requests.
- ~~When using handler names, modules are applied to all handlers instead of only those with the attribute.~~ (Fixed)

Refer also to `MultiProjectSetup.md` for architectural details.
