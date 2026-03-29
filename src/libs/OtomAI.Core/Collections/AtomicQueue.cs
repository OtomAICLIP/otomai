using System.Collections.Concurrent;

namespace OtomAI.Core.Collections;

/// <summary>
/// Thread-safe FIFO queue. Mirrors Bubble.Core's AtomicQueue.
/// Wraps ConcurrentQueue with blocking dequeue support.
/// </summary>
public sealed class AtomicQueue<T>
{
    private readonly ConcurrentQueue<T> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);

    public int Count => _queue.Count;

    public void Enqueue(T item)
    {
        _queue.Enqueue(item);
        _signal.Release();
    }

    public bool TryDequeue(out T? item) => _queue.TryDequeue(out item);

    public async Task<T> DequeueAsync(CancellationToken ct = default)
    {
        await _signal.WaitAsync(ct);
        _queue.TryDequeue(out var item);
        return item!;
    }

    public bool TryPeek(out T? item) => _queue.TryPeek(out item);

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}
