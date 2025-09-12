using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Space.SourceGenerator.Extensions;

public static class MethodSymbolExtensions
{
    internal static AttributeData GetMethodAttribute(this IMethodSymbol methodSymbol, string attributeName)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == attributeName)
            {
                return attribute;
            }
        }

        return null;
    }

    internal static AttributeData GetHandlerAttribute(this IMethodSymbol methodSymbol)
    {
        var attributeName = SourceGenConstants.HandleAttributeFullName; // typeof(HandleAttribute).FullName

        return methodSymbol.GetMethodAttribute(attributeName);
    }

    internal static AttributeData GetNotificationAttribute(this IMethodSymbol methodSymbol)
    {
        var attributeName = SourceGenConstants.NotificationAttributeFullName; // typeof(NotificationAttribute).FullName

        return methodSymbol.GetMethodAttribute(attributeName);
    }

    internal static AttributeData GetPipelineAttribute(this IMethodSymbol methodSymbol)
    {
        var attributeName = SourceGenConstants.PipelineAttributeFullName; // typeof(PipelineAttribute).FullName

        return methodSymbol.GetMethodAttribute(attributeName);
    }

    public static string GetAttributeArgument(this AttributeData attribute, string argumentName)
    {
        if (attribute is not { NamedArguments.Length: > 0 } || string.IsNullOrEmpty(argumentName))
        {
            return string.Empty;
        }

        var val = attribute.NamedArguments
                    .FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Key, argumentName))
                    .Value.Value?.ToString() ?? string.Empty;

        return val;
    }

    public static string GetResponseGenericTypeName(this IMethodSymbol methodSymbol, bool fullName = true)
    {
        if (methodSymbol.ReturnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
        {
            return fullName
                    ? namedType.TypeArguments.First().ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                    : namedType.TypeArguments.First().Name;
        }

        return "Nothing";
    }

    public static string GetResponseTaskName(this IMethodSymbol methodSymbol)
    {
        var taskName = SourceGenConstants.Type.ValueTask;

        if (methodSymbol.ReturnType is INamedTypeSymbol namedType)
        {
            taskName = namedType.Name;
        }

        return taskName;
    }

    public static Dictionary<string, object> GetProperties(this AttributeData attr)
    {
        try
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (attr == null) 
                return dict;

            // Named arguments first
            foreach (var named in attr.NamedArguments)
            {
                dict[named.Key] = ConvertTypedConstant(named.Value) ?? string.Empty;
            }

            // Positional (constructor) arguments
            var ctor = attr.AttributeConstructor;
            if (ctor != null)
            {
                var ctorArgs = attr.ConstructorArguments;
                var properties = attr.AttributeClass?.GetMembers().OfType<IPropertySymbol>().ToList() ?? [];
                var usedParamIndexes = new HashSet<int>();

                // Raw parameter names
                for (int i = 0; i < ctor.Parameters.Length && i < ctorArgs.Length; i++)
                {
                    var param = ctor.Parameters[i];
                    var value = ConvertTypedConstant(ctorArgs[i]);
                    dict[param.Name] = value ?? string.Empty;
                }

                // Type-based mapping to property names (unambiguous only)
                foreach (var prop in properties)
                {
                    if (dict.ContainsKey(prop.Name))
                        continue; // already set by named args

                    var candidates = new List<int>();
                    for (int i = 0; i < ctor.Parameters.Length && i < ctorArgs.Length; i++)
                    {
                        if (usedParamIndexes.Contains(i)) 
                            continue;

                        var p = ctor.Parameters[i];

                        if (SymbolEqualityComparer.Default.Equals(p.Type, prop.Type) ||
                            string.Equals(p.Type?.Name, prop.Type?.Name, StringComparison.Ordinal))
                        {
                            candidates.Add(i);
                        }
                    }

                    if (candidates.Count == 1)
                    {
                        var idx = candidates[0];
                        dict[prop.Name] = ConvertTypedConstant(ctorArgs[idx]) ?? string.Empty;
                        usedParamIndexes.Add(idx);
                    }
                }
            }

            return dict;
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Returns value of an attribute argument by logical property name.
    /// Order: named args -> positional ctor arg selected by type match (unambiguous).
    /// </summary>
    public static object GetAttributePropertyValue(this AttributeData attribute, string propertyName)
    {
        if (attribute == null || string.IsNullOrEmpty(propertyName))
        {
            return null;
        }

        // 1) Named arguments
        foreach (var named in attribute.NamedArguments)
        {
            if (string.Equals(named.Key, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return ConvertTypedConstant(named.Value);
            }
        }

        // 2) Positional by matching property type (only if unambiguous)
        var propSymbol = attribute.AttributeClass?.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        var ctor = attribute.AttributeConstructor;
        var ctorArgs = attribute.ConstructorArguments;

        if (propSymbol != null && ctor != null)
        {
            var candidateIndexes = new List<int>();
            for (int i = 0; i < ctor.Parameters.Length && i < ctorArgs.Length; i++)
            {
                var p = ctor.Parameters[i];
                if (SymbolEqualityComparer.Default.Equals(p.Type, propSymbol.Type) ||
                    string.Equals(p.Type?.Name, propSymbol.Type?.Name, StringComparison.Ordinal))
                {
                    candidateIndexes.Add(i);
                }
            }

            if (candidateIndexes.Count == 1)
            {
                return ConvertTypedConstant(ctorArgs[candidateIndexes[0]]);
            }
        }

        return null;
    }

    private static object ConvertTypedConstant(TypedConstant tc)
    {
        if (tc.IsNull)
            return null;

        if (tc.Kind == TypedConstantKind.Array)
        {
            return tc.Values.Select(ConvertTypedConstant).ToArray();
        }

        // Enum constants -> return fully-qualified member name if resolvable, else raw value
        if (tc.Type is INamedTypeSymbol tcType && tcType.TypeKind == TypeKind.Enum)
        {
            var val = tc.Value;
            if (val != null)
            {
                var field = tcType.GetMembers()
                                   .OfType<IFieldSymbol>()
                                   .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, val));

                if (field != null)
                {
                    return tcType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + "." + field.Name;
                }
            }

            return val; // fallback underlying numeric
        }

        // Type symbols
        if (tc.Value is ITypeSymbol ts)
        {
            return ts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        return tc.Value;
    }
}
