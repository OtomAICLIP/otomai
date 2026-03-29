namespace OtomAI.Bot.Fight;

/// <summary>
/// Characteristic stats for damage calculation.
/// Mirrors Bubble.D3.Bot's StatsField / StatsFields / StatsUsable / IStatsOwner.
/// </summary>
public sealed class StatsField
{
    public int Base { get; set; }
    public int Additional { get; set; }
    public int ObjectsAndMountBonus { get; set; }
    public int AlignGiftBonus { get; set; }
    public int ContextModif { get; set; }

    public int Total => Base + Additional + ObjectsAndMountBonus + AlignGiftBonus + ContextModif;
}

public sealed class StatsFields
{
    public StatsField Strength { get; set; } = new();
    public StatsField Intelligence { get; set; } = new();
    public StatsField Chance { get; set; } = new();
    public StatsField Agility { get; set; } = new();
    public StatsField Wisdom { get; set; } = new();
    public StatsField Vitality { get; set; } = new();
    public StatsField Power { get; set; } = new();
    public StatsField DamageBonus { get; set; } = new();
    public StatsField CriticalDamageBonus { get; set; } = new();
    public StatsField HealBonus { get; set; } = new();
    public StatsField Range { get; set; } = new();
    public StatsField SummonableCreaturesBoost { get; set; } = new();
}

public interface IStatsOwner
{
    StatsFields Stats { get; }
}
