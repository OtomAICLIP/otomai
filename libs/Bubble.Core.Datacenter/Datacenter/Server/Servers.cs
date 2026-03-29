using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Server;


[DatacenterObject("Core.DataCenter.Metadata.Server", "Servers", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Servers : IDofusRootObject
{
    public static string FileName => "data_assets_serversroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required string CommentId { get; set; }
    
    public required double OpeningDate { get; set; }
    
    public required string Language { get; set; }
    
    public required int PopulationId { get; set; }
    public required int GameTypeId { get; set; }
    public required int CommunityId { get; set; }
    public required List<string> RestrictedToLanguages { get; set; }
    public required bool MonoAccount { get; set; }
    public required string Illus { get; set; }
}