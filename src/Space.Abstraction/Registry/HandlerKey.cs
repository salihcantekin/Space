using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Space.Abstraction.Registry;

/// <summary>
/// Pre-computed handler key for fast dictionary lookup.
/// Avoids tuple allocation and provides consistent hash computation.
/// </summary>
public readonly struct HandlerKey : IEquatable<HandlerKey>
{
    public readonly int Hash;
    public readonly Type RequestType;
    public readonly Type ResponseType;
    public readonly string Name;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public HandlerKey(Type requestType, Type responseType, string name = null)
    {
        RequestType = requestType;
        ResponseType = responseType;
        Name = name ?? string.Empty;
        Hash = ComputeHash(requestType, responseType, Name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHash(Type requestType, Type responseType, string name)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (requestType?.GetHashCode() ?? 0);
            hash = hash * 31 + (responseType?.GetHashCode() ?? 0);
            hash = hash * 31 + (name?.GetHashCode() ?? 0);
            return hash;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HandlerKey other)
    {
        return Hash == other.Hash &&
               RequestType == other.RequestType &&
               ResponseType == other.ResponseType &&
               string.Equals(Name, other.Name, StringComparison.Ordinal);
    }

    public override bool Equals(object obj) => obj is HandlerKey other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Hash;

    public static bool operator ==(HandlerKey left, HandlerKey right) => left.Equals(right);
    public static bool operator !=(HandlerKey left, HandlerKey right) => !left.Equals(right);

    public override string ToString() => $"HandlerKey({RequestType?.Name}, {ResponseType?.Name}, '{Name}')";
}

/// <summary>
/// Comparer for HandlerKey to use with Dictionary.
/// </summary>
public sealed class HandlerKeyComparer : IEqualityComparer<HandlerKey>
{
    public static readonly HandlerKeyComparer Instance = new();

    private HandlerKeyComparer() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(HandlerKey x, HandlerKey y) => x.Equals(y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(HandlerKey obj) => obj.Hash;
}

/// <summary>
/// Simple type pair key for handler lookup by types only (no name).
/// </summary>
public readonly struct TypePairKey : IEquatable<TypePairKey>
{
    public readonly int Hash;
    public readonly Type RequestType;
    public readonly Type ResponseType;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TypePairKey(Type requestType, Type responseType)
    {
        RequestType = requestType;
        ResponseType = responseType;
        unchecked
        {
            Hash = (requestType?.GetHashCode() ?? 0) * 31 + (responseType?.GetHashCode() ?? 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TypePairKey other)
    {
        return Hash == other.Hash &&
               RequestType == other.RequestType &&
               ResponseType == other.ResponseType;
    }

    public override bool Equals(object obj) => obj is TypePairKey other && Equals(other);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Hash;

    public static bool operator ==(TypePairKey left, TypePairKey right) => left.Equals(right);
    public static bool operator !=(TypePairKey left, TypePairKey right) => !left.Equals(right);
}

/// <summary>
/// Comparer for TypePairKey to use with Dictionary.
/// </summary>
public sealed class TypePairKeyComparer : IEqualityComparer<TypePairKey>
{
    public static readonly TypePairKeyComparer Instance = new();

    private TypePairKeyComparer() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(TypePairKey x, TypePairKey y) => x.Equals(y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(TypePairKey obj) => obj.Hash;
}

/// <summary>
/// Static cache for pre-computed handler keys per generic type combination.
/// </summary>
public static class HandlerKeyCache<TRequest, TResponse>
{
    public static readonly HandlerKey Unnamed = new(typeof(TRequest), typeof(TResponse), string.Empty);
    public static readonly TypePairKey TypePair = new(typeof(TRequest), typeof(TResponse));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HandlerKey WithName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Unnamed;
        return new HandlerKey(typeof(TRequest), typeof(TResponse), name);
    }
}
