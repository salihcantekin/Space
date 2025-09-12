using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Audit;

public class NullAuditModuleProvider : IAuditModuleProvider
{
    ValueTask IAuditModuleProvider.Before<TRequest, TResponse>(TRequest request) => new();

    ValueTask IAuditModuleProvider.After<TRequest, TResponse>(TResponse response) => new();
}