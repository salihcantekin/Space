using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.IO; // Needed for Directory
using Space.Abstraction.Exceptions;

namespace Space.DependencyInjection;

public static class ServiceCollectionExtensions
{
    private const string GeneratedMainType = "Space.DependencyInjection.SourceGeneratorDependencyInjectionExtensions";
    private const string GeneratedMainMethod = "AddSpaceSourceGenerated";
    private const string GeneratedModulesType = "Space.DependencyInjection.SourceGeneratorModuleGenerationExtensions";
    private const string GeneratedModulesMethod = "AddSpaceModules";

    private static bool _assembliesLoaded;

    public static IServiceCollection AddSpace(this IServiceCollection services, Action<SpaceOptions> configure = null)
    {
        EnsureSpaceAssembliesLoaded();

        var spaceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name.StartsWith("Space", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        if (spaceAssemblies.Length == 0)
        {
            throw new SpaceAssemblyLoadException("No Space.* assemblies loaded. Ensure packages are referenced before calling AddSpace().");
        }

        var allTypes = spaceAssemblies.SelectMany(SafeGetTypes).ToArray();

        ScanModuleMasterClasses(services, allTypes);
        // Invoke generated main DI extension
        InvokeGeneratedExtension(services, configure, allTypes, GeneratedMainType, GeneratedMainMethod, allowConfigure: true);
        // Invoke generated module registration extension
        InvokeGeneratedExtension(services, null, allTypes, GeneratedModulesType, GeneratedModulesMethod, allowConfigure: false);

        return services;
    }

    #region Generated Extension Invocation
    private static void InvokeGeneratedExtension(IServiceCollection services,
                                                  Action<SpaceOptions> configure,
                                                  IEnumerable<Type> collectedTypes,
                                                  string generatedTypeName,
                                                  string methodName,
                                                  bool allowConfigure)
    {
        // 1. Direct attempt: look for known generated type in loaded assemblies
        var generatedType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(generatedTypeName, throwOnError: false, ignoreCase: false))
            .FirstOrDefault(t => t is not null);

        MethodInfo method = null;

        if (generatedType is not null)
        {
            method = SelectBestMethod(generatedType, methodName, allowConfigure);
        }

        // 2. Fallback: reflection scan across previously collected Space types
        method ??= collectedTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == methodName)
            .OrderByDescending(m => m.GetParameters().Length)
            .FirstOrDefault(m =>
                m.GetParameters().Length > 0 &&
                typeof(IServiceCollection).IsAssignableFrom(m.GetParameters()[0].ParameterType));

        if (method is null)
        {
            throw new SpaceGeneratedMethodNotFoundException(methodName, generatedTypeName);
        }

        var parameters = method.GetParameters();

        object[] callArgs = parameters.Length switch
        {
            1 => new object[] { services },
            2 when allowConfigure => new object[] { services, configure },
            _ => new object[] { services }
        };

        method.Invoke(null, callArgs);
    }

    private static MethodInfo SelectBestMethod(Type type, string name, bool allowConfigure) => type
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Where(m => m.Name == name)
        .OrderByDescending(m => m.GetParameters().Length)
        .FirstOrDefault(m =>
        {
            var ps = m.GetParameters();

            if (ps.Length == 0)
            {
                return false;
            }

            if (!typeof(IServiceCollection).IsAssignableFrom(ps[0].ParameterType))
            {
                return false;
            }

            if (ps.Length == 2 && !allowConfigure)
            {
                // Ignore overload with configure if not allowed
                return false;
            }

            return true;
        });
    #endregion

    #region Assembly Loading
    private static void EnsureSpaceAssembliesLoaded()
    {
        if (_assembliesLoaded)
        {
            return;
        }

        LoadSpaceAssemblies();
        _assembliesLoaded = true;
    }

    private static void LoadSpaceAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        bool IsLoaded(string name) => loaded.Any(a => a.GetName().Name.Equals(name, StringComparison.Ordinal));

        // Load referenced Space.* assemblies
        var referenced = loaded
            .SelectMany(a => a.GetReferencedAssemblies())
            .Where(an => an.Name.StartsWith("Space", StringComparison.Ordinal))
            .DistinctBy(an => an.Name);

        foreach (var an in referenced)
        {
            if (!IsLoaded(an.Name))
            {
                TryLoad(() => Assembly.Load(an));
            }
        }

        // Load Space*.dll from base directory
        var baseDir = AppContext.BaseDirectory;

        if (Directory.Exists(baseDir))
        {
            foreach (var path in Directory.EnumerateFiles(baseDir, "Space*.dll", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var asmName = AssemblyName.GetAssemblyName(path);

                    if (!IsLoaded(asmName.Name))
                    {
                        TryLoad(() => Assembly.Load(asmName));
                    }
                }
                catch
                {
                    /* ignore */
                }
            }
        }
    }
    #endregion

    #region Utilities
    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
        catch
        {
            return Array.Empty<Type>();
        }
    }

    private static void TryLoad(Action load)
    {
        try
        {
            load();
        }
        catch
        {
        }
    }
    #endregion

    #region Module Discovery
    private static void ScanModuleMasterClasses(IServiceCollection services, IEnumerable<Type> types)
    {
        var moduleTypes = types
            .Where(t => t.IsClass && !t.IsAbstract && typeof(SpaceModule).IsAssignableFrom(t));

        foreach (var moduleType in moduleTypes)
        {
            if (!Attribute.IsDefined(moduleType, typeof(SpaceModuleAttribute)))
            {
                continue;
            }

            var attribute = moduleType.GetCustomAttribute<SpaceModuleAttribute>();

            if (attribute?.IsEnabled != true)
            {
                continue;
            }

            var moduleAttributeName = attribute.ModuleAttributeType.Name;
            services.AddKeyedSingleton(serviceKey: moduleAttributeName, implementationInstance: moduleType);
            services.AddSingleton(moduleType);
        }
    }
    #endregion
}
