using System.Collections.Concurrent;

namespace Bubble.Core.Collections;

public class AtomicQueue
{
    protected readonly ConcurrentQueue<int> FreeIds;
    protected int HighestId;

    public AtomicQueue()
    {
        HighestId = 0;
        FreeIds = new ConcurrentQueue<int>();
    }

    public AtomicQueue(int lastId)
    {
        HighestId = lastId;
        FreeIds = new ConcurrentQueue<int>();
    }

    public AtomicQueue(IEnumerable<int> freeIds)
    {
        FreeIds = new ConcurrentQueue<int>();

        foreach (var freeId in freeIds) FreeIds.Enqueue(freeId);
    }

    protected virtual int Next()
    {
        return Interlocked.Increment(ref HighestId);
    }

    public virtual int Peek()
    {
        int id;

        if (!FreeIds.IsEmpty)
        {
            if (!FreeIds.TryPeek(out id)) return Interlocked.Increment(ref HighestId);
        }
        else
            return Interlocked.Increment(ref HighestId);

        return id;
    }

    public virtual int Pop()
    {
        int id;

        if (!FreeIds.IsEmpty)
        {
            if (!FreeIds.TryDequeue(out id)) return Next();
        }
        else
            return Next();

        return id;
    }

    public virtual void Push(int freeId)
    {
        FreeIds.Enqueue(freeId);
    }

    public void Reset()
    {
        HighestId = 0;
        FreeIds.Clear();
    }
}

public class AtomicQueueLong
{
    protected readonly ConcurrentQueue<long> FreeIds;
    protected long HighestId;

    public AtomicQueueLong()
    {
        HighestId = 0;
        FreeIds = new ConcurrentQueue<long>();
    }

    public AtomicQueueLong(long lastId)
    {
        HighestId = lastId;
        FreeIds = new ConcurrentQueue<long>();
    }

    public AtomicQueueLong(IEnumerable<int> freeIds)
    {
        FreeIds = new ConcurrentQueue<long>();

        foreach (var freeId in freeIds) FreeIds.Enqueue(freeId);
    }

    protected virtual long Next()
    {
        return Interlocked.Increment(ref HighestId);
    }

    public virtual long Peek()
    {
        long id;

        if (!FreeIds.IsEmpty)
        {
            if (!FreeIds.TryPeek(out id)) return Interlocked.Increment(ref HighestId);
        }
        else
            return Interlocked.Increment(ref HighestId);

        return id;
    }

    public virtual long Pop()
    {
        long id;

        if (!FreeIds.IsEmpty)
        {
            if (!FreeIds.TryDequeue(out id)) return Next();
        }
        else
            return Next();

        return id;
    }

    public virtual void Push(int freeId)
    {
        FreeIds.Enqueue(freeId);
    }
}