namespace OtomAI.Bot.Repository;

/// <summary>
/// Effect data repository (singleton). Mirrors Bubble.D3.Bot's EffectRepository.
/// </summary>
public sealed class EffectRepository
{
    private static readonly Lazy<EffectRepository> _instance = new(() => new EffectRepository());
    public static EffectRepository Instance => _instance.Value;

    private readonly Dictionary<int, EffectRecord> _effects = [];
    private EffectRepository() { }

    public void Register(EffectRecord effect) => _effects[effect.Id] = effect;
    public EffectRecord? Get(int id) => _effects.GetValueOrDefault(id);
}

public sealed class EffectRecord
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public int Category { get; set; }
    public bool IsBoost { get; set; }
}
