using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.Customs;

public class TargetResult
{
    public required IList<HaxeFighter>? TargetedFighters { get; set; }
    public required IList<HaxeFighter>? AdditionalTargets { get; set; }
    public required bool IsUsed { get; set; }
}