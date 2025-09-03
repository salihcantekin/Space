using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Audit;

public interface IAuditModuleProvider : IModuleProvider
{
    ValueTask Before<TRequest, TResponse>(TRequest request);
    ValueTask After<TRequest, TResponse>(TResponse response);
}