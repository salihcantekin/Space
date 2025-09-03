using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that checks for Notification method signature and return type in classes implementing INotificationHandler<TRequest>.
/// </summary>
public class NotificationAttributeRule : IDiagnosticRule
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

                var notificationHandlers = classSymbol.Interfaces
                    .Where(i => i.Name == SourceGenConstants.Contracts.INotificationHandler && i.TypeArguments.Length == 1);

                foreach (var notificationHandler in notificationHandlers)
                {
                    foreach (var methodNode in classNode.DescendantNodes().OfType<MethodDeclarationSyntax>())
                    {
                        if (semanticModel.GetDeclaredSymbol(methodNode) is not IMethodSymbol methodSymbol)
                            continue;

                        if (methodSymbol.Name != SourceGenConstants.Contracts.NotificationMethodName)
                            continue;

                        // Rule 1: Must have exactly one parameter
                        if (methodSymbol.Parameters.Length != 1)
                        {
                            ReportDiagnostic(context, methodNode, "NOTIFICATION010", "Method with NotificationAttribute must have exactly one parameter of type NotificationContext<TRequest>.");
                            hasErrors = true;
                            continue;
                        }

                        // Rule 2: Parameter must be NotificationContext<TRequest> or TRequest
                        var paramType = methodSymbol.Parameters[0].Type;
                        var requestType = paramType is INamedTypeSymbol namedParamType &&
                            namedParamType.Name == SourceGenConstants.Context.NotificationName &&
                            namedParamType.TypeArguments.Length == 1
                            ? namedParamType.TypeArguments[0]
                            : paramType;

                        if (!SymbolEqualityComparer.Default.Equals(requestType, notificationHandler.TypeArguments[0]))
                        {
                            ReportDiagnostic(context, methodNode, "NOTIFICATION011", "Parameter type does not match INotificationHandler<TRequest>.");
                            hasErrors = true;
                            continue;
                        }

                        // Rule 3: Return type must be Task or ValueTask
                        if (methodSymbol.ReturnType is not INamedTypeSymbol returnType || (returnType.Name != "Task" && returnType.Name != "ValueTask"))
                        {
                            ReportDiagnostic(context, methodNode, "NOTIFICATION012", "Return type must be Task or ValueTask.");
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
            new DiagnosticDescriptor(id, "Invalid Notification Method Signature", message, "Usage", DiagnosticSeverity.Error, true),
            methodNode.GetLocation());
        context.ReportDiagnostic(diagnostic);
    }
}
