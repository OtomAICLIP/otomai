using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Communication;

[DatacenterObject("Core.DataCenter.Metadata.Communication", "Emoticons", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public partial class Emoticons : IDofusRootObject
{
    public static string FileName => "data_assets_emoticonsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    public required string ShortcutId { get; set; }
    public required uint Order { get; set; }
    public required string AnimName { get; set; }
    public required bool Persistancy { get; set; }
    public required bool EightDirections { get; set; }
    public required bool Aura { get; set; }
    public required uint CoolDown { get; set; }
    public required uint Duration { get; set; }
    public required uint Weight { get; set; }
    public required uint SpellLevelId { get; set; }
    public required int Scale { get; set; }
    
}