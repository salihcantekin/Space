using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Space.Abstraction.Modules;


public class ModuleFactory(IServiceProvider sp)
{
    private readonly ConcurrentDictionary<string, SpaceModule> moduleMasters = [];

    public ModulePipelineWrapper<TRequest, TResponse> GetModule<TRequest, TResponse>(string moduleName, string profileName)
    {
        var masterClassType = sp.GetKeyedService<Type>(moduleName) ?? throw new SpaceModuleRegistrationMissingException(moduleName); // get master class type by module name

        var masterClass = moduleMasters.GetOrAdd(moduleName, _ =>
        {
            return sp.GetRequiredService(masterClassType) as SpaceModule;
        });

        return masterClass.GetModule<TRequest, TResponse>(profileName);
    }

    public ValueTask<TResponse> Invoke<TRequest, TResponse>(string moduleName, string profileName, PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next)
    {
        var module = GetModule<TRequest, TResponse>(moduleName, profileName);

        return module is not null
                ? module.HandlePipeline(ctx, next)
                : next(ctx);
    }
}