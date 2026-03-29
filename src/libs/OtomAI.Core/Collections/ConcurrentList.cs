namespace OtomAI.Core.Collections;

/// <summary>
/// Thread-safe list with lock-based synchronization.
/// Mirrors Bubble.Core's ConcurrentList for entity tracking.
/// </summary>
public sealed class ConcurrentList<T>
{
    private readonly List<T> _list = [];
    private readonly Lock _lock = new();

    public int Count { get { lock (_lock) return _list.Count; } }

    public void Add(T item) { lock (_lock) _list.Add(item); }

    public bool Remove(T item) { lock (_lock) return _list.Remove(item); }

    public void Clear() { lock (_lock) _list.Clear(); }

    public T? FirstOrDefault(Func<T, bool> predicate)
    {
        lock (_lock) return _list.FirstOrDefault(predicate);
    }

    public List<T> ToList() { lock (_lock) return [.. _list]; }

    public bool Any(Func<T, bool> predicate) { lock (_lock) return _list.Any(predicate); }
}
