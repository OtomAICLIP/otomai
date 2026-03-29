using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Job;

[DatacenterObject("Core.DataCenter.Metadata.Job", "Skills", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Skills : IDofusRootObject
{
    public static string FileName => "data_assets_skillsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    
    public required int ParentJobId { get; set; }
    
    public required bool IsForgemagus { get; set; }
    
    public required List<int> ModifiableItemTypeIds { get; set; }
    
    public required int GatheredRessourceItem { get; set; }
    
    public required List<int> CraftableItemIds { get; set; }
    
    public required int InteractiveId { get; set; }
    
    public required int Range { get; set; }
    
    public required bool UseRangeInClient { get; set; }
    
    public required string UseAnimation { get; set; }
    
    public required int Cursor { get; set; }
    
    public required int ElementActionId { get; set; }
    
    public required bool AvailableInHouse { get; set; }
    
    public required bool ClientDisplay { get; set; }
    
    public required int LevelMin { get; set; }
    
}