# Known Issues / Breaking Changes

- Breaking: `ISpace.Send<TRequest, TResponse>` now requires `TRequest : IRequest<TResponse>`.
  - Migration:
    - If your request already implements `IRequest<TRes>`, nothing to change.
    - If not, either add `IRequest<TRes>` to your request type, or use `Send<TRes>(IRequest<TRes> request)` or `Send<TRes>(object request)` overloads.
  - Benefit: compile-time safety and better discoverability.

- Circular dependency in `ISpace` and `SpaceRegistry`: The first handler receives a null `ISpace(Lazy)` instance, which is set on subsequent requests.
- ~~When using handler names, modules are applied to all handlers instead of only those with the attribute.~~ (Fixed)

Refer to [ProjectDoc.txt](ProjectDoc.txt) for more details.
