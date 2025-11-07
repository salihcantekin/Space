using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace Space.Tests.Diagnostics;

[TestClass]
public class HandleVoidConflictTests
{
    private static (Compilation, ImmutableArray<Diagnostic>) Compile(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Preview));
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();

        var compilation = CSharpCompilation.Create(
            assemblyName: "InMemoryTest",
            syntaxTrees: [syntaxTree],
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Attach Space.SourceGenerator
        var generator = new Space.SourceGenerator.SpaceSourceGenerator();
        CSharpGeneratorDriver.Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);

        var allDiags = diagnostics.AddRange(updated.GetDiagnostics());
        return (updated, allDiags);
    }

    [TestMethod]
    public void NonGeneric_Task_On_IRequest_NonNothing_Should_Diagnostic_HANDLE014()
    {
        var code = @"
using System.Threading.Tasks;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;

public record Req(int Id) : IRequest<int>;
public class H
{
    [Handle]
    public Task Handle(HandlerContext<Req> ctx) => Task.CompletedTask;
}
";
        var (_, diags) = Compile(code);
        var handle014 = diags.FirstOrDefault(d => d.Id == "HANDLE014");
        Assert.IsNotNull(handle014, "Expected HANDLE014 diagnostic");
    }
}
