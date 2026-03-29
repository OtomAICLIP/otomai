using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Quest;

[DatacenterObject("Core.DataCenter.Metadata.Quest", "Achievements", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Achievements : IDofusRootObject
{
    public static string FileName => "data_assets_achievementsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }

    public required int CategoryId { get; set; }
    
    [DatacenterPropertyText]
    public required int DescriptionId { get; set; }
    
    public required int IconId { get; set; }
    
    public required int Points { get; set; }
    
    public required int Level { get; set; }
    
    public required int Order { get; set; }
    
    public required int AccountLinked { get; set; }

    public required List<int> ObjectiveIds { get; set; } = [];

    public required List<int> RewardIds { get; set; } = [];
}