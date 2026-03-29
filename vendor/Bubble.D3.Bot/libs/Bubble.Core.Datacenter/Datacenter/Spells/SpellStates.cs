using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Spells;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "SpellStates", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class SpellStates : IDofusRootObject
{
    public static string FileName => "data_assets_spellstatesroot.asset.bundle";

    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required bool PreventsSpellCast { get; set; }
    public required bool PreventsFight { get; set; }
    public required bool IsSilent { get; set; }
    public required bool CantBeMoved { get; set; }
    public required bool CantBePushed { get; set; }
    public required bool CantDealDamage { get; set; }
    public required bool Invulnerable { get; set; }
    public required bool CantSwitchPosition { get; set; }
    public required bool Incurable { get; set; }
    public required List<int> EffectsIds { get; set; } = new();
    public required string Icon { get; set; }
    public required int IconVisibilityMask { get; set; }
    public required bool InvulnerableMelee { get; set; }
    public required bool InvulnerableRange { get; set; }   
    public required bool CantTackle { get; set; }
    public required bool CantBeTackle { get; set; }
    public required bool DisplayTurnRemaining { get; set; }
    public required bool IsMainState { get; set; }
}