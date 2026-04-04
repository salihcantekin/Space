using Space.Abstraction.Modules.Audit;

namespace Space.TestConsole;

public class NullAuditProvider : IAuditModuleProvider
{
    public ValueTask After<TRequest, TResponse>(TResponse response)
        where TRequest : notnull
        where TResponse : notnull
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask Before<TRequest, TResponse>(TRequest request)
        where TRequest : notnull
        where TResponse : notnull
    {
        return ValueTask.CompletedTask;
    }
}
