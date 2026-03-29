using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Quest;

[DatacenterObject("Core.DataCenter.Metadata.Quest", "AchievementObjectives", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class AchievementObjectives : IDofusRootObject
{
    public static string FileName => "data_assets_achievementobjectivesroot.asset.bundle";

    public required int Id { get; set; }
    
    public required int AchievementId { get; set; }
    
    public required int Order { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required string Criterion { get; set; }
}