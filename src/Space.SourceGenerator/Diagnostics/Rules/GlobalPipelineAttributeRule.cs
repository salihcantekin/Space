using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that validates GlobalPipeline method signatures.
/// If IGlobalPipeline interface is not implemented, validates that methods with GlobalPipelineAttribute
/// have the correct signature.
/// </summary>
public class GlobalPipelineAttributeRule : IDiagnosticRule
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

                // Check for methods with GlobalPipelineAttribute
                foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                {
                    if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                        continue;

                    var hasGlobalPipelineAttr = methodSymbol.GetAttributes()
                        .Any(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.GlobalPipelineAttributeFullName);

                    if (!hasGlobalPipelineAttr)
                        continue;

                    // Check if class implements IGlobalPipeline<TRequest, TResponse>
                    var implementsInterface = classSymbol.AllInterfaces
                        .Any(i => i.Name == SourceGenConstants.Contracts.IGlobalPipeline && i.TypeArguments.Length == 2);

                    if (implementsInterface)
                    {
                        // Interface is implemented, let the compiler enforce the signature
                        // We still validate some rules that the compiler won't catch
                        ValidateInterfaceImplementation(context, methodNode, methodSymbol, classSymbol, ref hasErrors);
                    }
                    else
                    {
                        // Interface not implemented, validate signature manually
                        ValidateMethodSignatureWithoutInterface(context, methodNode, methodSymbol, ref hasErrors);
                    }
                }
            }
        }
        return hasErrors;
    }

    private void ValidateInterfaceImplementation(SourceProductionContext context, MethodDeclarationSyntax methodNode,
        IMethodSymbol methodSymbol, INamedTypeSymbol classSymbol, ref bool hasErrors)
    {
        // Find the IGlobalPipeline interface implementation
        var globalPipelineInterface = classSymbol.AllInterfaces
            .FirstOrDefault(i => i.Name == SourceGenConstants.Contracts.IGlobalPipeline && i.TypeArguments.Length == 2);

        if (globalPipelineInterface == null)
            return;

        var requestType = globalPipelineInterface.TypeArguments[0];
        var responseType = globalPipelineInterface.TypeArguments[1];

        // Rule 1: Must have exactly two parameters
        if (methodSymbol.Parameters.Length != 2)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE010",
                "Method with GlobalPipelineAttribute must have exactly two parameters.");
            hasErrors = true;
            return;
        }

        // Rule 2: First parameter must be PipelineContext<TRequest>
        var paramType = methodSymbol.Parameters[0].Type;
        if (paramType is not INamedTypeSymbol namedParamType ||
            namedParamType.Name != SourceGenConstants.Context.PipelineName ||
            namedParamType.TypeArguments.Length != 1 ||
            !SymbolEqualityComparer.Default.Equals(namedParamType.TypeArguments[0], requestType))
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE011",
                $"First parameter must be PipelineContext<{requestType.Name}> to match IGlobalPipeline<{requestType.Name}, {responseType.Name}>.");
            hasErrors = true;
        }

        // Rule 3: Return type must be Task<TResponse> or ValueTask<TResponse>
        if (methodSymbol.ReturnType is not INamedTypeSymbol returnType ||
            (returnType.Name != "Task" && returnType.Name != "ValueTask") ||
            returnType.TypeArguments.Length != 1)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE012",
                "Return type must be Task<TResponse> or ValueTask<TResponse>.");
            hasErrors = true;
            return;
        }

        // Rule 4: Return type argument must match IGlobalPipeline<TRequest, TResponse>.TResponse
        if (!SymbolEqualityComparer.Default.Equals(returnType.TypeArguments[0], responseType))
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE013",
                $"Return type must be {returnType.Name}<{responseType.Name}> to match IGlobalPipeline<{requestType.Name}, {responseType.Name}>.");
            hasErrors = true;
        }
    }

    private void ValidateMethodSignatureWithoutInterface(SourceProductionContext context, MethodDeclarationSyntax methodNode,
        IMethodSymbol methodSymbol, ref bool hasErrors)
    {
        // Rule 1: Must have exactly two parameters
        if (methodSymbol.Parameters.Length != 2)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE020",
                "Method with GlobalPipelineAttribute must have exactly two parameters: PipelineContext<TRequest> and PipelineDelegate<TRequest, TResponse>.");
            hasErrors = true;
            return;
        }

        // Rule 2: First parameter must be PipelineContext<T>
        var firstParam = methodSymbol.Parameters[0].Type;
        if (firstParam is not INamedTypeSymbol namedFirstParam ||
            namedFirstParam.Name != SourceGenConstants.Context.PipelineName ||
            namedFirstParam.TypeArguments.Length != 1)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE021",
                "First parameter must be PipelineContext<TRequest>. Consider implementing IGlobalPipeline<TRequest, TResponse> for compile-time type safety.");
            hasErrors = true;
        }

        // Rule 3: Second parameter must be PipelineDelegate<TRequest, TResponse>
        var secondParam = methodSymbol.Parameters[1].Type;
        if (secondParam is not INamedTypeSymbol namedSecondParam ||
            namedSecondParam.Name != "PipelineDelegate" ||
            namedSecondParam.TypeArguments.Length != 2)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE022",
                "Second parameter must be PipelineDelegate<TRequest, TResponse>. Consider implementing IGlobalPipeline<TRequest, TResponse> for compile-time type safety.");
            hasErrors = true;
        }

        // Rule 4: Return type must be Task<TResponse> or ValueTask<TResponse>
        if (methodSymbol.ReturnType is not INamedTypeSymbol returnType ||
            (returnType.Name != "Task" && returnType.Name != "ValueTask") ||
            returnType.TypeArguments.Length != 1)
        {
            ReportDiagnostic(context, methodNode, "GLOBALPIPELINE023",
                "Return type must be Task<TResponse> or ValueTask<TResponse>. Consider implementing IGlobalPipeline<TRequest, TResponse> for compile-time type safety.");
            hasErrors = true;
        }

        // Rule 5: If we can extract types from parameters and return, validate consistency
        if (firstParam is INamedTypeSymbol fp && fp.TypeArguments.Length == 1 &&
            secondParam is INamedTypeSymbol sp && sp.TypeArguments.Length == 2 &&
            methodSymbol.ReturnType is INamedTypeSymbol rt && rt.TypeArguments.Length == 1)
        {
            var requestFromContext = fp.TypeArguments[0];
            var requestFromDelegate = sp.TypeArguments[0];
            var responseFromDelegate = sp.TypeArguments[1];
            var responseFromReturn = rt.TypeArguments[0];

            if (!SymbolEqualityComparer.Default.Equals(requestFromContext, requestFromDelegate))
            {
                ReportDiagnostic(context, methodNode, "GLOBALPIPELINE024",
                    "TRequest type mismatch between PipelineContext<TRequest> and PipelineDelegate<TRequest, TResponse>.");
                hasErrors = true;
            }

            if (!SymbolEqualityComparer.Default.Equals(responseFromDelegate, responseFromReturn))
            {
                ReportDiagnostic(context, methodNode, "GLOBALPIPELINE025",
                    "TResponse type mismatch between PipelineDelegate<TRequest, TResponse> and return type.");
                hasErrors = true;
            }
        }
    }

    private void ReportDiagnostic(SourceProductionContext context, MethodDeclarationSyntax methodNode, string id, string message)
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(id, "Invalid GlobalPipeline Method Signature", message, "Usage", DiagnosticSeverity.Error, true),
            methodNode.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
