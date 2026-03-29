namespace Bubble.Core.Services;

public abstract class Singleton<T>
    where T : class, new()
{
    private static readonly Lazy<T> LazyInstance = new(() => new T());

    public static T Instance =>
        LazyInstance.Value;
}