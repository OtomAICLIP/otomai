namespace Bubble.Core.Database;

public interface IEntity<out T>
    where T : class, IEntityRecord<T>
{
    public T Record { get; }
}