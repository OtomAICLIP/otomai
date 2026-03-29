using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Effects;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "SpellZoneDescr", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class SpellZoneDescr
{
    public required List<int> CellIds { get; set; }

    public required char Shape { get; set; }

    public required byte Param1 { get; set; }

    public required byte Param2 { get; set; }

    public required sbyte DamageDecreaseStepPercent { get; set; }

    public required sbyte MaxDamageDecreaseApplyCount { get; set; }

    public required bool IsStopAtTarget { get; set; }
}