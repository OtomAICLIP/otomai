namespace Bubble.Core.Network.Internal;

public sealed class EmptyServiceProvider : IServiceProvider
{
    private static readonly Lazy<IServiceProvider> LazyInstance = new(() => new EmptyServiceProvider(), LazyThreadSafetyMode.ExecutionAndPublication);

    public static IServiceProvider Instance =>
        LazyInstance.Value;

    public object? GetService(Type serviceType)
    {
        return null;
    }
}