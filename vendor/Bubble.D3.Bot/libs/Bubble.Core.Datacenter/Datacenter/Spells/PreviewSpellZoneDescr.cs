using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Spells;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "PreviewSpellZoneDescr", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class PreviewSpellZoneDescr
{
    public required uint Id { get; set; }
    public required SpellZoneDescr DisplayZoneDescr { get; set; }
    public required bool IsPreviewZoneHidden { get; set; }
    public required string CasterMask { get; set; }
    public required SpellZoneDescr ActivationZoneDescr { get; set; }
    public required string ActivationMask { get; set; }
}