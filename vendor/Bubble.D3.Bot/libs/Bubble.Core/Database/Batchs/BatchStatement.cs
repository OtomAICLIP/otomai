namespace Bubble.Core.Database.Batchs;

public sealed class BatchStatement : IEquatable<BatchStatement>
{
    public IEntityProperties EntityRecord { get; }

    public Type EntityType { get; }

    public BatchStatementMode Mode { get; }

    public BatchStatement(IEntityProperties entityRecord, BatchStatementMode mode)
    {
        EntityRecord = entityRecord;
        EntityType = entityRecord.GetType();
        Mode = mode;
    }

    public override bool Equals(object? obj)
    {
        return obj is BatchStatement other && Equals(other);
    }

    public override int GetHashCode()
    {
        return EntityRecord.GetHashCode() ^ Mode.GetHashCode();
    }

    public bool Equals(BatchStatement? other)
    {
        return EntityRecord == other?.EntityRecord && Mode == other.Mode;
    }
}