using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.World;

[DatacenterObject("Core.DataCenter.Metadata.World", "MapPositionPlaylist", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class MapPositionPlaylist
{
    public required string PlaylistMusic { get; set; } = string.Empty;
    public required string PlaylistAmbient { get; set; } = string.Empty;
    public required string PlaylistCombat { get; set; } = string.Empty;
    public required string PlaylistBossFight { get; set; } = string.Empty;
}