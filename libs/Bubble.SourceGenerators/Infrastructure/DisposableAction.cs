namespace Bubble.SourceGenerators.Infrastructure;

public sealed record DisposableAction(Action Action) : IDisposable
{
    public void Dispose()
    {
        Action();
    }
}