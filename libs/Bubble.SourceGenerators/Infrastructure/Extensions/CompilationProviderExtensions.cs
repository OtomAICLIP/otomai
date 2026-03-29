using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Infrastructure.Extensions;

public static class CompilationProviderExtensions
{
    public static IncrementalValueProvider<string> SelectAssemblyName(this IncrementalValueProvider<Compilation> provider)
    {
        return provider.Select((compilation, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            return compilation.Assembly.Name;
        });
    }
}