using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.World;

[DatacenterObject("Core.DataCenter.Metadata.World", "SubAreas", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class SubAreas : IDofusRootObject
{
    public static string FileName => "data_assets_subareasroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    public required int AreaId { get; set; }
    public required List<int> MapIds { get; set; }
    public required Rectangle Bounds { get; set; }
    public required List<int> Shape { get; set; }
    public required List<int> CustomWorldMap { get; set; }
    
    public required uint PackId { get; set; }
    public required uint Level { get; set; }
    public required bool IsConquestVillage { get; set; }
    public required bool BasicAccountAllowed { get; set; }
    public required bool DisplayOnWorldMap { get; set; }
    public required bool MountAutoTripAllowed { get; set; }
    public required bool PsiAllowed { get; set; }
    public required List<uint> Monsters { get; set; }
    public required List<long> EntranceMapIds { get; set; }
    public required List<long> ExitMapIds { get; set; }
    public required bool Capturable { get; set; }
    public required List<int> Achievements { get; set; }
    public required int ExploreAchievementId { get; set; }
    public required List<int> Harvestable { get; set; }
    public required int AssociatedZaapMapId { get; set; }
    public required List<int> Neighbors { get; set; }
    
    public required int DungeonId { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.World", "Rectangle", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class Rectangle
{
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Width { get; set; }
    public required float Height { get; set; }
}
