using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.World;

[DatacenterObject("Core.DataCenter.Metadata.World", "MapScrollActions", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class MapScrollActions : IDofusRootObject
{
    public static string FileName => "data_assets_mapscrollactionsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    public required bool RightExists { get; set; }
    public required bool BottomExists { get; set; }
    public required bool LeftExists { get; set; }
    public required bool TopExists { get; set; }
    
    public required int RightMapId { get; set; }
    public required int BottomMapId { get; set; }
    public required int LeftMapId { get; set; }
    public required int TopMapId { get; set; }
}