using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Spells;

/// <summary>
/// Spell definitions. Mirrors Bubble.Core.Datacenter's Spells model set.
/// </summary>
[DatacenterObject("Spells")]
public sealed class Spell
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public int TypeId { get; set; }
    public int[] SpellLevels { get; set; } = [];
}

[DatacenterObject("SpellLevels")]
public sealed class SpellLevel
{
    public int Id { get; set; }
    public int SpellId { get; set; }
    public int Grade { get; set; }
    public int ApCost { get; set; }
    public int MinRange { get; set; }
    public int MaxRange { get; set; }
    public bool CastInLine { get; set; }
    public bool CastInDiagonal { get; set; }
    public bool CastTestLos { get; set; }
    public int CriticalHitProbability { get; set; }
    public bool NeedFreeCell { get; set; }
    public bool NeedTakenCell { get; set; }
    public int MaxCastPerTurn { get; set; }
    public int MaxCastPerTarget { get; set; }
    public int MinCastInterval { get; set; }
    public bool RangeCanBeBoosted { get; set; }
    public int[] Effects { get; set; } = [];
    public int[] CriticalEffects { get; set; } = [];
    public int ZoneShape { get; set; }
    public int ZoneSize { get; set; }
    public int ZoneMinSize { get; set; }
}
