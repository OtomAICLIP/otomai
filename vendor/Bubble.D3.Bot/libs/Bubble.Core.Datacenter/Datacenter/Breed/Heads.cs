using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Breed;

[DatacenterObject("Core.DataCenter.Metadata.Breed", "Heads", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Heads : IDofusRootObject
{
    public static string FileName => "data_assets_headsroot.asset.bundle";

    public required int Id { get; set; }

    public required string Skins { get; set; }

    public required string AssetId { get; set; }

    public required int Breed { get; set; }

    public required int Gender { get; set; }

    public required string Label { get; set; }

    public required int Order { get; set; }
    
    public required byte Payable { get; set; }
}