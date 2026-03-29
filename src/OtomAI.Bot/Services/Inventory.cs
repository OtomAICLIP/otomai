using Serilog;

namespace OtomAI.Bot.Services;

/// <summary>
/// Inventory state tracking. Mirrors Bubble.D3.Bot's Inventory.
/// </summary>
public sealed class Inventory
{
    private readonly Dictionary<int, InventoryItem> _items = [];

    public IReadOnlyDictionary<int, InventoryItem> Items => _items;

    public void AddOrUpdate(InventoryItem item)
    {
        _items[item.ObjectUid] = item;
    }

    public void Remove(int objectUid)
    {
        _items.Remove(objectUid);
    }

    public void Clear()
    {
        _items.Clear();
    }

    public InventoryItem? GetByGid(int gid) =>
        _items.Values.FirstOrDefault(i => i.ObjectGid == gid);

    public int TotalWeight => _items.Values.Sum(i => i.Weight * i.Quantity);
}

public sealed class InventoryItem
{
    public int ObjectUid { get; set; }
    public int ObjectGid { get; set; }
    public int Quantity { get; set; }
    public int Weight { get; set; }
    public int Position { get; set; }
    public List<ItemEffect> Effects { get; set; } = [];
}

public sealed class ItemEffect
{
    public int EffectId { get; set; }
    public int Value { get; set; }
}
