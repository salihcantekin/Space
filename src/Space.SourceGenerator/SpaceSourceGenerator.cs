using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Diagnostics;
using Space.SourceGenerator.Generators;
using Space.SourceGenerator.Scanning;
using System.Linq;
using static Space.SourceGenerator.Scanning.ModuleScanners;


namespace Space.SourceGenerator;

[Generator]
public sealed class SpaceSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        // Find all methods with Handle attribute
        var handlerMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceGenConstants.HandleAttributeFullName,
                (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax,
                (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)ctx.TargetNode)
            )
            .Where(symbol => symbol is not null);

        // Find all methods with Pipeline attribute
        var pipelineMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceGenConstants.PipelineAttributeFullName,
                (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax,
                (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)ctx.TargetNode)
            )
            .Where(symbol => symbol is not null);

        // Find all methods with Notification attribute
        var notificationMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                SourceGenConstants.NotificationAttributeFullName,
                (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax,
                (ctx, _) => ctx.SemanticModel.GetDeclaredSymbol((Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)ctx.TargetNode)
            )
            .Where(symbol => symbol is not null);


        var modules = GetModuleProvider(context);

        // Combine all models into a single HandlersCompileWrapperModel
        var allModels = handlerMethods.Collect()
            .Combine(pipelineMethods.Collect())
            .Combine(notificationMethods.Collect())
            .Combine(modules.Collect())
            .Select((tuple, _) =>
            {
                //var ((handlers, pipelines), notifications) = tuple;
                var (((handlers, pipelines), notifications), modules) = tuple;
                var model = new HandlersCompileWrapperModel();

                foreach (var handlerSymbol in handlers.OfType<IMethodSymbol>())
                    model.AddRangeHandlerAttribute(HandlerScanner.ScanHandlers(handlerSymbol.ContainingType,
                                                                               handlerSymbol));

                ModuleScanningCompileModelContainer container = new();
                foreach (var moduleSymbol in modules.OfType<IMethodSymbol>())
                {
                    var moduleModels = ScanModules(moduleSymbol);


                    container.Models.AddRange(moduleModels);

                    model.AddRangeModuleAttribute(moduleModels);
                }

                foreach (var pipelineSymbol in pipelines.OfType<IMethodSymbol>())
                    model.AddRangePipelineAttribute(PipelineScanner.ScanPipelines(pipelineSymbol.ContainingType,
                                                                                  pipelineSymbol));

                foreach (var notificationSymbol in notifications.OfType<IMethodSymbol>())
                    model.AddRangeNotificationAttribute(NotificationScanner.ScanHandlers(notificationSymbol.ContainingType,
                                                                                         notificationSymbol));

                return model.Build();
            });

        var output = allModels.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(output, (spc, tuple) =>
        {
            var (model, compilation) = tuple;
            bool hasErrors = DiagnosticGenerator.ReportDiagnostics(spc, compilation, model);

            if (hasErrors)
                return;

            DependencyInjectionGenerator.Generate(spc, model);
            ModuleGenerator.Generate(spc, model);
        });
    }
}
