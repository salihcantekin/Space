using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Reports PIPELINE014 when a pipeline returns non-generic Task/ValueTask (void-like)
/// but targets (via HandleName) one or more handlers whose response type is not Nothing.
/// Global (unnamed) pipelines are skipped because they are matched by (Req,Res) and
/// generator already normalizes Nothing handlers; they cannot ambiguously attach to
/// non-Nothing handlers without differing response types.
/// </summary>
public sealed class PipelineVoidConflictRule : IDiagnosticRule
{
    private static readonly DiagnosticDescriptor Rule = new(
        id: "PIPELINE014",
        title: "Invalid void-like pipeline target",
        messageFormat: "Non-generic Task/ValueTask pipeline '{0}' can only target handlers with response 'Space.Abstraction.Nothing'. Found handler returning '{1}'.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel _)
    {
        bool hasErrors = false;

        var nothingSymbol = compilation.GetTypeByMetadataName("Space.Abstraction.Nothing");
        var taskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var valueTaskSymbol = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        if (nothingSymbol is null || taskSymbol is null || valueTaskSymbol is null)
            return false;

        // Build handler map: Handle.Name -> set of response type symbols
        var handlerMap = new Dictionary<string, HashSet<ITypeSymbol>>(StringComparer.Ordinal);

        foreach (var tree in compilation.SyntaxTrees)
        {
            var semantic = compilation.GetSemanticModel(tree);
            foreach (var methodNode in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (semantic.GetDeclaredSymbol(methodNode) is not IMethodSymbol method)
                    continue;

                var handleAttr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SourceGenConstants.HandleAttributeFullName);
                if (handleAttr is null)
                    continue;

                var nameArg = handleAttr.NamedArguments.FirstOrDefault(kv => string.Equals(kv.Key, "Name", StringComparison.OrdinalIgnoreCase)).Value;
                string handleName = nameArg.IsNull ? string.Empty : nameArg.Value?.ToString() ?? string.Empty;

                // Determine effective response type (generic -> T, non-generic -> Nothing)
                ITypeSymbol responseType = nothingSymbol; // default for void-like
                if (method.ReturnType is INamedTypeSymbol rts)
                {
                    if (rts.TypeArguments.Length == 1)
                    {
                        responseType = rts.TypeArguments[0];
                    }
                    else if (rts.TypeArguments.Length == 0 && rts.Name != "Task" && rts.Name != "ValueTask")
                    {
                        // Non async pattern or unsupported
                        continue;
                    }
                }

                if (!handlerMap.TryGetValue(handleName, out var set))
                {
                    set = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
                    handlerMap[handleName] = set;
                }
                set.Add(responseType);
            }
        }

        // Validate pipelines
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semantic = compilation.GetSemanticModel(tree);
            foreach (var methodNode in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (semantic.GetDeclaredSymbol(methodNode) is not IMethodSymbol method)
                    continue;

                var pipelineAttr = method.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == SourceGenConstants.PipelineAttributeFullName);
                if (pipelineAttr is null)
                    continue;

                // Must be non-generic Task/ValueTask
                if (method.ReturnType is not INamedTypeSymbol prt || prt.TypeArguments.Length != 0 || (prt.Name != "Task" && prt.Name != "ValueTask"))
                    continue;

                // Extract HandleName
                string handlerName = null;
                var namedHandle = pipelineAttr.NamedArguments.FirstOrDefault(kv => string.Equals(kv.Key, "HandleName", StringComparison.OrdinalIgnoreCase)).Value;
                if (!namedHandle.IsNull)
                {
                    handlerName = namedHandle.Value?.ToString();
                }
                else if (pipelineAttr.ConstructorArguments.Length > 0 && pipelineAttr.ConstructorArguments[0].Kind == TypedConstantKind.Primitive)
                {
                    handlerName = pipelineAttr.ConstructorArguments[0].Value?.ToString();
                }

                if (string.IsNullOrEmpty(handlerName))
                    continue; // global pipeline: skip

                if (handlerMap.TryGetValue(handlerName, out var responses))
                {
                    foreach (var res in responses)
                    {
                        if (!SymbolEqualityComparer.Default.Equals(res, nothingSymbol))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(Rule, methodNode.GetLocation(), handlerName, res.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                            hasErrors = true;
                        }
                    }
                }
            }
        }

        return hasErrors;
    }
}
