using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that checks for Pipeline method signature and return type in classes implementing IPipelineHandler<TRequest, TResponse>.
/// </summary>
public class PipelineAttributeRule : IDiagnosticRule
{
    public bool Analyze(SourceProductionContext context, Compilation compilation, HandlersCompileWrapperModel _)
    {
        bool hasErrors = false;
        foreach (var tree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(tree);
            var classNodes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classNodes)
            {
                if (semanticModel.GetDeclaredSymbol(classNode) is not INamedTypeSymbol classSymbol)
                    continue;

                var iPipelineAttributes = classSymbol.Interfaces
                    .Where(i => i.Name == SourceGenConstants.Contracts.IPipelineHandler && i.TypeArguments.Length == 2);

                foreach (var iPipelineAttribute in iPipelineAttributes)
                {
                    foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                            continue;

                        if (methodSymbol.Name != SourceGenConstants.Contracts.PipelineMethodName)
                            continue;

                        // Rule 1: Must have exactly two parameters
                        if (methodSymbol.Parameters.Length != 2)
                        {
                            ReportDiagnostic(context, methodNode, "PIPELINE010", "Method with PipelineAttribute must have exactly two parameters.");
                            hasErrors = true;
                            continue;
                        }

                        // Rule 2: First parameter must be PipelineContext<TRequest> or TRequest
                        var paramType = methodSymbol.Parameters[0].Type;
                        var requestType = paramType is INamedTypeSymbol namedParamType &&
                            namedParamType.Name == SourceGenConstants.Context.PipelineName &&
                            namedParamType.TypeArguments.Length == 1
                            ? namedParamType.TypeArguments[0]
                            : paramType;

                        if (!SymbolEqualityComparer.Default.Equals(requestType, iPipelineAttribute.TypeArguments[0]))
                        {
                            ReportDiagnostic(context, methodNode, "PIPELINE011", "First parameter type does not match IPipelineHandler<TRequest, TResponse>.");
                            hasErrors = true;
                            continue;
                        }

                        // Rule 3: Return type must be Task<TResponse> or ValueTask<TResponse>
                        if (methodSymbol.ReturnType is not INamedTypeSymbol returnType || (returnType.Name != "Task" && returnType.Name != "ValueTask") || returnType.TypeArguments.Length != 1)
                        {
                            ReportDiagnostic(context, methodNode, "PIPELINE012", "Return type must be Task<TResponse> or ValueTask<TResponse>.");
                            hasErrors = true;
                            continue;
                        }

                        // Rule 4: Return type argument must match IPipelineHandler<TRequest, TResponse>.TResponse
                        if (!SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], iPipelineAttribute.TypeArguments[1]))
                        {
                            ReportDiagnostic(context, methodNode, "PIPELINE013", "Return type argument does not match IPipelineHandler<TRequest, TResponse> response type.");
                            hasErrors = true;
                            continue;
                        }
                    }
                }
            }
        }
        return hasErrors;
    }

    private void ReportDiagnostic(SourceProductionContext context, MethodDeclarationSyntax methodNode, string id, string message)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(id, "Invalid Pipeline Method Signature", message, "Usage", DiagnosticSeverity.Error, true),
            methodNode.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
