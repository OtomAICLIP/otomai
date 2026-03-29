using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Job;

[DatacenterObject("Core.DataCenter.Metadata.Job", "Jobs", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Jobs : IDofusRootObject
{
    public static string FileName => "data_assets_jobsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required int IconId { get; set; }
    
    public required bool HasLegendaryCraft { get; set; }
}