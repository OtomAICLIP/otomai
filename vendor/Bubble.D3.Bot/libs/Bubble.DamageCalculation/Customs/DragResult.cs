using Bubble.DamageCalculation.DamageManagement;

namespace Bubble.DamageCalculation.Customs;

public class DragResult
{
    public int RemainingForce { get; set; }
    public int Cell { get; set; }
    public DragResults StopReason { get; set; }
}