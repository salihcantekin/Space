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

    private static bool assembliesLoaded;
    private static bool userAssembliesPreloaded; // new flag

    // Existing overload (kept for backward compatibility)
    public static IServiceCollection AddSpace(this IServiceCollection services, Action<SpaceOptions> configure = null)
        => AddSpaceInternal(services, configure, null);

    // New overload: only extra assemblies
    public static IServiceCollection AddSpace(this IServiceCollection services, params Assembly[] additionalAssemblies)
        => AddSpaceInternal(services, null, additionalAssemblies);

    // New overload: configure + extra assemblies
    public static IServiceCollection AddSpace(this IServiceCollection services, Action<SpaceOptions> configure, params Assembly[] additionalAssemblies)
        => AddSpaceInternal(services, configure, additionalAssemblies);

    private static IServiceCollection AddSpaceInternal(IServiceCollection services, Action<SpaceOptions> configure, Assembly[] additionalAssemblies)
    {
        EnsureSpaceAssembliesLoaded();
        PreloadUserAssemblies(); // ensure user (non Space.*) assemblies are loaded so root aggregator can discover registration classes

        // Original Space.* assemblies (core framework)
        var spaceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name.StartsWith("Space", StringComparison.Ordinal))
            .Distinct()
            .ToList();

        // Include explicitly provided assemblies
        if (additionalAssemblies != null && additionalAssemblies.Length > 0)
        {
            foreach (var asm in additionalAssemblies)
            {
                if (asm != null && !spaceAssemblies.Contains(asm))
                {
                    spaceAssemblies.Add(asm);
                }
            }
        }

        // Discover user assemblies that contain generated registration helpers
        var allLoaded = AppDomain.CurrentDomain.GetAssemblies();
        var generatedHelperAssemblies = allLoaded
            .Where(a =>
            {
                var name = a.GetName().Name;
                if (name.StartsWith("Space", StringComparison.Ordinal))
                    return false;

                foreach (var t in SafeGetTypes(a))
                {
                    if (t == null || !t.IsClass) continue;
                    if (t.Name.StartsWith("SpaceAssemblyRegistration_", StringComparison.Ordinal)) return true;
                    if (t.FullName == GeneratedMainType || t.FullName == GeneratedModulesType) return true;
                }
                return false;
            })
            .ToList();

        foreach (var ga in generatedHelperAssemblies)
        {
            if (!spaceAssemblies.Contains(ga))
                spaceAssemblies.Add(ga);
        }

        if (spaceAssemblies.Count == 0)
        {
            throw new SpaceAssemblyLoadException("No Space.* assemblies (or user assemblies with Space generated code) loaded. Ensure packages/projects are referenced before calling AddSpace().");
        }

        var allTypes = spaceAssemblies.SelectMany(SafeGetTypes).ToArray();

        ScanModuleMasterClasses(services, allTypes);

        // Try to invoke root aggregator if present (optional)
        TryInvokeGeneratedExtension(services, configure, allTypes, GeneratedMainType, GeneratedMainMethod, allowConfigure: true);
        // Try to invoke module generation if present (optional)
        TryInvokeGeneratedExtension(services, null, allTypes, GeneratedModulesType, GeneratedModulesMethod, allowConfigure: false);

        return services;
    }

    #region Generated Extension Invocation
    private static void TryInvokeGeneratedExtension(IServiceCollection services,
                                                    Action<SpaceOptions> configure,
                                                    IEnumerable<Type> collectedTypes,
                                                    string generatedTypeName,
                                                    string methodName,
                                                    bool allowConfigure)
    {
        try
        {
            InvokeGeneratedExtension(services, configure, collectedTypes, generatedTypeName, methodName, allowConfigure, throwIfMissing: false);
        }
        catch
        {
            // Swallow – optional extensions
        }
    }

    private static void InvokeGeneratedExtension(IServiceCollection services,
                                                  Action<SpaceOptions> configure,
                                                  IEnumerable<Type> collectedTypes,
                                                  string generatedTypeName,
                                                  string methodName,
                                                  bool allowConfigure,
                                                  bool throwIfMissing)
    {
        var generatedType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(generatedTypeName, throwOnError: false, ignoreCase: false))
            .FirstOrDefault(t => t is not null);

        MethodInfo method = null;

        if (generatedType is not null)
        {
            method = SelectBestMethod(generatedType, methodName, allowConfigure);
        }

        method ??= collectedTypes
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == methodName)
            .OrderByDescending(m => m.GetParameters().Length)
            .FirstOrDefault(m =>
                m.GetParameters().Length > 0 &&
                typeof(IServiceCollection).IsAssignableFrom(m.GetParameters()[0].ParameterType));

        if (method is null)
        {
            if (throwIfMissing)
            {
                throw new SpaceGeneratedMethodNotFoundException(methodName, generatedTypeName);
            }
            return; // optional – just skip
        }

        var parameters = method.GetParameters();

        object[] callArgs = parameters.Length switch
        {
            1 => [services],
            2 when allowConfigure => [services, configure],
            _ => [services]
        };

        method.Invoke(null, callArgs);
    }

    private static MethodInfo SelectBestMethod(Type type, string name, bool allowConfigure)
    {
        return type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == name)
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault(m =>
                {
                    var ps = m.GetParameters();

                    if (ps.Length == 0) 
                        return false;
                    
                    if (!typeof(IServiceCollection).IsAssignableFrom(ps[0].ParameterType)) 
                        return false;

                    if (ps.Length == 2 && !allowConfigure) 
                        return false;

                    return true;
                });
    }
    #endregion

    #region Assembly Loading
    private static void EnsureSpaceAssembliesLoaded()
    {
        if (assembliesLoaded) return;
        LoadSpaceAssemblies();
        assembliesLoaded = true;
    }

    private static void LoadSpaceAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies();
        bool IsLoaded(string name) => loaded.Any(a => a.GetName().Name.Equals(name, StringComparison.Ordinal));

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
                catch { }
            }
        }
    }

    private static void PreloadUserAssemblies()
    {
        if (userAssembliesPreloaded) 
            return;

        userAssembliesPreloaded = true;

        var entry = Assembly.GetEntryAssembly();
        if (entry == null) 
            return;

        var skipPrefixes = new string[] { "System", "Microsoft", "mscorlib", "netstandard", "Windows" };

        // Some test hosts / design-time contexts can load multiple assemblies with same simple name (different paths/versions)
        // Build dictionary manually and ignore duplicates by simple name to avoid ArgumentException.
        var loaded = new Dictionary<string, Assembly>(StringComparer.Ordinal);
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            var simple = asm.GetName().Name;
            loaded.TryAdd(simple, asm);
        }

        var q = new Queue<AssemblyName>(entry.GetReferencedAssemblies());

        while (q.Count > 0)
        {
            var an = q.Dequeue();
            var name = an.Name;

            if (loaded.ContainsKey(name)) 
                continue;

            if (skipPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal))) 
                continue;

            try
            {
                var asm = Assembly.Load(an);
                var simple = asm.GetName().Name;
                if (!loaded.ContainsKey(simple))
                {
                    loaded[simple] = asm;
                    foreach (var child in asm.GetReferencedAssemblies())
                    {
                        if (!loaded.ContainsKey(child.Name))
                            q.Enqueue(child);
                    }
                }
            }
            catch { }
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
            return []; 
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
            if (!Attribute.IsDefined(moduleType, typeof(SpaceModuleAttribute))) continue;
            var attribute = moduleType.GetCustomAttribute<SpaceModuleAttribute>();
            var moduleAttributeName = attribute.ModuleAttributeType.Name;
            services.AddKeyedSingleton(serviceKey: moduleAttributeName, implementationInstance: moduleType);
            services.AddSingleton(moduleType);
        }
    }
    #endregion
}
