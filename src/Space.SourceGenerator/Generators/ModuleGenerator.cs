using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using Space.SourceGenerator.Compile;
using Space.SourceGenerator.Helpers;
using System.Text;
using static Space.SourceGenerator.Scanning.ModuleScanners;

namespace Space.SourceGenerator.Generators;

internal class ModuleGenerator
{
    private const string modulesFilePath = "Resources/ModuleGenerationTemplate.scr";
    public static void Generate(SourceProductionContext spc, HandlersCompileWrapperModel model)
    {
        ModuleScanningCompileModelContainer container = new()
        {
            Models = [.. model.ModuleCompileModels]
        };

        var t = Template.Parse(EmbeddedResource.GetContent(modulesFilePath), modulesFilePath);
        var text = t.Render(container, member => member.Name);

        spc.AddSource("SpaceModules.g.cs", SourceText.From(text, Encoding.UTF8));
    }
}