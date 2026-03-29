using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Breed;

[DatacenterObject("Core.DataCenter.Metadata.Breed", "Breeds", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Breeds : IDofusRootObject
{
    public static string FileName => "data_assets_breedsroot.asset.bundle";

    public required int Id { get; set; }

    [DatacenterPropertyText]
    public required string ShortNameId { get; set; }

    [DatacenterPropertyText]
    public required string LongNameId { get; set; }

    [DatacenterPropertyText]
    public required int DescriptionId { get; set; }

    [DatacenterPropertyText]
    public required string GameplayDescriptionId { get; set; }

    [DatacenterPropertyText]
    public required string GameplayClassDescriptionId { get; set; }

    public required short GuideItemId { get; set; }

    public required string MaleLook { get; set; }

    public required string FemaleLook { get; set; }

    public required int CreatureBoneId { get; set; }

    public required int MaleArtwork { get; set; }

    public required int FemaleArtwork { get; set; }

    public required List<List<uint>> StatsPointsForStrength { get; set; }

    public required List<List<uint>> StatsPointsForIntelligence { get; set; }

    public required List<List<uint>> StatsPointsForChance { get; set; }

    public required List<List<uint>> StatsPointsForAgility { get; set; }

    public required List<List<uint>> StatsPointsForVitality { get; set; }

    public required List<List<uint>> StatsPointsForWisdom { get; set; }

    public required List<uint> BreedSpellIds { get; set; }

    public required List<BreedRoleByBreed> BreedRoles { get; set; }

    public required List<uint> MaleColors { get; set; }

    public required List<uint> FemaleColors { get; set; }

    public required int SpawnMap { get; set; }

    public required int Complexity { get; set; }

    public required int SortIndex { get; set; }
}