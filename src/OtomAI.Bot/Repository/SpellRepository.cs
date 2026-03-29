using Serilog;

namespace OtomAI.Bot.Repository;

/// <summary>
/// Spell data repository (singleton). Mirrors Bubble.D3.Bot's SpellRepository.
/// </summary>
public sealed class SpellRepository
{
    private static readonly Lazy<SpellRepository> _instance = new(() => new SpellRepository());
    public static SpellRepository Instance => _instance.Value;

    private readonly Dictionary<int, SpellRecord> _spells = [];
    private SpellRepository() { }

    public void Load(string dataPath)
    {
        Log.Debug("SpellRepository: load from {Path}", dataPath);
    }

    public SpellRecord? Get(int spellId) => _spells.GetValueOrDefault(spellId);
    public IEnumerable<SpellRecord> GetAll() => _spells.Values;
}

public sealed class SpellRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SpellTemplateId { get; set; }
    public int Level { get; set; }
    public int ApCost { get; set; }
    public int MinRange { get; set; }
    public int MaxRange { get; set; }
    public bool NeedLos { get; set; }
    public bool NeedFreeCell { get; set; }
    public int CooldownDuration { get; set; }
    public int MaxCastPerTurn { get; set; }
    public int ZoneShape { get; set; }
    public int ZoneSize { get; set; }
}
