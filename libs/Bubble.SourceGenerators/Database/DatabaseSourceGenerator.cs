using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Database;

[Generator(LanguageNames.CSharp)]
public sealed partial class DatabaseSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var syntaxProvider = context.SyntaxProvider.CreateSyntaxProvider(Predicate, Transform).Collect();

        var enabled = context.AnalyzerConfigOptionsProvider.IsEnabled("Database");
        var assemblyName = context.CompilationProvider.SelectAssemblyName();

        var provider = syntaxProvider
            .Combine(assemblyName)
            .Combine(enabled);

        context.RegisterSourceOutput(provider, (spc, pair) => Generate(spc, pair.Left.Left, pair.Left.Right, pair.Right));
    }
}