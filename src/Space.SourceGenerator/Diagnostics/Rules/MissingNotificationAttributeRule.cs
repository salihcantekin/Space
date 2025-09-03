using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Space.SourceGenerator.Compile;
using System.Linq;

namespace Space.SourceGenerator.Diagnostics.Rules;

/// <summary>
/// Diagnostic rule that checks for missing NotificationAttribute on methods named 'HandleNotification' in classes implementing INotificationHandler<TRequest>.
/// </summary>
public class MissingNotificationAttributeRule : IDiagnosticRule
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

                        // Attribute check
                        var hasNotificationAttribute = methodSymbol.GetAttributes()
                            .Any(attr => attr.AttributeClass?.ToDisplayString() == SourceGenConstants.NotificationAttributeFullName);

                        if (!hasNotificationAttribute)
                        {
                            var diagnostic = Diagnostic.Create(
                                new DiagnosticDescriptor(
                                    "NOTIFICATION001",
                                    "Missing NotificationAttribute",
                                    $"Method '{methodSymbol.Name}' must have {SourceGenConstants.NotificationAttributeFullName}",
                                    "Usage",
                                    DiagnosticSeverity.Error,
                                    true),
                                methodNode.GetLocation()
                            );
                            context.ReportDiagnostic(diagnostic);
                            hasErrors = true;
                        }
                    }
                }
            }
        }
        return hasErrors;
    }
}
