using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Characteristic;

[DatacenterObject("Core.DataCenter.Metadata.Characteristic", "Characteristics", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Characteristics : IDofusRootObject
{
    public static string FileName => "data_assets_characteristicsroot.asset.bundle";
    
    
    public required int Id { get; set; }
    
    public required string Keyword { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required string Asset { get; set; }
    
    public required int CategoryId { get; set; }
    
    public required bool Visible { get; set; }
    public required int Order { get; set; }
    
    public required int ScaleFormulaId { get; set; }
    
    public required bool Upgradable { get; set; }
    
}