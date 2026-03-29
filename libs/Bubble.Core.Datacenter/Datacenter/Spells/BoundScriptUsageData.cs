using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Spells;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "BoundScriptUsageData", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class BoundScriptUsageData
{
    public required int Id { get; set; }
    public required int Order { get; set; }
    public required int ScriptId { get; set; }
    public required List<uint> SpellLevels { get; set; }
    public required string Criterion { get; set; }
    public required string CasterMask { get; set; }
    public required string TargetMask { get; set; }
    public required string TargetZone { get; set; }
    public required string ActivationMask { get; set; }
    public required string ActivationZone { get; set; }
    public required int Random { get; set; }
    public required int RandomGroup { get; set; }
    public required int SequenceGroup { get; set; }
    public required bool IsTargetTreatedAsCaster { get; set; }
    public required bool AreTargetsAffectedConcurrently { get; set; }
}