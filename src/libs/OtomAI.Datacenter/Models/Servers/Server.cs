using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Servers;

/// <summary>
/// Server definitions. Mirrors Bubble.Core.Datacenter's Server model set.
/// </summary>
[DatacenterObject("Servers")]
public sealed class Server
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int CommunityId { get; set; }
    public int GameTypeId { get; set; }
}
