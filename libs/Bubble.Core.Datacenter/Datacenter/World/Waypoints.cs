using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.World;

[DatacenterObject("Core.DataCenter.Metadata.World", "Waypoints", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Waypoints : IDofusRootObject
{
    public static string FileName => "data_assets_waypointsroot.asset.bundle";
    
    public required int Id { get; set; }
    public required int MapId { get; set; }
    public required int SubAreaId { get; set; }
    public required bool Activated { get; set; }
}