using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Breed;

[DatacenterObject("Core.DataCenter.Metadata.Breed", "BreedRoleByBreed", "Ankama.Dofus.Core.DataCenter", nameof(RoleId))]
public sealed partial class BreedRoleByBreed
{
    public required int BreedId { get; set; }

    public required int RoleId { get; set; }

    [DatacenterPropertyText]
    public required int DescriptionId { get; set; }

    public required int Value { get; set; }

    public required int Order { get; set; }
}