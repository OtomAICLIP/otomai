using Serilog;

namespace OtomAI.Bot.Repository;

/// <summary>
/// Item data repository (singleton). Mirrors Bubble.D3.Bot's ItemRepository.
/// </summary>
public sealed class ItemRepository
{
    private static readonly Lazy<ItemRepository> _instance = new(() => new ItemRepository());
    public static ItemRepository Instance => _instance.Value;

    private readonly Dictionary<int, ItemRecord> _items = [];
    private ItemRepository() { }

    public void Load(string dataPath)
    {
        // TODO: Load from datacenter / JSON
        Log.Debug("ItemRepository: load from {Path}", dataPath);
    }

    public ItemRecord? Get(int itemId) => _items.GetValueOrDefault(itemId);
    public IEnumerable<ItemRecord> GetAll() => _items.Values;
}

public sealed class ItemRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int TypeId { get; set; }
    public int Level { get; set; }
    public int Weight { get; set; }
    public int Price { get; set; }
}
