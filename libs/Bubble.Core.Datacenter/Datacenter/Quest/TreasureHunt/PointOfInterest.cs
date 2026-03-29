using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Quest.TreasureHunt;

[DatacenterObject("Core.DataCenter.Metadata.Quest.TreasureHunt", "PointOfInterest", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public partial class PointOfInterest : IDofusRootObject
{
    public static string FileName => "data_assets_pointofinterestroot.asset.bundle";
    
    public required int Id { get; set; }
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    public required int CategoryId { get; set; }
}