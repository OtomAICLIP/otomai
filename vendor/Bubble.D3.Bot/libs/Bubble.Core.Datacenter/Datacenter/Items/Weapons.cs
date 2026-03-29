using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Items;

[DatacenterObject("Core.DataCenter.Metadata.Item", "Weapons", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Weapons : Items
{
    public required int CriticalFailureProbability { get; set; }

    public required int CriticalHitBonus { get; set; }

    public required int MinRange { get; set; }

    public required int CriticalHitProbability { get; set; }

    public required int Range { get; set; }

    public required bool CastInLine { get; set; }

    public required int ApCost { get; set; }

    public required bool CastInDiagonal { get; set; }

    public required bool CastTestLos { get; set; }

    public required int MaxCastPerTurn { get; set; }
}