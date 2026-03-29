using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Services;

[Generator(LanguageNames.CSharp)]
public sealed partial class ServiceSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform).Collect();

        var assemblyName = context.CompilationProvider.SelectAssemblyName();

        var enabled = context.AnalyzerConfigOptionsProvider.IsEnabled("Services");

        var provider = syntaxProvider
            .Combine(assemblyName)
            .Combine(enabled);

        context.RegisterSourceOutput(provider, (spc, pair) => Generate(spc, pair.Left.Left, pair.Left.Right, pair.Right));
    }
}