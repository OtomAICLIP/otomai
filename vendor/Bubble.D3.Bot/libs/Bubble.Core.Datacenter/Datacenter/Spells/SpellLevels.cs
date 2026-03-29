using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Spells;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "SpellLevels", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class SpellLevels : IDofusRootObject
{
    public static string FileName => "data_assets_spelllevelsroot.asset.bundle";
    
    public required ushort Flags { get; set; }
    public required int Id { get; set; }
    public required short SpellId { get; set; }
    public required int Grade { get; set; }
    public required ushort SpellBreed { get; set; }
    public required short ApCost { get; set; }
    public required byte MinRange { get; set; }
    public required byte Range { get; set; }
    public required short CriticalHitProbability { get; set; }
    public required sbyte MaxStack { get; set; }
    public required sbyte MaxCastPerTurn { get; set; }
    public required byte MaxCastPerTarget { get; set; }
    public required byte MinCastInterval { get; set; }
    public required sbyte InitialCooldown { get; set; }
    public required sbyte GlobalCooldown { get; set; }
    public required short MinPlayerLevel { get; set; }
    public required string StatesCriterion { get; set; }
    public required List<EffectInstanceDice> Effects { get; set; }
    public required List<EffectInstanceDice> CriticalEffect { get; set; }
    public required List<PreviewSpellZoneDescr> PreviewZones { get; set; }
    
    public bool CastInLine => (Flags & (ushort)SpellLevelFlags.CastInLine) != 0;
    public bool CastInDiagonal => (Flags & (ushort)SpellLevelFlags.CastInDiagonal) != 0;
    public bool CastTestLos => (Flags & (ushort)SpellLevelFlags.CastTestLos) != 0;
    public bool NeedFreeCell => (Flags & (ushort)SpellLevelFlags.NeedFreeCell) != 0;
    public bool NeedTakenCell => (Flags & (ushort)SpellLevelFlags.NeedTakenCell) != 0;
    public bool NeedFreeTrapCell => (Flags & (ushort)SpellLevelFlags.NeedFreeTrapCell) != 0;
    public bool RangeCanBeBoosted => (Flags & (ushort)SpellLevelFlags.RangeCanBeBoosted) != 0;
    public bool HideEffects => (Flags & (ushort)SpellLevelFlags.HideEffects) != 0;
    public bool Hidden => (Flags & (ushort)SpellLevelFlags.Hidden) != 0;
    public bool PlayAnimation => (Flags & (ushort)SpellLevelFlags.PlayAnimation) != 0;
    public bool NeedVisibleEntity => (Flags & (ushort)SpellLevelFlags.NeedVisibleEntity) != 0;
    public bool NeedCellWithoutPortal => (Flags & (ushort)SpellLevelFlags.NeedCellWithoutPortal) != 0;
    public bool PortalProjectionForbidden => (Flags & (ushort)SpellLevelFlags.PortalProjectionForbidden) != 0;
    
}