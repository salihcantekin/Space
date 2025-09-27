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
    private const string MultipleRootDiagnosticId = "SPACE_ROOT_MULTIPLE";
    private static readonly DiagnosticDescriptor MultipleRootDescriptor = new(
        id: MultipleRootDiagnosticId,
        title: "Multiple root aggregators detected",
        messageFormat: "Current project and referenced assembly '{0}' both appear to generate a root Space aggregator. Set <SpaceGenerateRootAggregator>false</SpaceGenerateRootAggregator> in one project or rely on automatic heuristics.",
        category: "Configuration",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //System.Diagnostics.Debugger.Launch();

        var assemblyNameProvider = context.CompilationProvider.Select((c, _) => c.AssemblyName ?? "UnknownAssembly");
        var analyzerConfig = context.AnalyzerConfigOptionsProvider.Select((p, _) =>
        {
            p.GlobalOptions.TryGetValue("build_property.SpaceGenerateRootAggregator", out var flag);
            return flag;
        });

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
            .Combine(assemblyNameProvider)
            .Combine(analyzerConfig)
            .Select((tuple, _) =>
            {
                // tuple: (((((handlers, pipelines), notifications), modulesSymbols), assemblyName), rootFlag)
                var handlers = tuple.Left.Left.Left.Left.Left;
                var pipelines = tuple.Left.Left.Left.Left.Right;
                var notifications = tuple.Left.Left.Left.Right;
                var modulesSymbols = tuple.Left.Left.Right;
                var assemblyName = tuple.Left.Right;
                var rootFlag = tuple.Right;

                bool hasExplicit = rootFlag is not null;
                bool isRootExplicit = bool.TryParse(rootFlag, out bool trueFlag) && trueFlag;
                bool isExplicitFalse = bool.TryParse(rootFlag, out bool falseFlag) && !falseFlag;

                var model = new HandlersCompileWrapperModel
                {
                    AssemblyName = assemblyName,
                    IsRootAggregator = isRootExplicit && !isExplicitFalse, // explicit true only
                    HasExplicitRootFlag = hasExplicit
                };

                foreach (var handlerSymbol in handlers.OfType<IMethodSymbol>())
                    model.AddRangeHandlerAttribute(HandlerScanner.ScanHandlers(handlerSymbol.ContainingType, handlerSymbol));

                foreach (var moduleSymbol in modulesSymbols.OfType<IMethodSymbol>())
                    model.AddRangeModuleAttribute(ScanModules(moduleSymbol));

                foreach (var pipelineSymbol in pipelines.OfType<IMethodSymbol>())
                    model.AddRangePipelineAttribute(PipelineScanner.ScanPipelines(pipelineSymbol.ContainingType, pipelineSymbol));

                foreach (var notificationSymbol in notifications.OfType<IMethodSymbol>())
                    model.AddRangeNotificationAttribute(NotificationScanner.ScanHandlers(notificationSymbol.ContainingType, notificationSymbol));

                return model.Build();
            });

        var output = allModels.Combine(context.CompilationProvider);

        context.RegisterSourceOutput(output, (spc, pair) =>
        {
            var model = pair.Left;
            var compilation = pair.Right;

            // Heuristic root detection if not explicitly set:
            // Only non-DLL outputs (Exe / Web) auto-root. Class libraries now stay non-root unless explicitly set.
            if (!model.HasExplicitRootFlag && !model.IsRootAggregator)
            {
                if (compilation.Options.OutputKind != OutputKind.DynamicallyLinkedLibrary)
                {
                    model.IsRootAggregator = true;
                }
            }

            bool hasErrors = DiagnosticGenerator.ReportDiagnostics(spc, compilation, model);
            if (hasErrors)
                return;

            // Multiple root diagnostic: if this project is root and a referenced assembly already exposes the generated root type.
            if (model.IsRootAggregator)
            {
                var existing = compilation.GetTypeByMetadataName("Space.DependencyInjection.SourceGeneratorDependencyInjectionExtensions");
                if (existing is not null && !SymbolEqualityComparer.Default.Equals(existing.ContainingAssembly, compilation.Assembly))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(MultipleRootDescriptor, Location.None, existing.ContainingAssembly.Name));
                }
            }

            DependencyInjectionGenerator.Generate(spc, model);
            ModuleGenerator.Generate(spc, model);
        });
    }
}
