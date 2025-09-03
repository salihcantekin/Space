using System;
using System.Threading.Tasks;

namespace Space.Abstraction.Modules.Audit;


public class AuditModulePipelineWrapper<TRequest, TResponse>(AuditModuleConfig AuditConfig)
    : ModulePipelineWrapper<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : notnull
{
    // Uses a static registry to simulate singleton Audit provider per TRequest/TResponse pair.
    // In production SpaceModule will "own" the provider and inject it here.
    private static IAuditModuleProvider moduleProvider;

    // Optionally allow setting external provider (used for custom Audit).
    public static void UseCustomAuditProvider(IAuditModuleProvider provider)
        => moduleProvider = provider ?? throw new ArgumentNullException(nameof(provider));

    public override async ValueTask<TResponse> HandlePipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next)
    {
        await moduleProvider.Before<TRequest, TResponse>(ctx.Request);
        Console.WriteLine("Audit Module Called");
        // call the actual handle method
        var response = await next(ctx);

        await moduleProvider.After<TRequest, TResponse>(response);

        return response;
    }
}
