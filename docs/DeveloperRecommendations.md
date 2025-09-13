# Developer Recommendations

- Use `Notification.Publish` with configurable `DispatchType` for flexible event handling.
- Integrate with `LoggerFactory` for advanced logging.
- Follow interface and config standards when implementing custom modules.
- Review the `Space.Modules.InMemoryCache` implementation for module development best practices.
- Prefer the strongly-typed Send API: `Send<TRequest, TResponse>(TRequest, string? name = null)` with `TRequest : IRequest<TResponse>`.
  - For dynamic scenarios, use `Send<TResponse>(IRequest<TResponse>)` or `Send<TResponse>(object)`.
- Keep handlers/pipelines ValueTask-based where possible to minimize allocations.
- Favor singleton lifetime to hit Space’s fast-path (no scope allocations) for hot paths.
- Avoid reflection at runtime; rely on the source generator output for registrations.
- Pipelines: keep them small; prefer ordered composition and share state via `PipelineContext.Items`.
- Benchmarks: run `tests/Space.Benchmarks` to compare typed/IRequest/object Send paths.
- Always run `dotnet format` before committing.

For more, see [ProjectDoc.txt](ProjectDoc.txt).
