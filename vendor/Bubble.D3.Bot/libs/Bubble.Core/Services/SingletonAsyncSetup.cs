namespace Bubble.Core.Services;

public abstract class SingletonAsyncSetup<T> : Singleton<T>
    where T : class, new()
{
    public abstract int Priority { get; }

    public abstract Task InitializeAsync();
}