using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Space.Abstraction.Modules; // for ModuleConstants

namespace Space.Abstraction;

public readonly struct Nothing
{
    public static Task<Nothing> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<Nothing> AsTask() => Task;

    public static ValueTask<Nothing> ValueTask => new(Value);

    public static Nothing Value { get; } = new Nothing();
}

public readonly struct ReqResIdentifier(Type requestType, Type responseType)
{
    public Type RequestType { get; } = requestType;
    public Type ResponseType { get; } = responseType;

    public static ReqResIdentifier From<TRequest, TResponse>()
    {
        return new ReqResIdentifier(typeof(TRequest), typeof(TResponse));
    }
}

public readonly struct HandleIdentifier(string name, Type requestType, Type responseType)
{
    public string Name { get; } = name;
    public Type RequestType { get; } = requestType;
    public Type ResponseType { get; } = responseType;

    public static HandleIdentifier From<TRequest, TResponse>(string name = null)
    {
        return new HandleIdentifier(name ?? "", typeof(TRequest), typeof(TResponse));
    }
}

public readonly struct ModuleIdentifier(HandleIdentifier handleIdentifier, string profileName)
{
    public HandleIdentifier HandleIdentifier { get; } = handleIdentifier;
    public string ProfileName { get; } = profileName;

    public static ModuleIdentifier From<TRequest, TResponse>(string moduleName, string profileName)
    {
        var handleIdentifier = HandleIdentifier.From<TRequest, TResponse>(moduleName);
        return From(handleIdentifier, profileName);
    }

    public static ModuleIdentifier From(HandleIdentifier handleIdentifier, string profileName)
    {
        return new ModuleIdentifier(handleIdentifier, string.IsNullOrEmpty(profileName) ? ModuleConstants.DefaultProfileName : profileName);
    }
}