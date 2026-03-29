using Bubble.Core.Datacenter.Attributes;
using Bubble.Core.Datacenter.Datacenter.Effects;

namespace Bubble.Core.Datacenter.Datacenter.Spells;

[DatacenterObject("Core.DataCenter.Metadata.Spell", "Spells", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Spells : IDofusRootObject
{
    public static string FileName => "data_assets_spellsroot.asset.bundle";
    
    public required int Flags { get; set; }
    public required ushort Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    [DatacenterPropertyText]
    public required int DescriptionId { get; set; }
    
    public required ushort TypeId { get; set; }
    public required byte Order { get; set; }
    public required string ScriptParams { get; set; }
    public required string ScriptParamsCritical { get; set; }
    public required byte ScriptId { get; set; }
    public required byte ScriptIdCritical { get; set; }
    public required short IconId { get; set; }
    public required List<uint> SpellLevels { get; set; }
    public required List<BoundScriptUsageData> ScriptUsageData { get; set; }
    public required List<BoundScriptUsageData> CriticalScriptUsageData { get; set; }
    public required SpellZoneDescr BasePreviewZoneDescr { get; set; }
    public required string AdminName { get; set; }
    
    public bool VerboseCast => (Flags & (int)SpellFlags.VerboseCast) != 0;
    public bool BypassSummoningLimit => (Flags & (int)SpellFlags.BypassSummoningLimit) != 0;
    public bool CanAlwaysTriggerSpells => (Flags & (int)SpellFlags.CanAlwaysTriggerSpells) != 0;
    public bool HideCastConditions => (Flags & (int)SpellFlags.HideCastConditions) != 0;
}