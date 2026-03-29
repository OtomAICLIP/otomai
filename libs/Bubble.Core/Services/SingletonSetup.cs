namespace Bubble.Core.Services;

public abstract class SingletonSetup<T> : Singleton<T>
    where T : class, new()
{
    public abstract int Priority { get; }

    public abstract void Initialize();
}