using System.Collections.Concurrent;

namespace Bubble.Core.Database.Batchs;

public sealed class BatchStatements
{
    public IList<BatchStatement> Statements { get; set; }

    public int LimitExecution { get; set; } = 1000;

    public BatchStatements()
    {
        Statements = new List<BatchStatement>();
    }

    public void Clear()
    {
        Statements.Clear();
    }

    public BatchStatements Delete(IEntityProperties record, bool force = false)
    {
        if (!force && Statements.Any(x => x.EntityRecord == record && x.Mode is BatchStatementMode.Delete))
            return this;

        Statements.Add(new BatchStatement(record, BatchStatementMode.Delete));
        return this;
    }

    public BatchStatements DeleteAll(IEnumerable<IEntityProperties> records)
    {
        foreach (var record in records)
            Statements.Add(new BatchStatement(record, BatchStatementMode.Delete));

        return this;
    }

    public BatchStatements Insert(IEntityProperties record, bool force = false)
    {
        if (!force)
            if (Statements.Any(x => x.EntityRecord == record && x.Mode is BatchStatementMode.Insert))
                return this;

        Statements.Add(new BatchStatement(record, BatchStatementMode.Insert));
        return this;
    }

    public BatchStatements InsertAll(IEnumerable<IEntityProperties> records)
    {
        foreach (var record in records)
            Statements.Add(new BatchStatement(record, BatchStatementMode.Insert));

        return this;
    }

    public void RemoveDuplicates()
    {
        var newStatements = new List<BatchStatement>();
        var seen = new HashSet<BatchStatement>();

        foreach (var statement in Statements)
        {
            if (!seen.Add(statement))
                continue;

            newStatements.Add(statement);
        }

        Statements = newStatements;
    }

    public void Save<T>(T record, bool force = false)
        where T : class, IEntityRecord<T>
    {
        if (record.IsNew)
            Insert(record, force);
        else if (record.MustBeDeleted)
            Delete(record, force);
        else if (record.IsDirty())
            Update(record, force);
    }

    public void SaveEntities<T>(IEnumerable<T> items, ConcurrentQueue<T> deletedItems, Action<T>? handleItemSpecifics = null)
        where T : class, IEntity<T>, IEntityRecord<T>
    {
        while (deletedItems.TryDequeue(out var deletedItem))
        {
            if (deletedItem.Record.IsNew) continue;

            Delete(deletedItem.Record, true);
            deletedItem.Record.MustBeDeleted = true;
        }

        foreach (var item in items)
        {
            handleItemSpecifics?.Invoke(item);

            if (item.Record.IsNew)
                Insert(item.Record, true);
            else if (item.Record.IsDirty()) Update(item.Record, true);
        }
    }

    public BatchStatements Update(IEntityProperties record, bool force = false)
    {
        Statements.Add(new BatchStatement(record, BatchStatementMode.Update));
        return this;
    }

    public BatchStatements UpdateAll(IEnumerable<IEntityProperties> records)
    {
        foreach (var record in records)
            Statements.Add(new BatchStatement(record, BatchStatementMode.Update));

        return this;
    }
}