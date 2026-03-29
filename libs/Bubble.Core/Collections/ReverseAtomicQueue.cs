namespace Bubble.Core.Collections;

public class ReverseAtomicQueue : AtomicQueue
{
    public ReverseAtomicQueue(int lastId) : base(lastId)
    {
    }

    protected override int Next()
    {
        return Interlocked.Decrement(ref HighestId);
    }

    public override int Peek()
    {
        int id;

        if (!FreeIds.IsEmpty)
        {
            if (!FreeIds.TryPeek(out id)) return Interlocked.Decrement(ref HighestId);
        }
        else
            return Interlocked.Decrement(ref HighestId);

        return id;
    }
}