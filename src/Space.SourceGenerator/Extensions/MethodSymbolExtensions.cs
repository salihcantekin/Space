using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

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
            var dict = new Dictionary<string, object>();

            foreach (var named in attr.NamedArguments)
            {
                dict[named.Key] = GetTypedConstantValue(named.Value);
            }

            return dict;
        }
        catch
        {
            return [];
        }
    }

    private static object GetTypedConstantValue(TypedConstant typedConstant)
    {
        if (typedConstant.Kind == TypedConstantKind.Array)
        {
            // Handle array properties
            var values = new object[typedConstant.Values.Length];
            for (int i = 0; i < typedConstant.Values.Length; i++)
            {
                values[i] = GetTypedConstantValue(typedConstant.Values[i]);
            }
            return values;
        }
        else
        {
            // Handle single values (existing logic)
            return typedConstant.Value ?? string.Empty;
        }
    }

    /// <summary>
    /// Takes AttributeData and a property name, and returns the property's value (supports named, ctor, and static/readonly property).
    /// </summary>
    public static object GetAttributePropertyValue(this AttributeData attribute, string propertyName)
    {
        if (attribute == null || string.IsNullOrEmpty(propertyName))
        {
            return null;
        }

        // 1. Named arguments
        var named = attribute.NamedArguments.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a.Key, propertyName));

        if (named.Value.Value != null)
        {
            return named.Value.Value;
        }

        // 2. Constructor arguments
        var property = attribute.AttributeClass?.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name, propertyName));

        if (property != null)
        {
            foreach (var ctor in attribute.AttributeClass.Constructors)
            {
                for (int i = 0; i < ctor.Parameters.Length && i < attribute.ConstructorArguments.Length; i++)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(ctor.Parameters[i].Name, propertyName))
                    {
                        var ctorArg = attribute.ConstructorArguments[i];

                        if (ctorArg.Value != null)
                        {
                            return ctorArg.Value;
                        }
                    }
                }
            }

            // 3. Static property
            if (property.IsStatic && property.GetMethod != null)
            {
                return property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        return null;
    }
}
