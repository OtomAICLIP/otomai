using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bubble.SourceGenerators.Infrastructure.Extensions;

public static class AnalyzerConfigOptionsProviderExtensions
{
    public static IncrementalValueProvider<bool> IsEnabled(this IncrementalValueProvider<AnalyzerConfigOptionsProvider> provider, string buildPropertyName)
    {
        return provider.Select((analyzer, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!analyzer.GlobalOptions.TryGetValue($"build_property.{buildPropertyName}", out var value))
                return false;

            return value is "enable" or "enabled" or "true";
        });
    }
}